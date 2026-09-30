// Espelham os DTOs da API (TaskFlow.Application).

export type TaskStatus = 'Todo' | 'InProgress' | 'Done'

export type TaskPriority = 'Low' | 'Medium' | 'High'

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
  title: string
  description: string | null
  status: TaskStatus
  priority: TaskPriority
  dueDate: string | null
  assignedToId: string | null
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
