import { Link } from 'react-router-dom'
import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectCard } from '../components/ProjectCard'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'

export function Dashboard() {
  const { data, loading, error, reload } = useAsync(api.getDashboard, [])

  if (loading && !data) return <Loading layout="dashboard" />
  if (error) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  const stats = [
    { label: 'Projetos', value: data.totalProjects },
    { label: 'Tarefas', value: data.totalTasks },
    { label: 'A fazer', value: data.tasksByStatus.todo, tone: 'todo' },
    { label: 'Em andamento', value: data.tasksByStatus.inProgress, tone: 'inprogress' },
    { label: 'Concluídas', value: data.tasksByStatus.done, tone: 'done' },
  ]

  const priorities = [
    { label: 'Alta', value: data.tasksByPriority.high, tone: 'high' },
    { label: 'Média', value: data.tasksByPriority.medium, tone: 'medium' },
    { label: 'Baixa', value: data.tasksByPriority.low, tone: 'low' },
  ]

  return (
    <>
      <h1>Dashboard</h1>

      <div className="stats">
        {stats.map((s) => (
          <div key={s.label} className={`card stat ${s.tone ? `stat--${s.tone}` : ''}`}>
            <span className="stat__value">{s.value}</span>
            <span className="stat__label">{s.label}</span>
          </div>
        ))}
      </div>

      <section className="section">
        <h2>Tarefas por prioridade</h2>
        <div className="card priority-list">
          {priorities.map((p) => (
            <div key={p.label} className="priority-row">
              <span className={`badge badge--${p.tone}`}>{p.label}</span>
              <div className="progress">
                <div
                  className={`progress__bar progress__bar--${p.tone}`}
                  style={{ transform: `scaleX(${data.totalTasks ? p.value / data.totalTasks : 0})` }}
                />
              </div>
              <span className="priority-row__value">{p.value}</span>
            </div>
          ))}
        </div>
      </section>

      <section className="section">
        <div className="section__header">
          <h2>Projetos recentes</h2>
          <Link to="/projects">Ver todos</Link>
        </div>

        {data.recentProjects.length === 0 ? (
          <p className="empty">
            Nenhum projeto ainda. <Link to="/projects">Crie o primeiro.</Link>
          </p>
        ) : (
          <div className="grid">
            {data.recentProjects.map((p) => (
              <ProjectCard key={p.id} project={p} />
            ))}
          </div>
        )}
      </section>
    </>
  )
}
