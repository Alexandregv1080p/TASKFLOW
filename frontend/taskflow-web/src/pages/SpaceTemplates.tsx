import { XIcon } from '@phosphor-icons/react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { ErrorMessage, Loading } from '../components/Feedback'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { SpaceTemplate } from '../types'

const MADE_FOR_YOU = 'Feitos para você'

// Galeria de modelos para criar um espaço, no formato da tela "Space templates" do Jira:
// categorias à esquerda, cartões de modelo à direita.
export function SpaceTemplates() {
  const { data: templates, loading, error, reload } = useAsync(api.listSpaceTemplates, [])
  const [category, setCategory] = useState(MADE_FOR_YOU)
  const [chosen, setChosen] = useState<SpaceTemplate | null>(null)

  if (loading && !templates) return <Loading layout="grid" />
  if (error) return <ErrorMessage message={error} onRetry={reload} />
  if (!templates) return null

  // Categorias vêm do próprio catálogo, na ordem em que aparecem.
  const categories = [...new Set(templates.map((t) => t.category))]
  const visible = category === MADE_FOR_YOU ? templates.filter((t) => t.recommended) : templates.filter((t) => t.category === category)

  return (
    <div className="templates">
      <aside className="templates__nav">
        <Link to="/spaces" className="tool-btn templates__close" aria-label="Fechar e voltar aos espaços" title="Fechar">
          <XIcon size={17} />
        </Link>
        <h1>Modelos de espaço</h1>

        <nav aria-label="Categorias de modelos">
          <button className="templates__category" aria-current={category === MADE_FOR_YOU ? 'true' : undefined} onClick={() => setCategory(MADE_FOR_YOU)}>
            {MADE_FOR_YOU}
          </button>

          <p className="templates__heading">Categorias</p>
          {categories.map((name) => (
            <button key={name} className="templates__category" aria-current={category === name ? 'true' : undefined} onClick={() => setCategory(name)}>
              {name}
            </button>
          ))}
        </nav>
      </aside>

      <section className="templates__main" aria-labelledby="templates-title">
        <h2 id="templates-title">{category}</h2>
        <p className="templates__intro">
          {category === MADE_FOR_YOU
            ? 'Modelos mais usados para começar. Cada um já cria o espaço com projetos e tarefas iniciais.'
            : `Modelos de ${category.toLowerCase()}.`}
        </p>

        <div className="template-grid">
          {visible.map((template) => {
            const taskCount = template.projects.reduce((sum, p) => sum + p.taskCount, 0)

            return (
              <button key={template.key} className="template-card" onClick={() => setChosen(template)}>
                {/* Prévia real do que o modelo cria: os projetos que virão dentro do espaço. */}
                <span className="template-card__preview" aria-hidden="true">
                  {template.projects.length === 0 ? (
                    <span className="template-card__blank">Sem projetos iniciais</span>
                  ) : (
                    template.projects.map((project) => (
                      <span key={project.name} className="template-card__row">
                        <ProjectAvatar name={project.name} size="sm" />
                        <span className="template-card__row-name">{project.name}</span>
                        <span className="template-card__row-count">{project.taskCount}</span>
                      </span>
                    ))
                  )}
                </span>

                <span className="template-card__title">
                  {template.name}
                  {template.recommended && category !== MADE_FOR_YOU && <span className="template-card__tag">Recomendado</span>}
                </span>
                <span className="template-card__description">{template.description}</span>
                <span className="template-card__meta">
                  {template.projects.length === 0
                    ? 'Começa vazio'
                    : `${template.projects.length} ${template.projects.length === 1 ? 'projeto' : 'projetos'}, ${taskCount} ${taskCount === 1 ? 'tarefa' : 'tarefas'}`}
                </span>
              </button>
            )
          })}
        </div>
      </section>

      {chosen && <CreateSpaceDialog template={chosen} onClose={() => setChosen(null)} />}
    </div>
  )
}

function CreateSpaceDialog({ template, onClose }: { template: SpaceTemplate; onClose: () => void }) {
  const navigate = useNavigate()
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      const space = await api.createSpace({ name, description: description.trim() || null, templateKey: template.key })
      navigate(`/spaces/${space.id}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar o espaço.')
      setSaving(false)
    }
  }

  return (
    <dialog
      ref={dialogRef}
      className="modal"
      aria-label={`Criar espaço com o modelo ${template.name}`}
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <form className="modal__surface" onSubmit={handleSubmit}>
        <header className="modal__header">
          <h2>Criar espaço</h2>
          <button type="button" className="tool-btn" onClick={onClose} aria-label="Fechar" title="Fechar">
            <XIcon size={17} />
          </button>
        </header>

        <p className="modal__intro">
          Modelo <strong>{template.name}</strong>.{' '}
          {template.projects.length === 0
            ? 'O espaço começa vazio.'
            : `Serão criados: ${template.projects.map((p) => p.name).join(', ')}.`}
        </p>

        <label className="field">
          <span className="field__label">Nome do espaço</span>
          <input autoFocus value={name} onChange={(e) => setName(e.target.value)} required maxLength={100} placeholder="Ex.: Marketing" />
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

        <footer className="modal__footer">
          <button type="button" className="btn btn--ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn--primary" disabled={saving}>
            {saving ? 'Criando…' : 'Criar espaço'}
          </button>
        </footer>
      </form>
    </dialog>
  )
}
