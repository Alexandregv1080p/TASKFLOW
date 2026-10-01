// Espelham os DTOs da API (TaskFlow.Application).

export type TaskStatus = 'Todo' | 'InProgress' | 'Done'

export type TaskPriority = 'Low' | 'Medium' | 'High'

export interface User {
  id: string
  name: string
  email: string
}

export interface UserSummary {
  id: string
  name: string
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: User
}

export interface Project {
  id: string
  name: string
  description: string | null
  ownerId: string
  createdAt: string
}

export interface ProjectSummary {
  id: string
  name: string
  description: string | null
  createdAt: string
  taskCount: number
  doneCount: number
}

export interface Task {
  id: string
  projectId: string
  number: number
  title: string
  description: string | null
  status: TaskStatus
  priority: TaskPriority
  dueDate: string | null
  assignedToId: string | null
  createdById: string | null
  createdAt: string
}

export type TaskEventType =
  | 'Created'
  | 'TitleChanged'
  | 'DescriptionChanged'
  | 'PriorityChanged'
  | 'DueDateChanged'
  | 'StatusChanged'
  | 'AssigneeChanged'

// oldValue/newValue: nome de enum (status, prioridade), data yyyy-MM-dd, id do usuário (responsável) ou texto (título).
export interface TaskEvent {
  id: string
  type: TaskEventType
  oldValue: string | null
  newValue: string | null
  userId: string | null
  createdAt: string
}

export interface TaskComment {
  id: string
  taskId: string
  authorId: string | null
  body: string
  createdAt: string
}

export interface Dashboard {
  totalProjects: number
  totalTasks: number
  tasksByStatus: { todo: number; inProgress: number; done: number }
  tasksByPriority: { low: number; medium: number; high: number }
  recentProjects: ProjectSummary[]
}

export interface ProjectInput {
  name: string
  description: string | null
}

export interface TaskInput {
  title: string
  description: string | null
  priority: TaskPriority
  dueDate: string | null
}
