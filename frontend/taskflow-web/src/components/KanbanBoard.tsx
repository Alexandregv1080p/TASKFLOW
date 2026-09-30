import type { Task, TaskInput, TaskStatus, UserSummary } from '../types'
import { STATUSES, STATUS_LABEL } from '../utils/format'
import { TaskCard } from './TaskCard'
import { TaskComposer } from './TaskComposer'

interface Props {
  tasks: Task[]
  keyPrefix: string
  usersById: Map<string, UserSummary>
  busyTaskId: string | null
  onCreate: (input: TaskInput) => Promise<void>
  onMove: (task: Task, status: TaskStatus) => void
  onDelete: (task: Task) => void
}

export function KanbanBoard({ tasks, keyPrefix, usersById, busyTaskId, onCreate, onMove, onDelete }: Props) {
  return (
    <div className="kanban">
      {STATUSES.map((status) => {
        const column = tasks.filter((t) => t.status === status)

        return (
          <section key={status} className="kanban__column" aria-label={STATUS_LABEL[status]}>
            <header className="kanban__header">
              <span className={`status-dot status-dot--${status.toLowerCase()}`} aria-hidden="true" />
              <h2>{STATUS_LABEL[status]}</h2>
              <span className="kanban__count">{column.length}</span>
            </header>

            <div className="kanban__list">
              {column.map((task) => (
                <TaskCard
                  key={task.id}
                  task={task}
                  taskKey={`${keyPrefix}-${task.number}`}
                  assignee={task.assignedToId ? usersById.get(task.assignedToId) : undefined}
                  busy={busyTaskId === task.id}
                  onMove={onMove}
                  onDelete={onDelete}
                />
              ))}

              {status === 'Todo' ? (
                <TaskComposer onSubmit={onCreate} />
              ) : (
                column.length === 0 && <p className="kanban__empty">Nenhuma tarefa</p>
              )}
            </div>
          </section>
        )
      })}
    </div>
  )
}
