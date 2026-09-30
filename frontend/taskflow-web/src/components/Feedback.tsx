import { WarningCircleIcon } from '@phosphor-icons/react'

type LoadingLayout = 'dashboard' | 'grid' | 'board'

// Skeletons no formato do conteúdo final: a página não "pula" quando os dados chegam.
export function Loading({ layout }: { layout: LoadingLayout }) {
  return (
    <div role="status" aria-label="Carregando">
      <div className="skeleton skeleton--title" />

      {layout === 'dashboard' && (
        <div className="stats">
          {Array.from({ length: 4 }, (_, i) => (
            <div key={i} className="skeleton skeleton--stat" />
          ))}
        </div>
      )}

      {layout === 'board' ? (
        <div className="kanban">
          {Array.from({ length: 3 }, (_, i) => (
            <div key={i} className="skeleton skeleton--column" />
          ))}
        </div>
      ) : (
        <div className="grid section">
          {Array.from({ length: 3 }, (_, i) => (
            <div key={i} className="skeleton skeleton--card" />
          ))}
        </div>
      )}
    </div>
  )
}

export function ErrorMessage({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="feedback feedback--error" role="alert">
      <WarningCircleIcon size={20} weight="bold" aria-hidden="true" />
      <span className="feedback__message">{message}</span>
      {onRetry && (
        <button className="btn btn--ghost" onClick={onRetry}>
          Tentar novamente
        </button>
      )}
    </div>
  )
}
