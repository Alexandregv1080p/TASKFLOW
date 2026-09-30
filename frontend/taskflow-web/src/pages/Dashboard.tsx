import { ArrowRightIcon } from '@phosphor-icons/react'
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

  const byStatus = [
    { label: 'A fazer', value: data.tasksByStatus.todo, tone: 'todo' },
    { label: 'Em andamento', value: data.tasksByStatus.inProgress, tone: 'inprogress' },
    { label: 'Concluídas', value: data.tasksByStatus.done, tone: 'done' },
  ]

  const byPriority = [
    { label: 'Alta', value: data.tasksByPriority.high, tone: 'high' },
    { label: 'Média', value: data.tasksByPriority.medium, tone: 'medium' },
    { label: 'Baixa', value: data.tasksByPriority.low, tone: 'low' },
  ]

  return (
    <>
      <header className="page-header">
        <div className="page-header__text">
          <h1>Dashboard</h1>
          <p className="page-header__description">
            {data.totalProjects} {data.totalProjects === 1 ? 'projeto' : 'projetos'} e {data.totalTasks}{' '}
            {data.totalTasks === 1 ? 'tarefa' : 'tarefas'} no total
          </p>
        </div>
      </header>

      <div className="stats">
        {byStatus.map((s) => (
          <div key={s.label} className="card stat">
            <span className="stat__label">
              <span className={`status-dot status-dot--${s.tone}`} aria-hidden="true" />
              {s.label}
            </span>
            <span className="stat__value">{s.value}</span>
          </div>
        ))}

        <div className="card stat stat--wide">
          <span className="stat__label">Por prioridade</span>
          <div className="priority-list">
            {byPriority.map((p) => (
              <div key={p.label} className="priority-row">
                <span className="priority-row__label">{p.label}</span>
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
        </div>
      </div>

      <section className="section">
        <div className="section__header">
          <h2>Projetos recentes</h2>
          <Link to="/projects" className="section__link">
            Ver todos
            <ArrowRightIcon size={14} weight="bold" aria-hidden="true" />
          </Link>
        </div>

        {data.recentProjects.length === 0 ? (
          <div className="empty-state">
            <p>Nenhum projeto ainda.</p>
            <Link to="/projects" className="btn btn--ghost">
              Ir para Projetos
            </Link>
          </div>
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
