import { BellIcon } from '@phosphor-icons/react'
import { useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAsync } from '../hooks/useAsync'
import { useLive } from '../hooks/useLive'
import { api } from '../services/api'
import type { AppNotification, NotificationType } from '../types'
import { formatDateTime, projectKey } from '../utils/format'
import { Avatar } from './Avatar'

const ACTION: Record<NotificationType, string> = {
  Assigned: 'atribuiu a você',
  Mentioned: 'mencionou você em',
  Commented: 'comentou em',
}

// Sino da barra superior: a lista chega pela API e é recarregada quando o servidor avisa que há notificação nova.
// O painel é um popover nativo: fecha com Esc ou clique fora, sem código para isso.
export function NotificationBell() {
  const { data, error, reload, setData } = useAsync(api.listNotifications, [])
  const panelRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const unread = data?.unreadCount ?? 0

  useLive('Notified', reload)

  function open(notification: AppNotification) {
    panelRef.current?.hidePopover()
    navigate(`/projects/${notification.projectId}/tasks/${notification.taskId}`)

    if (notification.read) return

    setData((current) => ({
      unreadCount: Math.max(current.unreadCount - 1, 0),
      items: current.items.map((n) => (n.id === notification.id ? { ...n, read: true } : n)),
    }))
    // Se falhar, a próxima recarga mostra o estado real.
    api.markNotificationRead(notification.id).catch(reload)
  }

  function markAllRead() {
    setData((current) => ({ unreadCount: 0, items: current.items.map((n) => ({ ...n, read: true })) }))
    api.markAllNotificationsRead().catch(reload)
  }

  return (
    <>
      <button
        className="icon-btn icon-btn--lg bell"
        popoverTarget="notifications"
        aria-label={unread > 0 ? `Notificações (${unread} ${unread === 1 ? 'não lida' : 'não lidas'})` : 'Notificações'}
        title="Notificações"
      >
        <BellIcon size={19} weight={unread > 0 ? 'fill' : 'regular'} />
        {unread > 0 && (
          <span className="bell__count" aria-hidden="true">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      <div id="notifications" popover="auto" ref={panelRef} className="notifications" aria-label="Notificações">
        <header className="notifications__header">
          <h2>Notificações</h2>
          {unread > 0 && (
            <button className="link-btn" onClick={markAllRead}>
              Marcar todas como lidas
            </button>
          )}
        </header>

        {error ? (
          <p className="notifications__empty">
            Não foi possível carregar as notificações.{' '}
            <button className="link-btn" onClick={reload}>
              Tentar novamente
            </button>
          </p>
        ) : !data || data.items.length === 0 ? (
          <p className="notifications__empty">
            {data
              ? 'Nada por aqui ainda. Você recebe um aviso quando alguém atribui uma tarefa a você, menciona você com @ ou comenta em uma tarefa sua.'
              : 'Carregando…'}
          </p>
        ) : (
          <ul className="notifications__list">
            {data.items.map((n) => (
              <li key={n.id}>
                <button className={n.read ? 'notification' : 'notification notification--unread'} onClick={() => open(n)}>
                  <Avatar name={n.actorName} size="sm" />
                  <span className="notification__text">
                    <span>
                      <strong>{n.actorName}</strong> {ACTION[n.type]}{' '}
                      <strong>
                        {projectKey(n.projectName)}-{n.taskNumber}
                      </strong>{' '}
                      {n.taskTitle}
                    </span>
                    <span className="notification__meta">
                      {n.projectName} · <time dateTime={n.createdAt}>{formatDateTime(n.createdAt)}</time>
                    </span>
                  </span>
                  {!n.read && <span className="notification__dot" role="img" aria-label="Não lida" />}
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </>
  )
}
