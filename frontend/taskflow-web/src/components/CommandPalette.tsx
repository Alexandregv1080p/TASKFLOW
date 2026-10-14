import { CheckSquareIcon, MagnifyingGlassIcon, RocketLaunchIcon, SquaresFourIcon } from '@phosphor-icons/react'
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../services/api'
import type { SearchResult } from '../types'
import { STATUS_LABEL, projectKey } from '../utils/format'

interface Option {
  id: string
  group: string
  to: string
  icon: ReactNode
  label: ReactNode
  meta?: string
}

// Busca global (Ctrl+K): tarefas, projetos e espaços em um só lugar, navegável pelo teclado.
// Segue o padrão combobox + listbox: o foco fica no campo e as setas movem a opção ativa.
export function CommandPalette({ onClose }: { onClose: () => void }) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<SearchResult>()
  const [error, setError] = useState(false)
  const [active, setActive] = useState(0)
  const term = query.trim()
  // Mesma regra da API: 2 letras ou um número de tarefa.
  const searchable = term.length >= 2 || /\d/.test(term)

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  // Espera uma pausa na digitação; resposta de um termo antigo é descartada.
  useEffect(() => {
    if (!searchable) return

    let cancelled = false
    const timer = setTimeout(() => {
      api.search(term).then(
        (data) => {
          if (cancelled) return
          setResult(data)
          setError(false)
          setActive(0)
        },
        () => !cancelled && setError(true),
      )
    }, 150)

    return () => {
      cancelled = true
      clearTimeout(timer)
    }
  }, [term, searchable])

  // "WEB-12": a API acha pelo número; aqui fica só a tarefa do projeto com essa sigla.
  const keyPrefix = /^([a-z]+)-\d+$/i.exec(term)?.[1].toLowerCase()
  const lower = term.toLowerCase()
  const data = searchable ? result : undefined
  const tasks = (data?.tasks ?? []).filter(
    (t) => !keyPrefix || projectKey(t.projectName).toLowerCase() === keyPrefix || t.title.toLowerCase().includes(lower),
  )

  const options: Option[] = [
    ...tasks.map((t) => ({
      id: `search-task-${t.id}`,
      group: 'Tarefas',
      to: `/projects/${t.projectId}/tasks/${t.id}`,
      icon: <CheckSquareIcon size={17} weight="fill" className="palette__task-icon" />,
      label: (
        <>
          <span className="palette__key">
            {projectKey(t.projectName)}-{t.number}
          </span>
          {t.title}
        </>
      ),
      meta: `${t.projectName} · ${STATUS_LABEL[t.status]}`,
    })),
    ...(data?.projects ?? []).map((p) => ({
      id: `search-project-${p.id}`,
      group: 'Projetos',
      to: `/projects/${p.id}`,
      icon: <RocketLaunchIcon size={17} />,
      label: p.name,
      meta: [p.spaceName, p.archived ? 'Arquivado' : null].filter(Boolean).join(' · ') || undefined,
    })),
    ...(data?.spaces ?? []).map((s) => ({
      id: `search-space-${s.id}`,
      group: 'Espaços',
      to: `/spaces/${s.id}`,
      icon: <SquaresFourIcon size={17} />,
      label: s.name,
    })),
  ]

  const current = options[Math.min(active, options.length - 1)]

  useEffect(() => {
    if (current) document.getElementById(current.id)?.scrollIntoView({ block: 'nearest' })
  }, [current])

  function go(option: Option | undefined) {
    if (!option) return
    onClose()
    navigate(option.to)
  }

  function onKeyDown(e: React.KeyboardEvent) {
    if (options.length === 0) return

    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault()
      const step = e.key === 'ArrowDown' ? 1 : -1
      setActive((i) => (Math.min(i, options.length - 1) + step + options.length) % options.length)
    } else if (e.key === 'Enter') {
      e.preventDefault()
      go(current)
    }
  }

  let message: string | null = null
  if (!term) message = 'Busque tarefas pelo título ou pela chave (ex.: WEB-12), projetos e espaços.'
  else if (!searchable) message = 'Digite pelo menos 2 letras.'
  else if (error) message = 'Não foi possível buscar agora. Tente de novo.'
  else if (data && options.length === 0) message = `Nada encontrado para "${term}".`

  return (
    <dialog
      ref={dialogRef}
      className="palette"
      aria-label="Buscar"
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="palette__surface">
        <div className="palette__field">
          <MagnifyingGlassIcon size={18} aria-hidden="true" />
          <input
            role="combobox"
            aria-label="Buscar tarefas, projetos e espaços"
            aria-expanded={options.length > 0}
            aria-controls="search-results"
            aria-autocomplete="list"
            aria-activedescendant={current?.id}
            placeholder="Buscar tarefas, projetos e espaços"
            autoFocus
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={onKeyDown}
          />
          <kbd className="palette__esc">Esc</kbd>
        </div>

        {options.length > 0 && (
          <ul id="search-results" role="listbox" aria-label="Resultados" className="palette__list">
            {options.map((option, i) => (
              <li key={option.id} role="presentation">
                {(i === 0 || options[i - 1].group !== option.group) && (
                  <span className="palette__group" aria-hidden="true">
                    {option.group}
                  </span>
                )}
                <div
                  id={option.id}
                  role="option"
                  aria-selected={option === current}
                  className="palette__option"
                  onClick={() => go(option)}
                  onMouseMove={() => i !== active && setActive(i)}
                >
                  <span className="palette__icon" aria-hidden="true">
                    {option.icon}
                  </span>
                  <span className="palette__label">{option.label}</span>
                  {option.meta && <span className="palette__meta">{option.meta}</span>}
                </div>
              </li>
            ))}
          </ul>
        )}

        {message && (
          <p className="palette__message" role="status">
            {message}
          </p>
        )}

        <footer className="palette__footer" aria-hidden="true">
          <span>
            <kbd>↑</kbd> <kbd>↓</kbd> navegar
          </span>
          <span>
            <kbd>Enter</kbd> abrir
          </span>
        </footer>
      </div>
    </dialog>
  )
}
