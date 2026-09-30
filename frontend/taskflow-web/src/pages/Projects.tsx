import { PlusIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectCard } from '../components/ProjectCard'
import { ProjectForm } from '../components/ProjectForm'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { ProjectInput } from '../types'

export function Projects() {
  const { data, loading, error, reload } = useAsync(api.listProjects, [])
  const [creating, setCreating] = useState(false)
  const navigate = useNavigate()

  async function create(input: ProjectInput) {
    const project = await api.createProject(input)
    navigate(`/projects/${project.id}`)
  }

  return (
    <>
      <header className="page-header">
        <div className="page-header__text">
          <h1>Projetos</h1>
          {data && data.length > 0 && (
            <p className="page-header__description">
              {data.length} {data.length === 1 ? 'projeto' : 'projetos'}
            </p>
          )}
        </div>
        {!creating && (
          <button className="btn btn--primary" onClick={() => setCreating(true)}>
            <PlusIcon size={16} weight="bold" aria-hidden="true" />
            Novo projeto
          </button>
        )}
      </header>

      {creating && <ProjectForm submitLabel="Criar projeto" onSubmit={create} onCancel={() => setCreating(false)} />}

      {loading && !data && <Loading layout="grid" />}
      {error && <ErrorMessage message={error} onRetry={reload} />}
      {data && data.length === 0 && !creating && (
        <div className="empty-state">
          <p>Nenhum projeto ainda.</p>
          <button className="btn btn--ghost" onClick={() => setCreating(true)}>
            Criar o primeiro projeto
          </button>
        </div>
      )}
      {data && data.length > 0 && (
        <div className="grid">
          {data.map((p) => (
            <ProjectCard key={p.id} project={p} />
          ))}
        </div>
      )}
    </>
  )
}
