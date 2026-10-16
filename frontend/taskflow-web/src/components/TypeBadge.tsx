import type { CSSProperties } from 'react'
import type { TaskType } from '../types'

// Selo do tipo da tarefa, na cor escolhida pelo dono do projeto.
export function TypeBadge({ type }: { type: TaskType }) {
  return (
    <span className="type-badge" style={{ '--type-color': type.color } as CSSProperties}>
      {type.name}
    </span>
  )
}
