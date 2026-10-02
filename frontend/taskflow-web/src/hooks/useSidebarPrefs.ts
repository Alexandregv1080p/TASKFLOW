import { useCallback, useState } from 'react'

// Seções da barra lateral que o usuário pode mostrar ou esconder. "Para você" não entra: é fixa.
export const SIDEBAR_SECTIONS = [
  { key: 'recent', label: 'Recentes', description: 'Atalho para as tarefas que você abriu por último.' },
  { key: 'starred', label: 'Com estrela', description: 'Atalho para as tarefas que você marcou com estrela.' },
  { key: 'projects', label: 'Projetos', description: 'Diretório de projetos e os projetos mais recentes.' },
  { key: 'goals', label: 'Metas', description: 'Seus objetivos e o progresso de cada um.' },
  { key: 'dashboards', label: 'Dashboards', description: 'Painéis com os números e as tarefas atribuídas a você.' },
] as const

export type SidebarSection = (typeof SIDEBAR_SECTIONS)[number]['key']

export type SidebarPrefs = Record<SidebarSection, boolean>

const DEFAULTS = Object.fromEntries(SIDEBAR_SECTIONS.map((s) => [s.key, true])) as SidebarPrefs

// ponytail: preferência de interface guardada no navegador, por usuário. Não acompanha a pessoa em outro
// aparelho; mover para a API (coluna no usuário) se isso virar necessidade.
const storageKey = (userId: string) => `taskflow.sidebar.${userId}`

function read(userId: string): SidebarPrefs {
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey(userId)) ?? '{}') as Partial<SidebarPrefs>

    // Mescla com o padrão: seção nova, criada depois de a pessoa salvar, aparece ligada.
    return Object.fromEntries(
      SIDEBAR_SECTIONS.map((s) => [s.key, typeof saved[s.key] === 'boolean' ? saved[s.key] : true]),
    ) as SidebarPrefs
  } catch {
    return DEFAULTS
  }
}

function write(userId: string, prefs: SidebarPrefs) {
  try {
    localStorage.setItem(storageKey(userId), JSON.stringify(prefs))
  } catch {
    // Sem storage (modo privado etc.): a escolha vale só até recarregar a página.
  }
}

export function useSidebarPrefs(userId: string) {
  const [prefs, setPrefs] = useState(() => read(userId))

  const setSection = useCallback(
    (section: SidebarSection, visible: boolean) =>
      setPrefs((current) => {
        const next = { ...current, [section]: visible }
        write(userId, next)
        return next
      }),
    [userId],
  )

  const reset = useCallback(() => {
    write(userId, DEFAULTS)
    setPrefs(DEFAULTS)
  }, [userId])

  return { prefs, setSection, reset }
}
