import { expect, test } from '@playwright/test'
import { apiPost, registerUser, signIn } from './helpers'

test('dono cadastra webhook, vê o segredo e o envio para rede interna é barrado', async ({ page, request }) => {
  const user = await registerUser(request, 'Dona Integra')
  const project = await apiPost(request, user, '/projects', { name: 'Integra' })
  await signIn(page, user)

  await page.goto(`/projects/${project.id}`)
  await page.getByRole('button', { name: 'Webhooks' }).click()
  const dialog = page.getByRole('dialog', { name: 'Webhooks' })

  // Um endereço interno: na stack (produção) o despachante não entrega em localhost/redes privadas (SSRF).
  await dialog.getByLabel('Novo webhook').fill('http://localhost:9/hook')
  await dialog.getByRole('button', { name: 'Adicionar' }).click()
  await expect(dialog.getByLabel('URL do webhook')).toHaveValue('http://localhost:9/hook')

  await dialog.getByRole('button', { name: 'Mostrar segredo' }).click()
  await expect(dialog.getByText(/^whsec_[0-9a-f]{48}$/)).toBeVisible()

  await dialog.getByRole('button', { name: 'Enviar teste' }).click()
  const deliveries = dialog.getByRole('list', { name: 'Entregas recentes' })
  await expect(deliveries.getByText('ping', { exact: true })).toBeVisible()

  // O despachante roda em segundo plano; a lista se atualiza sozinha enquanto há entrega na fila.
  await expect(deliveries.getByText(/1 tentativa · Blocked: .* private or internal network/)).toBeVisible({ timeout: 20_000 })
})
