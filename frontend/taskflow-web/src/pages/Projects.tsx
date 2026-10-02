import { ArchiveIcon, ArrowCounterClockwiseIcon, MagnifyingGlassIcon, PlusIcon, QuestionIcon } from '@phosphor-icons/react'
import { useEffect, useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { Avatar } from '../components/Avatar'
import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { ProjectForm } from '../components/ProjectForm'
import { ProjectsHelpDialog } from '../components/ProjectsHelpDialog'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { ProjectInput, ProjectScope, ProjectStatus, ProjectSummary } from '../types'
import { PROJECT_STATUSES, PROJECT_STATUS_LABEL, formatRelativeDay, projectKey } from '../utils/format'

const TABS: { scope: ProjectScope; label: string }[] = [
  { scope: 'All', label: 'Todos os projetos' },
  { scope: 'Mine', label: 'Meus projetos' },
  { scope: 'Archived', label: 'Arquivados' },
]

const SEARCH_DELAY_MS = 250

export function Projects() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const [scope, setScope] = useState<ProjectScope>('All')

  // A busca vive na URL (?search=): pode vir da barra do topo e sobrevive a recarregar a página.
  const search = params.get('search') ?? ''
  const creating = params.has('new')
  const [term, setTerm] = useState(search)

  const { data, loading, error, reload, setData } = useAsync(() => api.listProjects(scope, search), [scope, search])
  // Espaços que eu criei: são os únicos em que posso colocar um projeto.
  const { data: spaces } = useAsync(api.listSpaces, [])
  const ownSpaces = (spaces ?? []).filter((s) => s.ownerId === user?.id)
  // ?help=1 (link do dashboard) já abre a explicação de como funcionam os projetos.
  const [help, setHelp] = useState<'empty' | 'help' | null>(params.has('help') ? 'help' : null)
  const [emptyDismissed, setEmptyDismissed] = useState(false)
  const [actionError, setActionError] = useState<string>()

  useEffect(() => setTerm(search), [search])

  // Espera a pessoa parar de digitar antes de consultar a API.
  useEffect(() => {
    if (term === search) return

    const timer = setTimeout(() => setParam('search', term || null), SEARCH_DELAY_MS)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [term, search])

  function setParam(key: string, value: string | null) {
    setParams(
      (current) => {
        const next = new URLSearchParams(current)
        if (value === null) next.delete(key)
        else next.set(key, value)
        return next
      },
      { replace: true },
    )
  }

  const openCreate = () => {
    setHelp(null)
    setEmptyDismissed(true)
    setParam('new', '1')
  }

  async function create(input: ProjectInput) {
    const project = await api.createProject(input)
    navigate(`/projects/${project.id}`)
  }

  async function run(action: () => Promise<void>) {
    setActionError(undefined)
    try {
      await action()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro inesperado.')
    }
  }

  const changeStatus = (project: ProjectSummary, status: ProjectStatus) =>
    run(async () => {
      const updated = await api.changeProjectStatus(project.id, status)
      setData((list) =>
        list.map((p) => (p.id === project.id ? { ...p, status: updated.status, statusUpdatedAt: updated.statusUpdatedAt } : p)),
      )
    })

  function archive(project: ProjectSummary) {
    if (!window.confirm(`Arquivar "${project.name}"? Ele fica somente leitura e sai das listas, mas pode ser restaurado.`)) return
    run(async () => {
      await api.archiveProject(project.id)
      setData((list) => list.filter((p) => p.id !== project.id))
    })
  }

  const restore = (project: ProjectSummary) =>
    run(async () => {
      await api.restoreProject(project.id)
      setData((list) => list.filter((p) => p.id !== project.id))
    })

  // Sem nenhum projeto ativo (e sem busca filtrando): mostra o aviso com a explicação.
  const hasNoProjects = scope === 'All' && !search && !loading && data?.length === 0
  const dialog = help ?? (hasNoProjects && !emptyDismissed && !creating ? 'empty' : null)

  return (
    <>
      <header className="page-header">
        <div className="page-header__text">
          <h1>Projetos</h1>
        </div>
        <div className="page-header__actions">
          <button className="btn btn--ghost" onClick={() => setHelp('help')}>
            <QuestionIcon size={16} aria-hidden="true" />
            Como funcionam os projetos
          </button>
          {!creating && (
            <button className="btn btn--primary" onClick={openCreate}>
              <PlusIcon size={16} weight="bold" aria-hidden="true" />
              Criar projeto
            </button>
          )}
        </div>
      </header>

      {/* ?space= vem do botão "Novo projeto" de dentro de um espaço: o formulário já abre com ele escolhido. */}
      {creating && (
        <ProjectForm
          spaces={ownSpaces}
          defaultSpaceId={params.get('space')}
          submitLabel="Criar projeto"
          onSubmit={create}
          onCancel={() => setParam('new', null)}
        />
      )}

      <section aria-labelledby="directory-title">
        <h2 id="directory-title" className="directory__title">
          Diretório de projetos
        </h2>

        <div className="directory__bar">
          <div className="tabs" role="tablist" aria-label="Filtrar projetos">
            {TABS.map((tab) => (
              <button
                key={tab.scope}
                role="tab"
                aria-selected={scope === tab.scope}
                className="tabs__tab"
                onClick={() => setScope(tab.scope)}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <label className="search-field">
            <MagnifyingGlassIcon size={16} aria-hidden="true" />
            <input
              type="search"
              aria-label="Buscar projetos pelo nome"
              placeholder="Buscar projetos"
              value={term}
              onChange={(e) => setTerm(e.target.value)}
            />
          </label>
        </div>

        {actionError && <ErrorMessage message={actionError} />}
        {error && <ErrorMessage message={error} onRetry={reload} />}
        {loading && !data && <Loading layout="grid" />}

        {data && data.length === 0 && (
          <p className="empty">
            {search
              ? `Nenhum projeto encontrado para "${search}".`
              : scope === 'Archived'
                ? 'Nenhum projeto arquivado.'
                : scope === 'Mine'
                  ? 'Você ainda não criou nenhum projeto.'
                  : 'Nenhum projeto para mostrar.'}
          </p>
        )}

        {data && data.length > 0 && (
          <div className="table-wrap" role="tabpanel">
            <table className={loading ? 'directory directory--loading' : 'directory'}>
              <thead>
                <tr>
                  <th scope="col">Nome</th>
                  <th scope="col">Espaço</th>
                  <th scope="col">Status</th>
                  <th scope="col">Dono</th>
                  <th scope="col">Progresso</th>
                  <th scope="col">Atualização do status</th>
                  <th scope="col">
                    <span className="sr-only">Ações</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.map((project) => {
                  const isOwner = project.ownerId === user?.id
                  const progress = project.taskCount === 0 ? 0 : project.doneCount / project.taskCount

                  return (
                    <tr key={project.id}>
                      <td>
                        <Link to={`/projects/${project.id}`} className="directory__name">
                          <ProjectAvatar name={project.name} />
                          <span>
                            <span className="directory__name-text">{project.name}</span>
                            <span className="directory__key">{projectKey(project.name)}</span>
                          </span>
                        </Link>
                      </td>
                      <td>
                        {project.spaceId ? (
                          <Link to={`/spaces/${project.spaceId}`} className="directory__space">
                            {project.spaceName}
                          </Link>
                        ) : (
                          <span className="muted">Sem espaço</span>
                        )}
                      </td>
                      <td>
                        {isOwner && !project.archived ? (
                          <select
                            className={`status-pill status-pill--${project.status.toLowerCase()}`}
                            aria-label={`Status de ${project.name}`}
                            value={project.status}
                            onChange={(e) => changeStatus(project, e.target.value as ProjectStatus)}
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
                      </td>
                      <td>
                        <span className="directory__owner">
                          <Avatar name={project.ownerName} size="sm" />
                          {project.ownerName}
                          {isOwner && <span className="muted"> (você)</span>}
                        </span>
                      </td>
                      <td>
                        <span className="directory__progress">
                          <span className="progress" role="progressbar" aria-valuenow={Math.round(progress * 100)} aria-valuemin={0} aria-valuemax={100}>
                            <span className="progress__bar" style={{ transform: `scaleX(${progress})` }} />
                          </span>
                          {project.doneCount}/{project.taskCount}
                        </span>
                      </td>
                      <td className="directory__date">{formatRelativeDay(project.statusUpdatedAt)}</td>
                      <td className="directory__actions">
                        {isOwner && !project.archived && (
                          <button className="icon-btn" onClick={() => archive(project)} aria-label={`Arquivar ${project.name}`} title="Arquivar">
                            <ArchiveIcon size={16} />
                          </button>
                        )}
                        {isOwner && project.archived && (
                          <button className="btn btn--ghost btn--sm" onClick={() => restore(project)}>
                            <ArrowCounterClockwiseIcon size={14} aria-hidden="true" />
                            Restaurar
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {dialog && (
        <ProjectsHelpDialog
          key={dialog}
          initialView={dialog}
          onClose={() => {
            setHelp(null)
            setEmptyDismissed(true)
            setParam('help', null)
          }}
          onCreate={openCreate}
        />
      )}
    </>
  )
}
