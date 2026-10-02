import { XIcon } from '@phosphor-icons/react'
import { useEffect, useRef } from 'react'
import { SIDEBAR_SECTIONS, type SidebarPrefs, type SidebarSection } from '../hooks/useSidebarPrefs'

interface Props {
  prefs: SidebarPrefs
  onChange: (section: SidebarSection, visible: boolean) => void
  onReset: () => void
  onClose: () => void
}

// Cada interruptor vale na hora: a barra lateral muda atrás do diálogo enquanto a pessoa escolhe.
export function SidebarCustomizeDialog({ prefs, onChange, onReset, onClose }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  const allVisible = SIDEBAR_SECTIONS.every((s) => prefs[s.key])

  return (
    <dialog
      ref={dialogRef}
      className="modal"
      aria-label="Personalizar barra lateral"
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="modal__surface">
        <header className="modal__header">
          <h2>Personalizar barra lateral</h2>
          <button className="tool-btn" onClick={onClose} aria-label="Fechar" title="Fechar">
            <XIcon size={17} />
          </button>
        </header>

        <p className="modal__intro">Escolha as seções que aparecem no menu. As páginas continuam acessíveis mesmo com a seção escondida.</p>

        <ul className="toggle-list">
          <li className="toggle-list__item">
            <span className="toggle-list__text">
              <span className="toggle-list__label">Para você</span>
              <span className="toggle-list__description">Página inicial. Fica sempre visível.</span>
            </span>
            <input type="checkbox" role="switch" className="switch" checked disabled aria-label="Para você (sempre visível)" readOnly />
          </li>

          {SIDEBAR_SECTIONS.map((section) => (
            <li key={section.key} className="toggle-list__item">
              <label className="toggle-list__text" htmlFor={`sidebar-${section.key}`}>
                <span className="toggle-list__label">{section.label}</span>
                <span className="toggle-list__description">{section.description}</span>
              </label>
              <input
                id={`sidebar-${section.key}`}
                type="checkbox"
                role="switch"
                className="switch"
                checked={prefs[section.key]}
                onChange={(e) => onChange(section.key, e.target.checked)}
              />
            </li>
          ))}
        </ul>

        <footer className="modal__footer">
          <button className="btn btn--ghost" onClick={onReset} disabled={allVisible}>
            Mostrar todas
          </button>
          <button className="btn btn--primary" onClick={onClose}>
            Concluir
          </button>
        </footer>
      </div>
    </dialog>
  )
}
