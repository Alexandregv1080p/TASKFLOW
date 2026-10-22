import {
  ArchiveIcon,
  ArrowCounterClockwiseIcon,
  ArrowLeftIcon,
  DownloadSimpleIcon,
  EyeIcon,
  PencilSimpleIcon,
  PlugsConnectedIcon,
  TrashIcon,
  TagIcon,
  UserPlusIcon,
} from '@phosphor-icons/react'
import { useMemo, useState } from 'react'
import { Link, Outlet, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { Avatar } from '../components/Avatar'
import { ErrorMessage, Loading } from '../components/Feedback'
import { KanbanBoard } from '../components/KanbanBoard'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { ProjectForm } from '../components/ProjectForm'
import { ProjectMembersDialog } from '../components/ProjectMembersDialog'
import { TaskTypesDialog } from '../components/TaskTypesDialog'
import { WebhooksDialog } from '../components/WebhooksDialog'
import { useAsync } from '../hooks/useAsync'
import { useLive } from '../hooks/useLive'
import { api } from '../services/api'
import type { Project, ProjectInput, ProjectMember, ProjectStatus, Task, TaskInput, TaskStatus, TaskType } from '../types'
import { PROJECT_STATUSES, PROJECT_STATUS_LABEL, projectKey } from '../utils/format'

// Compartilhado com o modal de detalhe da tarefa (rota filha).
export interface ProjectOutletContext {
  project: Project
  tasks: Task[]
  // Pessoas do projeto; quem pode ser responsável são as que não são visualizadoras.
  users: ProjectMember[]
  keyPrefix: string
  // Projeto arquivado: nada pode ser alterado.
  readOnly: boolean
  types: TaskType[]
  replaceTask: (task: Task) => void
  removeTask: (id: string) => void
  // Subtarefa criada no detalhe entra no quadro.
  addTask: (task: Task) => void
  // Sobe a cada alteração feita por outra pessoa: o modal recarrega a atividade.
  liveVersion: number
}

export function ProjectDetails() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { data, loading, error, reload, setData } = useAsync(
    () => Promise.all([api.getProject(id), api.listTasks(id), api.listMembers(id)]),
    [id],
  )
  const { user } = useAuth()
  // Outra pessoa mexeu no projeto: recarrega o quadro sem tirar nada da tela.
  const [liveVersion, setLiveVersion] = useState(0)
  const live = useLive(
    'BoardChanged',
    () => {
      reload()
      reloadTypes()
      setLiveVersion((v) => v + 1)
    },
    id,
  )
  // Para o formulário de edição: espaços que eu criei, onde posso colocar o projeto.
  const { data: spaces } = useAsync(api.listSpaces, [])
  const [editing, setEditing] = useState(false)
  const [membersOpen, setMembersOpen] = useState(false)
  const [typesOpen, setTypesOpen] = useState(false)
  const [webhooksOpen, setWebhooksOpen] = useState(false)
  // Tipos de tarefa do projeto (Bug, Integração...), à parte do quadro: mudam pouco e têm diálogo próprio.
  const { data: types, reload: reloadTypes } = useAsync(() => api.listTaskTypes(id), [id])
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string>()
  // Só quem pode trabalhar nas tarefas (dono e membros) aparece como responsável possível.
  const usersById = useMemo(() => new Map((data?.[2] ?? []).filter((u) => u.role !== 'Viewer').map((u) => [u.id, u])), [data])

  if (loading && !data) return <Loading layout="board" />
  if (error) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  const [project, tasks, users] = data
  // Só o dono edita/exclui o projeto e gerencia pessoas; a API aplica a mesma regra (403).
  const isOwner = project.ownerId === user?.id
  const archived = project.archivedAt !== null
  // Visualizador vê tudo e não altera nada; a API aplica a mesma regra (403).
  const viewer = users.find((u) => u.id === user?.id)?.role === 'Viewer'
  const readOnly = archived || viewer
  const keyPrefix = projectKey(project.name)
  const done = tasks.filter((t) => t.status === 'Done').length

  const replaceTask = (task: Task) => setData(([p, t, u]) => [p, t.map((x) => (x.id === task.id ? task : x)), u])
  const removeTask = (taskId: string) => setData(([p, t, u]) => [p, t.filter((x) => x.id !== taskId && x.parentId !== taskId), u])
  const addTask = (task: Task) => setData(([p, t, u]) => [p, [...t, task], u])

  async function run(action: () => Promise<void>, taskId: string | null = null) {
    setBusyTaskId(taskId)
    setActionError(undefined)
    try {
      await action()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro inesperado.')
    } finally {
      setBusyTaskId(null)
    }
  }

  async function updateProject(input: ProjectInput) {
    setProject(await api.updateProject(id, input))
    setEditing(false)
  }

  const setProject = (updated: Project) => setData(([, t, u]) => [updated, t, u])

  const changeStatus = (status: ProjectStatus) => run(async () => setProject(await api.changeProjectStatus(id, status)))

  function archiveProject() {
    if (!window.confirm(`Arquivar "${project.name}"? Ele fica somente leitura e sai das listas, mas pode ser restaurado.`)) return
    run(async () => setProject(await api.archiveProject(id)))
  }

  const restoreProject = () => run(async () => setProject(await api.restoreProject(id)))

  function deleteProject() {
    if (!window.confirm(`Excluir o projeto "${project.name}" e todas as suas tarefas?`)) return
    run(async () => {
      await api.deleteProject(id)
      navigate('/projects')
    })
  }

  // Tarefa criada abre direto no detalhe, para completar descrição, responsável e prazo.
  async function createTask(input: TaskInput) {
    const task = await api.createTask(id, input)
    addTask(task)
    navigate(`tasks/${task.id}`)
  }

  function moveTask(task: Task, status: TaskStatus) {
    run(async () => replaceTask(await api.changeTaskStatus(task.id, status)), task.id)
  }

  function deleteTask(task: Task) {
    if (!window.confirm(`Excluir a tarefa "${task.title}"?`)) return
    run(async () => {
      await api.deleteTask(task.id)
      removeTask(task.id)
    }, task.id)
  }

  const outletContext: ProjectOutletContext = {
    project,
    tasks,
    users,
    keyPrefix,
    readOnly,
    types: types ?? [],
    replaceTask,
    removeTask,
    addTask,
    liveVersion,
  }

  return (
    <>
      {/* Projeto dentro de um espaço volta para o espaço; projeto avulso volta para o diretório. */}
      <Link to={project.spaceId ? `/spaces/${project.spaceId}` : '/projects'} className="back-link">
        <ArrowLeftIcon size={14} weight="bold" aria-hidden="true" />
        {project.spaceName ?? 'Projetos'}
      </Link>

      {editing ? (
        <ProjectForm
          initial={{ name: project.name, description: project.description, spaceId: project.spaceId }}
          spaces={(spaces ?? []).filter((s) => s.ownerId === user?.id)}
          submitLabel="Salvar"
          onSubmit={updateProject}
          onCancel={() => setEditing(false)}
        />
      ) : (
        <header className="page-header">
          <div className="page-header__text">
            <div className="page-header__title">
              <ProjectAvatar name={project.name} size="lg" />
              <h1>{project.name}</h1>

              {isOwner && !archived ? (
                <select
                  className={`status-pill status-pill--${project.status.toLowerCase()}`}
                  aria-label="Status do projeto"
                  value={project.status}
                  onChange={(e) => changeStatus(e.target.value as ProjectStatus)}
                >
                  {PROJECT_STATUSES.map((status) => (
                    <option key={status} value={status}>
                      {PROJECT_STATUS_LABEL[status]}
                    </option>
                  ))}
                </select>
              ) : (
                <span className={`status-pill status-pill--${project.status.toLowerCase()}`}>
                  {PROJECT_STATUS_LABEL[project.status]}
                </span>
              )}
            </div>
            {project.description && <p className="page-header__description">{project.description}</p>}
          </div>

          <div className="page-header__actions">
            {/* Exportar é leitura: vale para qualquer pessoa do projeto, inclusive visualizador e projeto arquivado. */}
            <button className="btn btn--ghost" onClick={() => run(() => api.exportProjectCsv(id))} title="Baixar as tarefas em planilha (CSV)">
              <DownloadSimpleIcon size={16} aria-hidden="true" />
              Exportar CSV
            </button>
            <button
              className="avatar-stack"
              onClick={() => setMembersOpen(true)}
              aria-label={`Pessoas do projeto (${users.length})`}
              title="Pessoas do projeto"
            >
              {users.slice(0, 4).map((u) => (
                <Avatar key={u.id} name={u.name} />
              ))}
              {users.length > 4 && <span className="avatar avatar--more">+{users.length - 4}</span>}
            </button>

            {isOwner ? (
              <>
                {!archived && (
                  <>
                    <button className="btn btn--ghost" onClick={() => setMembersOpen(true)}>
                      <UserPlusIcon size={16} aria-hidden="true" />
                      Compartilhar
                    </button>
                    <button className="btn btn--ghost" onClick={() => setTypesOpen(true)}>
                      <TagIcon size={16} aria-hidden="true" />
                      Tipos
                    </button>
                    <button className="btn btn--ghost" onClick={() => setWebhooksOpen(true)} title="Avisar outros sistemas quando algo muda">
                      <PlugsConnectedIcon size={16} aria-hidden="true" />
                      Webhooks
                    </button>
                    <button className="btn btn--ghost" onClick={() => setEditing(true)}>
                      <PencilSimpleIcon size={16} aria-hidden="true" />
                      Editar
                    </button>
                    <button className="icon-btn icon-btn--lg" onClick={archiveProject} aria-label="Arquivar projeto" title="Arquivar projeto">
                      <ArchiveIcon size={18} />
                    </button>
                  </>
                )}
                <button
                  className="icon-btn icon-btn--danger icon-btn--lg"
                  onClick={deleteProject}
                  aria-label="Excluir projeto"
                  title="Excluir projeto"
                >
                  <TrashIcon size={18} />
                </button>
              </>
            ) : (
              <span className="badge badge--shared">Compartilhado com você</span>
            )}
          </div>
        </header>
      )}

      {viewer && !archived && (
        <div className="notice" role="status">
          <EyeIcon size={18} aria-hidden="true" />
          <span className="notice__text">Você é visualizador deste projeto: pode acompanhar tudo, mas não alterar.</span>
        </div>
      )}

      {archived && (
        <div className="notice" role="status">
          <ArchiveIcon size={18} aria-hidden="true" />
          <span className="notice__text">Este projeto está arquivado e é somente leitura.</span>
          {isOwner && (
            <button className="btn btn--ghost btn--sm" onClick={restoreProject}>
              <ArrowCounterClockwiseIcon size={14} aria-hidden="true" />
              Restaurar
            </button>
          )}
        </div>
      )}

      <div className="project-progress">
        <span className="project-progress__label">
          <strong>{done}</strong> de {tasks.length} {tasks.length === 1 ? 'tarefa concluída' : 'tarefas concluídas'}
        </span>
        <div className="progress" role="progressbar" aria-valuenow={done} aria-valuemin={0} aria-valuemax={tasks.length}>
          <div className="progress__bar" style={{ transform: `scaleX(${tasks.length ? done / tasks.length : 0})` }} />
        </div>
        {live && (
          <span className="live" title="Alterações de outras pessoas aparecem aqui na hora">
            <span className="live__dot" aria-hidden="true" />
            Ao vivo
          </span>
        )}
      </div>

      {actionError && <ErrorMessage message={actionError} />}

      <KanbanBoard
        tasks={tasks}
        keyPrefix={keyPrefix}
        usersById={usersById}
        types={types ?? []}
        currentUserId={user?.id}
        busyTaskId={busyTaskId}
        readOnly={readOnly}
        onCreate={createTask}
        onMove={moveTask}
        onDelete={deleteTask}
      />

      {webhooksOpen && <WebhooksDialog project={project} onClose={() => setWebhooksOpen(false)} />}

      {typesOpen && (
        <TaskTypesDialog project={project} types={types ?? []} onClose={() => setTypesOpen(false)} onChanged={reloadTypes} />
      )}

      {membersOpen && user && (
        <ProjectMembersDialog
          project={project}
          members={users}
          currentUserId={user.id}
          onClose={() => setMembersOpen(false)}
          onChanged={reload}
          onLeft={() => navigate('/projects')}
        />
      )}

      <Outlet context={outletContext} />
    </>
  )
}
