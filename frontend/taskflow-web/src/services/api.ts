import type { Dashboard, Project, ProjectInput, ProjectSummary, Task, TaskInput, TaskStatus } from '../types'

// Único ponto de comunicação com a API: base URL, headers e tratamento de erros ficam aqui.
// Vazio = mesma origem (proxy do Vite em dev). O token JWT entra aqui na Fase 7.
const BASE_URL = `${import.meta.env.VITE_API_URL ?? ''}/api`

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  let response: Response

  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'Não foi possível conectar à API.')
  }

  if (!response.ok) {
    // A API responde erros como { status, title, message, timestamp }.
    const error = await response.json().catch(() => null)
    throw new ApiError(response.status, error?.message ?? `Erro ${response.status}`)
  }

  return response.status === 204 ? (undefined as T) : response.json()
}

export const api = {
  getDashboard: () => request<Dashboard>('/dashboard'),

  listProjects: () => request<ProjectSummary[]>('/projects'),
  getProject: (id: string) => request<Project>(`/projects/${id}`),
  createProject: (input: ProjectInput) => request<Project>('/projects', 'POST', input),
  updateProject: (id: string, input: ProjectInput) => request<Project>(`/projects/${id}`, 'PUT', input),
  deleteProject: (id: string) => request<void>(`/projects/${id}`, 'DELETE'),

  listTasks: (projectId: string) => request<Task[]>(`/projects/${projectId}/tasks`),
  createTask: (projectId: string, input: TaskInput) => request<Task>(`/projects/${projectId}/tasks`, 'POST', input),
  changeTaskStatus: (id: string, status: TaskStatus) => request<Task>(`/tasks/${id}/status`, 'PATCH', { status }),
  deleteTask: (id: string) => request<void>(`/tasks/${id}`, 'DELETE'),
}
