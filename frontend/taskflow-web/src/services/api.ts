import type {
  Attachment,
  AuthResponse,
  Dashboard,
  ForYouFilter,
  ForYouResponse,
  Goal,
  GoalInput,
  NotificationList,
  Project,
  ProjectInput,
  ProjectMember,
  ProjectRole,
  ProjectScope,
  ProjectStatus,
  SearchResult,
  ProjectSummary,
  Space,
  SpaceSummary,
  SpaceTemplate,
  Task,
  TaskComment,
  TaskEvent,
  TaskInput,
  TaskStatus,
  TaskType,
  User,
} from '../types'

// Único ponto de comunicação com a API: base URL, token, headers e tratamento de erros ficam aqui.
// Vazio = mesma origem (proxy do Vite em dev).
const BASE_URL = `${import.meta.env.VITE_API_URL ?? ''}/api`
const TOKEN_KEY = 'taskflow.token'

// ponytail: token em localStorage é exposto a XSS; cookie httpOnly exigiria CSRF e mudanças na API.
// Mitigação atual: token de vida curta (60 min) e nenhum HTML de usuário renderizado sem escape.
function readToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}

let unauthorizedListener: (() => void) | null = null

export const session = {
  get token() {
    return readToken()
  },
  save(token: string) {
    try {
      localStorage.setItem(TOKEN_KEY, token)
    } catch {
      // Sem storage (modo privado etc.): a sessão dura só até recarregar a página.
    }
  },
  clear() {
    try {
      localStorage.removeItem(TOKEN_KEY)
    } catch {
      // ignorado
    }
  },
  onUnauthorized(listener: () => void) {
    unauthorizedListener = listener
  },
}

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const token = session.token
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (token) headers.Authorization = `Bearer ${token}`

  let response: Response

  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'Não foi possível conectar à API.')
  }

  if (!response.ok) {
    // Token expirado ou inválido: encerra a sessão e volta para o login.
    if (response.status === 401 && token) {
      session.clear()
      unauthorizedListener?.()
    }

    // A API responde erros como { status, title, message, timestamp }.
    const error = await response.json().catch(() => null)
    throw new ApiError(response.status, error?.message ?? `Erro ${response.status}`)
  }

  return response.status === 204 ? (undefined as T) : response.json()
}

// Busca um arquivo da API com o token (um <a href> ou <img src> comum não enviaria o header).
async function fetchFile(path: string): Promise<Response> {
  const token = session.token
  const response = await fetch(`${BASE_URL}${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
  if (!response.ok) {
    const error = await response.json().catch(() => null)
    throw new ApiError(response.status, error?.message ?? `Erro ${response.status}`)
  }
  return response
}

// Envia um arquivo (multipart). O navegador monta o Content-Type com o boundary.
async function upload<T>(path: string, file: File): Promise<T> {
  const body = new FormData()
  body.append('file', file)
  const token = session.token
  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, { method: 'POST', body, headers: token ? { Authorization: `Bearer ${token}` } : {} })
  } catch {
    throw new ApiError(0, 'Não foi possível conectar à API.')
  }

  if (!response.ok) {
    if (response.status === 413) throw new ApiError(413, 'O arquivo passa de 10 MB.')
    const error = await response.json().catch(() => null)
    throw new ApiError(response.status, error?.message ?? `Erro ${response.status}`)
  }
  return response.json()
}

// Baixa um arquivo da API (com o token, que um link comum não enviaria) e entrega ao navegador para salvar.
async function download(path: string): Promise<void> {
  const response = await fetchFile(path)

  // Nome sugerido pela API (Content-Disposition), com acentos via filename*.
  const disposition = response.headers.get('Content-Disposition') ?? ''
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition)?.[1]
  const plain = /filename="?([^";]+)"?/i.exec(disposition)?.[1]

  const url = URL.createObjectURL(await response.blob())
  const link = document.createElement('a')
  link.href = url
  link.download = encoded ? decodeURIComponent(encoded) : (plain ?? 'download')
  link.click()
  URL.revokeObjectURL(url)
}

export const api = {
  register: (name: string, email: string, password: string) =>
    request<AuthResponse>('/auth/register', 'POST', { name, email, password }),
  login: (email: string, password: string) => request<AuthResponse>('/auth/login', 'POST', { email, password }),
  me: () => request<User>('/auth/me'),

  listMembers: (projectId: string) => request<ProjectMember[]>(`/projects/${projectId}/members`),
  addMember: (projectId: string, email: string, role: ProjectRole) =>
    request<ProjectMember>(`/projects/${projectId}/members`, 'POST', { email, role }),
  changeMemberRole: (projectId: string, userId: string, role: ProjectRole) =>
    request<ProjectMember>(`/projects/${projectId}/members/${userId}`, 'PATCH', { role }),
  removeMember: (projectId: string, userId: string) => request<void>(`/projects/${projectId}/members/${userId}`, 'DELETE'),

  getDashboard: () => request<Dashboard>('/dashboard'),
  getForYou: (filter: ForYouFilter) => request<ForYouResponse>(`/for-you?filter=${filter}`),

  search: (q: string) => request<SearchResult>(`/search?${new URLSearchParams({ q })}`),

  listNotifications: () => request<NotificationList>('/notifications'),
  markNotificationRead: (id: string) => request<void>(`/notifications/${id}/read`, 'POST'),
  markAllNotificationsRead: () => request<void>('/notifications/read-all', 'POST'),

  listSpaces: () => request<SpaceSummary[]>('/spaces'),
  listSpaceTemplates: () => request<SpaceTemplate[]>('/spaces/templates'),
  getSpace: (id: string) => request<Space>(`/spaces/${id}`),
  createSpace: (input: { name: string; description: string | null; templateKey: string }) =>
    request<Space>('/spaces', 'POST', input),
  updateSpace: (id: string, input: { name: string; description: string | null }) => request<Space>(`/spaces/${id}`, 'PUT', input),
  deleteSpace: (id: string) => request<void>(`/spaces/${id}`, 'DELETE'),

  listGoals: () => request<Goal[]>('/goals'),
  createGoal: (input: GoalInput) => request<Goal>('/goals', 'POST', input),
  updateGoal: (id: string, input: GoalInput) => request<Goal>(`/goals/${id}`, 'PUT', input),
  deleteGoal: (id: string) => request<void>(`/goals/${id}`, 'DELETE'),

  // Diretório de projetos: aba (escopo) e busca por nome.
  listProjects: (scope: ProjectScope = 'All', search = '') =>
    request<ProjectSummary[]>(`/projects?${new URLSearchParams({ scope, ...(search.trim() && { search: search.trim() }) })}`),
  getProject: (id: string) => request<Project>(`/projects/${id}`),
  createProject: (input: ProjectInput) => request<Project>('/projects', 'POST', input),
  updateProject: (id: string, input: ProjectInput) => request<Project>(`/projects/${id}`, 'PUT', input),
  changeProjectStatus: (id: string, status: ProjectStatus) => request<Project>(`/projects/${id}/status`, 'PATCH', { status }),
  archiveProject: (id: string) => request<Project>(`/projects/${id}/archive`, 'POST'),
  restoreProject: (id: string) => request<Project>(`/projects/${id}/restore`, 'POST'),
  deleteProject: (id: string) => request<void>(`/projects/${id}`, 'DELETE'),
  exportProjectCsv: (id: string) => download(`/projects/${id}/export`),

  listAttachments: (taskId: string) => request<Attachment[]>(`/tasks/${taskId}/attachments`),
  uploadAttachment: (taskId: string, file: File) => upload<Attachment>(`/tasks/${taskId}/attachments`, file),
  downloadAttachment: (id: string) => download(`/attachments/${id}`),
  attachmentBlob: async (id: string) => (await fetchFile(`/attachments/${id}`)).blob(),
  deleteAttachment: (id: string) => request<void>(`/attachments/${id}`, 'DELETE'),

  setTaskStar: (id: string, starred: boolean) => request<Task>(`/tasks/${id}/star`, starred ? 'PUT' : 'DELETE'),
  markTaskViewed: (id: string) => request<void>(`/tasks/${id}/view`, 'POST'),

  listTaskTypes: (projectId: string) => request<TaskType[]>(`/projects/${projectId}/task-types`),
  createTaskType: (projectId: string, input: { name: string; color: string }) =>
    request<TaskType>(`/projects/${projectId}/task-types`, 'POST', input),
  updateTaskType: (id: string, input: { name: string; color: string }) => request<TaskType>(`/task-types/${id}`, 'PUT', input),
  deleteTaskType: (id: string) => request<void>(`/task-types/${id}`, 'DELETE'),
  changeTaskParent: (id: string, parentId: string | null) => request<Task>(`/tasks/${id}/parent`, 'PATCH', { parentId }),
  changeTaskType: (id: string, typeId: string | null) => request<Task>(`/tasks/${id}/type`, 'PATCH', { typeId }),

  listTasks: (projectId: string) => request<Task[]>(`/projects/${projectId}/tasks`),
  createTask: (projectId: string, input: TaskInput) => request<Task>(`/projects/${projectId}/tasks`, 'POST', input),
  taskHistory: (id: string) => request<TaskEvent[]>(`/tasks/${id}/history`),
  listComments: (taskId: string) => request<TaskComment[]>(`/tasks/${taskId}/comments`),
  addComment: (taskId: string, body: string) => request<TaskComment>(`/tasks/${taskId}/comments`, 'POST', { body }),
  deleteComment: (id: string) => request<void>(`/comments/${id}`, 'DELETE'),
  updateTask: (id: string, input: TaskInput) => request<Task>(`/tasks/${id}`, 'PUT', input),
  changeTaskStatus: (id: string, status: TaskStatus) => request<Task>(`/tasks/${id}/status`, 'PATCH', { status }),
  assignTask: (id: string, userId: string | null) => request<Task>(`/tasks/${id}/assignee`, 'PATCH', { userId }),
  deleteTask: (id: string) => request<void>(`/tasks/${id}`, 'DELETE'),
}
