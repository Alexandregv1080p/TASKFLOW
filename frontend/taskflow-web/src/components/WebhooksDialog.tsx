import { ArrowClockwiseIcon, CopyIcon, EyeIcon, EyeSlashIcon, PaperPlaneTiltIcon, TrashIcon, XIcon } from '@phosphor-icons/react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { Project, Webhook, WebhookDelivery, WebhookDeliveryStatus } from '../types'
import { formatDateTime } from '../utils/format'

interface Props {
  project: Project
  onClose: () => void
}

const STATUS_LABEL: Record<WebhookDeliveryStatus, string> = {
  Pending: 'Na fila',
  Succeeded: 'Entregue',
  Failed: 'Falhou',
}

// Webhooks do projeto: só o dono abre (a API aplica a mesma regra, o segredo permite forjar eventos).
export function WebhooksDialog({ project, onClose }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const { data: webhooks, error: loadError, reload } = useAsync(() => api.listWebhooks(project.id), [project.id])
  const [url, setUrl] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string>()

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  async function add(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(undefined)
    try {
      await api.createWebhook(project.id, url.trim())
      setUrl('')
      reload()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <dialog
      ref={dialogRef}
      className="modal modal--wide"
      aria-label="Webhooks"
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="modal__surface">
        <header className="modal__header">
          <h2>Webhooks</h2>
          <button className="tool-btn" onClick={onClose} aria-label="Fechar" title="Fechar">
            <XIcon size={17} />
          </button>
        </header>

        <p className="modal__intro">
          Avise outros sistemas quando algo muda no projeto: tarefa criada, status alterado, comentário, anexo. Cada evento chega
          por POST em JSON, assinado com o segredo no header <code>X-TaskFlow-Signature</code>. Falhou? O TaskFlow tenta de novo
          por cerca de 15 minutos.
        </p>

        {(error || loadError) && (
          <p className="field__error" role="alert">
            {error ?? loadError}
          </p>
        )}

        <ul className="webhook-list">
          {webhooks?.map((webhook) => (
            <WebhookItem key={webhook.id} webhook={webhook} onChanged={reload} onError={setError} />
          ))}
          {webhooks?.length === 0 && <li className="empty">Nenhum webhook ainda.</li>}
        </ul>

        <form className="member-form" onSubmit={add}>
          <label className="field member-form__field">
            <span className="field__label">Novo webhook</span>
            <input
              type="url"
              value={url}
              onChange={(e) => setUrl(e.target.value)}
              required
              maxLength={500}
              placeholder="https://exemplo.com/hooks/taskflow"
            />
          </label>
          <button className="btn btn--primary" disabled={saving}>
            {saving ? 'Adicionando…' : 'Adicionar'}
          </button>
        </form>
      </div>
    </dialog>
  )
}

function WebhookItem({ webhook, onChanged, onError }: { webhook: Webhook; onChanged: () => void; onError: (message: string) => void }) {
  const [showSecret, setShowSecret] = useState(false)
  const [open, setOpen] = useState(false)
  const deliveries = useAsync(() => (open ? api.webhookDeliveries(webhook.id) : Promise.resolve([])), [webhook.id, open])
  const reloadDeliveries = deliveries.reload

  // Entregas acontecem em segundo plano: com o histórico aberto, atualiza sozinho enquanto houver algo na fila.
  const pending = deliveries.data?.some((d) => d.status === 'Pending') ?? false
  useEffect(() => {
    if (!open || !pending) return
    const timer = setInterval(reloadDeliveries, 3000)
    return () => clearInterval(timer)
  }, [open, pending, reloadDeliveries])

  async function run(action: () => Promise<unknown>, after: () => void = onChanged) {
    try {
      await action()
      after()
    } catch (err) {
      onError(err instanceof Error ? err.message : 'Erro ao salvar.')
    }
  }

  function changeUrl(next: string) {
    const trimmed = next.trim()
    if (trimmed && trimmed !== webhook.url) run(() => api.updateWebhook(webhook.id, { url: trimmed, active: webhook.active }))
  }

  function rotate() {
    if (!window.confirm('Gerar um novo segredo? O atual deixa de valer na hora: atualize o sistema que recebe os eventos.')) return
    run(() => api.rotateWebhookSecret(webhook.id))
  }

  function remove() {
    if (!window.confirm(`Excluir o webhook ${webhook.url}? O histórico de entregas vai junto.`)) return
    run(() => api.deleteWebhook(webhook.id))
  }

  function ping() {
    setOpen(true)
    run(() => api.pingWebhook(webhook.id), reloadDeliveries)
  }

  return (
    <li className="webhook">
      <div className="webhook__row">
        <input
          type="checkbox"
          role="switch"
          className="switch"
          checked={webhook.active}
          onChange={(e) => run(() => api.updateWebhook(webhook.id, { url: webhook.url, active: e.target.checked }))}
          aria-label={webhook.active ? 'Desativar webhook' : 'Ativar webhook'}
          title={webhook.active ? 'Ativo' : 'Desativado'}
        />
        <input
          className="webhook__url"
          defaultValue={webhook.url}
          maxLength={500}
          aria-label="URL do webhook"
          onBlur={(e) => changeUrl(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && e.currentTarget.blur()}
        />
        <button className="icon-btn icon-btn--danger" onClick={remove} aria-label="Excluir webhook" title="Excluir webhook">
          <TrashIcon size={16} />
        </button>
      </div>

      <div className="webhook__row webhook__secret">
        <span className="field__label">Segredo</span>
        <code>{showSecret ? webhook.secret : `whsec_${'•'.repeat(16)}`}</code>
        <button
          className="icon-btn"
          onClick={() => setShowSecret((v) => !v)}
          aria-label={showSecret ? 'Esconder segredo' : 'Mostrar segredo'}
          title={showSecret ? 'Esconder' : 'Mostrar'}
        >
          {showSecret ? <EyeSlashIcon size={16} /> : <EyeIcon size={16} />}
        </button>
        <button
          className="icon-btn"
          onClick={() => navigator.clipboard?.writeText(webhook.secret)}
          aria-label="Copiar segredo"
          title="Copiar"
        >
          <CopyIcon size={16} />
        </button>
        <button className="link-btn" onClick={rotate}>
          Trocar segredo
        </button>
      </div>

      <div className="webhook__row">
        <button className="btn btn--ghost" onClick={ping} disabled={!webhook.active}>
          <PaperPlaneTiltIcon size={16} aria-hidden="true" />
          Enviar teste
        </button>
        <button className="link-btn" onClick={() => setOpen((v) => !v)} aria-expanded={open}>
          {open ? 'Esconder entregas' : 'Ver entregas'}
        </button>
        {open && (
          <button className="icon-btn" onClick={reloadDeliveries} aria-label="Atualizar entregas" title="Atualizar">
            <ArrowClockwiseIcon size={16} />
          </button>
        )}
      </div>

      {open && (
        <ul className="deliveries" aria-label="Entregas recentes">
          {deliveries.data?.map((d) => (
            <DeliveryItem key={d.id} delivery={d} onRedeliver={() => run(() => api.redeliverWebhook(d.id), reloadDeliveries)} />
          ))}
          {deliveries.data?.length === 0 && <li className="empty">Nenhuma entrega ainda. Use "Enviar teste".</li>}
        </ul>
      )}
    </li>
  )
}

function DeliveryItem({ delivery: d, onRedeliver }: { delivery: WebhookDelivery; onRedeliver: () => void }) {
  const result = d.lastStatusCode ? `HTTP ${d.lastStatusCode}` : d.lastError
  const retry = d.status === 'Pending' && d.attempts > 0 && d.nextAttemptAt ? ` · nova tentativa ${formatDateTime(d.nextAttemptAt)}` : ''

  return (
    <li className="delivery">
      <details>
        <summary className="delivery__summary">
          <span className={`badge badge--delivery-${d.status.toLowerCase()}`}>{STATUS_LABEL[d.status]}</span>
          <code className="delivery__event">{d.event}</code>
          <span className="delivery__meta">
            {formatDateTime(d.createdAt)}
            {d.attempts > 0 && ` · ${d.attempts} ${d.attempts === 1 ? 'tentativa' : 'tentativas'}`}
            {result && ` · ${result}`}
            {retry}
          </span>
          {d.status !== 'Pending' && (
            <button
              className="link-btn"
              onClick={(e) => {
                e.preventDefault()
                onRedeliver()
              }}
            >
              Reenviar
            </button>
          )}
        </summary>
        <pre className="delivery__payload">{JSON.stringify(JSON.parse(d.payload), null, 2)}</pre>
      </details>
    </li>
  )
}
