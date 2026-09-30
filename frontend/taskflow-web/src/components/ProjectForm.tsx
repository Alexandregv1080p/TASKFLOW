import { useState, type FormEvent } from 'react'
import type { ProjectInput } from '../types'

interface Props {
  initial?: ProjectInput
  submitLabel: string
  onSubmit: (input: ProjectInput) => Promise<void>
  onCancel: () => void
}

export function ProjectForm({ initial, submitLabel, onSubmit, onCancel }: Props) {
  const [name, setName] = useState(initial?.name ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      await onSubmit({ name, description: description.trim() || null })
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
