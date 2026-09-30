# TaskFlow

[![CI](https://github.com/Alexandregv1080p/TASKFLOW/actions/workflows/ci.yml/badge.svg)](https://github.com/Alexandregv1080p/TASKFLOW/actions/workflows/ci.yml)

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
frontend/
└── taskflow-web              # React + TypeScript + Vite
tests/
├── TaskFlow.UnitTests        # Regras de domínio
└── TaskFlow.IntegrationTests # Persistência, casos de uso e API contra PostgreSQL
```

## Rodando com Docker (aplicação completa)

Pré-requisito: Docker.

```bash
cp .env.example .env
```

Edite o `.env` e defina `JWT_SECRET` (mínimo 32 caracteres, ex.: `openssl rand -base64 48`). Depois:

```bash
docker compose up -d --build
```

* Frontend: http://localhost:3000 (crie uma conta em "Criar conta")
* Swagger: http://localhost:8080/swagger
* Health: http://localhost:8080/health

Portas ocupadas? Ajuste `WEB_PORT` / `API_PORT` no `.env`.

## Rodando para desenvolvimento

Pré-requisitos: .NET 8 SDK, Node.js 20+ e Docker.

```bash
docker compose up -d postgres
dotnet run --project backend/TaskFlow.Api
```

Em outro terminal:

```bash
cd frontend/taskflow-web
npm install
npm run dev
```

Em Development a API aplica as migrations automaticamente e usa o segredo JWT de desenvolvimento do `appsettings.Development.json`.

* Frontend: http://localhost:5173
* Swagger: http://localhost:5033/swagger
* Health: http://localhost:5033/health
* PostgreSQL: `localhost:5436` (db/usuário/senha: `taskflow`, apenas para desenvolvimento).

## Testes

```bash
dotnet test backend/TaskFlow.sln
```

Os testes de integração usam o banco `taskflow_tests` no mesmo PostgreSQL (recriado a cada execução).

Frontend (checagem de tipos, build e lint):

```bash
cd frontend/taskflow-web
npm run build
npm run lint
```
