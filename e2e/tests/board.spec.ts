import { expect, test } from '@playwright/test'
import { column, registerUser, signIn } from './helpers'

test('criar projeto e tarefa, editar e levar a tarefa até concluída', async ({ page, request }) => {
  const user = await registerUser(request, 'Quadro Teste')
  await signIn(page, user)

  // Projeto pelo botão "Criar" da barra superior.
  await page.goto('/')
  await page.getByRole('link', { name: 'Criar', exact: true }).click()
  await page.getByRole('textbox', { name: 'Nome' }).fill('Website Institucional')
  await page.getByRole('button', { name: 'Criar projeto' }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Website Institucional' })).toBeVisible()

  // Tarefa nova abre direto no detalhe.
  await column(page, 'A fazer').getByRole('button', { name: 'Nova tarefa' }).click()
  await page.getByLabel('Título').fill('Escrever testes E2E')
  await page.getByRole('button', { name: 'Adicionar' }).click()

  const detail = page.getByRole('dialog', { name: /WI-1 Escrever testes E2E/ })
  await expect(detail).toBeVisible()

  // Edição no detalhe: prioridade e comentário entram na atividade.
  await detail.getByLabel('Prioridade').selectOption('High')
  await detail.getByLabel('Novo comentário').fill('Começando pelo fluxo principal.')
  await detail.getByRole('button', { name: 'Comentar' }).click()
  await expect(detail.getByText('Começando pelo fluxo principal.')).toBeVisible()
  await expect(detail.getByText('alterou a prioridade')).toBeVisible()
  await detail.getByRole('button', { name: 'Fechar' }).click()
  await expect(detail).toBeHidden()

  // Mover pelo botão do card...
  const todo = column(page, 'A fazer')
  await todo.getByText('Escrever testes E2E').hover()
  await todo.getByRole('button', { name: 'Mover para Em andamento' }).click()
  await expect(column(page, 'Em andamento').getByText('Escrever testes E2E')).toBeVisible()

  // ...e arrastando o card para a próxima coluna.
  await column(page, 'Em andamento').getByText('Escrever testes E2E').dragTo(column(page, 'Concluído'))
  await expect(column(page, 'Concluído').getByText('Escrever testes E2E')).toBeVisible()
  await expect(page.getByText(/1\s*de 1 tarefa concluída/)).toBeVisible()

  // O estado está gravado: sobrevive a recarregar a página.
  await page.reload()
  await expect(column(page, 'Concluído').getByText('Escrever testes E2E')).toBeVisible()
})
