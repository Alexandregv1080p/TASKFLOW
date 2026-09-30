import { Link } from 'react-router-dom'
import type { ProjectSummary } from '../types'

export function ProjectCard({ project }: { project: ProjectSummary }) {
  const progress = project.taskCount === 0 ? 0 : project.doneCount / project.taskCount

  return (
    <Link to={`/projects/${project.id}`} className="card project-card">
      <h3>{project.name}</h3>
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
