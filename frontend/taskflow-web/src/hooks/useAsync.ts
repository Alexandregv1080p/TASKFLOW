import { useCallback, useEffect, useState } from 'react'

interface AsyncState<T> {
  data?: T
  error?: string
  loading: boolean
}

// Carrega dados assíncronos expondo loading/error, com reload e atualização local após mutações.
export function useAsync<T>(load: () => Promise<T>, deps: unknown[]) {
  const [state, setState] = useState<AsyncState<T>>({ loading: true })
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let cancelled = false
    setState((s) => ({ ...s, loading: true, error: undefined }))

    load().then(
      (data) => !cancelled && setState({ data, loading: false }),
      (e: unknown) => !cancelled && setState({ error: e instanceof Error ? e.message : 'Erro inesperado.', loading: false }),
    )

    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, version])

  const reload = useCallback(() => setVersion((v) => v + 1), [])
  const setData = useCallback((update: (data: T) => T) => setState((s) => (s.data ? { ...s, data: update(s.data) } : s)), [])

  return { ...state, reload, setData }
}
