import {
  ArchiveIcon,
  ArrowCounterClockwiseIcon,
  ArrowLeftIcon,
  PencilSimpleIcon,
  TrashIcon,
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
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { Project, ProjectInput, ProjectStatus, Task, TaskInput, TaskStatus, UserSummary } from '../types'
import { PROJECT_STATUSES, PROJECT_STATUS_LABEL, projectKey } from '../utils/format'

// Compartilhado com o modal de detalhe da tarefa (rota filha).
export interface ProjectOutletContext {
  project: Project
  tasks: Task[]
  users: UserSummary[]
  keyPrefix: string
  // Projeto arquivado: nada pode ser alterado.
  readOnly: boolean
  replaceTask: (task: Task) => void
  removeTask: (id: string) => void
}

export function ProjectDetails() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { data, loading, error, reload, setData } = useAsync(
    () => Promise.all([api.getProject(id), api.listTasks(id), api.listMembers(id)]),
    [id],
  )
  const { user } = useAuth()
  const [editing, setEditing] = useState(false)
  const [membersOpen, setMembersOpen] = useState(false)
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string>()
  const usersById = useMemo(() => new Map((data?.[2] ?? []).map((u) => [u.id, u])), [data])

  if (loading && !data) return <Loading layout="board" />
  if (error) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  const [project, tasks, users] = data
  // Só o dono edita/exclui o projeto e gerencia pessoas; a API aplica a mesma regra (403).
  const isOwner = project.ownerId === user?.id
  const archived = project.archivedAt !== null
  const keyPrefix = projectKey(project.name)
  const done = tasks.filter((t) => t.status === 'Done').length

  const replaceTask = (task: Task) => setData(([p, t, u]) => [p, t.map((x) => (x.id === task.id ? task : x)), u])
  const removeTask = (taskId: string) => setData(([p, t, u]) => [p, t.filter((x) => x.id !== taskId), u])

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
    setData(([p, t, u]) => [p, [...t, task], u])
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

  const outletContext: ProjectOutletContext = { project, tasks, users, keyPrefix, readOnly: archived, replaceTask, removeTask }

  return (
    <>
      <Link to="/projects" className="back-link">
        <ArrowLeftIcon size={14} weight="bold" aria-hidden="true" />
        Projetos
      </Link>

      {editing ? (
        <ProjectForm
          initial={{ name: project.name, description: project.description }}
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
      </div>

      {actionError && <ErrorMessage message={actionError} />}

      <KanbanBoard
        tasks={tasks}
        keyPrefix={keyPrefix}
        usersById={usersById}
        busyTaskId={busyTaskId}
        readOnly={archived}
        onCreate={createTask}
        onMove={moveTask}
        onDelete={deleteTask}
      />

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
