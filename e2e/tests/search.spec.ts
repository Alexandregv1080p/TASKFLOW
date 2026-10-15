import { expect, test } from '@playwright/test'
import { apiPost, registerUser, signIn } from './helpers'

test('busca global com Ctrl+K encontra a tarefa e abre pelo teclado', async ({ page, request }) => {
  const user = await registerUser(request, 'Busca Teste')
  const project = await apiPost(request, user, '/projects', { name: 'Portal do Cliente' })
  await apiPost(request, user, `/projects/${project.id}/tasks`, { title: 'Ajustar login', priority: 'Low' })
  await apiPost(request, user, `/projects/${project.id}/tasks`, { title: 'Página de faturas', priority: 'High' })
  await signIn(page, user)

  await page.goto('/')
  await expect(page.getByRole('heading', { level: 2, name: 'Para você' })).toBeVisible()
  await page.keyboard.press('Control+K')

  const input = page.getByRole('combobox', { name: 'Buscar tarefas, projetos e espaços' })
  await expect(input).toBeFocused()

  // Pela chave da tarefa (sigla do projeto + número).
  await input.fill('PDC-2')
  await expect(page.getByRole('option')).toHaveCount(1)
  await expect(page.getByRole('option')).toContainText('Página de faturas')

  // Pelo nome: tarefa e projeto; a seta escolhe e Enter abre.
  await input.fill('portal')
  await expect(page.getByRole('option', { name: /Portal do Cliente/ })).toBeVisible()
  await input.fill('ajust')
  await expect(page.getByRole('option', { name: /Ajustar login/ })).toHaveAttribute('aria-selected', 'true')
  await input.press('Enter')

  await expect(page).toHaveURL(new RegExp(`/projects/${project.id}/tasks/`))
  await expect(page.getByRole('dialog', { name: /PDC-1 Ajustar login/ })).toBeVisible()
})
