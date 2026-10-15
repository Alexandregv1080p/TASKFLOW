import { expect, test } from '@playwright/test'
import { PASSWORD, uniqueEmail } from './helpers'

test('cadastro, saída e login pela tela', async ({ page }) => {
  const email = uniqueEmail('cadastro')

  // Sem sessão, qualquer página leva ao login.
  await page.goto('/projects')
  await expect(page).toHaveURL(/\/login/)

  await page.getByRole('link', { name: 'Criar conta' }).click()
  await page.getByLabel('Nome').fill('Ana Teste')
  await page.getByLabel('E-mail').fill(email)
  await page.getByLabel('Senha').fill(PASSWORD)
  await page.getByRole('button', { name: 'Criar conta' }).click()

  await expect(page.getByRole('heading', { level: 2, name: 'Para você' })).toBeVisible()
  await expect(page.getByText('Ana Teste')).toBeVisible()

  await page.getByRole('button', { name: 'Sair' }).click()
  await expect(page).toHaveURL(/\/login/)

  // Senha errada: mensagem de erro, continua no login.
  await page.getByLabel('E-mail').fill(email)
  await page.getByLabel('Senha').fill('senha-errada-123')
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page.getByRole('alert')).toBeVisible()

  await page.getByLabel('Senha').fill(PASSWORD)
  await page.getByRole('button', { name: 'Entrar' }).click()
  await expect(page.getByRole('heading', { level: 2, name: 'Para você' })).toBeVisible()
})
