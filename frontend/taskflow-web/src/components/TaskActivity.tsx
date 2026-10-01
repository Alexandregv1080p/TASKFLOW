import { ArrowRightIcon } from '@phosphor-icons/react'
import type { ReactNode } from 'react'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { TaskEvent, TaskPriority, TaskStatus, UserSummary } from '../types'
import { PRIORITY_LABEL, STATUS_LABEL, formatDate, formatDateTime } from '../utils/format'
import { Avatar } from './Avatar'

interface Props {
  taskId: string
  // Muda a cada alteração salva no modal, para recarregar o histórico.
  version: number
  users: UserSummary[]
}

export function TaskActivity({ taskId, version, users }: Props) {
  const { data, error, reload } = useAsync(() => api.taskHistory(taskId), [taskId, version])
  const nameOf = (id: string | null) => users.find((u) => u.id === id)?.name

  if (error) {
    return (
      <p className="activity__error">
        Não foi possível carregar a atividade.{' '}
        <button className="link-btn" onClick={reload}>
          Tentar novamente
        </button>
      </p>
    )
  }

  if (!data) return <div className="skeleton skeleton--activity" aria-label="Carregando atividade" role="status" />

  return (
    <ol className="activity">
      {data.map((event) => {
        const author = nameOf(event.userId) ?? 'Alguém'

        return (
          <li key={event.id} className="activity__item">
            <Avatar name={author} size="sm" />
            <div>
              <p className="activity__text">
                <strong>{author}</strong> {describe(event, nameOf)}
              </p>
              <time className="activity__time" dateTime={event.createdAt}>
                {formatDateTime(event.createdAt)}
              </time>
            </div>
          </li>
        )
      })}
    </ol>
  )
}

function Change({ from, to }: { from: string; to: string }) {
  return (
    <>
      <span className="activity__value activity__value--old">{from}</span>
      <ArrowRightIcon size={12} weight="bold" className="activity__arrow" aria-hidden="true" />
      <span className="sr-only"> para </span>
      <span className="activity__value">{to}</span>
    </>
  )
}

function describe(event: TaskEvent, nameOf: (id: string | null) => string | undefined): ReactNode {
  const { oldValue, newValue } = event

  switch (event.type) {
    case 'Created':
      return 'criou a tarefa'
    case 'DescriptionChanged':
      return 'atualizou a descrição'
    case 'TitleChanged':
      return (
        <>
          alterou o título: <Change from={oldValue ?? ''} to={newValue ?? ''} />
        </>
      )
    case 'StatusChanged':
      return (
        <>
          alterou o status: <Change from={STATUS_LABEL[oldValue as TaskStatus]} to={STATUS_LABEL[newValue as TaskStatus]} />
        </>
      )
    case 'PriorityChanged':
      return (
        <>
          alterou a prioridade:{' '}
          <Change from={PRIORITY_LABEL[oldValue as TaskPriority]} to={PRIORITY_LABEL[newValue as TaskPriority]} />
        </>
      )
    case 'DueDateChanged':
      if (!newValue) return 'removeu a data limite'
      if (!oldValue) return <>definiu a data limite para <span className="activity__value">{formatDate(newValue)}</span></>
      return (
        <>
          alterou a data limite: <Change from={formatDate(oldValue)} to={formatDate(newValue)} />
        </>
      )
    case 'AssigneeChanged':
      if (!newValue) return 'removeu o responsável'
      return (
        <>
          atribuiu a tarefa a <span className="activity__value">{nameOf(newValue) ?? 'um usuário removido'}</span>
        </>
      )
  }
}
