import { useEffect, useState, type ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { api, session } from '../services/api'
import type { AuthResponse, User } from '../types'
import { AuthContext, useAuth } from './context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  // Só há o que verificar se existe um token salvo.
  const [loading, setLoading] = useState(() => session.token !== null)

  useEffect(() => {
    session.onUnauthorized(() => setUser(null))

    if (!session.token) return

    api
      .me()
      .then(setUser)
      .catch(() => session.clear())
      .finally(() => setLoading(false))
  }, [])

  function start(response: AuthResponse) {
    session.save(response.accessToken)
    setUser(response.user)
  }

  const value = {
    user,
    loading,
    login: async (email: string, password: string) => start(await api.login(email, password)),
    register: async (name: string, email: string, password: string) => start(await api.register(name, email, password)),
    logout: () => {
      session.clear()
      setUser(null)
    },
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// Rotas protegidas: sem usuário, vai para o login e volta para cá depois de entrar.
export function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) return null
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />

  return children
}
