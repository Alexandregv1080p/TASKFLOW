import { FileIcon, PaperclipIcon, TrashIcon } from '@phosphor-icons/react'
import { useEffect, useRef, useState, type ChangeEvent, type DragEvent } from 'react'
import { useAsync } from '../hooks/useAsync'
import { api } from '../services/api'
import type { Attachment, UserSummary } from '../types'
import { formatDateTime } from '../utils/format'

// Mesma lista e mesmo limite da API (TaskAttachment); aqui só para avisar antes de enviar.
const ACCEPT = '.png,.jpg,.jpeg,.gif,.webp,.pdf,.txt,.log,.md,.csv,.json,.zip,.docx,.xlsx,.pptx'
const MAX_BYTES = 10 * 1024 * 1024

interface Props {
  projectId: string
  taskId: string
  // Muda a cada alteração (inclusive de outras pessoas, em tempo real): recarrega a lista.
  version: number
  users: UserSummary[]
  currentUserId: string | undefined
  isProjectOwner: boolean
  readOnly: boolean
  // Anexou ou excluiu: o modal sobe a versão, o que recarrega esta lista, a cota e o histórico.
  onChange: () => void
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`
  return `${new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 }).format(bytes / 1024 / 1024)} MB`
}

// Anexos da tarefa: prints de bug, documentos, logs. Também dá para soltar o arquivo em cima da seção.
export function Attachments({ projectId, taskId, version, users, currentUserId, isProjectOwner, readOnly, onChange }: Props) {
  const { data, error, reload } = useAsync(() => api.listAttachments(taskId), [taskId, version])
  // Espaço do projeto (cota somando os anexos de todas as tarefas); recarrega junto com a lista.
  const { data: storage, reload: reloadStorage } = useAsync(() => api.projectStorage(projectId), [projectId, version, data?.length])
  const [uploading, setUploading] = useState(false)
  const [dragOver, setDragOver] = useState(false)
  const [actionError, setActionError] = useState<string>()
  const inputRef = useRef<HTMLInputElement>(null)

  async function upload(files: FileList | null) {
    if (!files?.length) return
    setActionError(undefined)

    for (const file of Array.from(files)) {
      if (file.size > MAX_BYTES) {
        setActionError(`"${file.name}" passa de 10 MB.`)
        continue
      }
      setUploading(true)
      try {
        await api.uploadAttachment(taskId, file)
        onChange()
      } catch (err) {
        setActionError(err instanceof Error ? err.message : 'Erro ao anexar.')
      } finally {
        setUploading(false)
      }
    }
    reloadStorage()
  }

  async function remove(attachment: Attachment) {
    if (!window.confirm(`Excluir o anexo "${attachment.fileName}"?`)) return
    setActionError(undefined)
    try {
      await api.deleteAttachment(attachment.id)
      onChange()
    } catch (err) {
      setActionError(err instanceof Error ? err.message : 'Erro ao excluir.')
    }
  }

  function download(attachment: Attachment) {
    setActionError(undefined)
    api.downloadAttachment(attachment.id).catch((err) => setActionError(err instanceof Error ? err.message : 'Erro ao baixar.'))
  }

  const dropHandlers = readOnly
    ? {}
    : {
        onDragOver: (e: DragEvent) => {
          if (!e.dataTransfer.types.includes('Files')) return
          e.preventDefault()
          setDragOver(true)
        },
        onDragLeave: () => setDragOver(false),
        onDrop: (e: DragEvent) => {
          e.preventDefault()
          setDragOver(false)
          upload(e.dataTransfer.files)
        },
      }

  return (
    <div className={dragOver ? 'attachments attachments--over' : 'attachments'} {...dropHandlers}>
      {error && (
        <p className="activity__error">
          Não foi possível carregar os anexos.{' '}
          <button className="link-btn" onClick={reload}>
            Tentar novamente
          </button>
        </p>
      )}

      {data && data.length > 0 && (
        <ul className="attachments__list">
          {data.map((a) => {
            const uploader = users.find((u) => u.id === a.uploadedById)?.name ?? 'Alguém'
            const canDelete = !readOnly && (a.uploadedById === currentUserId || isProjectOwner)
            return (
              <li key={a.id} className="attachments__item">
                {a.contentType.startsWith('image/') && a.sizeBytes <= THUMB_MAX_BYTES ? <AuthImage id={a.id} /> : <FileIcon size={28} className="attachments__icon" aria-hidden="true" />}
                <div className="attachments__text">
                  <button className="link-btn attachments__name" onClick={() => download(a)} title="Baixar">
                    {a.fileName}
                  </button>
                  <span className="attachments__meta">
                    {formatSize(a.sizeBytes)} · {uploader} · {formatDateTime(a.createdAt)}
                  </span>
                </div>
                {canDelete && (
                  <button
                    className="icon-btn icon-btn--danger"
                    onClick={() => remove(a)}
                    aria-label={`Excluir o anexo ${a.fileName}`}
                    title="Excluir anexo"
                  >
                    <TrashIcon size={15} />
                  </button>
                )}
              </li>
            )
          })}
        </ul>
      )}

      {data && data.length === 0 && readOnly && <p className="empty">Nenhum anexo.</p>}

      {!readOnly && (
        <>
          <input
            ref={inputRef}
            type="file"
            accept={ACCEPT}
            multiple
            hidden
            aria-label="Escolher arquivos para anexar"
            onChange={(e: ChangeEvent<HTMLInputElement>) => {
              upload(e.target.files)
              e.target.value = ''
            }}
          />
          <button className="link-btn attachments__add" onClick={() => inputRef.current?.click()} disabled={uploading}>
            <PaperclipIcon size={14} weight="bold" aria-hidden="true" />
            {uploading ? 'Enviando…' : 'Anexar arquivo'}
          </button>
          <span className="field__hint">Ou solte o arquivo aqui. Até 10 MB: imagens, PDF, texto, CSV, ZIP e documentos do Office.</span>
        </>
      )}

      {storage && (
        <p className={storage.usedBytes / storage.quotaBytes >= 0.8 ? 'attachments__quota attachments__quota--high' : 'attachments__quota'}>
          Espaço do projeto: {formatSize(storage.usedBytes)} de {formatSize(storage.quotaBytes)}
          {storage.usedBytes / storage.quotaBytes >= 0.8 && ' (quase cheio: exclua anexos antigos para liberar)'}
        </p>
      )}

      {actionError && (
        <p className="field__error" role="alert">
          {actionError}
        </p>
      )}
    </div>
  )
}

// Miniatura de imagem anexada: o arquivo só sai da API com o token, então vem como blob e vira URL local.
// Baixa o arquivo inteiro, por isso só para imagens pequenas; as maiores ficam com o ícone.
// ponytail: sem miniatura gerada no servidor; gerar no upload se imagens grandes forem comuns.
const THUMB_MAX_BYTES = 1024 * 1024

function AuthImage({ id }: { id: string }) {
  const [url, setUrl] = useState<string>()

  useEffect(() => {
    let objectUrl: string | undefined
    let cancelled = false
    api
      .attachmentBlob(id)
      .then((blob) => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
      })
      .catch(() => {})

    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [id])

  return url ? <img src={url} alt="" className="attachments__thumb" /> : <span className="attachments__thumb attachments__thumb--loading" />
}
