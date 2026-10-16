import { PlusIcon } from '@phosphor-icons/react'
import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../services/api'
import type { Task, UserSummary } from '../types'
import { STATUS_LABEL } from '../utils/format'
import { Avatar } from './Avatar'

interface Props {
  parent: Task
  subtasks: Task[]
  keyPrefix: string
  users: UserSummary[]
  readOnly: boolean
  // A subtarefa criada entra no quadro (ela também é uma tarefa do projeto).
  onCreated: (task: Task) => void
}

// Subtarefas no detalhe da tarefa, como no Jira: progresso, lista e criação rápida pelo título.
export function Subtasks({ parent, subtasks, keyPrefix, users, readOnly, onCreated }: Props) {
  const [adding, setAdding] = useState(false)
  const [title, setTitle] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()
  const done = subtasks.filter((t) => t.status === 'Done').length

  async function add(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)
    try {
      // Herda o tipo da tarefa-pai; prioridade e o resto se ajustam no detalhe da subtarefa.
      onCreated(await api.createTask(parent.projectId, { title, description: null, priority: 'Medium', dueDate: null, parentId: parent.id, typeId: parent.typeId }))
      setTitle('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar a subtarefa.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      {subtasks.length > 0 && (
        <div className="project-progress subtasks__progress">
          <span className="project-progress__label">
            <strong>{done}</strong> de {subtasks.length} concluídas
          </span>
          <div className="progress" role="progressbar" aria-label="Subtarefas concluídas" aria-valuenow={done} aria-valuemin={0} aria-valuemax={subtasks.length}>
            <div className="progress__bar" style={{ transform: `scaleX(${done / subtasks.length})` }} />
          </div>
        </div>
      )}

      {subtasks.length > 0 && (
        <ul className="subtasks">
          {subtasks.map((sub) => {
            const assignee = users.find((u) => u.id === sub.assignedToId)
            return (
              <li key={sub.id} className="subtasks__item">
                <Link to={`/projects/${sub.projectId}/tasks/${sub.id}`} className="subtasks__link">
                  <span className="subtasks__key">
                    {keyPrefix}-{sub.number}
                  </span>
                  <span className={sub.status === 'Done' ? 'subtasks__title subtasks__title--done' : 'subtasks__title'}>{sub.title}</span>
                </Link>
                <span className={`status-pill status-pill--task-${sub.status.toLowerCase()}`}>{STATUS_LABEL[sub.status]}</span>
                {assignee ? (
                  <Avatar name={assignee.name} size="sm" />
                ) : (
                  <span className="avatar avatar--sm avatar--empty" title="Sem responsável" aria-hidden="true" />
                )}
              </li>
            )
          })}
        </ul>
      )}

      {!readOnly &&
        (adding ? (
          <form className="subtasks__form" onSubmit={add} onKeyDown={(e) => e.key === 'Escape' && (e.preventDefault(), setAdding(false))}>
            <input
              autoFocus
              aria-label="Título da subtarefa"
              placeholder="O que precisa ser feito?"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              maxLength={200}
            />
            <button className="btn btn--primary btn--sm" disabled={saving}>
              {saving ? 'Criando…' : 'Criar'}
            </button>
            <button type="button" className="btn btn--ghost btn--sm" onClick={() => setAdding(false)}>
              Cancelar
            </button>
          </form>
        ) : (
          <button className="link-btn subtasks__add" onClick={() => setAdding(true)}>
            <PlusIcon size={14} weight="bold" aria-hidden="true" />
            Criar subtarefa
          </button>
        ))}

      {error && (
        <p className="field__error" role="alert">
          {error}
        </p>
      )}
    </>
  )
}
