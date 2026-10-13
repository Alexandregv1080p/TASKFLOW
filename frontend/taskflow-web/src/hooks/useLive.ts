import { HubConnectionBuilder, HubConnectionState, LogLevel, type HubConnection } from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import { session } from '../services/api'

const HUB_URL = `${import.meta.env.VITE_API_URL ?? ''}/hubs/board`

// Uma conexão SignalR por aba, compartilhada entre o sino de notificações e o quadro aberto.
// Abre com o primeiro uso e fecha quando o último componente sai (ex.: logout).
interface Shared {
  connection: HubConnection
  started: Promise<void>
  refs: number
  // Avisados quando a conexão cai (false) ou volta (true).
  status: Set<(connected: boolean) => void>
}

let shared: Shared | undefined

function acquire(): Shared {
  if (!shared) {
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => session.token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()
    const status: Shared['status'] = new Set()
    const emit = (connected: boolean) => status.forEach((listener) => listener(connected))

    connection.onreconnecting(() => emit(false))
    connection.onclose(() => emit(false))
    connection.onreconnected(() => emit(true))

    // ponytail: se a primeira conexão falhar não há nova tentativa até recarregar a página
    // (a reconexão automática só cobre quedas depois de conectar). Sem tempo real, tudo segue funcionando.
    shared = { connection, status, refs: 0, started: connection.start() }
  }

  shared.refs++
  return shared
}

function release(current: Shared) {
  if (--current.refs > 0) return

  current.connection.stop()
  if (shared === current) shared = undefined
}

// Chama onEvent a cada aviso do servidor e também quando a conexão volta, porque avisos podem ter se perdido.
// O aviso não traz dados: quem recebe recarrega pela API. Com projectId, entra no grupo do projeto enquanto
// o componente estiver montado. Devolve se o tempo real está ativo.
// ponytail: quem fez a alteração também recebe o aviso e recarrega (uma leitura a mais); para evitar,
// enviar o id da conexão nas requisições e excluí-la do envio no servidor.
export function useLive(event: 'BoardChanged' | 'Notified', onEvent: () => void, projectId?: string) {
  const [live, setLive] = useState(false)
  const handler = useRef(onEvent)

  useEffect(() => {
    handler.current = onEvent
  })

  useEffect(() => {
    const current = acquire()
    const { connection } = current
    let active = true

    const onMessage = (payload: { projectId?: string }) => {
      if (!projectId || payload.projectId === projectId) handler.current()
    }

    const join = (reload: boolean) =>
      (projectId ? connection.invoke('JoinProject', projectId) : Promise.resolve())
        .then(() => {
          if (!active) return
          setLive(true)
          if (reload) handler.current()
        })
        .catch(() => {})

    // Ao reconectar, os grupos se perderam: entra de novo e recarrega.
    const onStatus = (connected: boolean) => {
      if (connected) join(true)
      else setLive(false)
    }

    connection.on(event, onMessage)
    current.status.add(onStatus)
    current.started.then(
      () => {
        if (active) join(false)
      },
      () => {},
    )

    return () => {
      active = false
      setLive(false)
      connection.off(event, onMessage)
      current.status.delete(onStatus)
      if (projectId && connection.state === HubConnectionState.Connected) connection.invoke('LeaveProject', projectId).catch(() => {})
      release(current)
    }
  }, [event, projectId])

  return live
}
