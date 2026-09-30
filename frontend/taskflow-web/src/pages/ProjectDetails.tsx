import { ArrowLeftIcon, PencilSimpleIcon, TrashIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorMessage, Loading } from '../components/Feedback'
import { KanbanBoard } from '../components/KanbanBoard'
import { ProjectForm } from '../components/ProjectForm'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { ProjectInput, Task, TaskInput, TaskStatus } from '../types'

export function ProjectDetails() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { data, loading, error, reload, setData } = useAsync(
    () => Promise.all([api.getProject(id), api.listTasks(id)]),
    [id],
  )
  const [editing, setEditing] = useState(false)
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string>()

  if (loading && !data) return <Loading layout="board" />
  if (error) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  const [project, tasks] = data
  const done = tasks.filter((t) => t.status === 'Done').length

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
    const updated = await api.updateProject(id, input)
    setData(([, t]) => [updated, t])
    setEditing(false)
  }

  function deleteProject() {
    if (!window.confirm(`Excluir o projeto "${project.name}" e todas as suas tarefas?`)) return
    run(async () => {
      await api.deleteProject(id)
      navigate('/projects')
    })
  }

  async function createTask(input: TaskInput) {
    const task = await api.createTask(id, input)
    setData(([p, t]) => [p, [...t, task]])
  }

  function moveTask(task: Task, status: TaskStatus) {
    run(async () => {
      const updated = await api.changeTaskStatus(task.id, status)
      setData(([p, t]) => [p, t.map((x) => (x.id === updated.id ? updated : x))])
    }, task.id)
  }

  function deleteTask(task: Task) {
    if (!window.confirm(`Excluir a tarefa "${task.title}"?`)) return
    run(async () => {
      await api.deleteTask(task.id)
      setData(([p, t]) => [p, t.filter((x) => x.id !== task.id)])
    }, task.id)
  }

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
            <h1>{project.name}</h1>
            {project.description && <p className="page-header__description">{project.description}</p>}
          </div>

          <div className="page-header__actions">
            <button className="btn btn--ghost" onClick={() => setEditing(true)}>
              <PencilSimpleIcon size={16} aria-hidden="true" />
              Editar
            </button>
            <button className="icon-btn icon-btn--danger icon-btn--lg" onClick={deleteProject} aria-label="Excluir projeto" title="Excluir projeto">
              <TrashIcon size={18} />
            </button>
          </div>
        </header>
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
        busyTaskId={busyTaskId}
        onCreate={createTask}
        onMove={moveTask}
        onDelete={deleteTask}
      />
    </>
  )
}
