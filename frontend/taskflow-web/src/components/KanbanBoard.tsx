import type { Task, TaskStatus } from '../types'
import { STATUSES, STATUS_LABEL } from '../utils/format'
import { TaskCard } from './TaskCard'

interface Props {
  tasks: Task[]
  busyTaskId: string | null
  onMove: (task: Task, status: TaskStatus) => void
  onDelete: (task: Task) => void
}

export function KanbanBoard({ tasks, busyTaskId, onMove, onDelete }: Props) {
  return (
    <div className="kanban">
      {STATUSES.map((status) => {
        const column = tasks.filter((t) => t.status === status)

        return (
          <section key={status} className={`kanban__column kanban__column--${status.toLowerCase()}`}>
            <header className="kanban__header">
              <h3>{STATUS_LABEL[status]}</h3>
              <span className="kanban__count">{column.length}</span>
            </header>

            {column.length === 0 ? (
              <p className="kanban__empty">Nenhuma tarefa</p>
            ) : (
              column.map((task) => (
                <TaskCard
                  key={task.id}
                  task={task}
                  busy={busyTaskId === task.id}
                  onMove={onMove}
                  onDelete={onDelete}
                />
              ))
            )}
          </section>
        )
      })}
    </div>
  )
}
