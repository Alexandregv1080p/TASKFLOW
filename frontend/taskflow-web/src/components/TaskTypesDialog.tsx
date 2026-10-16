import { TrashIcon, XIcon } from '@phosphor-icons/react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { api } from '../services/api'
import type { Project, TaskType } from '../types'

interface Props {
  project: Project
  types: TaskType[]
  onClose: () => void
  // Depois de criar, editar ou excluir: a página recarrega os tipos.
  onChanged: () => void
}

// Tipos de tarefa do projeto (Bug, Integração...): só o dono abre este diálogo; a API aplica a mesma regra.
export function TaskTypesDialog({ project, types, onClose, onChanged }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [name, setName] = useState('')
  const [color, setColor] = useState('#22A06B')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  async function run(action: () => Promise<unknown>) {
    setError(undefined)
    try {
      await action()
      onChanged()
      return true
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
      return false
    }
  }

  async function add(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    if (await run(() => api.createTaskType(project.id, { name, color }))) setName('')
    setSaving(false)
  }

  function rename(type: TaskType, next: string) {
    const trimmed = next.trim()
    if (trimmed && trimmed !== type.name) run(() => api.updateTaskType(type.id, { name: trimmed, color: type.color }))
  }

  function remove(type: TaskType) {
    if (!window.confirm(`Excluir o tipo "${type.name}"? As tarefas desse tipo ficam sem tipo; nada mais é apagado.`)) return
    run(() => api.deleteTaskType(type.id))
  }

  return (
    <dialog
      ref={dialogRef}
      className="modal"
      aria-label="Tipos de tarefa"
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="modal__surface">
        <header className="modal__header">
          <h2>Tipos de tarefa</h2>
          <button className="tool-btn" onClick={onClose} aria-label="Fechar" title="Fechar">
            <XIcon size={17} />
          </button>
        </header>

        <p className="modal__intro">
          O tipo diz por que o card existe: um bug, uma integração, um desenvolvimento novo. Crie quantos o projeto precisar.
        </p>

        {error && (
          <p className="field__error" role="alert">
            {error}
          </p>
        )}

        <ul className="type-list">
          {types.map((type) => (
            <li key={type.id} className="type-list__item">
              <input
                type="color"
                className="type-list__color"
                value={type.color}
                aria-label={`Cor de ${type.name}`}
                onChange={(e) => run(() => api.updateTaskType(type.id, { name: type.name, color: e.target.value }))}
              />
              <input
                className="type-list__name"
                defaultValue={type.name}
                maxLength={40}
                aria-label={`Nome do tipo ${type.name}`}
                onBlur={(e) => rename(type, e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && e.currentTarget.blur()}
              />
              <button
                className="icon-btn icon-btn--danger"
                onClick={() => remove(type)}
                aria-label={`Excluir o tipo ${type.name}`}
                title="Excluir tipo"
              >
                <TrashIcon size={16} />
              </button>
            </li>
          ))}
          {types.length === 0 && <li className="empty">Nenhum tipo ainda.</li>}
        </ul>

        <form className="member-form" onSubmit={add}>
          <label className="field member-form__field">
            <span className="field__label">Novo tipo</span>
            <input value={name} onChange={(e) => setName(e.target.value)} required maxLength={40} placeholder="Ex.: Melhoria" />
          </label>
          <label className="field">
            <span className="field__label">Cor</span>
            <input type="color" className="type-list__color" value={color} onChange={(e) => setColor(e.target.value)} />
          </label>
          <button className="btn btn--primary" disabled={saving}>
            {saving ? 'Adicionando…' : 'Adicionar'}
          </button>
        </form>
      </div>
    </dialog>
  )
}
