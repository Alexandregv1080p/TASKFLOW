import { NavLink, Outlet } from 'react-router-dom'

export function MainLayout() {
  return (
    <div className="layout">
      <header className="topbar">
        <NavLink to="/" className="brand">
          <span className="brand__logo" aria-hidden="true">✓</span>
          TaskFlow
        </NavLink>
        {/* Usuário demo até a autenticação (Fase 7). */}
        <span className="topbar__user">Usuário Demo</span>
      </header>

      <nav className="sidebar" aria-label="Navegação principal">
        <NavLink to="/" end className="sidebar__link">
          Dashboard
        </NavLink>
        <NavLink to="/projects" className="sidebar__link">
          Projetos
        </NavLink>
      </nav>

      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
