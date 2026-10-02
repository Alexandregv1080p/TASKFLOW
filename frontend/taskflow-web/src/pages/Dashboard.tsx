import { ArrowClockwiseIcon, CaretLeftIcon, CaretRightIcon, ChartLineUpIcon, CheckSquareIcon } from '@phosphor-icons/react'
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { ErrorMessage, Loading } from '../components/Feedback'
import { PriorityIcon } from '../components/PriorityIcon'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import { PRIORITY_LABEL, projectKey } from '../utils/format'

const ASSIGNED_PAGE_SIZE = 15

const timeFormat = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' })

// Dashboard padrão: painéis fixos, no formato do dashboard do Jira.
export function Dashboard() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useAsync(
    () =>
      Promise.all([api.getDashboard(), api.listProjects('All'), api.getForYou('Assigned')]).then(
        ([summary, projects, forYou]) => ({ summary, projects, assigned: forYou.items, loadedAt: new Date() }),
      ),
    [],
  )
  const [page, setPage] = useState(0)

  if (loading && !data) return <Loading layout="dashboard" />
  if (error && !data) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  const { summary, projects, assigned, loadedAt } = data
  const updated = `Atualizado às ${timeFormat.format(loadedAt)}`

  // Se a lista encolheu depois de atualizar, volta para a última página que ainda existe.
  const lastPage = Math.max(0, Math.ceil(assigned.length / ASSIGNED_PAGE_SIZE) - 1)
  const currentPage = Math.min(page, lastPage)
  const first = currentPage * ASSIGNED_PAGE_SIZE
  const pageItems = assigned.slice(first, first + ASSIGNED_PAGE_SIZE)

  const byStatus = [
    { label: 'A fazer', value: summary.tasksByStatus.todo, tone: 'todo' },
    { label: 'Em andamento', value: summary.tasksByStatus.inProgress, tone: 'inprogress' },
    { label: 'Concluídas', value: summary.tasksByStatus.done, tone: 'done' },
  ]

  const byPriority = [
    { label: 'Alta', value: summary.tasksByPriority.high, tone: 'high' },
    { label: 'Média', value: summary.tasksByPriority.medium, tone: 'medium' },
    { label: 'Baixa', value: summary.tasksByPriority.low, tone: 'low' },
  ]

  return (
    <>
      <header className="page-header">
        <div className="page-header__text">
          <h1>Dashboard padrão</h1>
        </div>
        <button className="btn btn--ghost" onClick={reload} disabled={loading}>
          <ArrowClockwiseIcon size={16} aria-hidden="true" />
          {loading ? 'Atualizando…' : 'Atualizar'}
        </button>
      </header>

      {error && <ErrorMessage message={error} onRetry={reload} />}

      <div className={loading ? 'gadgets gadgets--loading' : 'gadgets'}>
        <div className="gadgets__column">
          <Gadget title="Introdução" footer={updated}>
            <div className="intro">
              <span className="intro__icon" aria-hidden="true">
                <ChartLineUpIcon size={34} />
              </span>
              <div>
                <p className="intro__title">Bem-vindo ao TaskFlow{user ? `, ${user.name.split(' ')[0]}` : ''}</p>
                <p>
                  É a primeira vez por aqui? Veja <Link to="/projects?help=1">como funcionam os projetos</Link>.
                </p>
              </div>
            </div>
          </Gadget>

          <Gadget title="Projetos" footer={updated}>
            {projects.length === 0 ? (
              <p className="gadget__empty">
                Você ainda não participa de nenhum projeto. <Link to="/projects?new=1">Criar projeto</Link>
              </p>
            ) : (
              <ul className="gadget-list">
                {projects.map((project) => (
                  <li key={project.id} className="gadget-list__item">
                    <ProjectAvatar name={project.name} size="sm" />
                    <div className="gadget-list__text">
                      <Link to={`/projects/${project.id}`} className="gadget-list__name">
                        {project.name}
                      </Link>{' '}
                      <span className="gadget-list__key">({projectKey(project.name)})</span>
                      <p className="gadget-list__meta">
                        <strong>Líder do projeto</strong> {project.ownerName}
                      </p>
                    </div>
                    <span className="gadget-list__count">
                      {project.doneCount}/{project.taskCount}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </Gadget>

          <Gadget title="Resumo das tarefas" footer={updated}>
            <div className="summary">
              {byStatus.map((s) => (
                <div key={s.label} className="summary__stat">
                  <span className="summary__value">{s.value}</span>
                  <span className="summary__label">
                    <span className={`status-dot status-dot--${s.tone}`} aria-hidden="true" />
                    {s.label}
                  </span>
                </div>
              ))}
            </div>

            <div className="priority-list">
              {byPriority.map((p) => (
                <div key={p.label} className="priority-row">
                  <span className="priority-row__label">{p.label}</span>
                  <div className="progress">
                    <div
                      className={`progress__bar progress__bar--${p.tone}`}
                      style={{ transform: `scaleX(${summary.totalTasks ? p.value / summary.totalTasks : 0})` }}
                    />
                  </div>
                  <span className="priority-row__value">{p.value}</span>
                </div>
              ))}
            </div>
          </Gadget>
        </div>

        <div className="gadgets__column">
          <Gadget title="Atribuído a mim" footer={updated}>
            {assigned.length === 0 ? (
              <p className="gadget__empty">Nenhuma tarefa aberta atribuída a você.</p>
            ) : (
              <>
                <table className="gadget-table">
                  <thead>
                    <tr>
                      <th scope="col">
                        <abbr title="Tipo">T</abbr>
                      </th>
                      <th scope="col">Chave</th>
                      <th scope="col">Resumo</th>
                      <th scope="col">
                        <abbr title="Prioridade">P</abbr>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    {pageItems.map((item) => {
                      const to = `/projects/${item.projectId}/tasks/${item.taskId}`

                      return (
                        <tr key={item.taskId}>
                          <td>
                            <CheckSquareIcon size={16} weight="bold" className="gadget-table__type" aria-label="Tarefa" />
                          </td>
                          <td className="gadget-table__key">
                            <Link to={to}>
                              {projectKey(item.projectName)}-{item.number}
                            </Link>
                          </td>
                          <td>
                            <Link to={to}>{item.title}</Link>
                          </td>
                          <td title={`Prioridade ${PRIORITY_LABEL[item.priority]}`}>
                            <PriorityIcon priority={item.priority} />
                            <span className="sr-only">{PRIORITY_LABEL[item.priority]}</span>
                          </td>
                        </tr>
                      )
                    })}
                  </tbody>
                </table>

                <div className="pager">
                  <span>
                    <strong>
                      {first + 1}-{first + pageItems.length}
                    </strong>{' '}
                    de {assigned.length}
                  </span>
                  {lastPage > 0 && (
                    <span className="pager__buttons">
                      <button className="icon-btn" disabled={currentPage === 0} onClick={() => setPage(currentPage - 1)} aria-label="Página anterior">
                        <CaretLeftIcon size={15} weight="bold" />
                      </button>
                      <button className="icon-btn" disabled={currentPage === lastPage} onClick={() => setPage(currentPage + 1)} aria-label="Próxima página">
                        <CaretRightIcon size={15} weight="bold" />
                      </button>
                    </span>
                  )}
                </div>
              </>
            )}
          </Gadget>
        </div>
      </div>
    </>
  )
}

function Gadget({ title, footer, children }: { title: string; footer: string; children: ReactNode }) {
  return (
    <section className="gadget">
      <h2 className="gadget__title">{title}</h2>
      <div className="gadget__body">{children}</div>
      <p className="gadget__footer">{footer}</p>
    </section>
  )
}
