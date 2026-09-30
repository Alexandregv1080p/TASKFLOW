import { KanbanIcon } from '@phosphor-icons/react'
import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/context'

type Mode = 'login' | 'register'

const COPY: Record<Mode, { title: string; submit: string; switchText: string; switchLink: string; switchTo: string }> = {
  login: {
    title: 'Entrar no TaskFlow',
    submit: 'Entrar',
    switchText: 'Ainda não tem conta?',
    switchLink: 'Criar conta',
    switchTo: '/register',
  },
  register: {
    title: 'Criar sua conta',
    submit: 'Criar conta',
    switchText: 'Já tem conta?',
    switchLink: 'Entrar',
    switchTo: '/login',
  },
}

export function AuthPage({ mode }: { mode: Mode }) {
  const { user, login, register } = useAuth()
  const location = useLocation()
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()
  const copy = COPY[mode]

  if (user) {
    const from = (location.state as { from?: string } | null)?.from ?? '/'
    return <Navigate to={from} replace />
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      if (mode === 'login') await login(email, password)
      else await register(name, email, password)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro inesperado.')
      setSaving(false)
    }
  }

  return (
    <main className="auth">
      <div className="auth__panel">
        <div className="brand">
          <span className="brand__logo" aria-hidden="true">
            <KanbanIcon size={18} weight="bold" />
          </span>
          TaskFlow
        </div>

        <h1>{copy.title}</h1>

        <form className="auth__form" onSubmit={handleSubmit}>
          {mode === 'register' && (
            <label className="field">
              <span className="field__label">Nome</span>
              <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={100} autoComplete="name" />
            </label>
          )}

          <label className="field">
            <span className="field__label">E-mail</span>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              maxLength={254}
              autoComplete="email"
            />
          </label>

          <label className="field">
            <span className="field__label">Senha</span>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={mode === 'register' ? 8 : undefined}
              maxLength={128}
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            />
            {mode === 'register' && <span className="field__hint">Mínimo de 8 caracteres.</span>}
          </label>

          {error && (
            <p className="field__error" role="alert">
              {error}
            </p>
          )}

          <button className="btn btn--primary btn--block" disabled={saving}>
            {saving ? 'Aguarde…' : copy.submit}
          </button>
        </form>

        <p className="auth__switch">
          {copy.switchText} <Link to={copy.switchTo}>{copy.switchLink}</Link>
        </p>
      </div>
    </main>
  )
}
