import { ArrowRightIcon, TrashIcon } from '@phosphor-icons/react'
import { useState, type FormEvent, type ReactNode } from 'react'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { TaskComment, TaskEvent, TaskPriority, TaskStatus, UserSummary } from '../types'
import { PRIORITY_LABEL, STATUS_LABEL, formatDate, formatDateTime } from '../utils/format'
import { Avatar } from './Avatar'

interface Props {
  taskId: string
  // Muda a cada alteração salva no modal, para recarregar o histórico.
  version: number
  users: UserSummary[]
  currentUserId: string | undefined
  // Projeto arquivado: sem comentar nem excluir comentário.
  readOnly: boolean
}

type Entry = { kind: 'comment'; at: string; comment: TaskComment } | { kind: 'event'; at: string; event: TaskEvent }

// Comentários e alterações em uma única linha do tempo, do mais recente para o mais antigo.
export function TaskActivity({ taskId, version, users, currentUserId, readOnly }: Props) {
  const { data, error, reload, setData } = useAsync(
    () => Promise.all([api.taskHistory(taskId), api.listComments(taskId)]),
    [taskId, version],
  )
  const [actionError, setActionError] = useState<string>()
  const nameOf = (id: string | null) => users.find((u) => u.id === id)?.name

  async function addComment(body: string) {
    const comment = await api.addComment(taskId, body)
    setData(([events, comments]) => [events, [comment, ...comments]])
  }

  async function deleteComment(comment: TaskComment) {
    if (!window.confirm('Excluir este comentário?')) return
    setActionError(undefined)
    try {
      await api.deleteComment(comment.id)
      setData(([events, comments]) => [events, comments.filter((c) => c.id !== comment.id)])
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao excluir.')
    }
  }

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

  const entries: Entry[] = data
    ? [
        ...data[0].map((event): Entry => ({ kind: 'event', at: event.createdAt, event })),
        ...data[1].map((comment): Entry => ({ kind: 'comment', at: comment.createdAt, comment })),
      ].sort((a, b) => Date.parse(b.at) - Date.parse(a.at))
    : []

  return (
    <>
      {!readOnly && <CommentComposer currentUserName={nameOf(currentUserId ?? null)} onSubmit={addComment} />}

      {actionError && (
        <p className="field__error" role="alert">
          {actionError}
        </p>
      )}

      {!data ? (
        <div className="skeleton skeleton--activity" aria-label="Carregando atividade" role="status" />
      ) : (
        <ol className="activity">
          {entries.map((entry) => {
            const authorId = entry.kind === 'comment' ? entry.comment.authorId : entry.event.userId
            const author = nameOf(authorId) ?? 'Alguém'

            return (
              <li key={entry.kind + (entry.kind === 'comment' ? entry.comment.id : entry.event.id)} className="activity__item">
                <Avatar name={author} size="sm" />
                <div className="activity__content">
                  {entry.kind === 'comment' ? (
                    <>
                      <p className="activity__text">
                        <strong>{author}</strong> comentou
                      </p>
                      <p className="activity__comment">{entry.comment.body}</p>
                    </>
                  ) : (
                    <p className="activity__text">
                      <strong>{author}</strong> {describe(entry.event, nameOf)}
                    </p>
                  )}

                  <div className="activity__meta">
                    <time dateTime={entry.at}>{formatDateTime(entry.at)}</time>
                    {!readOnly && entry.kind === 'comment' && entry.comment.authorId === currentUserId && (
                      <button
                        className="icon-btn icon-btn--danger icon-btn--xs"
                        onClick={() => deleteComment(entry.comment)}
                        aria-label="Excluir comentário"
                        title="Excluir comentário"
                      >
                        <TrashIcon size={13} />
                      </button>
                    )}
                  </div>
                </div>
              </li>
            )
          })}
        </ol>
      )}
    </>
  )
}

function CommentComposer({ currentUserName, onSubmit }: { currentUserName?: string; onSubmit: (body: string) => Promise<void> }) {
  const [body, setBody] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  async function submit(e?: FormEvent) {
    e?.preventDefault()
    if (!body.trim() || saving) return

    setSaving(true)
    setError(undefined)
    try {
      await onSubmit(body)
      setBody('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao comentar.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="comment-composer" onSubmit={submit}>
      <Avatar name={currentUserName ?? '?'} size="sm" />
      <div className="comment-composer__field">
        <textarea
          aria-label="Novo comentário"
          placeholder="Adicionar um comentário…"
          rows={body ? 3 : 1}
          maxLength={2000}
          value={body}
          onChange={(e) => setBody(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) submit()
            // Esc limpa o rascunho em vez de fechar o modal inteiro.
            if (e.key === 'Escape' && body) {
              e.preventDefault()
              setBody('')
            }
          }}
        />

        {error && (
          <p className="field__error" role="alert">
            {error}
          </p>
        )}

        {body.trim() && (
          <div className="comment-composer__actions">
            <button className="btn btn--primary btn--sm" disabled={saving}>
              {saving ? 'Enviando…' : 'Comentar'}
            </button>
            <span className="field__hint">Ctrl + Enter envia</span>
          </div>
        )}
      </div>
    </form>
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
