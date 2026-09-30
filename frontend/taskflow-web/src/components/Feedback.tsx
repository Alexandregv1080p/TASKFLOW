export function Loading() {
  return (
    <div className="feedback" role="status">
      <span className="spinner" aria-hidden="true" />
      Carregando…
    </div>
  )
}

export function ErrorMessage({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="feedback feedback--error" role="alert">
      <span>{message}</span>
      {onRetry && (
        <button className="btn btn--ghost" onClick={onRetry}>
          Tentar novamente
        </button>
      )}
    </div>
  )
}
