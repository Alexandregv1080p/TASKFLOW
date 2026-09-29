# TaskFlow

Plataforma web de gerenciamento de projetos e tarefas (ASP.NET Core 8 + PostgreSQL + React).

A especificação completa está em [TASKFLOW.md](TASKFLOW.md).

## Estrutura

```text
backend/
├── TaskFlow.sln
├── TaskFlow.Api              # Controllers, middleware, configuração HTTP
├── TaskFlow.Application      # Casos de uso, DTOs, interfaces
├── TaskFlow.Domain           # Entidades e regras de negócio
└── TaskFlow.Infrastructure   # EF Core, PostgreSQL
```

## Rodando localmente

Pré-requisitos: .NET 8 SDK e Docker.

```bash
docker compose up -d postgres
dotnet build backend/TaskFlow.sln
dotnet run --project backend/TaskFlow.Api
```

PostgreSQL: `localhost:5436` (db/usuário/senha: `taskflow`, apenas para desenvolvimento).
