import { SignOutIcon, TrashIcon, XIcon } from '@phosphor-icons/react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { api, ApiError } from '../services/api'
import type { Project, ProjectMember, ProjectRole } from '../types'
import { Avatar } from './Avatar'

interface Props {
  project: Project
  members: ProjectMember[]
  currentUserId: string
  onClose: () => void
  // Chamado depois de adicionar/remover alguém, para a página recarregar pessoas e tarefas.
  onChanged: () => void
  // Chamado quando o próprio usuário sai do projeto.
  onLeft: () => void
}

const ROLE_LABEL: Record<ProjectRole, string> = { Owner: 'Dono', Member: 'Membro', Viewer: 'Visualizador' }

export function ProjectMembersDialog({ project, members, currentUserId, onClose, onChanged, onLeft }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<ProjectRole>('Member')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()
  const isOwner = project.ownerId === currentUserId

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  async function add(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)
    try {
      await api.addMember(project.id, email, role)
      setEmail('')
      onChanged()
    } catch (err) {
      const status = err instanceof ApiError ? err.status : 0
      if (status === 404) setError('Nenhuma conta do TaskFlow usa esse e-mail.')
      else if (status === 409) setError('Essa pessoa já está no projeto.')
      else setError(err instanceof Error ? err.message : 'Erro ao adicionar.')
    } finally {
      setSaving(false)
    }
  }

  async function changeRole(member: ProjectMember, next: ProjectRole) {
    if (next === 'Viewer' && !window.confirm(`${member.name} vai virar visualizador: só acompanha, e as tarefas dele ficam sem responsável.`)) return

    setError(undefined)
    try {
      await api.changeMemberRole(project.id, member.id, next)
      onChanged()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao trocar o papel.')
    }
  }

  async function remove(member: ProjectMember) {
    const leaving = member.id === currentUserId
    const question = leaving
      ? `Sair do projeto "${project.name}"? Você perde o acesso a ele.`
      : `Remover ${member.name} do projeto? As tarefas atribuídas a essa pessoa ficam sem responsável.`
    if (!window.confirm(question)) return

    setError(undefined)
    try {
      await api.removeMember(project.id, member.id)
      if (leaving) onLeft()
      else onChanged()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao remover.')
    }
  }

  return (
    <dialog
      ref={dialogRef}
      className="modal"
      aria-label="Pessoas do projeto"
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="modal__surface">
        <header className="modal__header">
          <h2>Pessoas do projeto</h2>
          <button className="tool-btn" onClick={onClose} aria-label="Fechar" title="Fechar">
            <XIcon size={17} />
          </button>
        </header>

        {isOwner && (
          <form className="member-form" onSubmit={add}>
            <label className="field member-form__field">
              <span className="field__label">Adicionar por e-mail</span>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                maxLength={254}
                placeholder="pessoa@empresa.com"
                autoComplete="off"
              />
              <span className="field__hint">A pessoa precisa já ter uma conta no TaskFlow.</span>
            </label>
            <label className="field">
              <span className="field__label">Papel</span>
              <select value={role} onChange={(e) => setRole(e.target.value as ProjectRole)}>
                <option value="Member">Membro</option>
                <option value="Viewer">Visualizador</option>
              </select>
            </label>
            <button className="btn btn--primary" disabled={saving}>
              {saving ? 'Adicionando…' : 'Adicionar'}
            </button>
          </form>
        )}

        {error && (
          <p className="field__error" role="alert">
            {error}
          </p>
        )}

        <ul className="member-list">
          {members.map((member) => (
            <li key={member.id} className="member-list__item">
              <Avatar name={member.name} />
              <span className="member-list__name">
                {member.name}
                {member.id === currentUserId && <span className="member-list__you"> (você)</span>}
              </span>
              {isOwner && !member.isOwner ? (
                <select
                  className="inline-select member-list__role"
                  aria-label={`Papel de ${member.name}`}
                  value={member.role}
                  onChange={(e) => changeRole(member, e.target.value as ProjectRole)}
                >
                  <option value="Member">Membro</option>
                  <option value="Viewer">Visualizador</option>
                </select>
              ) : (
                <span className="member-list__role">{ROLE_LABEL[member.role]}</span>
              )}

              {!member.isOwner && isOwner && (
                <button
                  className="icon-btn icon-btn--danger"
                  onClick={() => remove(member)}
                  aria-label={`Remover ${member.name}`}
                  title="Remover do projeto"
                >
                  <TrashIcon size={16} />
                </button>
              )}
              {!member.isOwner && !isOwner && member.id === currentUserId && (
                <button className="btn btn--ghost btn--sm" onClick={() => remove(member)}>
                  <SignOutIcon size={15} aria-hidden="true" />
                  Sair
                </button>
              )}
            </li>
          ))}
        </ul>
      </div>
    </dialog>
  )
}
