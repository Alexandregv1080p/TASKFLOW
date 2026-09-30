import type { TaskPriority, TaskStatus } from '../types'

export const STATUSES: TaskStatus[] = ['Todo', 'InProgress', 'Done']

export const STATUS_LABEL: Record<TaskStatus, string> = {
  Todo: 'A fazer',
  InProgress: 'Em andamento',
  Done: 'Concluído',
}

export const PRIORITY_LABEL: Record<TaskPriority, string> = {
  Low: 'Baixa',
  Medium: 'Média',
  High: 'Alta',
}

// Chave curta do projeto para montar a chave da tarefa (ex.: "Website" -> WEB, "Mobile App" -> MA).
export function projectKey(name: string): string {
  const words = name
    .normalize('NFD')
    .replace(/\p{M}/gu, '')
    .replace(/[^\p{L}\p{N}\s]/gu, '')
    .toUpperCase()
    .split(/\s+/)
    .filter(Boolean)

  if (words.length === 0) return 'TF'
  if (words.length === 1) return words[0].slice(0, 3)
  return words
    .slice(0, 3)
    .map((w) => w[0])
    .join('')
}

const dateTimeFormat = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'long', timeStyle: 'short' })

export function formatDateTime(value: string): string {
  return dateTimeFormat.format(new Date(value))
}

// dueDate chega como "2026-10-05T00:00:00" (data sem fuso): formata sem passar por Date para não deslocar o dia.
export function formatDate(value: string): string {
  const [year, month, day] = value.slice(0, 10).split('-')
  return `${day}/${month}/${year}`
}

export function isOverdue(dueDate: string | null, status: TaskStatus): boolean {
  if (!dueDate || status === 'Done') return false
  const today = new Date().toLocaleDateString('sv') // YYYY-MM-DD no fuso local
  return dueDate.slice(0, 10) < today
}
