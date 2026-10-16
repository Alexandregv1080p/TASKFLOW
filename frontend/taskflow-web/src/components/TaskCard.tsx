import {
  ArrowElbowDownRightIcon,
  ArrowLeftIcon,
  ArrowRightIcon,
  CalendarBlankIcon,
  CheckSquareOffsetIcon,
  StarIcon,
  TrashIcon,
  WarningIcon,
} from '@phosphor-icons/react'
import { Link } from 'react-router-dom'
import type { Task, TaskStatus, TaskType, UserSummary } from '../types'
import { PRIORITY_LABEL, STATUSES, STATUS_LABEL, formatDate, isOverdue } from '../utils/format'
import { Avatar } from './Avatar'
import { PriorityIcon } from './PriorityIcon'
import { TypeBadge } from './TypeBadge'

interface Props {
  task: Task
  taskKey: string
  assignee?: UserSummary
  type?: TaskType
  subtasks: Task[]
  parent?: Task
  keyPrefix: string
  busy: boolean
  readOnly: boolean
  dragging: boolean
  onDragStart: () => void
  onDragEnd: () => void
  onMove: (task: Task, status: TaskStatus) => void
  onDelete: (task: Task) => void
}

export function TaskCard({
  task,
  taskKey,
  assignee,
  type,
  subtasks,
  parent,
  keyPrefix,
  busy,
  readOnly,
  dragging,
  onDragStart,
  onDragEnd,
  onMove,
  onDelete,
}: Props) {
  // O domínio só permite andar uma coluna por vez; os botões refletem isso.
  // Em projeto arquivado (somente leitura) não há para onde mover.
  const index = STATUSES.indexOf(task.status)
  const previous = readOnly ? undefined : STATUSES[index - 1]
  const next = readOnly ? undefined : STATUSES[index + 1]
  const overdue = isOverdue(task.dueDate, task.status)

  return (
    <article
      className="task-card"
      aria-busy={busy}
      data-dragging={dragging || undefined}
      draggable={!busy && !readOnly}
      onDragStart={(e) => {
        e.dataTransfer.effectAllowed = 'move'
        e.dataTransfer.setData('text/plain', task.title)
        onDragStart()
      }}
      onDragEnd={onDragEnd}
    >
      {/* O link do título cobre o card inteiro (::after); os botões ficam acima dele. */}
      {/* draggable={false}: sem isso o navegador arrastaria o link (a URL), e não o card. */}
      <h3 className="task-card__title">
        <Link to={`tasks/${task.id}`} className="task-card__link" draggable={false}>
          {task.title}
        </Link>
      </h3>
      {task.description && <p className="task-card__description clamp">{task.description}</p>}

      <div className="task-card__meta">
        {type && <TypeBadge type={type} />}
        {parent && (
          <span className="task-card__parent" title={`Subtarefa de ${parent.title}`}>
            <ArrowElbowDownRightIcon size={13} aria-hidden="true" />
            <span className="sr-only">Subtarefa de </span>
            {keyPrefix}-{parent.number}
          </span>
        )}
        {subtasks.length > 0 && (
          <span className="task-card__subtasks" title="Subtarefas concluídas">
            <CheckSquareOffsetIcon size={13} aria-hidden="true" />
            {subtasks.filter((s) => s.status === 'Done').length}/{subtasks.length}
            <span className="sr-only"> subtarefas concluídas</span>
          </span>
        )}
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
      </div>

      <footer className="task-card__footer">
        <span className="task-card__key" title={`Prioridade ${PRIORITY_LABEL[task.priority]}`}>
          <PriorityIcon priority={task.priority} size={14} />
          {taskKey}
          {task.starred && (
            <>
              <StarIcon size={13} weight="fill" className="task-card__star" aria-hidden="true" />
              <span className="sr-only"> (com estrela)</span>
            </>
          )}
        </span>

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
          {!readOnly && (
            <button
              className="icon-btn icon-btn--danger"
              disabled={busy}
              onClick={() => onDelete(task)}
              aria-label={`Excluir tarefa ${task.title}`}
              title="Excluir tarefa"
            >
              <TrashIcon size={15} />
            </button>
          )}
        </div>

        {assignee ? (
          <Avatar name={assignee.name} size="sm" />
        ) : (
          <span className="avatar avatar--sm avatar--empty" title="Sem responsável" aria-hidden="true" />
        )}
      </footer>
    </article>
  )
}
