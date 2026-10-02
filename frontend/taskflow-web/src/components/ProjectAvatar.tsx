import { projectKey } from '../utils/format'

const TONES = 6

// Ícone quadrado do projeto, como no Jira: sigla do projeto sobre uma cor estável derivada do nome.
export function ProjectAvatar({ name, size = 'md' }: { name: string; size?: 'sm' | 'md' | 'lg' }) {
  let hash = 0
  for (const char of name) hash = (hash * 31 + char.charCodeAt(0)) % 997

  return (
    <span className={`project-avatar project-avatar--${size} project-avatar--tone${hash % TONES}`} aria-hidden="true">
      {projectKey(name).slice(0, 2)}
    </span>
  )
}
