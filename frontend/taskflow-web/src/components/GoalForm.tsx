import { useState, type FormEvent } from 'react'
import type { Goal, GoalInput, ProjectStatus, ProjectSummary } from '../types'
import { PROJECT_STATUSES, PROJECT_STATUS_LABEL } from '../utils/format'
import { ProjectAvatar } from './ProjectAvatar'

interface Props {
  goal?: Goal
  // Projetos que podem ser ligados à meta (os ativos a que o usuário tem acesso).
  projects: ProjectSummary[]
  submitLabel: string
  onSubmit: (input: GoalInput) => Promise<void>
  onCancel: () => void
}

export function GoalForm({ goal, projects, submitLabel, onSubmit, onCancel }: Props) {
  const [name, setName] = useState(goal?.name ?? '')
  const [description, setDescription] = useState(goal?.description ?? '')
  const [targetDate, setTargetDate] = useState(goal?.targetDate?.slice(0, 10) ?? '')
  const [status, setStatus] = useState<ProjectStatus>(goal?.status ?? 'OnTrack')
  const [projectIds, setProjectIds] = useState(() => new Set(goal?.projects.map((p) => p.id)))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  function toggleProject(id: string, checked: boolean) {
    setProjectIds((current) => {
      const next = new Set(current)
      if (checked) next.add(id)
      else next.delete(id)
      return next
    })
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)

    try {
      await onSubmit({
        name,
        description: description.trim() || null,
        targetDate: targetDate || null,
        status,
        projectIds: [...projectIds],
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
      setSaving(false)
    }
  }

  return (
    <form className="card goal-form" onSubmit={handleSubmit} onKeyDown={(e) => e.key === 'Escape' && onCancel()}>
      <label className="field">
        <span className="field__label">Nome da meta</span>
        <input
          autoFocus
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          maxLength={100}
          placeholder="Ex.: Lançar a nova área do cliente"
        />
      </label>

      <label className="field">
        <span className="field__label">Descrição</span>
        <textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={1000} rows={2} />
      </label>

      <div className="goal-form__row">
        <label className="field">
          <span className="field__label">Data alvo</span>
          <input type="date" value={targetDate} onChange={(e) => setTargetDate(e.target.value)} />
        </label>

        <label className="field">
          <span className="field__label">Situação</span>
          <select value={status} onChange={(e) => setStatus(e.target.value as ProjectStatus)}>
            {PROJECT_STATUSES.map((s) => (
              <option key={s} value={s}>
                {PROJECT_STATUS_LABEL[s]}
              </option>
            ))}
          </select>
        </label>
      </div>

      <fieldset className="goal-form__projects">
        <legend className="field__label">Projetos que contribuem para a meta</legend>
        <span className="field__hint">O progresso da meta é a soma das tarefas concluídas nesses projetos.</span>

        {projects.length === 0 ? (
          <p className="field__hint">Você ainda não tem projetos. A meta pode ser criada sem eles e ligada depois.</p>
        ) : (
          <div className="check-list">
            {projects.map((project) => (
              <label key={project.id} className="check-list__item">
                <input
                  type="checkbox"
                  checked={projectIds.has(project.id)}
                  onChange={(e) => toggleProject(project.id, e.target.checked)}
                />
                <ProjectAvatar name={project.name} size="sm" />
                <span className="check-list__text">{project.name}</span>
              </label>
            ))}
          </div>
        )}
      </fieldset>

      {error && (
        <p className="field__error" role="alert">
          {error}
        </p>
      )}

      <div className="project-form__actions">
        <button type="button" className="btn btn--ghost" onClick={onCancel}>
          Cancelar
        </button>
        <button className="btn btn--primary" disabled={saving}>
          {saving ? 'Salvando…' : submitLabel}
        </button>
      </div>
    </form>
  )
}
