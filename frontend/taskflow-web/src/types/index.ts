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

// Papel no projeto: dono, membro (trabalha nas tarefas) ou visualizador (só lê).
export type ProjectRole = 'Owner' | 'Member' | 'Viewer'

// Pessoa de um projeto: o dono ou alguém com quem ele foi compartilhado.
export interface ProjectMember extends UserSummary {
  isOwner: boolean
  role: ProjectRole
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
  // Espaço a que o projeto pertence; nulo = projeto sem espaço.
  spaceId: string | null
  spaceName: string | null
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
  spaceId: string | null
  spaceName: string | null
  status: ProjectStatus
  statusUpdatedAt: string
  archived: boolean
  createdAt: string
  taskCount: number
  doneCount: number
}

// Meta pessoal; o progresso (taskCount/doneCount) soma as tarefas dos projetos ligados.
export interface Goal {
  id: string
  name: string
  description: string | null
  targetDate: string | null
  status: ProjectStatus
  createdAt: string
  projects: { id: string; name: string; taskCount: number; doneCount: number }[]
  taskCount: number
  doneCount: number
}

export interface GoalInput {
  name: string
  description: string | null
  targetDate: string | null
  status: ProjectStatus
  projectIds: string[]
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

// Espaço: agrupa projetos de uma área ou time. projectCount conta os projetos ativos que eu consigo ver.
export interface SpaceSummary {
  id: string
  name: string
  description: string | null
  ownerId: string
  ownerName: string
  projectCount: number
  createdAt: string
}

export interface Space {
  id: string
  name: string
  description: string | null
  ownerId: string
  ownerName: string
  createdAt: string
  projects: ProjectSummary[]
}

// Modelo da galeria: criar um espaço a partir dele já traz projetos e tarefas iniciais.
export interface SpaceTemplate {
  key: string
  name: string
  description: string
  category: string
  recommended: boolean
  projects: { name: string; description: string; taskCount: number }[]
}

export interface ForYouResponse {
  recommendedSpaces: SpaceSummary[]
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
  typeId: string | null
  // Tarefa-pai, quando esta é uma subtarefa.
  parentId: string | null
}

// Tipo da tarefa (Bug, Integração...): cada projeto tem os seus, criados pelo dono.
export interface TaskType {
  id: string
  name: string
  color: string
}

export type TaskEventType =
  | 'Created'
  | 'TitleChanged'
  | 'DescriptionChanged'
  | 'PriorityChanged'
  | 'DueDateChanged'
  | 'StatusChanged'
  | 'AssigneeChanged'
  | 'TypeChanged'
  | 'ParentChanged'
  | 'AttachmentAdded'
  | 'AttachmentRemoved'

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

export interface SearchResult {
  tasks: { id: string; projectId: string; projectName: string; number: number; title: string; status: TaskStatus }[]
  projects: { id: string; name: string; spaceName: string | null; archived: boolean }[]
  spaces: { id: string; name: string }[]
}

export type NotificationType = 'Assigned' | 'Mentioned' | 'Commented'

// "App" no nome para não colidir com o Notification global do navegador.
export interface AppNotification {
  id: string
  type: NotificationType
  actorName: string
  taskId: string
  projectId: string
  projectName: string
  taskNumber: number
  taskTitle: string
  createdAt: string
  read: boolean
}

export interface NotificationList {
  unreadCount: number
  items: AppNotification[]
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
  spaceId: string | null
}

// Arquivo anexado a uma tarefa; o conteúdo se baixa à parte (com o token).
export interface Attachment {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedById: string | null
  createdAt: string
}

export interface TaskInput {
  title: string
  description: string | null
  priority: TaskPriority
  dueDate: string | null
  // Só na criação: tipo, tarefa-pai (subtarefa) e responsável.
  typeId?: string | null
  parentId?: string | null
  assignedToId?: string | null
}
