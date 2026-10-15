import { expect, test } from '@playwright/test'
import { apiPost, column, registerUser, signIn } from './helpers'

// Duas pessoas, cada uma no seu navegador (contextos isolados), no mesmo projeto.
test('quadro em tempo real e notificação de menção entre duas pessoas', async ({ browser, request }) => {
  const ana = await registerUser(request, 'Ana Dona')
  const bia = await registerUser(request, 'Bia Colega')
  const project = await apiPost(request, ana, '/projects', { name: 'Time Ao Vivo' })
  await apiPost(request, ana, `/projects/${project.id}/members`, { email: bia.email })
  const task = await apiPost(request, ana, `/projects/${project.id}/tasks`, { title: 'Revisar contrato', priority: 'Low' })

  const anaContext = await browser.newContext()
  const biaContext = await browser.newContext()
  const anaPage = await anaContext.newPage()
  const biaPage = await biaContext.newPage()
  await signIn(anaPage, ana)
  await signIn(biaPage, bia)

  await anaPage.goto(`/projects/${project.id}`)
  await biaPage.goto(`/projects/${project.id}`)
  await expect(biaPage.getByText('Ao vivo', { exact: true })).toBeVisible()
  await expect(anaPage.getByText('Ao vivo', { exact: true })).toBeVisible()

  // Ana move o card; o quadro da Bia muda sem recarregar.
  const card = column(anaPage, 'A fazer').getByText('Revisar contrato')
  await card.hover()
  await column(anaPage, 'A fazer').getByRole('button', { name: 'Mover para Em andamento' }).click()
  await expect(column(biaPage, 'Em andamento').getByText('Revisar contrato')).toBeVisible()

  // Ana menciona a Bia: o sino da Bia acende, e a notificação leva à tarefa.
  const bell = biaPage.getByRole('button', { name: /^Notificações/ })
  await expect(bell).toHaveAccessibleName('Notificações')

  await anaPage.goto(`/projects/${project.id}/tasks/${task.id}`)
  const composer = anaPage.getByLabel('Novo comentário')
  await composer.fill('Pode validar, @bia')
  await anaPage.getByRole('button', { name: 'Bia Colega' }).click() // sugestão de menção
  await expect(composer).toHaveValue('Pode validar, @Bia Colega ')
  await anaPage.getByRole('button', { name: 'Comentar' }).click()

  await expect(bell).toHaveAccessibleName('Notificações (1 não lida)')
  await bell.click()
  await biaPage.getByRole('button', { name: /Ana Dona mencionou você em/ }).click()
  await expect(biaPage.getByRole('dialog', { name: /Revisar contrato/ })).toBeVisible()
  await expect(biaPage.locator('.activity__comment .mention')).toHaveText('@Bia Colega')
  await expect(bell).toHaveAccessibleName('Notificações')

  await anaContext.close()
  await biaContext.close()
})
