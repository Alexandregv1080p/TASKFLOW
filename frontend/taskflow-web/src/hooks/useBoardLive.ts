import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useEffect, useRef, useState } from 'react'
import { session } from '../services/api'

const HUB_URL = `${import.meta.env.VITE_API_URL ?? ''}/hubs/board`

// Tempo real do quadro: entra no grupo do projeto e chama onChange quando qualquer pessoa altera algo nele.
// O aviso não traz dados; quem recebe recarrega pela API. Devolve se a conexão está ativa.
// ponytail: quem fez a alteração também recebe o aviso e recarrega (uma leitura a mais); para evitar,
// enviar o id da conexão nas requisições e excluí-la do envio no servidor.
export function useBoardLive(projectId: string, onChange: () => void) {
  const [live, setLive] = useState(false)
  const handler = useRef(onChange)

  useEffect(() => {
    handler.current = onChange
  })

  useEffect(() => {
    let active = true
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => session.token ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    const join = () => connection.invoke('JoinProject', projectId).then(() => active && setLive(true))

    connection.on('BoardChanged', () => handler.current())
    connection.onreconnecting(() => setLive(false))
    connection.onclose(() => setLive(false))
    // Ao reconectar o grupo se perdeu, e avisos também podem ter se perdido: entra de novo e recarrega.
    connection.onreconnected(() => {
      join().catch(() => {})
      handler.current()
    })

    // Sem tempo real o quadro continua funcionando; só não atualiza sozinho.
    connection.start().then(join).catch(() => {})

    return () => {
      active = false
      setLive(false)
      connection.stop()
    }
  }, [projectId])

  return live
}
