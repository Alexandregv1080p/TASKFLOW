import { FolderSimpleIcon, KanbanIcon, SignOutIcon, SquaresFourIcon } from '@phosphor-icons/react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { Avatar } from '../components/Avatar'

export function MainLayout() {
  const { user, logout } = useAuth()

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

        {user && (
          <div className="sidebar__user">
            <Avatar name={user.name} />
            <span className="sidebar__user-name">{user.name}</span>
            <button className="icon-btn" onClick={logout} aria-label="Sair" title="Sair">
              <SignOutIcon size={17} />
            </button>
          </div>
        )}
      </aside>

      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
