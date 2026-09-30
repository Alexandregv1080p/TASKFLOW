import { ArrowLeftIcon, ArrowRightIcon, CalendarBlankIcon, TrashIcon, WarningIcon } from '@phosphor-icons/react'
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
    <article className="task-card" aria-busy={busy}>
      <h3 className="task-card__title">{task.title}</h3>
      {task.description && <p className="task-card__description clamp">{task.description}</p>}

      <footer className="task-card__footer">
        <span className={`badge badge--${task.priority.toLowerCase()}`}>{PRIORITY_LABEL[task.priority]}</span>

        {task.dueDate && (
          <span className={overdue ? 'task-card__due task-card__due--late' : 'task-card__due'}>
            {overdue ? (
              <WarningIcon size={13} weight="bold" aria-hidden="true" />
            ) : (
              <CalendarBlankIcon size={13} aria-hidden="true" />
            )}
            {formatDate(task.dueDate)}
            {overdue && <span className="sr-only"> (atrasada)</span>}
          </span>
        )}

        <div className="task-card__actions">
          {previous && (
            <button
              className="icon-btn"
              disabled={busy}
              onClick={() => onMove(task, previous)}
              aria-label={`Mover para ${STATUS_LABEL[previous]}`}
              title={`Mover para ${STATUS_LABEL[previous]}`}
            >
              <ArrowLeftIcon size={15} weight="bold" />
            </button>
          )}
          {next && (
            <button
              className="icon-btn"
              disabled={busy}
              onClick={() => onMove(task, next)}
              aria-label={`Mover para ${STATUS_LABEL[next]}`}
              title={`Mover para ${STATUS_LABEL[next]}`}
            >
              <ArrowRightIcon size={15} weight="bold" />
            </button>
          )}
          <button
            className="icon-btn icon-btn--danger"
            disabled={busy}
            onClick={() => onDelete(task)}
            aria-label={`Excluir tarefa ${task.title}`}
            title="Excluir tarefa"
          >
            <TrashIcon size={15} />
          </button>
        </div>
      </footer>
    </article>
  )
}
