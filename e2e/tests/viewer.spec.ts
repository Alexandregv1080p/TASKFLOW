import { readFileSync } from 'node:fs'
import { expect, test } from '@playwright/test'
import { apiPost, column, registerUser, signIn } from './helpers'

test('visualizador acompanha o projeto sem conseguir alterar', async ({ page, request }) => {
  const owner = await registerUser(request, 'Dona Projeto')
  const client = await registerUser(request, 'Cliente Final')
  const project = await apiPost(request, owner, '/projects', { name: 'Portal' })
  await apiPost(request, owner, `/projects/${project.id}/tasks`, { title: 'Nova home', priority: 'High' })
  await apiPost(request, owner, `/projects/${project.id}/members`, { email: client.email, role: 'Viewer' })

  await signIn(page, client)
  await page.goto(`/projects/${project.id}`)

  await expect(page.getByText('Você é visualizador deste projeto')).toBeVisible()
  await expect(column(page, 'A fazer').getByText('Nova home')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Nova tarefa' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: /Mover para/ })).toHaveCount(0)

  // No detalhe: lê tudo, sem campo de comentário e com os campos travados.
  await column(page, 'A fazer').getByText('Nova home').click()
  const detail = page.getByRole('dialog', { name: /POR-1 Nova home/ })
  await expect(detail.getByLabel('Novo comentário')).toHaveCount(0)
  await expect(detail.getByLabel('Responsável')).toBeDisabled()
  await expect(detail.getByRole('button', { name: 'Excluir tarefa' })).toHaveCount(0)
  await detail.getByRole('button', { name: 'Fechar' }).click()

  // Exportar é leitura: o visualizador baixa a planilha das tarefas.
  const downloading = page.waitForEvent('download')
  await page.getByRole('button', { name: 'Exportar CSV' }).click()
  const file = await downloading
  expect(file.suggestedFilename()).toMatch(/^taskflow-por-\d{4}-\d{2}-\d{2}\.csv$/)
  const csv = readFileSync(await file.path(), 'utf-8')
  expect(csv).toContain('Chave;Título;Tipo')
  expect(csv).toContain('POR-1;Nova home;;A fazer;Alta;;Dona Projeto')
})
