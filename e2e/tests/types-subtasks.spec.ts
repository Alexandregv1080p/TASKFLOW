import { expect, test } from '@playwright/test'
import { apiPost, column, registerUser, signIn } from './helpers'

test('tipos do projeto, responsável na criação e subtarefas', async ({ page, request }) => {
  const owner = await registerUser(request, 'Dona Tipos')
  const project = await apiPost(request, owner, '/projects', { name: 'Checkout' })
  await signIn(page, owner)
  await page.goto(`/projects/${project.id}`)

  // O dono cria um tipo novo além dos padrão (Desenvolvimento, Bug, Integração).
  await page.getByRole('button', { name: 'Tipos' }).click()
  const typesDialog = page.getByRole('dialog', { name: 'Tipos de tarefa' })
  await expect(typesDialog.getByLabel('Nome do tipo Bug')).toBeVisible()
  await typesDialog.getByLabel('Novo tipo').fill('Melhoria')
  await typesDialog.getByRole('button', { name: 'Adicionar' }).click()
  await expect(typesDialog.getByLabel('Nome do tipo Melhoria')).toBeVisible()
  await typesDialog.getByRole('button', { name: 'Fechar' }).click()

  // Card criado já com tipo e responsável ("Atribuir a mim").
  await column(page, 'A fazer').getByRole('button', { name: 'Nova tarefa' }).click()
  await page.getByLabel('Título').fill('Pagamento com Pix')
  await page.getByLabel('Tipo').selectOption({ label: 'Melhoria' })
  await page.getByRole('button', { name: 'Atribuir a mim' }).click()
  await expect(page.getByLabel('Responsável')).toHaveValue(owner.id)
  await page.getByRole('button', { name: 'Adicionar' }).click()

  const detail = page.getByRole('dialog', { name: /CHE-1 Pagamento com Pix/ })
  await expect(detail.getByLabel('Tipo')).toHaveValue(/.+/)
  await expect(detail.getByLabel('Tipo').locator('option:checked')).toHaveText('Melhoria')
  await expect(detail.getByLabel('Responsável').locator('option:checked')).toHaveText('Dona Tipos')

  // Subtarefa criada pelo detalhe, com progresso.
  await detail.getByRole('button', { name: 'Criar subtarefa' }).click()
  await detail.getByLabel('Título da subtarefa').fill('Gerar QR code')
  await detail.getByRole('button', { name: 'Criar', exact: true }).click()
  await expect(detail.getByText('de 1 concluídas')).toBeVisible()

  // A subtarefa abre com a tarefa-pai no caminho e anda até Concluído.
  await detail.getByRole('link', { name: /CHE-2 Gerar QR code/ }).click()
  const sub = page.getByRole('dialog', { name: /CHE-2 Gerar QR code/ })
  await expect(sub.getByRole('link', { name: 'CHE-1' })).toBeVisible()
  await expect(sub.getByRole('heading', { name: 'Subtarefas' })).toHaveCount(0) // um nível só
  await sub.getByLabel('Status').selectOption('InProgress')
  await expect(sub.getByLabel('Status')).toHaveValue('InProgress')
  await sub.getByLabel('Status').selectOption('Done')
  await expect(sub.getByLabel('Status')).toHaveValue('Done')

  await sub.getByRole('link', { name: 'CHE-1' }).click()
  await expect(page.getByRole('dialog', { name: /CHE-1 Pagamento com Pix/ }).getByText(/1\s*de 1 concluídas/)).toBeVisible()
  await page.getByRole('dialog', { name: /CHE-1/ }).getByRole('button', { name: 'Fechar' }).click()

  // No quadro: selo do tipo, progresso das subtarefas e a indicação de subtarefa.
  const card = page.getByRole('article').filter({ hasText: 'Pagamento com Pix' })
  await expect(card.getByText('Melhoria')).toBeVisible()
  await expect(card.getByText(/1\/1/)).toBeVisible()
  await expect(page.getByRole('article').filter({ hasText: 'Gerar QR code' }).getByText('Subtarefa de')).toBeAttached()

  // "Sem tipo" também é uma escolha válida. O formulário continua aberto depois de adicionar (para criar vários seguidos).
  await page.getByLabel('Título').fill('Reunião de alinhamento')
  await page.getByLabel('Tipo').selectOption({ label: 'Sem tipo' })
  await page.getByRole('button', { name: 'Adicionar' }).click()
  const meeting = page.getByRole('dialog', { name: /CHE-3 Reunião de alinhamento/ })
  await expect(meeting.getByLabel('Tipo')).toHaveValue('')

  // Tarefa que já existia vira subtarefa pelo campo "Tarefa-pai".
  await meeting.getByLabel('Tarefa-pai').selectOption({ label: 'CHE-1 Pagamento com Pix' })
  await expect(meeting.getByText('tornou esta tarefa subtarefa de')).toBeVisible()
  await meeting.getByRole('link', { name: 'CHE-1' }).click()

  const parentDetail = page.getByRole('dialog', { name: /CHE-1 Pagamento com Pix/ })
  await expect(parentDetail.getByText(/1\s*de 2 concluídas/)).toBeVisible()
  // Quem tem subtarefas não pode virar subtarefa.
  await expect(parentDetail.getByLabel('Tarefa-pai')).toBeDisabled()
})
