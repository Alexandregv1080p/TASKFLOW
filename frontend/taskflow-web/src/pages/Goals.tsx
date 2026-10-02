import { CalendarBlankIcon, PencilSimpleIcon, PlusIcon, TargetIcon, TrashIcon, WarningIcon } from '@phosphor-icons/react'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ErrorMessage, Loading } from '../components/Feedback'
import { GoalForm } from '../components/GoalForm'
import { ProjectAvatar } from '../components/ProjectAvatar'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { Goal, GoalInput } from '../types'
import { PROJECT_STATUS_LABEL, formatDate } from '../utils/format'

// Meta vencida: passou da data alvo sem estar concluída.
function isLate(goal: Goal): boolean {
  if (!goal.targetDate || goal.status === 'Completed') return false
  return goal.targetDate.slice(0, 10) < new Date().toLocaleDateString('sv')
}

export function Goals() {
  const { data, loading, error, reload, setData } = useAsync(() => Promise.all([api.listGoals(), api.listProjects('All')]), [])
  // null = nenhum formulário; 'new' = criando; id = editando aquela meta.
  const [editing, setEditing] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string>()

  if (loading && !data) return <Loading layout="grid" />
  if (error && !data) return <ErrorMessage message={error} onRetry={reload} />
  if (!data) return null

  const [goals, projects] = data

  async function create(input: GoalInput) {
    const goal = await api.createGoal(input)
    setData(([g, p]) => [[goal, ...g], p])
    setEditing(null)
  }

  async function update(id: string, input: GoalInput) {
    const goal = await api.updateGoal(id, input)
    setData(([g, p]) => [g.map((x) => (x.id === id ? goal : x)), p])
    setEditing(null)
  }

  async function remove(goal: Goal) {
    if (!window.confirm(`Excluir a meta "${goal.name}"? Os projetos ligados a ela não são alterados.`)) return
    setActionError(undefined)
    try {
      await api.deleteGoal(goal.id)
      setData(([g, p]) => [g.filter((x) => x.id !== goal.id), p])
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao excluir.')
    }
  }

  return (
    <>
      <header className="page-header">
        <div className="page-header__text">
          <h1>Metas</h1>
          <p className="page-header__description">
            Objetivos seus, com data alvo e situação. Ligue projetos a uma meta para acompanhar o progresso pelas tarefas concluídas.
          </p>
        </div>
        {editing !== 'new' && (
          <button className="btn btn--primary" onClick={() => setEditing('new')}>
            <PlusIcon size={16} weight="bold" aria-hidden="true" />
            Criar meta
          </button>
        )}
      </header>

      {actionError && <ErrorMessage message={actionError} />}

      {editing === 'new' && <GoalForm projects={projects} submitLabel="Criar meta" onSubmit={create} onCancel={() => setEditing(null)} />}

      {goals.length === 0 && editing !== 'new' ? (
        <div className="empty-state">
          <TargetIcon size={28} aria-hidden="true" />
          <p>Você ainda não tem metas. Crie a primeira para acompanhar um objetivo que envolve um ou mais projetos.</p>
          <button className="btn btn--ghost" onClick={() => setEditing('new')}>
            Criar a primeira meta
          </button>
        </div>
      ) : (
        <ul className="goal-list">
          {goals.map((goal) =>
            editing === goal.id ? (
              <li key={goal.id}>
                <GoalForm
                  goal={goal}
                  projects={projects}
                  submitLabel="Salvar"
                  onSubmit={(input) => update(goal.id, input)}
                  onCancel={() => setEditing(null)}
                />
              </li>
            ) : (
              <li key={goal.id} className="card goal-card">
                <div className="goal-card__header">
                  <h2>{goal.name}</h2>
                  <span className={`status-pill status-pill--${goal.status.toLowerCase()}`}>{PROJECT_STATUS_LABEL[goal.status]}</span>

                  <div className="goal-card__actions">
                    <button className="icon-btn" onClick={() => setEditing(goal.id)} aria-label={`Editar meta ${goal.name}`} title="Editar">
                      <PencilSimpleIcon size={16} />
                    </button>
                    <button className="icon-btn icon-btn--danger" onClick={() => remove(goal)} aria-label={`Excluir meta ${goal.name}`} title="Excluir">
                      <TrashIcon size={16} />
                    </button>
                  </div>
                </div>

                {goal.description && <p className="goal-card__description">{goal.description}</p>}

                <div className="goal-card__progress">
                  <div
                    className="progress"
                    role="progressbar"
                    aria-label="Progresso da meta"
                    aria-valuenow={goal.doneCount}
                    aria-valuemin={0}
                    aria-valuemax={goal.taskCount}
                  >
                    <div className="progress__bar" style={{ transform: `scaleX(${goal.taskCount ? goal.doneCount / goal.taskCount : 0})` }} />
                  </div>
                  <span className="goal-card__count">
                    {goal.projects.length === 0
                      ? 'Sem projetos ligados'
                      : `${goal.doneCount} de ${goal.taskCount} ${goal.taskCount === 1 ? 'tarefa concluída' : 'tarefas concluídas'}`}
                  </span>
                </div>

                <div className="goal-card__footer">
                  {goal.targetDate && (
                    <span className={isLate(goal) ? 'goal-card__date goal-card__date--late' : 'goal-card__date'}>
                      {isLate(goal) ? <WarningIcon size={14} weight="bold" aria-hidden="true" /> : <CalendarBlankIcon size={14} aria-hidden="true" />}
                      {isLate(goal) ? 'Venceu em ' : 'Até '}
                      {formatDate(goal.targetDate)}
                    </span>
                  )}

                  {goal.projects.map((project) => (
                    <Link key={project.id} to={`/projects/${project.id}`} className="chip">
                      <ProjectAvatar name={project.name} size="sm" />
                      {project.name}
                      <span className="chip__count">
                        {project.doneCount}/{project.taskCount}
                      </span>
                    </Link>
                  ))}
                </div>
              </li>
            ),
          )}
        </ul>
      )}
    </>
  )
}
