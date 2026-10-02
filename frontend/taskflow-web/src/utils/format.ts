import type { ProjectStatus, TaskPriority, TaskStatus } from '../types'

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

export const PROJECT_STATUSES: ProjectStatus[] = ['OnTrack', 'AtRisk', 'OffTrack', 'Paused', 'Completed']

export const PROJECT_STATUS_LABEL: Record<ProjectStatus, string> = {
  OnTrack: 'No prazo',
  AtRisk: 'Em risco',
  OffTrack: 'Atrasado',
  Paused: 'Pausado',
  Completed: 'Concluído',
}

// Dias de calendário (fuso local) entre a data e hoje: 0 = hoje, 1 = ontem...
function daysAgo(value: string): number {
  const date = new Date(value)
  const today = new Date()
  const startOf = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()

  return Math.round((startOf(today) - startOf(date)) / 86_400_000)
}

// Grupo de data da lista "Para você".
export function dateGroup(value: string): string {
  const days = daysAgo(value)

  if (days <= 0) return 'Hoje'
  if (days === 1) return 'Ontem'
  if (days < 7) return 'Nesta semana'
  if (days <= 30) return 'No último mês'
  return 'Há mais de um mês'
}

const relativeFormat = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' })

// "hoje", "ontem", "há 5 dias", "há 2 meses".
export function formatRelativeDay(value: string): string {
  const days = daysAgo(value)

  if (days < 31) return relativeFormat.format(-Math.max(days, 0), 'day')
  if (days < 365) return relativeFormat.format(-Math.floor(days / 30), 'month')
  return relativeFormat.format(-Math.floor(days / 365), 'year')
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
