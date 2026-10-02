import { useState, type DragEvent } from 'react'
import type { Task, TaskInput, TaskStatus, UserSummary } from '../types'
import { STATUSES, STATUS_LABEL } from '../utils/format'
import { TaskCard } from './TaskCard'
import { TaskComposer } from './TaskComposer'

interface Props {
  tasks: Task[]
  keyPrefix: string
  usersById: Map<string, UserSummary>
  busyTaskId: string | null
  // Projeto arquivado: sem criar, mover ou excluir.
  readOnly: boolean
  onCreate: (input: TaskInput) => Promise<void>
  onMove: (task: Task, status: TaskStatus) => void
  onDelete: (task: Task) => void
}

// Mesma regra do domínio: a tarefa anda uma coluna por vez.
function canMove(task: Task, status: TaskStatus): boolean {
  return Math.abs(STATUSES.indexOf(status) - STATUSES.indexOf(task.status)) === 1
}

export function KanbanBoard({ tasks, keyPrefix, usersById, busyTaskId, readOnly, onCreate, onMove, onDelete }: Props) {
  // ponytail: drag-and-drop nativo (HTML5) não dispara em telas de toque; lá, e no teclado,
  // os botões de seta do card fazem o mesmo. Trocar por pointer events se o toque virar requisito.
  const [dragging, setDragging] = useState<Task | null>(null)
  const [over, setOver] = useState<TaskStatus | null>(null)

  function endDrag() {
    setDragging(null)
    setOver(null)
  }

  function handleDragOver(e: DragEvent, status: TaskStatus) {
    if (!dragging || !canMove(dragging, status)) return

    // preventDefault é o que marca a coluna como destino válido para o navegador.
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    if (over !== status) setOver(status)
  }

  function handleDrop(e: DragEvent, status: TaskStatus) {
    e.preventDefault()
    if (dragging && canMove(dragging, status)) onMove(dragging, status)
    endDrag()
  }

  return (
    <div className="kanban">
      {STATUSES.map((status) => {
        const column = tasks.filter((t) => t.status === status)
        const dropState = !dragging || dragging.status === status ? '' : canMove(dragging, status) ? 'target' : 'blocked'

        return (
          <section
            key={status}
            className="kanban__column"
            aria-label={STATUS_LABEL[status]}
            data-drop={dropState || undefined}
            data-over={over === status || undefined}
            onDragOver={(e) => handleDragOver(e, status)}
            onDragLeave={(e) => {
              // Só limpa ao sair da coluna de fato, não ao passar por cima dos filhos.
              if (!e.currentTarget.contains(e.relatedTarget as Node | null)) setOver(null)
            }}
            onDrop={(e) => handleDrop(e, status)}
          >
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
                  readOnly={readOnly}
                  dragging={dragging?.id === task.id}
                  onDragStart={() => setDragging(task)}
                  onDragEnd={endDrag}
                  onMove={onMove}
                  onDelete={onDelete}
                />
              ))}

              {status === 'Todo' && !readOnly ? (
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
