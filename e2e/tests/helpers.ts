import { expect, type APIRequestContext, type Page } from '@playwright/test'

export const PASSWORD = 'e2e-senha-segura-123'

export interface TestUser {
  id: string
  name: string
  email: string
  token: string
}

export const uniqueEmail = (prefix: string) => `${prefix}.${Date.now()}.${Math.random().toString(36).slice(2, 8)}@taskflow.test`

// Preparação pela API: deixa a tela só para o que o teste quer verificar.
export async function registerUser(request: APIRequestContext, name: string): Promise<TestUser> {
  const email = uniqueEmail(name.split(' ')[0].toLowerCase())
  const response = await request.post('/api/auth/register', { data: { name, email, password: PASSWORD } })
  expect(response.ok()).toBeTruthy()
  const body = await response.json()

  return { id: body.user.id, name, email, token: body.accessToken }
}

export async function apiPost<T = { id: string }>(request: APIRequestContext, user: TestUser, path: string, data: unknown): Promise<T> {
  const response = await request.post(`/api${path}`, { data, headers: { Authorization: `Bearer ${user.token}` } })
  expect(response.ok(), `POST ${path} -> ${response.status()}`).toBeTruthy()

  return response.json()
}

// Entra como o usuário sem passar pela tela de login (o app lê o token do localStorage).
export async function signIn(page: Page, user: TestUser) {
  await page.addInitScript((token) => localStorage.setItem('taskflow.token', token), user.token)
}

export const column = (page: Page, name: 'A fazer' | 'Em andamento' | 'Concluído') =>
  page.getByRole('region', { name, exact: true })
