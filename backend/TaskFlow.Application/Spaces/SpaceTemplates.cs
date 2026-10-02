using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Spaces;

public record TemplateTask(string Title, TaskPriority Priority = TaskPriority.Medium);

public record TemplateProject(string Name, string Description, IReadOnlyList<TemplateTask> Tasks);

// Modelo de espaço: ao criar um espaço a partir dele, os projetos e as tarefas iniciais já vêm prontos.
public record SpaceTemplate(
    string Key,
    string Name,
    string Description,
    string Category,
    // Aparece em "Feitos para você" na galeria.
    bool Recommended,
    IReadOnlyList<TemplateProject> Projects);

// Catálogo fixo, em código: mudar um modelo é mudar este arquivo. Vira tabela (modelos personalizados por
// usuário) só se isso for pedido.
public static class SpaceTemplateCatalog
{
    public const string BlankKey = "blank";

    public static readonly IReadOnlyList<SpaceTemplate> All =
    [
        new(BlankKey, "Espaço em branco", "Comece do zero e crie os projetos do seu jeito.",
            "Geral", Recommended: true, Projects: []),

        new("kanban", "Kanban", "Visualize o trabalho em um quadro com A fazer, Em andamento e Concluído.",
            "Desenvolvimento de software", Recommended: true,
            [
                new("Quadro Kanban", "Fluxo contínuo de trabalho do time.",
                [
                    new("Definir o limite de tarefas em andamento", TaskPriority.High),
                    new("Cadastrar as demandas atuais no quadro"),
                    new("Combinar a reunião semanal de revisão do quadro", TaskPriority.Low)
                ])
            ]),

        new("scrum", "Scrum", "Planeje e execute o trabalho em sprints, a partir de um backlog priorizado.",
            "Desenvolvimento de software", Recommended: true,
            [
                new("Backlog do produto", "Tudo o que ainda pode entrar em uma sprint, em ordem de prioridade.",
                [
                    new("Escrever as histórias de usuário da próxima entrega", TaskPriority.High),
                    new("Estimar e priorizar o backlog com o time"),
                    new("Definir os critérios de pronto", TaskPriority.Low)
                ]),
                new("Sprint atual", "O que o time se comprometeu a entregar nesta sprint.",
                [
                    new("Fazer o planejamento da sprint", TaskPriority.High),
                    new("Agendar a revisão e a retrospectiva")
                ])
            ]),

        new("dev-requests", "Solicitações de desenvolvimento",
            "Receba pedidos de funcionalidades e relatos de bugs em um só lugar, separados do que está em construção.",
            "Desenvolvimento de software", Recommended: false,
            [
                new("Bugs", "Problemas relatados, da triagem à correção.",
                [
                    new("Definir como um bug deve ser relatado (passos, esperado, obtido)", TaskPriority.High),
                    new("Fazer a triagem dos bugs em aberto")
                ]),
                new("Novas funcionalidades", "Pedidos de melhoria e de funcionalidades novas.",
                [
                    new("Levantar os pedidos mais frequentes"),
                    new("Decidir o que entra no próximo ciclo", TaskPriority.High)
                ])
            ]),

        new("product-roadmap", "Roadmap de produto", "Organize as ideias em descoberta e o que já tem data para ser entregue.",
            "Gestão de produto", Recommended: true,
            [
                new("Descoberta", "Problemas e oportunidades ainda em investigação.",
                [
                    new("Entrevistar cinco clientes sobre o problema principal", TaskPriority.High),
                    new("Mapear as oportunidades encontradas")
                ]),
                new("Roadmap", "Entregas planejadas para os próximos trimestres.",
                [
                    new("Definir os objetivos do trimestre", TaskPriority.High),
                    new("Apresentar o roadmap para as áreas envolvidas"),
                    new("Revisar o roadmap no fim do mês", TaskPriority.Low)
                ])
            ]),

        new("marketing-campaign", "Campanha de marketing", "Do planejamento à publicação: uma campanha com conteúdo e canais organizados.",
            "Marketing", Recommended: false,
            [
                new("Planejamento da campanha", "Objetivo, público, orçamento e cronograma.",
                [
                    new("Definir o objetivo e o público da campanha", TaskPriority.High),
                    new("Aprovar o orçamento", TaskPriority.High),
                    new("Montar o cronograma de publicação")
                ]),
                new("Conteúdo", "Peças e textos da campanha.",
                [
                    new("Escrever os textos dos anúncios"),
                    new("Produzir as peças visuais"),
                    new("Revisar tudo antes de publicar", TaskPriority.High)
                ])
            ]),

        new("hr-onboarding", "Integração de novas pessoas", "Um roteiro para receber quem está chegando ao time, do primeiro dia ao primeiro mês.",
            "Recursos humanos", Recommended: false,
            [
                new("Onboarding", "Etapas da integração de uma nova pessoa.",
                [
                    new("Preparar acessos e equipamento antes do primeiro dia", TaskPriority.High),
                    new("Apresentar o time e as ferramentas"),
                    new("Combinar as metas dos primeiros 30 dias"),
                    new("Fazer a conversa de acompanhamento do primeiro mês", TaskPriority.Low)
                ])
            ]),

        new("personal", "Organização pessoal", "Suas tarefas do dia a dia e os planos de mais longo prazo, separados.",
            "Pessoal", Recommended: false,
            [
                new("Tarefas do dia a dia", "O que precisa ser feito nesta semana.",
                [
                    new("Listar as tarefas da semana"),
                    new("Separar o que é urgente do que pode esperar", TaskPriority.Low)
                ]),
                new("Planos", "Objetivos pessoais com prazo mais longo.",
                [
                    new("Escolher um objetivo para os próximos três meses", TaskPriority.High)
                ])
            ])
    ];

    public static SpaceTemplate? Find(string? key) =>
        string.IsNullOrWhiteSpace(key) ? All[0] : All.FirstOrDefault(t => t.Key == key.Trim());
}
