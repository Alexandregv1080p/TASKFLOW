import type { Task, TaskStatus } from '../types'
import { PRIORITY_LABEL, STATUSES, STATUS_LABEL, formatDate, isOverdue } from '../utils/format'

interface Props {
  task: Task
  busy: boolean
  onMove: (task: Task, status: TaskStatus) => void
  onDelete: (task: Task) => void
}

export function TaskCard({ task, busy, onMove, onDelete }: Props) {
  // O domínio só permite andar uma coluna por vez; os botões refletem isso.
  const index = STATUSES.indexOf(task.status)
  const previous = STATUSES[index - 1]
  const next = STATUSES[index + 1]
  const overdue = isOverdue(task.dueDate, task.status)

  return (
    <article className="card task-card">
      <div className="task-card__header">
        <h4>{task.title}</h4>
        <span className={`badge badge--${task.priority.toLowerCase()}`}>{PRIORITY_LABEL[task.priority]}</span>
      </div>

      {task.description && <p className="muted clamp">{task.description}</p>}

      {task.dueDate && (
        <p className={overdue ? 'task-card__due task-card__due--late' : 'task-card__due'}>
          {overdue ? 'Atrasada · ' : 'Prazo · '}
          {formatDate(task.dueDate)}
        </p>
      )}

      <div className="task-card__actions">
        {previous && (
          <button className="btn btn--ghost btn--sm" disabled={busy} onClick={() => onMove(task, previous)}>
            ← {STATUS_LABEL[previous]}
          </button>
        )}
        {next && (
          <button className="btn btn--ghost btn--sm" disabled={busy} onClick={() => onMove(task, next)}>
            {STATUS_LABEL[next]} →
          </button>
        )}
        <button
          className="btn btn--danger-ghost btn--sm task-card__delete"
          disabled={busy}
          onClick={() => onDelete(task)}
          aria-label={`Excluir tarefa ${task.title}`}
        >
          Excluir
        </button>
      </div>
    </article>
  )
}
