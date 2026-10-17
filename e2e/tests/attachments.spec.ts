import { readFileSync } from 'node:fs'
import { expect, test } from '@playwright/test'
import { apiPost, registerUser, signIn } from './helpers'

// PNG de 1x1 pixel.
const PNG = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==', 'base64')

test('anexar imagem e arquivo grande, baixar e excluir', async ({ page, request }) => {
  const user = await registerUser(request, 'Dona Anexos')
  const project = await apiPost(request, user, '/projects', { name: 'Loja' })
  const task = await apiPost(request, user, `/projects/${project.id}/tasks`, { title: 'Erro no checkout', priority: 'High' })
  await signIn(page, user)

  await page.goto(`/projects/${project.id}/tasks/${task.id}`)
  const detail = page.getByRole('dialog', { name: /LOJ-1 Erro no checkout/ })

  // Imagem: aparece com miniatura (o arquivo só sai da API com o token) e entra na atividade.
  await detail.getByLabel('Escolher arquivos para anexar').setInputFiles({ name: 'print.png', mimeType: 'image/png', buffer: PNG })
  await expect(detail.getByRole('button', { name: 'print.png', exact: true })).toBeVisible()
  await expect(detail.locator('img.attachments__thumb')).toHaveJSProperty('naturalWidth', 1)
  await expect(detail.getByText('anexou')).toBeVisible()

  // 2 MB passa pelo nginx (o padrão dele seria 1 MB).
  await detail.getByLabel('Escolher arquivos para anexar').setInputFiles({
    name: 'contrato.pdf',
    mimeType: 'application/pdf',
    buffer: Buffer.alloc(2 * 1024 * 1024, 1),
  })
  await expect(detail.getByRole('button', { name: 'contrato.pdf', exact: true })).toBeVisible()
  await expect(detail.getByText('2 MB ·')).toBeVisible()
  await expect(detail.getByText('Espaço do projeto: 2 MB de 100 MB')).toBeVisible()

  // Tipo não aceito: recusado pela API com mensagem.
  await detail.getByLabel('Escolher arquivos para anexar').setInputFiles({ name: 'virus.exe', mimeType: 'application/octet-stream', buffer: Buffer.from('MZ') })
  await expect(detail.getByRole('alert')).toContainText('not allowed')

  // Baixar devolve o mesmo conteúdo, com o nome original.
  const downloading = page.waitForEvent('download')
  await detail.getByRole('button', { name: 'print.png', exact: true }).click()
  const file = await downloading
  expect(file.suggestedFilename()).toBe('print.png')
  expect(readFileSync(await file.path()).equals(PNG)).toBe(true)

  page.once('dialog', (d) => d.accept())
  await detail.getByRole('button', { name: 'Excluir o anexo contrato.pdf' }).click()
  await expect(detail.getByRole('button', { name: 'contrato.pdf', exact: true })).toHaveCount(0)
  await expect(detail.getByText('removeu o anexo')).toBeVisible()
})
