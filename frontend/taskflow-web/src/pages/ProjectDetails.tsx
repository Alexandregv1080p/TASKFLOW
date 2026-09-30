import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ErrorMessage, Loading } from '../components/Feedback'
import { KanbanBoard } from '../components/KanbanBoard'
import { ProjectForm } from '../components/ProjectForm'
import { TaskForm } from '../components/TaskForm'
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

  if (loading && !data) return <Loading />
  if (error) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  const [project, tasks] = data

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
        ← Projetos
      </Link>

      {editing ? (
        <ProjectForm
          initial={{ name: project.name, description: project.description }}
          submitLabel="Salvar"
          onSubmit={updateProject}
          onCancel={() => setEditing(false)}
        />
      ) : (
        <div className="page-header">
          <div>
            <h1>{project.name}</h1>
            {project.description && <p className="muted">{project.description}</p>}
          </div>
          <div className="page-header__actions">
            <button className="btn btn--ghost" onClick={() => setEditing(true)}>
              Editar
            </button>
            <button className="btn btn--danger-ghost" onClick={deleteProject}>
              Excluir
            </button>
          </div>
        </div>
      )}

      {actionError && <ErrorMessage message={actionError} />}

      <TaskForm onSubmit={createTask} />

      <KanbanBoard tasks={tasks} busyTaskId={busyTaskId} onMove={moveTask} onDelete={deleteTask} />
    </>
  )
}
