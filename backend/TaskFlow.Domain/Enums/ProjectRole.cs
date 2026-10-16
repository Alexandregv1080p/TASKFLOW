namespace TaskFlow.Domain.Enums;

// Papel de uma pessoa no projeto. O dono é Project.OwnerId; membros e visualizadores ficam em ProjectMember.
public enum ProjectRole
{
    Owner,

    // Trabalha nas tarefas: cria, edita, move, comenta, pode ser responsável.
    Member,

    // Só lê: vê quadro, tarefas, comentários e histórico; pode marcar estrela e exportar.
    Viewer
}
