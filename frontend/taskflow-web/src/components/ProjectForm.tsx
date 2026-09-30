import { useState, type FormEvent } from 'react'
import type { ProjectInput } from '../types'

interface Props {
  initial?: ProjectInput
  submitLabel: string
  onSubmit: (input: ProjectInput) => Promise<void>
  onCancel?: () => void
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
      if (!initial) {
        setName('')
        setDescription('')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="card form" onSubmit={handleSubmit}>
      <label>
        Nome
        <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={100} placeholder="Ex.: Website" />
      </label>
      <label>
        Descrição
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={1000} rows={2} />
      </label>

      {error && <p className="form__error" role="alert">{error}</p>}

      <div className="form__actions">
        {onCancel && (
          <button type="button" className="btn btn--ghost" onClick={onCancel}>
            Cancelar
          </button>
        )}
        <button className="btn btn--primary" disabled={saving}>
          {saving ? 'Salvando…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
