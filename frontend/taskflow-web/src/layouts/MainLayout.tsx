import { FolderSimpleIcon, KanbanIcon, SquaresFourIcon } from '@phosphor-icons/react'
import { NavLink, Outlet } from 'react-router-dom'

export function MainLayout() {
  return (
    <div className="layout">
      <aside className="sidebar">
        <NavLink to="/" className="brand">
          <span className="brand__logo" aria-hidden="true">
            <KanbanIcon size={18} weight="bold" />
          </span>
          TaskFlow
        </NavLink>

        <nav className="sidebar__nav" aria-label="Navegação principal">
          <NavLink to="/" end className="sidebar__link">
            <SquaresFourIcon size={18} aria-hidden="true" />
            Dashboard
          </NavLink>
          <NavLink to="/projects" className="sidebar__link">
            <FolderSimpleIcon size={18} aria-hidden="true" />
            Projetos
          </NavLink>
        </nav>

        {/* Usuário demo até a autenticação (Fase 7). */}
        <div className="sidebar__user">
          <span className="avatar" aria-hidden="true">
            UD
          </span>
          <span>Usuário Demo</span>
        </div>
      </aside>

      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
