import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectCard } from '../components/ProjectCard'
import { ProjectForm } from '../components/ProjectForm'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { ProjectInput } from '../types'

export function Projects() {
  const { data, loading, error, reload } = useAsync(api.listProjects, [])

  async function create(input: ProjectInput) {
    await api.createProject(input)
    reload()
  }

  return (
    <>
      <h1>Projetos</h1>

      <ProjectForm submitLabel="Criar projeto" onSubmit={create} />

      <section className="section">
        {loading && !data && <Loading layout="grid" />}
        {error && <ErrorMessage message={error} onRetry={reload} />}
        {data && data.length === 0 && (
          <p className="empty">Nenhum projeto ainda. Dê um nome no formulário acima para criar o primeiro.</p>
        )}
        {data && data.length > 0 && (
          <div className="grid">
            {data.map((p) => (
              <ProjectCard key={p.id} project={p} />
            ))}
          </div>
        )}
      </section>
    </>
  )
}
