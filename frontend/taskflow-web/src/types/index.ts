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

// Pessoa de um projeto: o dono ou um membro com quem ele foi compartilhado.
export interface ProjectMember extends UserSummary {
  isOwner: boolean
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
  status: ProjectStatus
  statusUpdatedAt: string
  // Preenchido quando o projeto está arquivado (somente leitura).
  archivedAt: string | null
  createdAt: string
}

export type ProjectStatus = 'OnTrack' | 'AtRisk' | 'OffTrack' | 'Paused' | 'Completed'

// Abas do diretório de projetos.
export type ProjectScope = 'All' | 'Mine' | 'Archived'

export interface ProjectSummary {
  id: string
  name: string
  description: string | null
  ownerId: string
  ownerName: string
  status: ProjectStatus
  statusUpdatedAt: string
  archived: boolean
  createdAt: string
  taskCount: number
  doneCount: number
}

// Abas da página "Para você".
export type ForYouFilter = 'Recommended' | 'Assigned' | 'Starred' | 'Worked' | 'Viewed'

export interface ForYouItem {
  taskId: string
  projectId: string
  projectName: string
  number: number
  title: string
  status: TaskStatus
  priority: TaskPriority
  assignedToId: string | null
  assigneeName: string | null
  starred: boolean
  // Momento que colocou a tarefa na aba (última atividade, quando marquei, quando abri...).
  date: string
}

export interface ForYouResponse {
  recommendedProjects: ProjectSummary[]
  assignedCount: number
  items: ForYouItem[]
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
  // Se eu marquei a tarefa com estrela.
  starred: boolean
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
