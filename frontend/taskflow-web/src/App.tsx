import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { AuthProvider, RequireAuth } from './auth/AuthProvider'
import { TaskDetail } from './components/TaskDetail'
import { MainLayout } from './layouts/MainLayout'
import { AuthPage } from './pages/AuthPage'
import { Dashboard } from './pages/Dashboard'
import { ProjectDetails } from './pages/ProjectDetails'
import { Projects } from './pages/Projects'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<AuthPage mode="login" />} />
          <Route path="/register" element={<AuthPage mode="register" />} />

          <Route
            element={
              <RequireAuth>
                <MainLayout />
              </RequireAuth>
            }
          >
            <Route index element={<Dashboard />} />
            <Route path="projects" element={<Projects />} />
            {/* O detalhe da tarefa abre em modal sobre o quadro, com URL própria (compartilhável). */}
            <Route path="projects/:id" element={<ProjectDetails />}>
              <Route path="tasks/:taskId" element={<TaskDetail />} />
            </Route>
            <Route
              path="*"
              element={
                <p className="empty">
                  Página não encontrada. <Link to="/">Voltar ao início</Link>
                </p>
              }
            />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
