import {
  ChartBarIcon,
  KanbanIcon,
  MagnifyingGlassIcon,
  PlusIcon,
  RocketLaunchIcon,
  SignOutIcon,
  UserCircleIcon,
} from '@phosphor-icons/react'
import { useState, type FormEvent } from 'react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { Avatar } from '../components/Avatar'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'

const RECENT_PROJECTS = 5

export function MainLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [search, setSearch] = useState('')

  // ponytail: os projetos recentes do menu são recarregados a cada troca de página; é uma consulta leve
  // e evita um estado global. Trocar por cache compartilhado se o menu ganhar mais dados.
  const { data: projects } = useAsync(() => api.listProjects('All'), [location.pathname])

  // A busca do topo leva ao diretório de projetos já filtrado.
  function submitSearch(e: FormEvent) {
    e.preventDefault()
    navigate(`/projects?search=${encodeURIComponent(search.trim())}`)
    setSearch('')
  }

  return (
    <div className="layout">
      <header className="topbar">
        <Link to="/" className="brand">
          <span className="brand__logo" aria-hidden="true">
            <KanbanIcon size={18} weight="bold" />
          </span>
          TaskFlow
        </Link>

        <form className="topbar__search" role="search" onSubmit={submitSearch}>
          <MagnifyingGlassIcon size={16} aria-hidden="true" />
          <input
            type="search"
            aria-label="Buscar projetos"
            placeholder="Buscar projetos"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </form>

        <Link to="/projects?new=1" className="btn btn--primary">
          <PlusIcon size={16} weight="bold" aria-hidden="true" />
          Criar
        </Link>

        {user && (
          <div className="topbar__user">
            <Avatar name={user.name} />
            <span className="topbar__user-name">{user.name}</span>
            <button className="icon-btn" onClick={logout} aria-label="Sair" title="Sair">
              <SignOutIcon size={17} />
            </button>
          </div>
        )}
      </header>

      <aside className="sidebar">
        <nav className="sidebar__nav" aria-label="Navegação principal">
          <NavLink to="/" end className="sidebar__link">
            <UserCircleIcon size={18} aria-hidden="true" />
            Para você
          </NavLink>
          <NavLink to="/projects" end className="sidebar__link">
            <RocketLaunchIcon size={18} aria-hidden="true" />
            Projetos
          </NavLink>

          {projects && projects.length > 0 && (
            <div className="sidebar__group" aria-label="Projetos recentes">
              <span className="sidebar__group-title">Recentes</span>
              {projects.slice(0, RECENT_PROJECTS).map((project) => (
                <NavLink key={project.id} to={`/projects/${project.id}`} className="sidebar__link sidebar__link--sub">
                  <ProjectAvatar name={project.name} size="sm" />
                  <span className="sidebar__link-text">{project.name}</span>
                </NavLink>
              ))}
            </div>
          )}

          <NavLink to="/dashboard" className="sidebar__link">
            <ChartBarIcon size={18} aria-hidden="true" />
            Dashboard
          </NavLink>
        </nav>
      </aside>

      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
