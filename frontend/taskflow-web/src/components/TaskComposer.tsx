import { PlusIcon } from '@phosphor-icons/react'
import { useRef, useState, type FormEvent } from 'react'
import type { TaskInput, TaskPriority, TaskType, UserSummary } from '../types'
import { PRIORITY_LABEL } from '../utils/format'

// "+ Nova tarefa" dentro da coluna A fazer. Fica aberto após adicionar, para cadastrar várias em sequência.
// Valor do "Sem tipo" no select (vazio já significa "o primeiro tipo do projeto").
const NO_TYPE = 'none'

interface Props {
  onSubmit: (input: TaskInput) => Promise<void>
  types: TaskType[]
  // Pessoas do projeto, que podem ser responsáveis.
  users: UserSummary[]
  currentUserId: string | undefined
}

export function TaskComposer({ onSubmit, types, users, currentUserId }: Props) {
  const [open, setOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<TaskPriority>('Medium')
  const [dueDate, setDueDate] = useState('')
  // Vazio = o primeiro tipo do projeto (ou sem tipo, se não houver nenhum).
  const [typeId, setTypeId] = useState('')
  const [assignedToId, setAssignedToId] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()
  const titleRef = useRef<HTMLInputElement>(null)

  function close() {
    setOpen(false)
    setError(undefined)
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      await onSubmit({
        title,
        description: description.trim() || null,
        priority,
        dueDate: dueDate || null,
        typeId: typeId === NO_TYPE ? null : (typeId || types[0]?.id) ?? null,
        assignedToId: assignedToId || null,
      })
      setTitle('')
      setDescription('')
      setDueDate('')
      titleRef.current?.focus()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
    } finally {
      setSaving(false)
    }
  }

  if (!open) {
    return (
      <button className="composer-trigger" onClick={() => setOpen(true)}>
        <PlusIcon size={16} weight="bold" aria-hidden="true" />
        Nova tarefa
      </button>
    )
  }

  return (
    <form className="card composer" onSubmit={handleSubmit} onKeyDown={(e) => e.key === 'Escape' && close()}>
      <label className="field">
        <span className="field__label">Título</span>
        <input
          ref={titleRef}
          autoFocus
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          maxLength={200}
          placeholder="O que precisa ser feito?"
        />
      </label>

      <label className="field">
        <span className="field__label">Descrição</span>
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} rows={2} />
      </label>

      <label className="field">
        <span className="field__label">Tipo</span>
        <select value={typeId || types[0]?.id || NO_TYPE} onChange={(e) => setTypeId(e.target.value)}>
          {types.map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
          <option value={NO_TYPE}>Sem tipo</option>
        </select>
      </label>

      <div className="field">
        <label className="field__label" htmlFor="composer-assignee">
          Responsável
        </label>
        <select id="composer-assignee" value={assignedToId} onChange={(e) => setAssignedToId(e.target.value)}>
          <option value="">Sem responsável</option>
          {users.map((u) => (
            <option key={u.id} value={u.id}>
              {u.name}
            </option>
          ))}
        </select>
        {currentUserId && assignedToId !== currentUserId && (
          <button type="button" className="link-btn composer__me" onClick={() => setAssignedToId(currentUserId)}>
            Atribuir a mim
          </button>
        )}
      </div>

      <div className="composer__row">
        <label className="field">
          <span className="field__label">Prioridade</span>
          <select value={priority} onChange={(e) => setPriority(e.target.value as TaskPriority)}>
            {(Object.keys(PRIORITY_LABEL) as TaskPriority[]).map((p) => (
              <option key={p} value={p}>
                {PRIORITY_LABEL[p]}
              </option>
            ))}
          </select>
        </label>
        <label className="field">
          <span className="field__label">Prazo</span>
          <input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} />
        </label>
      </div>

      {error && (
        <p className="field__error" role="alert">
          {error}
        </p>
      )}

      <div className="composer__actions">
        <button type="button" className="btn btn--ghost btn--sm" onClick={close}>
          Fechar
        </button>
        <button className="btn btn--primary btn--sm" disabled={saving}>
          {saving ? 'Adicionando…' : 'Adicionar'}
        </button>
      </div>
    </form>
  )
}
