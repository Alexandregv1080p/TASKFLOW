import { ArrowLeftIcon, RocketLaunchIcon, XIcon } from '@phosphor-icons/react'
import { useEffect, useRef, useState } from 'react'

interface Props {
  // 'empty': aviso de que ainda não há projetos; 'help': só a explicação.
  initialView: 'empty' | 'help'
  onClose: () => void
  onCreate: () => void
}

export function ProjectsHelpDialog({ initialView, onClose, onCreate }: Props) {
  const dialogRef = useRef<HTMLDialogElement>(null)
  const [view, setView] = useState(initialView)

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog && !dialog.open) dialog.showModal()
  }, [])

  return (
    <dialog
      ref={dialogRef}
      className={view === 'help' ? 'modal modal--wide' : 'modal'}
      aria-label={view === 'help' ? 'Como funcionam os projetos' : 'Você ainda não tem projetos'}
      onCancel={(e) => {
        e.preventDefault()
        onClose()
      }}
      onMouseDown={(e) => e.target === e.currentTarget && onClose()}
    >
      <div className="modal__surface">
        {view === 'empty' ? (
          <div className="empty-projects">
            <button className="tool-btn empty-projects__close" onClick={onClose} aria-label="Fechar" title="Fechar">
              <XIcon size={17} />
            </button>

            <span className="empty-projects__icon" aria-hidden="true">
              <RocketLaunchIcon size={30} />
            </span>
            <h2>Você ainda não tem projetos para mostrar</h2>
            <p>
              Projetos reúnem as tarefas de um objetivo em um quadro. Crie o primeiro, ou peça para alguém compartilhar um
              projeto com você.
            </p>

            <div className="empty-projects__actions">
              <button className="btn btn--primary" onClick={onCreate}>
                Criar projeto
              </button>
              <button className="btn btn--ghost" onClick={() => setView('help')}>
                Mais sobre como funcionam os projetos
              </button>
            </div>
          </div>
        ) : (
          <>
            <header className="modal__header">
              <h2>Como funcionam os projetos</h2>
              <button className="tool-btn" onClick={onClose} aria-label="Fechar" title="Fechar">
                <XIcon size={17} />
              </button>
            </header>

            <HowProjectsWork />

            <footer className="modal__footer">
              {initialView === 'empty' && (
                <button className="btn btn--ghost" onClick={() => setView('empty')}>
                  <ArrowLeftIcon size={15} weight="bold" aria-hidden="true" />
                  Voltar
                </button>
              )}
              <button className="btn btn--primary" onClick={onCreate}>
                Criar projeto
              </button>
            </footer>
          </>
        )}
      </div>
    </dialog>
  )
}

// Texto de ajuda: o que é um projeto e o que dá para fazer com ele.
function HowProjectsWork() {
  return (
    <div className="help-text">
      <section>
        <h3>O que é um projeto</h3>
        <p>
          Um projeto é o espaço onde ficam todas as tarefas de um objetivo: um produto, uma entrega, uma área do time. Cada
          projeto tem um quadro com três colunas (A fazer, Em andamento e Concluído) e uma sigla, criada a partir do nome,
          que identifica as tarefas: no projeto "Portal do Cliente" elas são PDC-1, PDC-2 e assim por diante.
        </p>
      </section>

      <section>
        <h3>O que você faz dentro dele</h3>
        <ul>
          <li>
            <strong>Cria tarefas</strong> com título, descrição, prioridade e data limite.
          </li>
          <li>
            <strong>Move as tarefas</strong> entre as colunas, arrastando ou pelos botões do card. Elas andam uma coluna por
            vez: de A fazer para Em andamento, e daí para Concluído.
          </li>
          <li>
            <strong>Define um responsável</strong> para cada tarefa, entre as pessoas do projeto.
          </li>
          <li>
            <strong>Comenta e acompanha o histórico:</strong> toda alteração fica registrada com quem fez e quando.
          </li>
          <li>
            <strong>Marca com estrela</strong> as tarefas que quer encontrar rápido na página Para você.
          </li>
        </ul>
      </section>

      <section>
        <h3>Quem participa</h3>
        <p>
          Quem cria o projeto é o <strong>dono</strong>. Ele pode compartilhar o projeto com outras pessoas pelo e-mail da
          conta delas, que entram como <strong>membros</strong>. Membros trabalham nas tarefas normalmente; editar, arquivar
          ou excluir o projeto e gerenciar pessoas é só com o dono.
        </p>
      </section>

      <section>
        <h3>Status do projeto</h3>
        <p>
          O dono informa como o projeto está indo: <strong>No prazo</strong>, <strong>Em risco</strong>,{' '}
          <strong>Atrasado</strong>, <strong>Pausado</strong> ou <strong>Concluído</strong>. O status e a data da última
          atualização aparecem no diretório, para todos verem a situação sem precisar abrir o quadro.
        </p>
      </section>

      <section>
        <h3>O diretório de projetos</h3>
        <ul>
          <li>
            <strong>Todos os projetos:</strong> os projetos ativos de que você participa, como dono ou membro.
          </li>
          <li>
            <strong>Meus projetos:</strong> só os que você criou.
          </li>
          <li>
            <strong>Arquivados:</strong> projetos encerrados. Um projeto arquivado sai das listas do dia a dia e fica somente
            leitura; nada é apagado, e o dono pode restaurá-lo quando quiser.
          </li>
        </ul>
        <p>Use a busca para encontrar um projeto pelo nome em qualquer uma das abas.</p>
      </section>
    </div>
  )
}
