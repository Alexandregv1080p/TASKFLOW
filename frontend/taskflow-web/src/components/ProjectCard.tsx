import { UsersIcon } from '@phosphor-icons/react'
import { Link } from 'react-router-dom'
import { useAuth } from '../auth/context'
import type { ProjectSummary } from '../types'

export function ProjectCard({ project }: { project: ProjectSummary }) {
  const { user } = useAuth()
  const progress = project.taskCount === 0 ? 0 : project.doneCount / project.taskCount
  const shared = project.ownerId !== user?.id

  return (
    <Link to={`/projects/${project.id}`} className="card project-card">
      <div className="project-card__header">
        <h3>{project.name}</h3>
        {shared && (
          <span className="project-card__shared" title="Compartilhado com você">
            <UsersIcon size={15} aria-hidden="true" />
            <span className="sr-only">Compartilhado com você</span>
          </span>
        )}
      </div>
      <p className="project-card__description clamp">{project.description ?? 'Sem descrição'}</p>

      <div className="project-card__footer">
        <div className="progress" role="progressbar" aria-valuenow={Math.round(progress * 100)} aria-valuemin={0} aria-valuemax={100}>
          <div className="progress__bar" style={{ transform: `scaleX(${progress})` }} />
        </div>
        <span className="project-card__count">
          {project.doneCount}/{project.taskCount}
        </span>
      </div>
    </Link>
  )
}
