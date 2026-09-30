import { useState, type FormEvent } from 'react'
import type { TaskInput, TaskPriority } from '../types'
import { PRIORITY_LABEL } from '../utils/format'

export function TaskForm({ onSubmit }: { onSubmit: (input: TaskInput) => Promise<void> }) {
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [dueDate, setDueDate] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      await onSubmit({ title, description: description.trim() || null, priority, dueDate: dueDate || null })
      setTitle('')
      setDescription('')
      setDueDate('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="card form form--inline" onSubmit={handleSubmit}>
      <label className="form__grow">
        Nova tarefa
        <input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={200} placeholder="Título" />
      </label>
      <label className="form__grow">
        Descrição
        <input value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} placeholder="Opcional" />
      </label>
      <label>
        Prioridade
        <select value={priority} onChange={(e) => setPriority(e.target.value as TaskPriority)}>
          {(Object.keys(PRIORITY_LABEL) as TaskPriority[]).map((p) => (
            <option key={p} value={p}>
              {PRIORITY_LABEL[p]}
            </option>
          ))}
        </select>
      </label>
      <label>
        Prazo
        <input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
      </label>
      <button className="btn btn--primary" disabled={saving}>
        {saving ? 'Adicionando…' : 'Adicionar'}
      </button>

      {error && <p className="form__error" role="alert">{error}</p>}
    </form>
  )
}
