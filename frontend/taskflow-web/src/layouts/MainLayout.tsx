import {
  ChartBarIcon,
  ClockIcon,
  GaugeIcon,
  KanbanIcon,
  MagnifyingGlassIcon,
  PlusIcon,
  RocketLaunchIcon,
  SignOutIcon,
  SlidersHorizontalIcon,
  SquaresFourIcon,
  StarIcon,
  TargetIcon,
  UserCircleIcon,
} from '@phosphor-icons/react'
import { useState, type FormEvent } from 'react'
import { Link, NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/context'
import { Avatar } from '../components/Avatar'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { SidebarCustomizeDialog } from '../components/SidebarCustomizeDialog'
import { useAsync } from '../hooks/useAsync'
import { useSidebarPrefs } from '../hooks/useSidebarPrefs'
import { api } from '../services/api'

const RECENT_PROJECTS = 5

export function MainLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [search, setSearch] = useState('')
  const [customizing, setCustomizing] = useState(false)
  const { prefs, setSection, reset } = useSidebarPrefs(user?.id ?? 'anon')

  // ponytail: os projetos recentes do menu são recarregados a cada troca de página; é uma consulta leve
  // e evita um estado global. Trocar por cache compartilhado se o menu ganhar mais dados.
  const { data: projects } = useAsync(() => api.listProjects('All'), [location.pathname])
  const { data: spaces } = useAsync(api.listSpaces, [location.pathname])

  // "Para você", "Recentes" e "Com estrela" são a mesma página com abas diferentes (?filter=),
  // então o item ativo depende da query string, que o NavLink não considera.
  const forYouFilter = location.pathname === '/' ? (new URLSearchParams(location.search).get('filter') ?? '') : null
  const isActive = (item: 'home' | 'Viewed' | 'Starred') =>
    forYouFilter !== null && (item === 'home' ? forYouFilter !== 'Viewed' && forYouFilter !== 'Starred' : forYouFilter === item)
  const forYouClass = (item: 'home' | 'Viewed' | 'Starred') => (isActive(item) ? 'sidebar__link active' : 'sidebar__link')
  const current = (item: 'home' | 'Viewed' | 'Starred') => (isActive(item) ? ('page' as const) : undefined)

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
          <Link to="/" className={forYouClass('home')} aria-current={current('home')}>
            <UserCircleIcon size={18} aria-hidden="true" />
            Para você
          </Link>

          {prefs.recent && (
            <Link to="/?filter=Viewed" className={forYouClass('Viewed')} aria-current={current('Viewed')}>
              <ClockIcon size={18} aria-hidden="true" />
              Recentes
            </Link>
          )}

          {prefs.starred && (
            <Link to="/?filter=Starred" className={forYouClass('Starred')} aria-current={current('Starred')}>
              <StarIcon size={18} aria-hidden="true" />
              Com estrela
            </Link>
          )}

          {prefs.spaces && (
            <>
              <div className="sidebar__row">
                <NavLink to="/spaces" end className="sidebar__link">
                  <SquaresFourIcon size={18} aria-hidden="true" />
                  Espaços
                </NavLink>
                <Link to="/spaces/new" className="icon-btn" aria-label="Criar espaço" title="Criar espaço">
                  <PlusIcon size={15} weight="bold" />
                </Link>
              </div>

              {spaces && spaces.length > 0 && (
                <div className="sidebar__group" aria-label="Espaços recentes">
                  <span className="sidebar__group-title">Recentes</span>
                  {spaces.slice(0, RECENT_PROJECTS).map((space) => (
                    <NavLink key={space.id} to={`/spaces/${space.id}`} className="sidebar__link sidebar__link--sub">
                      <ProjectAvatar name={space.name} size="sm" />
                      <span className="sidebar__link-text">{space.name}</span>
                    </NavLink>
                  ))}
                </div>
              )}
            </>
          )}

          {prefs.projects && (
            <>
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
            </>
          )}

          {prefs.goals && (
            <NavLink to="/goals" className="sidebar__link">
              <TargetIcon size={18} aria-hidden="true" />
              Metas
            </NavLink>
          )}

          {prefs.dashboards && (
            <>
              <span className="sidebar__link sidebar__link--label">
                <ChartBarIcon size={18} aria-hidden="true" />
                Dashboards
              </span>
              <div className="sidebar__group" aria-label="Dashboards">
                <NavLink to="/dashboard" className="sidebar__link sidebar__link--sub">
                  <GaugeIcon size={16} aria-hidden="true" />
                  <span className="sidebar__link-text">Dashboard padrão</span>
                </NavLink>
              </div>
            </>
          )}
        </nav>

        <button className="sidebar__link sidebar__customize" onClick={() => setCustomizing(true)}>
          <SlidersHorizontalIcon size={18} aria-hidden="true" />
          Personalizar barra lateral
        </button>
      </aside>

      <main className="content">
        <Outlet />
      </main>

      {customizing && (
        <SidebarCustomizeDialog prefs={prefs} onChange={setSection} onReset={reset} onClose={() => setCustomizing(false)} />
      )}
    </div>
  )
}
