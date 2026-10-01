import type {
  AuthResponse,
  Dashboard,
  Project,
  ProjectInput,
  ProjectSummary,
  Task,
  TaskEvent,
  TaskInput,
  TaskStatus,
  User,
  UserSummary,
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

export const api = {
  register: (name: string, email: string, password: string) =>
    request<AuthResponse>('/auth/register', 'POST', { name, email, password }),
  login: (email: string, password: string) => request<AuthResponse>('/auth/login', 'POST', { email, password }),
  me: () => request<User>('/auth/me'),

  listUsers: () => request<UserSummary[]>('/users'),
  getDashboard: () => request<Dashboard>('/dashboard'),

  listProjects: () => request<ProjectSummary[]>('/projects'),
  getProject: (id: string) => request<Project>(`/projects/${id}`),
  createProject: (input: ProjectInput) => request<Project>('/projects', 'POST', input),
  updateProject: (id: string, input: ProjectInput) => request<Project>(`/projects/${id}`, 'PUT', input),
  deleteProject: (id: string) => request<void>(`/projects/${id}`, 'DELETE'),

  listTasks: (projectId: string) => request<Task[]>(`/projects/${projectId}/tasks`),
  createTask: (projectId: string, input: TaskInput) => request<Task>(`/projects/${projectId}/tasks`, 'POST', input),
  taskHistory: (id: string) => request<TaskEvent[]>(`/tasks/${id}/history`),
  updateTask: (id: string, input: TaskInput) => request<Task>(`/tasks/${id}`, 'PUT', input),
  changeTaskStatus: (id: string, status: TaskStatus) => request<Task>(`/tasks/${id}/status`, 'PATCH', { status }),
  assignTask: (id: string, userId: string | null) => request<Task>(`/tasks/${id}/assignee`, 'PATCH', { userId }),
  deleteTask: (id: string) => request<void>(`/tasks/${id}`, 'DELETE'),
}
