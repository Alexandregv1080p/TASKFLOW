import { useState, type FormEvent } from 'react'
import type { ProjectInput, SpaceSummary } from '../types'

interface Props {
  initial?: ProjectInput
  // Espaços em que o usuário pode colocar o projeto (os que ele criou).
  spaces: SpaceSummary[]
  // Espaço já escolhido ao abrir (ex.: "Novo projeto" clicado dentro de um espaço).
  defaultSpaceId?: string | null
  submitLabel: string
  onSubmit: (input: ProjectInput) => Promise<void>
  onCancel: () => void
}

export function ProjectForm({ initial, spaces, defaultSpaceId, submitLabel, onSubmit, onCancel }: Props) {
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [spaceId, setSpaceId] = useState(initial?.spaceId ?? defaultSpaceId ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      await onSubmit({ name, description: description.trim() || null, spaceId: spaceId || null })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
      setSaving(false)
    }
  }

  return (
    <form className="card project-form" onSubmit={handleSubmit} onKeyDown={(e) => e.key === 'Escape' && onCancel()}>
      <label className="field">
        <span className="field__label">Nome</span>
        <input
          autoFocus
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={100}
          placeholder="Ex.: Website institucional"
        />
      </label>
      <label className="field">
        <span className="field__label">Descrição</span>
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={1000} rows={2} />
      </label>

      <label className="field">
        <span className="field__label">Espaço</span>
        <select value={spaceId} onChange={(e) => setSpaceId(e.target.value)}>
          <option value="">Sem espaço</option>
          {spaces.map((space) => (
            <option key={space.id} value={space.id}>
              {space.name}
            </option>
          ))}
        </select>
        <span className="field__hint">
          {spaces.length === 0
            ? 'Você ainda não criou espaços. O projeto pode ficar sem espaço e ser movido depois.'
            : 'Um espaço agrupa os projetos de uma mesma área ou time.'}
        </span>
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
          {saving ? 'Salvando…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
