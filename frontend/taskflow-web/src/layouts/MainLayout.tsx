import { FolderSimpleIcon, KanbanIcon, SquaresFourIcon, UserCircleIcon } from '@phosphor-icons/react'
import { NavLink, Outlet } from 'react-router-dom'

export function MainLayout() {
  return (
    <div className="layout">
      <header className="topbar">
        <NavLink to="/" className="brand">
          <span className="brand__logo" aria-hidden="true">
            <KanbanIcon size={18} weight="bold" />
          </span>
          TaskFlow
        </NavLink>
        {/* Usuário demo até a autenticação (Fase 7). */}
        <span className="topbar__user">
          <UserCircleIcon size={20} aria-hidden="true" />
          Usuário Demo
        </span>
      </header>

      <nav className="sidebar" aria-label="Navegação principal">
        <NavLink to="/" end className="sidebar__link">
          <SquaresFourIcon size={18} aria-hidden="true" />
          Dashboard
        </NavLink>
        <NavLink to="/projects" className="sidebar__link">
          <FolderSimpleIcon size={18} aria-hidden="true" />
          Projetos
        </NavLink>
      </nav>

      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
