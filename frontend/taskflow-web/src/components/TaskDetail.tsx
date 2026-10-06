import {
  CaretDownIcon,
  CheckIcon,
  CheckSquareIcon,
  FolderSimpleIcon,
  LinkSimpleIcon,
  StarIcon,
  TrashIcon,
  XIcon,
} from '@phosphor-icons/react'
import { useEffect, useRef, useState } from 'react'
import { useNavigate, useOutletContext, useParams } from 'react-router-dom'
import { useAuth } from '../auth/context'
import type { ProjectOutletContext } from '../pages/ProjectDetails'
import { api } from '../services/api'
import type { Task, TaskInput, TaskPriority, TaskStatus } from '../types'
import { PRIORITY_LABEL, STATUSES, STATUS_LABEL, formatDateTime } from '../utils/format'
import { Avatar } from './Avatar'
import { PriorityIcon } from './PriorityIcon'
import { TaskActivity } from './TaskActivity'

// Detalhe da tarefa em modal, no layout do Jira: conteúdo à esquerda, status e detalhes à direita.
export function TaskDetail() {
  const { taskId = '' } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const { project, tasks, users, keyPrefix, readOnly, replaceTask, removeTask, liveVersion } = useOutletContext<ProjectOutletContext>()
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [saving, setSaving] = useState(false)
  // Campos travados enquanto salva ou quando o projeto está arquivado.
  const locked = saving || readOnly
  const [error, setError] = useState<string>()
  const [copied, setCopied] = useState(false)
  // Incrementado a cada alteração salva: recarrega a seção Atividade.
  const [version, setVersion] = useState(0)
  const task = tasks.find((t) => t.id === taskId)
  const exists = task !== undefined

  const close = () => navigate(`/projects/${project.id}`)

  // Abrir a tarefa registra a visualização (aba "Visto" da página Para você). Falhar aqui não atrapalha nada.
  useEffect(() => {
    if (exists) api.markTaskViewed(taskId).catch(() => {})
  }, [taskId, exists])

  // <dialog> nativo: foco preso no modal e o resto da página fica inerte.
  // Fechar é sempre navegar de volta ao quadro (a rota desmonta o modal).
  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  async function save(action: () => Promise<Task>) {
    setSaving(true)
    setError(undefined)
    try {
      replaceTask(await action())
      setVersion((v) => v + 1)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
    } finally {
      setSaving(false)
    }
  }

  function update(current: Task, changes: Partial<TaskInput>) {
    const input: TaskInput = {
      title: current.title,
      description: current.description,
      priority: current.priority,
      dueDate: current.dueDate?.slice(0, 10) ?? null,
      ...changes,
    }
    return save(() => api.updateTask(current.id, input))
  }

  // Estrela é pessoal: não entra no histórico da tarefa nem depende do projeto estar ativo.
  async function toggleStar(current: Task) {
    setError(undefined)
    try {
      replaceTask(await api.setTaskStar(current.id, !current.starred))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao marcar a tarefa.')
    }
  }

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(window.location.href)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      setError('Não foi possível copiar o link.')
    }
  }

  async function remove(current: Task) {
    if (!window.confirm(`Excluir a tarefa "${current.title}"?`)) return
    try {
      await api.deleteTask(current.id)
      removeTask(current.id)
      close()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao excluir.')
    }
  }

  const taskKey = task ? `${keyPrefix}-${task.number}` : ''
  const creator = task?.createdById ? users.find((u) => u.id === task.createdById) : undefined

  return (
    <dialog
      ref={dialogRef}
      className="task-detail"
      aria-label={task ? `${taskKey} ${task.title}` : 'Tarefa'}
      onCancel={(e) => {
        // Esc: em vez de só esconder o dialog, sai da rota do modal.
        e.preventDefault()
        close()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && close()}
    >
      <div className="task-detail__surface">
        <header className="task-detail__topbar">
          <nav className="breadcrumb" aria-label="Localização">
            <span className="breadcrumb__item">
              <FolderSimpleIcon size={15} aria-hidden="true" />
              {project.name}
            </span>
            <span className="breadcrumb__sep" aria-hidden="true">
              /
            </span>
            <span className="breadcrumb__item breadcrumb__item--current">
              <CheckSquareIcon size={15} weight="fill" className="breadcrumb__task-icon" aria-hidden="true" />
              {taskKey}
            </span>
          </nav>

          <div className="task-detail__tools">
            {task && (
              <button
                className={task.starred ? 'tool-btn tool-btn--starred' : 'tool-btn'}
                onClick={() => toggleStar(task)}
                aria-pressed={task.starred}
                aria-label={task.starred ? 'Remover estrela' : 'Marcar com estrela'}
                title={task.starred ? 'Remover estrela' : 'Marcar com estrela'}
              >
                <StarIcon size={17} weight={task.starred ? 'fill' : 'regular'} />
              </button>
            )}
            <button className="tool-btn" onClick={copyLink} aria-label="Copiar link" title={copied ? 'Link copiado' : 'Copiar link'}>
              {copied ? <CheckIcon size={17} weight="bold" /> : <LinkSimpleIcon size={17} />}
            </button>
            {task && !readOnly && (
              <button className="tool-btn tool-btn--danger" onClick={() => remove(task)} aria-label="Excluir tarefa" title="Excluir tarefa">
                <TrashIcon size={17} />
              </button>
            )}
            <button className="tool-btn" onClick={close} aria-label="Fechar" title="Fechar">
              <XIcon size={17} />
            </button>
          </div>
        </header>

        {!task ? (
          <p className="task-detail__missing">Esta tarefa não existe mais ou você não tem acesso a ela.</p>
        ) : (
          <div className="task-detail__body">
            <div className="task-detail__main">
              {readOnly ? (
                <h2 className="task-detail__title">{task.title}</h2>
              ) : (
                <EditableTitle value={task.title} onSave={(title) => update(task, { title })} />
              )}

              {error && (
                <p className="field__error" role="alert">
                  {error}
                </p>
              )}

              <section className="task-detail__section">
                <h3>Descrição</h3>
                {readOnly ? (
                  <p className="task-detail__description">{task.description ?? 'Sem descrição.'}</p>
                ) : (
                  <EditableDescription value={task.description} onSave={(description) => update(task, { description })} />
                )}
              </section>

              <section className="task-detail__section">
                <h3>Atividade</h3>
                <TaskActivity taskId={task.id} version={version + liveVersion} users={users} currentUserId={user?.id} readOnly={readOnly} />
              </section>
            </div>

            <aside className="task-detail__side" aria-busy={saving}>
              <StatusSelect
                status={task.status}
                disabled={locked}
                onChange={(status) => save(() => api.changeTaskStatus(task.id, status))}
              />

              <details className="details-panel" open>
                <summary>
                  <CaretDownIcon size={14} weight="bold" className="details-panel__caret" aria-hidden="true" />
                  Detalhes
                </summary>

                <dl className="details-panel__grid">
                  <dt>Responsável</dt>
                  <dd>
                    <div className="details-value">
                      {task.assignedToId ? (
                        <Avatar name={users.find((u) => u.id === task.assignedToId)?.name ?? '?'} size="sm" />
                      ) : (
                        <span className="avatar avatar--sm avatar--empty" aria-hidden="true" />
                      )}
                      <select
                        className="inline-select"
                        aria-label="Responsável"
                        value={task.assignedToId ?? ''}
                        disabled={locked}
                        onChange={(e) => save(() => api.assignTask(task.id, e.target.value || null))}
                      >
                        <option value="">Sem responsável</option>
                        {users.map((u) => (
                          <option key={u.id} value={u.id}>
                            {u.name}
                          </option>
                        ))}
                      </select>
                    </div>
                    {user && !readOnly && task.assignedToId !== user.id && (
                      <button
                        className="link-btn"
                        disabled={locked}
                        onClick={() => save(() => api.assignTask(task.id, user.id))}
                      >
                        Atribuir a mim
                      </button>
                    )}
                  </dd>

                  <dt>Criador</dt>
                  <dd>
                    <div className="details-value">
                      {creator ? <Avatar name={creator.name} size="sm" /> : <span className="avatar avatar--sm avatar--empty" aria-hidden="true" />}
                      <span>{creator?.name ?? 'Desconhecido'}</span>
                    </div>
                  </dd>

                  <dt>Prioridade</dt>
                  <dd>
                    <div className="details-value">
                      <PriorityIcon priority={task.priority} />
                      <select
                        className="inline-select"
                        aria-label="Prioridade"
                        value={task.priority}
                        disabled={locked}
                        onChange={(e) => update(task, { priority: e.target.value as TaskPriority })}
                      >
                        {(['High', 'Medium', 'Low'] as TaskPriority[]).map((p) => (
                          <option key={p} value={p}>
                            {PRIORITY_LABEL[p]}
                          </option>
                        ))}
                      </select>
                    </div>
                  </dd>

                  <dt>Data limite</dt>
                  <dd>
                    <input
                      type="date"
                      className="inline-input"
                      aria-label="Data limite"
                      value={task.dueDate?.slice(0, 10) ?? ''}
                      disabled={locked}
                      onChange={(e) => update(task, { dueDate: e.target.value || null })}
                    />
                  </dd>
                </dl>
              </details>

              <p className="task-detail__created">Criado em {formatDateTime(task.createdAt)}</p>
            </aside>
          </div>
        )}
      </div>
    </dialog>
  )
}

// Status como botão-dropdown do Jira; só oferece as transições que o domínio permite (uma coluna por vez).
function StatusSelect({ status, disabled, onChange }: { status: TaskStatus; disabled: boolean; onChange: (s: TaskStatus) => void }) {
  const index = STATUSES.indexOf(status)
  const options = STATUSES.filter((_, i) => Math.abs(i - index) <= 1)

  return (
    <label className={`status-select status-select--${status.toLowerCase()}`}>
      <span className="sr-only">Status</span>
      <select value={status} disabled={disabled} onChange={(e) => onChange(e.target.value as TaskStatus)}>
        {options.map((s) => (
          <option key={s} value={s}>
            {STATUS_LABEL[s]}
          </option>
        ))}
      </select>
      <CaretDownIcon size={14} weight="bold" aria-hidden="true" />
    </label>
  )
}

function EditableTitle({ value, onSave }: { value: string; onSave: (title: string) => void }) {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(value)

  function commit() {
    setEditing(false)
    const title = draft.trim()
    if (title && title !== value) onSave(title)
  }

  if (!editing) {
    return (
      <h2 className="task-detail__title">
        <button
          className="inline-edit"
          onClick={() => {
            setDraft(value)
            setEditing(true)
          }}
        >
          {value}
        </button>
      </h2>
    )
  }

  return (
    <input
      className="task-detail__title-input"
      aria-label="Título"
      autoFocus
      maxLength={200}
      value={draft}
      onChange={(e) => setDraft(e.target.value)}
      onBlur={commit}
      onKeyDown={(e) => {
        if (e.key === 'Enter') {
          e.preventDefault()
          commit()
        }
        if (e.key === 'Escape') {
          // Cancela a edição sem fechar o modal.
          e.preventDefault()
          setEditing(false)
        }
      }}
    />
  )
}

function EditableDescription({ value, onSave }: { value: string | null; onSave: (description: string | null) => void }) {
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(value ?? '')

  if (!editing) {
    return (
      <button
        className={value ? 'inline-edit inline-edit--block' : 'inline-edit inline-edit--block inline-edit--placeholder'}
        onClick={() => {
          setDraft(value ?? '')
          setEditing(true)
        }}
      >
        {value ?? 'Adicionar uma descrição…'}
      </button>
    )
  }

  return (
    <div className="description-editor">
      <textarea
        aria-label="Descrição"
        autoFocus
        rows={6}
        maxLength={2000}
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Escape') {
            e.preventDefault()
            setEditing(false)
          }
        }}
      />
      <div className="description-editor__actions">
        <button
          className="btn btn--primary btn--sm"
          onClick={() => {
            setEditing(false)
            onSave(draft.trim() || null)
          }}
        >
          Salvar
        </button>
        <button className="btn btn--ghost btn--sm" onClick={() => setEditing(false)}>
          Cancelar
        </button>
      </div>
    </div>
  )
}
