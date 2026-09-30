import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { MainLayout } from './layouts/MainLayout'
import { Dashboard } from './pages/Dashboard'
import { ProjectDetails } from './pages/ProjectDetails'
import { Projects } from './pages/Projects'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<MainLayout />}>
          <Route index element={<Dashboard />} />
          <Route path="projects" element={<Projects />} />
          <Route path="projects/:id" element={<ProjectDetails />} />
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
  )
}
