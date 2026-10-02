import { PlusIcon, SquaresFourIcon } from '@phosphor-icons/react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'

// Lista de espaços. Um espaço agrupa os projetos de uma área ou time.
export function Spaces() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useAsync(api.listSpaces, [])

  return (
    <>
      <header className="page-header">
        <div className="page-header__text">
          <h1>Espaços</h1>
          <p className="page-header__description">
            Um espaço reúne os projetos de uma mesma área ou time, como Marketing ou Engenharia.
          </p>
        </div>
        <Link to="/spaces/new" className="btn btn--primary">
          <PlusIcon size={16} weight="bold" aria-hidden="true" />
          Criar espaço
        </Link>
      </header>

      {loading && !data && <Loading layout="grid" />}
      {error && <ErrorMessage message={error} onRetry={reload} />}

      {data && data.length === 0 && (
        <div className="empty-state">
          <SquaresFourIcon size={28} aria-hidden="true" />
          <p>Você ainda não tem espaços. Crie um a partir de um modelo e ele já vem com projetos e tarefas iniciais.</p>
          <Link to="/spaces/new" className="btn btn--ghost">
            Escolher um modelo
          </Link>
        </div>
      )}

      {data && data.length > 0 && (
        <div className="grid">
          {data.map((space) => (
            <Link key={space.id} to={`/spaces/${space.id}`} className="card space-tile">
              <div className="space-tile__header">
                <ProjectAvatar name={space.name} size="lg" />
                <h2>{space.name}</h2>
              </div>
              <p className="space-tile__description clamp">{space.description ?? 'Sem descrição'}</p>
              <p className="space-tile__meta">
                <span>
                  {space.projectCount} {space.projectCount === 1 ? 'projeto' : 'projetos'}
                </span>
                <span>{space.ownerId === user?.id ? 'Criado por você' : `De ${space.ownerName}`}</span>
              </p>
            </Link>
          ))}
        </div>
      )}
    </>
  )
}
