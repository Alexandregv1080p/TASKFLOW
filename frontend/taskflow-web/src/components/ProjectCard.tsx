import { Link } from 'react-router-dom'
import type { ProjectSummary } from '../types'

export function ProjectCard({ project }: { project: ProjectSummary }) {
  const progress = project.taskCount === 0 ? 0 : Math.round((project.doneCount / project.taskCount) * 100)

  return (
    <Link to={`/projects/${project.id}`} className="card project-card">
      <h3>{project.name}</h3>
      {project.description && <p className="muted clamp">{project.description}</p>}
      <div className="project-card__stats">
        <span>{project.taskCount} {project.taskCount === 1 ? 'tarefa' : 'tarefas'}</span>
        <span>{project.doneCount} concluídas</span>
      </div>
      <div className="progress" role="progressbar" aria-valuenow={progress} aria-valuemin={0} aria-valuemax={100}>
        <div className="progress__bar" style={{ width: `${progress}%` }} />
      </div>
    </Link>
  )
}
