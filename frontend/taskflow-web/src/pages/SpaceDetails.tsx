import { ArrowLeftIcon, PencilSimpleIcon, PlusIcon, TrashIcon } from '@phosphor-icons/react'
import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { ProjectCard } from '../components/ProjectCard'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { Space } from '../types'

export function SpaceDetails() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const { data: space, loading, error, reload, setData } = useAsync(() => api.getSpace(id), [id])
  const [editing, setEditing] = useState(false)
  const [actionError, setActionError] = useState<string>()

  if (loading && !space) return <Loading layout="grid" />
  if (error) return <ErrorMessage message={error} onRetry={reload} />
  if (!space) return null

  // Só o dono edita/exclui o espaço e cria projetos nele; a API aplica a mesma regra.
  const isOwner = space.ownerId === user?.id

  async function remove(current: Space) {
    const count = current.projects.length
    const detail = count === 0 ? '' : ` Os ${count === 1 ? 'projeto dele não é apagado: fica' : 'projetos dele não são apagados: ficam'} sem espaço.`
    if (!window.confirm(`Excluir o espaço "${current.name}"?${detail}`)) return

    setActionError(undefined)
    try {
      await api.deleteSpace(current.id)
      navigate('/spaces')
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao excluir.')
    }
  }

  return (
    <>
      <Link to="/spaces" className="back-link">
        <ArrowLeftIcon size={14} weight="bold" aria-hidden="true" />
        Espaços
      </Link>

      {editing ? (
        <SpaceForm
          space={space}
          onSaved={(updated) => {
            setData(() => updated)
            setEditing(false)
          }}
          onCancel={() => setEditing(false)}
        />
      ) : (
        <header className="page-header">
          <div className="page-header__text">
            <div className="page-header__title">
              <ProjectAvatar name={space.name} size="lg" />
              <h1>{space.name}</h1>
            </div>
            {space.description && <p className="page-header__description">{space.description}</p>}
          </div>

          <div className="page-header__actions">
            {isOwner ? (
              <>
                <Link to={`/projects?new=1&space=${space.id}`} className="btn btn--primary">
                  <PlusIcon size={16} weight="bold" aria-hidden="true" />
                  Novo projeto
                </Link>
                <button className="btn btn--ghost" onClick={() => setEditing(true)}>
                  <PencilSimpleIcon size={16} aria-hidden="true" />
                  Editar
                </button>
                <button className="icon-btn icon-btn--danger icon-btn--lg" onClick={() => remove(space)} aria-label="Excluir espaço" title="Excluir espaço">
                  <TrashIcon size={18} />
                </button>
              </>
            ) : (
              <span className="badge badge--shared">Espaço de {space.ownerName}</span>
            )}
          </div>
        </header>
      )}

      {actionError && <ErrorMessage message={actionError} />}

      <section aria-labelledby="space-projects">
        <h2 id="space-projects" className="directory__title">
          Projetos deste espaço
        </h2>

        {space.projects.length === 0 ? (
          <div className="empty-state">
            <p>
              {isOwner
                ? 'Este espaço ainda não tem projetos. Crie um aqui, ou mova um projeto existente editando-o.'
                : 'Nenhum projeto deste espaço está compartilhado com você.'}
            </p>
            {isOwner && (
              <Link to={`/projects?new=1&space=${space.id}`} className="btn btn--ghost">
                Criar o primeiro projeto
              </Link>
            )}
          </div>
        ) : (
          <div className="grid">
            {space.projects.map((project) => (
              <ProjectCard key={project.id} project={project} />
            ))}
          </div>
        )}
      </section>
    </>
  )
}

function SpaceForm({ space, onSaved, onCancel }: { space: Space; onSaved: (space: Space) => void; onCancel: () => void }) {
  const [name, setName] = useState(space.name)
  const [description, setDescription] = useState(space.description ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      onSaved(await api.updateSpace(space.id, { name, description: description.trim() || null }))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
      setSaving(false)
    }
  }

  return (
    <form className="card project-form" onSubmit={handleSubmit} onKeyDown={(e) => e.key === 'Escape' && onCancel()}>
      <label className="field">
        <span className="field__label">Nome do espaço</span>
        <input autoFocus value={name} onChange={(e) => setName(e.target.value)} required maxLength={100} />
      </label>
      <label className="field">
        <span className="field__label">Descrição</span>
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={1000} rows={2} />
      </label>

      {error && (
        <p className="field__error" role="alert">
          {error}
        </p>
      )}

      <div className="project-form__actions">
        <button type="button" className="btn btn--ghost" onClick={onCancel}>
          Cancelar
        </button>
        <button className="btn btn--primary" disabled={saving}>
          {saving ? 'Salvando…' : 'Salvar'}
        </button>
      </div>
    </form>
  )
}
