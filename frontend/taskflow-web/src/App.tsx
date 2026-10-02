import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { AuthProvider, RequireAuth } from './auth/AuthProvider'
import { TaskDetail } from './components/TaskDetail'
import { MainLayout } from './layouts/MainLayout'
import { AuthPage } from './pages/AuthPage'
import { Dashboard } from './pages/Dashboard'
import { ForYou } from './pages/ForYou'
import { Goals } from './pages/Goals'
import { ProjectDetails } from './pages/ProjectDetails'
import { Projects } from './pages/Projects'
import { SpaceDetails } from './pages/SpaceDetails'
import { SpaceTemplates } from './pages/SpaceTemplates'
import { Spaces } from './pages/Spaces'

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
            <Route index element={<ForYou />} />
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="goals" element={<Goals />} />
            <Route path="spaces" element={<Spaces />} />
            <Route path="spaces/new" element={<SpaceTemplates />} />
            <Route path="spaces/:id" element={<SpaceDetails />} />
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
