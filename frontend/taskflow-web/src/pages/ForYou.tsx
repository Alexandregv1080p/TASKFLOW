import { CheckSquareIcon, StarIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { Avatar } from '../components/Avatar'
import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { ForYouFilter, ForYouItem } from '../types'
import { STATUS_LABEL, dateGroup, projectKey } from '../utils/format'

const TABS: { filter: ForYouFilter; label: string }[] = [
  { filter: 'Recommended', label: 'Recomendado' },
  { filter: 'Assigned', label: 'Atribuído a mim' },
  { filter: 'Starred', label: 'Com estrela' },
  { filter: 'Worked', label: 'Trabalhado' },
  { filter: 'Viewed', label: 'Visto' },
]

const EMPTY: Record<ForYouFilter, string> = {
  Recommended: 'Nada pedindo sua atenção agora. Tarefas abertas atribuídas a você ou em que você trabalhou aparecem aqui.',
  Assigned: 'Nenhuma tarefa aberta atribuída a você.',
  Starred: 'Você ainda não marcou nenhuma tarefa com estrela. Use a estrela no detalhe da tarefa ou nesta lista.',
  Worked: 'Você ainda não alterou nem comentou nenhuma tarefa.',
  Viewed: 'Você ainda não abriu nenhuma tarefa.',
}

export function ForYou() {
  // A aba vive na URL (?filter=): os atalhos "Recentes" e "Com estrela" do menu abrem direto nela.
  const [params, setParams] = useSearchParams()
  const requested = params.get('filter')
  const filter = TABS.find((tab) => tab.filter === requested)?.filter ?? 'Recommended'
  const setFilter = (next: ForYouFilter) => setParams(next === 'Recommended' ? {} : { filter: next })

  const { data, loading, error, reload, setData } = useAsync(() => api.getForYou(filter), [filter])
  const [actionError, setActionError] = useState<string>()

  async function toggleStar(item: ForYouItem) {
    setActionError(undefined)
    try {
      const task = await api.setTaskStar(item.taskId, !item.starred)

      setData((current) => ({
        ...current,
        items:
          // Na aba Com estrela, tirar a estrela tira a tarefa da lista.
          filter === 'Starred' && !task.starred
            ? current.items.filter((i) => i.taskId !== item.taskId)
            : current.items.map((i) => (i.taskId === item.taskId ? { ...i, starred: task.starred } : i)),
      }))
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao marcar a tarefa.')
    }
  }

  if (loading && !data) return <Loading layout="grid" />
  if (error && !data) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  // Os itens já vêm do mais recente para o mais antigo: agrupar preserva a ordem dos grupos.
  const groups = new Map<string, ForYouItem[]>()
  for (const item of data.items) {
    const label = dateGroup(item.date)
    groups.set(label, [...(groups.get(label) ?? []), item])
  }

  return (
    <>
      <h1 className="sr-only">Para você</h1>

      <section className="for-you__section">
        <div className="section__header">
          <h2>Espaços recomendados</h2>
          <Link to="/projects" className="section__link">
            Ver todos os projetos
          </Link>
        </div>

        {data.recommendedProjects.length === 0 ? (
          <div className="empty-state">
            <p>Você ainda não participa de nenhum projeto.</p>
            <Link to="/projects?new=1" className="btn btn--primary">
              Criar projeto
            </Link>
          </div>
        ) : (
          <div className="space-list">
            {data.recommendedProjects.map((project) => (
              <Link key={project.id} to={`/projects/${project.id}`} className="space-card">
                <ProjectAvatar name={project.name} size="lg" />
                <span className="space-card__text">
                  <span className="space-card__name">{project.name}</span>
                  <span className="space-card__meta">
                    {project.taskCount} {project.taskCount === 1 ? 'tarefa' : 'tarefas'}
                  </span>
                </span>
              </Link>
            ))}
          </div>
        )}
      </section>

      <section className="for-you__section">
        <div className="for-you__bar">
          <h2>Para você</h2>

          <div className="tabs" role="tablist" aria-label="Filtrar tarefas">
            {TABS.map((tab) => (
              <button
                key={tab.filter}
                role="tab"
                aria-selected={filter === tab.filter}
                className="tabs__tab"
                onClick={() => setFilter(tab.filter)}
              >
                {tab.label}
                {tab.filter === 'Assigned' && data.assignedCount > 0 && (
                  <span className="tabs__count">{data.assignedCount > 99 ? '99+' : data.assignedCount}</span>
                )}
              </button>
            ))}
          </div>
        </div>

        {(error || actionError) && <ErrorMessage message={(error ?? actionError)!} onRetry={error ? reload : undefined} />}

        <div className={loading ? 'for-you__list for-you__list--loading' : 'for-you__list'} role="tabpanel">
          {data.items.length === 0 ? (
            <p className="empty">{EMPTY[filter]}</p>
          ) : (
            [...groups].map(([label, items]) => (
              <div key={label} className="work-group">
                <h3 className="work-group__title">{label}</h3>

                <ul className="work-list">
                  {items.map((item) => (
                    <li key={item.taskId} className="work-item">
                      <span className="work-item__icon" aria-hidden="true">
                        <CheckSquareIcon size={18} weight="bold" />
                      </span>

                      <div className="work-item__text">
                        {/* O link do título cobre a linha inteira (::after); a estrela fica acima dele. */}
                        <Link to={`/projects/${item.projectId}/tasks/${item.taskId}`} className="work-item__title">
                          {item.title}
                        </Link>
                        <span className="work-item__meta">
                          <span>Tarefa</span>
                          <span>
                            {projectKey(item.projectName)}-{item.number}
                          </span>
                          <span>{item.projectName}</span>
                          <span>{STATUS_LABEL[item.status]}</span>
                        </span>
                      </div>

                      <button
                        className={item.starred ? 'icon-btn work-item__star work-item__star--on' : 'icon-btn work-item__star'}
                        onClick={() => toggleStar(item)}
                        aria-pressed={item.starred}
                        aria-label={item.starred ? `Remover estrela de ${item.title}` : `Marcar ${item.title} com estrela`}
                        title={item.starred ? 'Remover estrela' : 'Marcar com estrela'}
                      >
                        <StarIcon size={16} weight={item.starred ? 'fill' : 'regular'} />
                      </button>

                      {item.assigneeName ? (
                        <Avatar name={item.assigneeName} size="sm" />
                      ) : (
                        <span className="avatar avatar--sm avatar--empty" title="Sem responsável" aria-hidden="true" />
                      )}
                    </li>
                  ))}
                </ul>
              </div>
            ))
          )}
        </div>
      </section>
    </>
  )
}
