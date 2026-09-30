import { CaretDownIcon, CaretUpIcon, EqualsIcon } from '@phosphor-icons/react'
import type { TaskPriority } from '../types'

// Mesma convenção visual do Jira: seta para cima (alta), igual (média), seta para baixo (baixa).
export function PriorityIcon({ priority, size = 16 }: { priority: TaskPriority; size?: number }) {
  const className = `priority-icon priority-icon--${priority.toLowerCase()}`

  if (priority === 'High') return <CaretUpIcon className={className} size={size} weight="bold" aria-hidden="true" />
  if (priority === 'Low') return <CaretDownIcon className={className} size={size} weight="bold" aria-hidden="true" />
  return <EqualsIcon className={className} size={size} weight="bold" aria-hidden="true" />
}
