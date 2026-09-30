# TaskFlow — Especificação Completa do Projeto

> Documento-base para implementação do projeto TaskFlow.
>
> Este documento deve ser tratado como a especificação técnica principal do projeto. A implementação deve seguir as decisões e restrições descritas aqui, evitando adicionar tecnologias ou complexidade sem justificativa.

---

# 1. Visão geral

O **TaskFlow** é uma plataforma web de gerenciamento de projetos e tarefas, semelhante conceitualmente a ferramentas como Trello e Asana.

O projeto foi concebido como uma aplicação **cloud-native**, com foco em demonstrar conhecimentos de:

* C#
* .NET / ASP.NET Core
* REST APIs
* Clean Architecture
* Domain-Driven Design em nível pragmático
* Entity Framework Core
* PostgreSQL
* React
* TypeScript
* Docker
* Docker Compose
* Git
* GitLab
* GitLab CI/CD
* Testes automatizados
* Linux
* Cloud
* Observabilidade
* Segurança
* Escalabilidade

O projeto também deverá servir como demonstração prática de decisões de arquitetura e engenharia de software.

A prioridade é construir uma aplicação funcional e bem estruturada antes de adicionar componentes de infraestrutura mais avançados.

---

# 2. Objetivo do projeto

O objetivo principal é desenvolver uma aplicação que permita gerenciar:

* Usuários
* Projetos
* Tarefas
* Responsáveis
* Prioridades
* Status
* Prazos
* Histórico de alterações
* Métricas de produtividade

Além da funcionalidade de negócio, o projeto deve demonstrar uma arquitetura preparada para:

* Containerização
* CI/CD
* Deploy em cloud
* Monitoramento
* Escalabilidade
* Auditoria
* Evolução para Kubernetes

---

# 3. Princípios do projeto

Durante a implementação, seguir estes princípios:

1. Não adicionar tecnologias apenas para aumentar a quantidade de tecnologias.
2. Cada componente deve possuir uma justificativa técnica.
3. Manter separação clara de responsabilidades.
4. Evitar lógica de negócio dentro dos Controllers.
5. Evitar acesso direto ao banco a partir dos Controllers.
6. Manter o domínio independente de infraestrutura.
7. Priorizar código simples e legível.
8. Escrever testes para regras importantes.
9. Utilizar configurações por ambiente.
10. Nunca armazenar secrets diretamente no código.
11. Evitar overengineering.
12. Implementar primeiro o MVP e evoluir incrementalmente.

---

# 4. Stack principal

## Backend

* C#
* .NET 8
* ASP.NET Core Web API
* Entity Framework Core
* PostgreSQL
* Swagger / OpenAPI

## Frontend

* React
* TypeScript
* Vite

## Infraestrutura

* Docker
* Docker Compose
* Linux

## Versionamento

* Git
* GitLab

## CI/CD

* GitLab CI/CD
* GitLab Container Registry

## Testes

* xUnit
* ASP.NET Core Integration Tests

## Evolução futura

* MongoDB
* AWS
* DigitalOcean
* Prometheus
* Grafana
* Kubernetes
* Rancher

---

# 5. Arquitetura geral

A arquitetura deve seguir uma abordagem baseada em **Clean Architecture**, mantendo o domínio isolado de detalhes externos.

```text
                         ┌──────────────────────┐
                         │       React          │
                         │    TypeScript        │
                         └──────────┬───────────┘
                                    │
                                    │ HTTP / JSON
                                    ▼
                         ┌──────────────────────┐
                         │    TaskFlow.Api      │
                         │                      │
                         │ Controllers          │
                         │ Middleware           │
                         │ HTTP Configuration   │
                         └──────────┬───────────┘
                                    │
                                    ▼
                     ┌──────────────────────────────┐
                     │    TaskFlow.Application      │
                     │                              │
                     │ Use Cases                    │
                     │ DTOs                         │
                     │ Services                     │
                     │ Interfaces                   │
                     └──────────────┬───────────────┘
                                    │
                                    ▼
                     ┌──────────────────────────────┐
                     │       TaskFlow.Domain        │
                     │                              │
                     │ Entities                     │
                     │ Value Objects                │
                     │ Enums                        │
                     │ Business Rules               │
                     └──────────────────────────────┘
                                    ▲
                                    │
                     ┌──────────────┴───────────────┐
                     │  TaskFlow.Infrastructure     │
                     │                              │
                     │ Entity Framework Core         │
                     │ PostgreSQL                    │
                     │ Persistence                   │
                     │ Repositories                 │
                     └──────────────────────────────┘
```

---

# 6. Regra de dependências

As dependências devem respeitar:

```text
TaskFlow.Api
    ↓
TaskFlow.Application
    ↓
TaskFlow.Domain

TaskFlow.Infrastructure
    ↓
TaskFlow.Application
    ↓
TaskFlow.Domain
```

O `Domain` não deve depender de:

* Entity Framework
* PostgreSQL
* ASP.NET Core
* React
* Docker
* APIs externas
* qualquer detalhe de infraestrutura

A camada de domínio deve ser independente.

---

# 7. Estrutura de diretórios

A estrutura inicial esperada:

```text
TaskFlow/
│
├── backend/
│   │
│   ├── TaskFlow.sln
│   │
│   ├── TaskFlow.Api/
│   │   ├── Controllers/
│   │   ├── Middlewares/
│   │   ├── Extensions/
│   │   ├── Properties/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   ├── TaskFlow.Application/
│   │   ├── Projects/
│   │   ├── Tasks/
│   │   ├── Users/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   └── Common/
│   │
│   ├── TaskFlow.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Exceptions/
│   │   ├── ValueObjects/
│   │   └── Common/
│   │
│   └── TaskFlow.Infrastructure/
│       ├── Persistence/
│       ├── Configurations/
│       ├── Repositories/
│       ├── Services/
│       └── Migrations/
│
├── frontend/
│   │
│   └── taskflow-web/
│       ├── src/
│       │   ├── components/
│       │   ├── pages/
│       │   ├── layouts/
│       │   ├── services/
│       │   ├── hooks/
│       │   ├── types/
│       │   ├── utils/
│       │   └── App.tsx
│       │
│       ├── package.json
│       ├── tsconfig.json
│       └── vite.config.ts
│
├── tests/
│   │
│   ├── TaskFlow.UnitTests/
│   │
│   └── TaskFlow.IntegrationTests/
│
├── docker/
│   ├── backend/
│   │   └── Dockerfile
│   │
│   └── frontend/
│       └── Dockerfile
│
├── docker-compose.yml
├── .gitlab-ci.yml
├── .gitignore
├── .dockerignore
└── README.md
```

---

# 8. Modelo de domínio

O MVP terá três entidades principais:

```text
User
Project
TaskItem
```

---

# 9. Entidade User

Representa um usuário da plataforma.

Modelo conceitual:

```text
User
├── Id
├── Name
├── Email
├── PasswordHash
└── CreatedAt
```

Propriedades:

* `Id`: Guid
* `Name`: string
* `Email`: string
* `PasswordHash`: string
* `CreatedAt`: DateTime

A senha nunca deve ser armazenada em texto puro.

---

# 10. Entidade Project

Representa um projeto.

Modelo:

```text
Project
├── Id
├── Name
├── Description
├── OwnerId
└── CreatedAt
```

Propriedades:

* `Id`: Guid
* `Name`: string
* `Description`: string?
* `OwnerId`: Guid
* `CreatedAt`: DateTime

Um projeto pertence a um usuário.

Um projeto possui várias tarefas.

---

# 11. Entidade TaskItem

A entidade deve ser chamada `TaskItem`, e não simplesmente `Task`, para evitar confusão com `System.Threading.Tasks.Task`.

Modelo:

```text
TaskItem
├── Id
├── Title
├── Description
├── Status
├── Priority
├── DueDate
├── ProjectId
├── AssignedToId
└── CreatedAt
```

Propriedades:

* `Id`: Guid
* `Title`: string
* `Description`: string?
* `Status`: TaskItemStatus
* `Priority`: TaskPriority
* `DueDate`: DateTime?
* `ProjectId`: Guid
* `AssignedToId`: Guid?
* `CreatedAt`: DateTime

---

# 12. Status das tarefas

Criar o enum:

```text
TaskItemStatus
```

> Decisão: o nome `TaskStatus` colide com `System.Threading.Tasks.TaskStatus` (incluído nos usings implícitos do .NET), gerando erro de ambiguidade. Pelo mesmo motivo de `TaskItem`, o enum se chama `TaskItemStatus`.

Valores:

```text
Todo
InProgress
Done
```

Fluxo inicial:

```text
TODO
  │
  ▼
IN_PROGRESS
  │
  ▼
DONE
```

Regra implementada no MVP: a tarefa anda **um passo por vez**, para frente ou para trás (`Todo ↔ InProgress ↔ Done`). Não é permitido pular etapas (ex.: `Todo → Done`).

O sistema poderá posteriormente permitir outras transições, mas o MVP deve permanecer simples.

---

# 13. Prioridade das tarefas

Criar o enum:

```text
TaskPriority
```

Valores:

```text
Low
Medium
High
```

---

# 14. Relacionamentos

Modelo:

```text
User
 │
 ├──────────── owns ──────────────► Project
 │                                      │
 │                                      │ contains
 │                                      ▼
 └──────────── assigned to ───────► TaskItem
```

Relacionamentos:

* User 1:N Project
* Project 1:N TaskItem
* User 1:N TaskItem através de `AssignedToId`

---

# 15. Regras de domínio

As entidades não devem ser apenas estruturas de dados.

Devem possuir comportamento relacionado ao próprio domínio.

Exemplo:

```csharp
project.Update(name, description);
```

em vez de:

```csharp
project.Name = name;
project.Description = description;
```

Para TaskItem:

```csharp
task.AssignTo(userId);

task.ChangeStatus(TaskItemStatus.InProgress);

task.ChangePriority(TaskPriority.High);
```

O objetivo é encapsular alterações importantes dentro do domínio.

---

# 16. PostgreSQL

O PostgreSQL será o banco principal do sistema.

Ele armazenará:

* Users
* Projects
* TaskItems

Motivação:

O domínio possui relacionamentos fortes e dados transacionais.

Exemplo:

```text
User
 │
 ├── Projects
 │      │
 │      └── Tasks
 │
 └── Assigned Tasks
```

Um banco relacional é adequado para:

* Relacionamentos
* Foreign Keys
* Constraints
* Transações
* Integridade dos dados
* Consultas relacionais

---

# 17. Entity Framework Core

O Entity Framework Core será utilizado como ORM.

O acesso ao banco deverá ficar dentro de:

```text
TaskFlow.Infrastructure
```

O `DbContext` deverá ser:

```text
TaskFlowDbContext
```

Exemplo conceitual:

```csharp
public class TaskFlowDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}
```

---

# 18. Configuração das entidades

As configurações do Entity Framework deverão preferencialmente ficar separadas das entidades.

Exemplo:

```text
TaskFlow.Infrastructure/
└── Configurations/
    ├── UserConfiguration.cs
    ├── ProjectConfiguration.cs
    └── TaskItemConfiguration.cs
```

Isso evita colocar detalhes de persistência dentro do domínio.

---

# 19. Migrations

O banco deverá utilizar Entity Framework Migrations.

Fluxo esperado:

```text
Alteração do Domain
       ↓
Alteração da configuração
       ↓
Migration
       ↓
PostgreSQL
```

---

# 20. API REST

A API deverá utilizar endpoints REST.

## Projects

### POST

```http
POST /api/projects
```

Request:

```json
{
  "name": "TaskFlow Development",
  "description": "Projeto de portfólio"
}
```

---

### GET

```http
GET /api/projects
```

---

### GET por ID

```http
GET /api/projects/{id}
```

---

### PUT

```http
PUT /api/projects/{id}
```

---

### DELETE

```http
DELETE /api/projects/{id}
```

---

### Dashboard

```http
GET /api/dashboard
```

Response:

```json
{
  "totalProjects": 3,
  "totalTasks": 12,
  "tasksByStatus": { "todo": 5, "inProgress": 3, "done": 4 },
  "tasksByPriority": { "low": 4, "medium": 5, "high": 3 },
  "recentProjects": [
    { "id": "...", "name": "Website", "description": null, "createdAt": "...", "taskCount": 12, "doneCount": 4 }
  ]
}
```

`GET /api/projects` retorna o mesmo formato de `recentProjects` (com `taskCount` e `doneCount`).

---

# 21. Tasks API

### Criar tarefa

```http
POST /api/projects/{projectId}/tasks
```

Request:

```json
{
  "title": "Implementar autenticação",
  "description": "Criar autenticação utilizando JWT",
  "priority": "High",
  "dueDate": "2026-10-05"
}
```

---

### Listar tarefas

```http
GET /api/projects/{projectId}/tasks
```

---

### Buscar tarefa

```http
GET /api/tasks/{id}
```

---

### Atualizar tarefa

```http
PUT /api/tasks/{id}
```

---

### Excluir tarefa

```http
DELETE /api/tasks/{id}
```

---

### Alterar status

```http
PATCH /api/tasks/{id}/status
```

Request:

```json
{
  "status": "InProgress"
}
```

---

### Atribuir usuário

```http
PATCH /api/tasks/{id}/assignee
```

Request:

```json
{
  "userId": "00000000-0000-0000-0000-000000000000"
}
```

---

# 22. Application Layer

A Application Layer será responsável por orquestrar os casos de uso.

Ela não deve conhecer detalhes específicos do PostgreSQL.

Estrutura implementada:

```text
TaskFlow.Application/
│
├── Common/
│   ├── ITaskFlowDbContext.cs     # abstração de acesso a dados (ver seção 26)
│   └── NotFoundException.cs
│
├── Projects/
│   ├── ProjectDtos.cs            # CreateProjectRequest, UpdateProjectRequest, ProjectResponse
│   └── ProjectService.cs         # Create, List, Get, Update, Delete
│
├── Tasks/
│   ├── TaskDtos.cs               # CreateTaskRequest, UpdateTaskRequest, ChangeTaskStatusRequest, AssignTaskRequest, TaskResponse
│   └── TaskService.cs            # Create, ListByProject, Get, Update, Delete, ChangeStatus, Assign
│
└── DependencyInjection.cs        # AddApplication()
```

> Decisão: um service por feature, com **um método por caso de uso**, em vez de uma pasta/classe por caso de uso. Os casos de uso continuam explícitos e nomeados, com menos arquivos e sem boilerplate. Se um caso de uso crescer (várias dependências, regras próprias), ele pode ser extraído para uma classe dedicada.
>
> Os casos de uso de `Users` (cadastro e perfil) ficam para a Fase 7, junto com password hashing e JWT.

---

# 23. Use Cases

Cada operação importante deverá possuir um caso de uso.

Exemplo:

```text
CreateProject
GetProjects
GetProject
UpdateProject
DeleteProject
CreateTask
ChangeTaskStatus
AssignTask
```

Os Controllers deverão chamar esses casos de uso.

Eles não devem conter regras de negócio complexas.

---

# 24. DTOs

Não retornar diretamente as entidades do domínio na API.

Utilizar DTOs.

Exemplo:

```text
CreateProjectRequest
UpdateProjectRequest
ProjectResponse
CreateTaskRequest
UpdateTaskRequest
TaskResponse
```

Isso permite separar:

```text
Domain Model
      ≠
API Contract
```

---

# 25. Dependency Injection

ASP.NET Core Dependency Injection deverá ser utilizada.

Exemplo:

```text
Controller
     ↓
Application Service / Use Case
     ↓
Interface
     ↓
Infrastructure Implementation
```

A implementação concreta deve ser registrada no `Program.cs` ou através de extensions específicas.

---

# 26. Repository Pattern

O Repository Pattern deverá ser utilizado apenas quando houver uma justificativa.

Não criar abstrações desnecessárias para cada tabela.

Caso utilizado, a interface deverá ficar em uma camada interna:

```text
TaskFlow.Application
```

e a implementação:

```text
TaskFlow.Infrastructure
```

Exemplo:

```text
IProjectRepository
        ▲
        │
ProjectRepository
```

> Decisão (Fase 4): **não** foram criados repositories. O `DbContext` do EF Core já implementa os padrões Repository (`DbSet<T>`) e Unit of Work (`SaveChangesAsync`). A Application depende apenas da interface `ITaskFlowDbContext`, que usa as abstrações do EF Core mas não conhece o PostgreSQL (Npgsql fica só na Infrastructure). A implementação concreta é o próprio `TaskFlowDbContext`.
>
> Um repository dedicado passa a se justificar se surgirem consultas complexas reaproveitadas em vários casos de uso, ou se for necessário trocar o EF Core.

---

# 27. Tratamento global de erros

A API deverá possuir um middleware global de exceções.

Objetivo:

* Centralizar tratamento de erros
* Padronizar respostas
* Evitar `try/catch` repetitivos nos Controllers
* Evitar exposição de stack traces em produção

Formato sugerido:

```json
{
  "status": 400,
  "title": "Validation Error",
  "message": "Project name is required",
  "timestamp": "2026-09-29T18:00:00Z"
}
```

Em produção, detalhes internos da exceção não devem ser expostos.

---

# 28. Validação

Todas as entradas externas devem ser validadas.

Exemplos:

* Nome obrigatório
* Email válido
* Título da tarefa obrigatório
* Limite de tamanho de strings
* IDs válidos
* Datas válidas

A validação deve acontecer antes da persistência.

---

# 29. Swagger

A API deverá possuir documentação OpenAPI/Swagger.

A documentação deverá permitir:

* Visualizar endpoints
* Visualizar schemas
* Testar endpoints
* Entender requests e responses

---

# 30. Health Checks

A API deverá possuir:

```http
GET /health
```

Exemplo:

```json
{
  "status": "Healthy"
}
```

Implementado: o health check já verifica a conexão com o PostgreSQL (retorna `Unhealthy` com HTTP 503 se o banco estiver fora).

Posteriormente o health check poderá verificar:

* MongoDB
* Serviços externos

---

# 31. Logs

A aplicação deverá utilizar logs estruturados.

Exemplo:

```text
INFO ProjectCreated
projectId=123
userId=456
timestamp=2026-09-29T18:00:00Z
```

Eventos importantes:

* Login
* Criação de projeto
* Criação de tarefa
* Alteração de status
* Erros
* Falhas de integração
* Operações administrativas

---

# 32. Segurança

A aplicação deverá evoluir para possuir:

* JWT
* Authentication
* Authorization
* Password hashing
* Role-based access control
* HTTPS
* CORS
* Input validation
* Secrets via environment variables

Nunca armazenar:

```text
password
JWT secret
database password
API key
```

diretamente no código-fonte.

---

# 33. Autenticação

A autenticação será implementada após o CRUD básico.

Endpoints previstos:

```http
POST /api/auth/register
POST /api/auth/login
GET /api/auth/me
```

O login deverá retornar um JWT.

Exemplo:

```json
{
  "accessToken": "...",
  "expiresAt": "..."
}
```

Implementado:

* `register` e `login` retornam `{ accessToken, expiresAt, user }`; `me` retorna `{ id, name, email }`.
* Senha: 8 a 128 caracteres, armazenada com o `PasswordHasher` do ASP.NET Core Identity (PBKDF2 com salt).
* Login com e-mail inexistente e com senha errada devolve a **mesma** resposta 401 (não revela quais e-mails existem). E-mail já cadastrado → 409.
* JWT HS256 com `sub`, `name`, `jti`; validade de 60 min. Configuração na seção `Jwt` (`Issuer`, `Audience`, `ExpirationMinutes`); o segredo **não** fica no `appsettings.json`: em desenvolvimento está no `appsettings.Development.json`, fora dele vem de `Jwt__Secret` (mínimo 32 caracteres, validado na inicialização).
* Frontend guarda o token em `localStorage` e encerra a sessão ao receber 401. Trade-off documentado em `services/api.ts`: cookie httpOnly protegeria contra XSS, mas exigiria proteção CSRF.

---

# 34. Autorização

Inicialmente podem existir papéis:

```text
User
Admin
```

Posteriormente poderão existir:

```text
ProjectOwner
ProjectMember
Admin
```

A autorização deverá impedir que um usuário acesse ou modifique projetos aos quais não possui acesso.

Implementado:

* Política padrão (`FallbackPolicy`) exige usuário autenticado em **todos** os endpoints; só `register`, `login` e `/health` são anônimos.
* Cada usuário vê apenas os projetos que criou, e as tarefas desses projetos; dashboard também é por usuário. Recurso de outro usuário responde **404** (não revela que existe).
* Papéis (`User`/`Admin`) e membros de projeto ainda não foram criados: sem um caso de uso que precise deles, seriam código sem uso. Entram quando houver compartilhamento de projetos.
* `GET /api/users` lista apenas `id` e `name` (para escolher responsável), sem expor e-mails.

---

# 35. React

O frontend será desenvolvido utilizando:

* React
* TypeScript
* Vite

Estrutura:

```text
src/
│
├── components/
│   ├── Button/
│   ├── Modal/
│   ├── TaskCard/
│   ├── ProjectCard/
│   └── KanbanBoard/
│
├── pages/
│   ├── Login/
│   ├── Dashboard/
│   ├── Projects/
│   └── ProjectDetails/
│
├── layouts/
│
├── services/
│   └── api.ts
│
├── hooks/
│
├── types/
│
├── utils/
│
└── App.tsx
```

---

# 36. Frontend — Dashboard

O Dashboard deverá apresentar:

* Número de projetos
* Número total de tarefas
* Tarefas pendentes
* Tarefas em andamento
* Tarefas concluídas
* Projetos recentes

Exemplo visual:

```text
┌─────────────────────────────────────────────────────────┐
│ TaskFlow                                 Alexandre      │
├──────────────┬──────────────────────────────────────────┤
│              │                                          │
│ Dashboard    │  Projects                                │
│              │                                          │
│ Projects     │  ┌────────────────┐ ┌────────────────┐  │
│              │  │ Website        │ │ Mobile App     │  │
│ Tasks        │  │ 12 tasks       │ │ 8 tasks        │  │
│              │  │ 4 completed    │ │ 3 completed    │  │
│              │  └────────────────┘ └────────────────┘  │
│              │                                          │
└──────────────┴──────────────────────────────────────────┘
```

---

# 37. Frontend — Kanban

Cada projeto deverá possuir um Kanban:

```text
┌──────────────┐ ┌────────────────┐ ┌──────────────┐
│ TODO         │ │ IN PROGRESS    │ │ DONE         │
├──────────────┤ ├────────────────┤ ├──────────────┤
│              │ │                │ │              │
│ Login        │ │ Dashboard      │ │ Database     │
│ HIGH         │ │ MEDIUM         │ │ HIGH         │
│              │ │                │ │              │
├──────────────┤ ├────────────────┤ ├──────────────┤
│ Register     │ │ API            │ │ Docker       │
│ LOW          │ │ HIGH           │ │ MEDIUM       │
│              │ │                │ │              │
└──────────────┘ └────────────────┘ └──────────────┘
```

O MVP pode utilizar alteração de status através de botões.

Drag-and-drop é uma evolução opcional.

---

# 38. Comunicação frontend/backend

O frontend deverá possuir um serviço central para comunicação com a API.

Exemplo:

```text
src/services/api.ts
```

Esse serviço deverá centralizar:

* Base URL
* Headers
* Token
* Tratamento de erros
* Requests

Evitar espalhar chamadas HTTP diretamente pelos componentes.

---

# 39. Docker

O projeto deverá ser containerizado.

Containers principais:

```text
┌─────────────────────────────┐
│        Docker Compose       │
│                             │
│ ┌─────────────┐             │
│ │  Frontend   │             │
│ │   React     │             │
│ └─────────────┘             │
│                             │
│ ┌─────────────┐             │
│ │     API     │             │
│ │ ASP.NET Core│             │
│ └─────────────┘             │
│                             │
│ ┌─────────────┐             │
│ │ PostgreSQL  │             │
│ └─────────────┘             │
│                             │
└─────────────────────────────┘
```

---

# 40. Docker Compose

Inicialmente:

```yaml
services:

  postgres:
    image: postgres:16
    container_name: taskflow-postgres
    restart: unless-stopped

    environment:
      POSTGRES_DB: taskflow
      POSTGRES_USER: taskflow
      POSTGRES_PASSWORD: taskflow

    ports:
      - "5436:5432"   # 5432 ocupada localmente por outros projetos

    volumes:
      - taskflow_postgres_data:/var/lib/postgresql/data

volumes:
  taskflow_postgres_data:
```

Implementado (Fase 9): o `docker-compose.yml` sobe `postgres`, `api` e `web`.

```text
web (nginx, :3000) ──/api──► api (ASP.NET Core, :8080) ──► postgres (:5432)
      rede "frontend"              redes "frontend" + "backend"     rede "backend"
```

* O frontend não alcança o banco: só a API está nas duas redes.
* A API espera o PostgreSQL ficar saudável (`healthcheck` com `pg_isready`) antes de iniciar.
* Portas no host configuráveis por `.env` (`WEB_PORT`, `API_PORT`, `POSTGRES_PORT`).
* `docker compose up -d postgres` continua servindo para desenvolvimento local (dotnet run / npm run dev).

---

# 41. Environment Variables

Configurações sensíveis e específicas do ambiente não devem ficar hardcoded.

Exemplo:

```text
DATABASE_HOST
DATABASE_PORT
DATABASE_NAME
DATABASE_USER
DATABASE_PASSWORD
JWT_SECRET
```

Desenvolvimento pode utilizar:

```text
.env
```

ou `appsettings.Development.json`.

Produção deve utilizar secrets fornecidos pelo ambiente de deploy.

---

# 42. Testes

O projeto deverá possuir dois tipos principais de testes.

## Unit Tests

Testam regras isoladas.

Exemplo:

```text
CreateProject
├── Deve criar projeto com dados válidos
├── Deve rejeitar nome vazio
└── Deve definir CreatedAt
```

---

## Integration Tests

Testam a integração da aplicação.

Exemplo:

```text
HTTP Request
     ↓
Controller
     ↓
Application
     ↓
Infrastructure
     ↓
PostgreSQL
```

---

# 43. Testes de API

Testar endpoints importantes:

```text
POST /api/projects
GET /api/projects
GET /api/projects/{id}
PUT /api/projects/{id}
DELETE /api/projects/{id}

POST /api/projects/{projectId}/tasks
GET /api/projects/{projectId}/tasks
PATCH /api/tasks/{id}/status
```

---

# 44. Git

O projeto deverá utilizar Git desde o início.

Branches sugeridas:

```text
main
develop
feature/*
fix/*
```

Exemplos:

```text
feature/project-crud
feature/task-crud
feature/authentication
feature/docker
feature/gitlab-ci
```

---

# 45. Commits

Utilizar commits descritivos.

Exemplos:

```text
feat: add project entity
feat: implement project creation
feat: add task status management
test: add project service tests
fix: validate empty project name
refactor: move persistence configuration
ci: add backend pipeline
docker: add postgres container
docs: update architecture documentation
```

---

# 46. GitLab CI/CD

O pipeline deverá inicialmente possuir:

```text
build
test
```

Exemplo:

```yaml
stages:
  - build
  - test

backend_build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:8.0

  script:
    - dotnet restore backend/TaskFlow.sln
    - dotnet build backend/TaskFlow.sln --no-restore

backend_test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0

  script:
    - dotnet test backend/TaskFlow.sln
```

---

# 47. Pipeline futuro

O pipeline completo deverá evoluir para:

```text
Git Push
   │
   ▼
Build
   │
   ▼
Unit Tests
   │
   ▼
Integration Tests
   │
   ▼
Docker Build
   │
   ▼
Security Checks
   │
   ▼
Container Registry
   │
   ▼
Deploy Staging
   │
   ▼
Smoke Tests
   │
   ▼
Deploy Production
```

---

# 48. Docker Registry

As imagens poderão ser publicadas no GitLab Container Registry.

Exemplo conceitual:

```text
registry.gitlab.com/<project>/taskflow-api
registry.gitlab.com/<project>/taskflow-frontend
```

As imagens deverão possuir tags previsíveis.

Exemplo:

```text
latest
main
1.0.0
commit-sha
```

---

# 49. Cloud

Depois do MVP e CI/CD, a aplicação poderá ser publicada em:

* AWS
* DigitalOcean

Não é necessário implementar ambas.

A infraestrutura deverá ser escolhida de acordo com o objetivo de demonstração.

---

# 50. Deploy inicial

Uma primeira estratégia simples:

```text
GitLab
   │
   ▼
Docker Registry
   │
   ▼
Linux Server
   │
   ├── Frontend
   ├── API
   └── PostgreSQL
```

Posteriormente:

```text
Load Balancer
      │
      ├── API Instance 1
      ├── API Instance 2
      └── API Instance N
```

---

# 51. Observabilidade

A aplicação deverá evoluir para uma arquitetura observável.

Componentes:

```text
TaskFlow
   │
   ├── Logs
   │
   ├── Metrics
   │
   └── Health Checks
          │
          ▼
      Prometheus
          │
          ▼
       Grafana
```

Métricas possíveis:

* HTTP request count
* HTTP error count
* HTTP latency
* CPU
* Memory
* Database connections
* Tasks created
* Tasks completed

---

# 52. MongoDB e auditoria

MongoDB será uma extensão opcional.

Não utilizar MongoDB no MVP apenas para marcar presença na stack.

Caso implementado, utilizar para eventos de auditoria.

Exemplo:

```json
{
  "event": "TASK_STATUS_CHANGED",
  "taskId": "123",
  "userId": "456",
  "oldStatus": "Todo",
  "newStatus": "InProgress",
  "timestamp": "2026-09-29T18:00:00Z"
}
```

Outros eventos:

```text
PROJECT_CREATED
PROJECT_UPDATED
PROJECT_DELETED
TASK_CREATED
TASK_UPDATED
TASK_STATUS_CHANGED
TASK_ASSIGNED
TASK_DELETED
USER_LOGIN
```

---

# 53. Kubernetes

Kubernetes não faz parte do MVP.

Será uma evolução posterior.

Arquitetura futura:

```text
Kubernetes Cluster
│
├── Frontend
│   └── Pods
│
├── API
│   └── Pods
│
├── Services
│
├── Ingress
│
└── Configuration
```

---

# 54. Rancher

Rancher poderá ser utilizado posteriormente para gerenciamento do cluster Kubernetes.

Não implementar Rancher antes de possuir uma necessidade concreta.

---

# 55. Escalabilidade

A aplicação deverá ser projetada para permitir múltiplas instâncias da API.

Evitar depender de estado armazenado localmente na aplicação.

Exemplo:

```text
                 Load Balancer
                       │
          ┌────────────┼────────────┐
          │            │            │
          ▼            ▼            ▼
       API #1       API #2       API #3
          │            │            │
          └────────────┼────────────┘
                       │
                       ▼
                  PostgreSQL
```

---

# 56. Stateless API

A API deverá ser stateless sempre que possível.

Não armazenar informações importantes exclusivamente em memória.

Isso permite:

* Horizontal scaling
* Load balancing
* Restart dos containers
* Deploy sem depender de uma instância específica

---

# 57. Backup

O PostgreSQL deverá possuir estratégia de backup quando o projeto chegar à fase de produção.

Considerar:

* Backup automático
* Retenção
* Restore
* Teste periódico de recuperação

---

# 58. Requisitos funcionais

## Usuários

* Cadastro
* Login
* Visualização do perfil

## Projetos

* Criar projeto
* Listar projetos
* Visualizar projeto
* Atualizar projeto
* Excluir projeto

## Tarefas

* Criar tarefa
* Listar tarefas
* Visualizar tarefa
* Atualizar tarefa
* Excluir tarefa
* Alterar status
* Alterar prioridade
* Definir prazo
* Atribuir usuário

## Dashboard

* Total de projetos
* Total de tarefas
* Tarefas por status
* Tarefas por prioridade
* Projetos recentes

---

# 59. Requisitos não funcionais

## Performance

A API deverá responder rapidamente para operações normais do MVP.

## Segurança

Não expor credenciais ou informações sensíveis.

## Manutenibilidade

Código organizado em camadas.

## Testabilidade

Regras importantes devem possuir testes.

## Escalabilidade

API preparada para múltiplas instâncias.

## Observabilidade

Logs e health checks desde as primeiras versões.

## Deploy

Aplicação executável via Docker.

---

# 60. Requisitos de qualidade

O projeto deverá seguir:

* SOLID
* Clean Code
* Separation of Concerns
* Dependency Injection
* REST
* Conventional Commits
* Code Review
* Automated Tests

Evitar:

* God Classes
* Controllers gigantes
* Services gigantes
* SQL espalhado pela aplicação
* Secrets no código
* Código duplicado
* Abstrações sem necessidade

---

# 61. Roadmap de implementação

## Fase 1 — Setup

* [x] Criar repositório Git
* [x] Criar solution
* [x] Criar projetos .NET
* [x] Configurar referências
* [x] Criar `.gitignore`
* [x] Configurar README
* [x] Configurar PostgreSQL via Docker

---

# Fase 2 — Domain

* [x] Criar User
* [x] Criar Project
* [x] Criar TaskItem
* [x] Criar TaskItemStatus
* [x] Criar TaskPriority
* [x] Implementar regras básicas
* [x] Criar relacionamentos (chaves `OwnerId`, `ProjectId`, `AssignedToId`; mapeamento no EF na Fase 3)

---

# Fase 3 — Infrastructure

* [x] Instalar EF Core
* [x] Criar DbContext
* [x] Configurar entidades
* [x] Configurar PostgreSQL
* [x] Criar migrations
* [x] Criar banco
* [x] Testar persistência (`tests/TaskFlow.IntegrationTests`, banco separado `taskflow_tests`)

Decisões de mapeamento:

* Enums gravados como texto (`HasConversion<string>()`): legíveis no banco e imunes à reordenação do enum.
* `DueDate` como coluna `date` (prazo não tem hora nem fuso).
* Exclusões: Project → Tasks em cascata; User com projetos não pode ser excluído (`Restrict`); responsável excluído deixa a tarefa sem responsável (`SetNull`).
* `Email` com índice único.
* Connection string apenas em `appsettings.Development.json`; fora disso via `ConnectionStrings__Default`. Testes de integração aceitam `TASKFLOW_TEST_CONNECTION`.

---

# Fase 4 — Application

* [x] Criar DTOs
* [x] Criar CreateProject
* [x] Criar GetProjects
* [x] Criar GetProject
* [x] Criar UpdateProject
* [x] Criar DeleteProject
* [x] Criar CreateTask
* [x] Criar GetTasks (e GetTask)
* [x] Criar UpdateTask
* [x] Criar DeleteTask
* [x] Criar ChangeTaskStatus
* [x] Criar AssignTask (aceita `userId: null` para remover o responsável)

Testes dos casos de uso em `tests/TaskFlow.IntegrationTests/Application`, contra o PostgreSQL real.

---

# Fase 5 — API

* [x] Criar ProjectsController
* [x] Criar TasksController
* [x] Configurar Dependency Injection
* [x] Configurar Swagger
* [x] Configurar middleware (`GlobalExceptionHandler`: `DomainException` → 400, `NotFoundException` → 404, demais → 500 sem detalhes internos)
* [x] Configurar validation
* [x] Criar health check (`/health`, inclui conexão com o PostgreSQL)
* [x] Testar endpoints (`tests/TaskFlow.IntegrationTests/Api`, via `WebApplicationFactory`)

Decisões da API:

* Enums trafegam como texto no JSON (`"High"`, `"InProgress"`); valor desconhecido → 400.
* Validação em duas camadas, sem duplicar regras: o model binding rejeita JSON malformado, tipos errados e campos obrigatórios ausentes; o domínio valida conteúdo (vazio, tamanho, transições). Ambos respondem no formato da seção 27.
* **Usuário demo (temporário):** em Development, a API aplica as migrations e cria `demo@taskflow.local` na inicialização; ele é o "usuário atual" (`ICurrentUser`) e dono dos projetos criados. Não consegue fazer login. Será substituído pelo usuário do JWT na Fase 7.
* Em desenvolvimento a API roda em `http://localhost:5033` (Swagger em `/swagger`).

---

# Fase 6 — Frontend

* [x] Criar React + TypeScript
* [x] Configurar Vite
* [x] Criar layout
* [x] Criar Dashboard
* [x] Criar Projects
* [x] Criar Project Details
* [x] Criar Task Card
* [x] Criar Kanban
* [x] Integrar API
* [x] Implementar loading states
* [x] Implementar error states

Decisões do frontend:

* Dependências de runtime: `react`, `react-dom`, `react-router-dom` (rotas `/`, `/projects`, `/projects/:id`) e `@phosphor-icons/react` (uma única família de ícones). Estilo em CSS puro com variáveis, tema claro/escuro automático e layout responsivo.
* Linguagem visual: neutros zinc + um único acento cobalto; cores de status/prioridade só em badges, barras e bordas. Raios: cards 12px, controles 8px, badges em pílula. Contraste WCAG AA nos dois temas.
* Movimento com propósito: botões com `scale(0.97)` ao pressionar; card de tarefa entra com opacity + translateY(4px) em 200ms (`@starting-style`) ao ser criado ou mudar de coluna; barras de progresso animam `transform: scaleX` (não `width`); hover só em dispositivos com mouse; tudo respeita `prefers-reduced-motion`.
* Loading com skeletons no formato do conteúdo final (dashboard, grade, kanban).
* Layout: sidebar de altura total (marca, navegação, usuário), conteúdo até 1440px e cabeçalho de página padronizado. Fonte Geist auto-hospedada (`@fontsource-variable/geist`, sem CDN).
* Criação de tarefa estilo Trello: "+ Nova tarefa" dentro da coluna A fazer, formulário compacto que continua aberto após adicionar (Esc fecha). Criação de projeto por botão no cabeçalho, que leva direto ao quadro do novo projeto.
* Em dev o Vite (`http://localhost:5173`) faz proxy de `/api` e `/health` para a API: mesma origem, sem necessidade de CORS. No Docker o mesmo papel é do nginx (Fase 9).
* `useAsync` (hook) centraliza loading/erro/reload; mutações atualizam o estado local sem recarregar a página.
* Os botões do Kanban só movem uma coluna por vez, espelhando a regra do domínio. Prazo vencido aparece como "Atrasada".
* Backend: a listagem de projetos passou a trazer `taskCount`/`doneCount` e foi criado `GET /api/dashboard` (contagens calculadas no banco), evitando uma requisição por projeto no frontend.
* Fora do MVP da tela, por ora: edição de título/prioridade da tarefa e atribuição de responsável (a API já suporta; entram quando houver usuários reais, Fase 7).

---

# Fase 7 — Authentication

* [x] Criar User
* [x] Criar Register
* [x] Criar Login
* [x] Password hashing
* [x] JWT
* [x] Authentication middleware
* [x] Authorization (projetos por dono; papéis adiados, ver seção 34)
* [x] Proteger endpoints

Junto com a Fase 7:

* Tarefas ganharam **número sequencial por projeto** e **criador** (migration `AddTaskNumberAndCreator`, que numera as tarefas existentes). A chave exibida é `SIGLA-N`, com a sigla derivada do nome do projeto no frontend (ex.: "Website Institucional" → `WI-1`).
* **Detalhe da tarefa no estilo Jira**: modal com URL própria (`/projects/:id/tasks/:taskId`), breadcrumb com a chave, título e descrição editáveis no lugar, status em dropdown (só transições permitidas) e painel "Detalhes" com responsável ("Atribuir a mim"), criador, prioridade com ícone, data limite e data de criação. Clicar no card abre o detalhe; a tarefa recém-criada abre direto nele. Esc, clique fora e o botão fechar voltam ao quadro.
* Telas de login e cadastro; rotas protegidas redirecionam para o login e voltam à página pedida depois de entrar.
* O antigo usuário demo foi removido. Projetos criados por ele em bancos de desenvolvimento antigos ficam inacessíveis (ninguém consegue logar como ele).

---

# Fase 8 — Tests

* [x] Criar Unit Tests (`tests/TaskFlow.UnitTests`: regras de domínio)
* [x] Criar Integration Tests (`tests/TaskFlow.IntegrationTests`: persistência, casos de uso e API via `WebApplicationFactory` contra PostgreSQL real)
* [x] Testar Projects
* [x] Testar Tasks
* [x] Testar Authentication (cadastro, login, `/me`, token inválido, senha em hash, mesma resposta para senha errada e e-mail inexistente)
* [x] Testar validações (400 no formato padrão, 409 em e-mail duplicado)
* [x] Testar autorização (usuário não enxerga nem altera projeto/tarefa de outro)

Os testes foram escritos junto com cada fase (seção 70, regra 9). Cada teste de API cria um usuário próprio, então os testes não dependem da ordem de execução nem dos dados uns dos outros.

---

# Fase 9 — Docker

* [x] Dockerfile backend (`docker/backend/Dockerfile`)
* [x] Dockerfile frontend (`docker/frontend/Dockerfile` + `nginx.conf`)
* [x] PostgreSQL container (com healthcheck)
* [x] Docker Compose
* [x] Networks (`frontend` e `backend` separadas)
* [x] Volumes (`taskflow_postgres_data`)
* [x] Environment variables (`.env`, modelo em `.env.example`)
* [x] Testar aplicação completa via Docker

Decisões:

* **Backend:** build multi-stage (SDK só no build; runtime `aspnet:8.0`), restore em camada própria para cache, processo rodando como usuário não-root (`$APP_UID`).
* **Frontend:** build com Node e entrega por `nginx-unprivileged` (não-root). O nginx serve o SPA (rotas desconhecidas → `index.html`), faz proxy de `/api` e `/health` para a API (mesma origem, sem CORS), aplica cache longo em `/assets` (nomes com hash) e headers de segurança (`nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`).
* **Configuração:** a API roda como `Production` no container. `Database__ApplyMigrationsOnStartup` aplica as migrations ao subir e `Swagger__Enabled` liga o Swagger no ambiente de demonstração; fora disso, ambos só em Development.
* **Segredos:** `JWT_SECRET` vem do `.env` (fora do Git) e não tem valor padrão: sem ele a API recusa iniciar. A senha do PostgreSQL também vem do `.env`.

---

# Fase 10 — GitLab CI/CD

* [ ] Criar GitLab repository
* [ ] Configurar `.gitlab-ci.yml`
* [ ] Build
* [ ] Unit tests
* [ ] Integration tests
* [ ] Docker build
* [ ] Container Registry
* [ ] Deploy

---

# Fase 11 — Cloud

* [ ] Escolher AWS ou DigitalOcean
* [ ] Criar servidor/infraestrutura
* [ ] Configurar Docker
* [ ] Configurar PostgreSQL
* [ ] Configurar domínio
* [ ] Configurar HTTPS
* [ ] Configurar deploy automático
* [ ] Configurar backup

---

# Fase 12 — Observabilidade

* [ ] Structured Logging
* [ ] Metrics
* [ ] Prometheus
* [ ] Grafana
* [ ] Health Checks
* [ ] Dashboards
* [ ] Alertas

---

# Fase 13 — Evolução

* [ ] MongoDB
* [ ] Audit Events
* [ ] Kubernetes
* [ ] Rancher
* [ ] Horizontal Scaling
* [ ] Load Balancer

---

# 62. MVP

O MVP será considerado concluído quando:

* [x] PostgreSQL estiver funcionando via Docker
* [x] API ASP.NET Core estiver funcionando
* [x] Entity Framework estiver configurado
* [x] CRUD de projetos estiver funcionando
* [x] CRUD de tarefas estiver funcionando
* [x] Alteração de status estiver funcionando
* [x] React estiver funcionando
* [x] React estiver consumindo a API
* [x] Dashboard estiver funcionando
* [x] Kanban estiver funcionando
* [x] Testes básicos estiverem implementados
* [x] Docker Compose executar a aplicação
* [ ] GitLab CI executar build e testes

---

# 63. Critérios de conclusão do MVP

Para considerar o MVP concluído, executar:

```bash
docker compose up -d
```

E conseguir acessar:

```text
Frontend
http://localhost:3000

API
http://localhost:8080

Swagger
http://localhost:8080/swagger

Health
http://localhost:8080/health
```

Os valores das portas podem ser ajustados conforme a implementação.

Implementado: antes do primeiro `up`, `cp .env.example .env` e definir `JWT_SECRET`. Com as portas padrão, os endereços acima valem como estão; se 3000/8080 estiverem ocupadas, ajuste `WEB_PORT`/`API_PORT` no `.env`. O frontend também expõe `/health` (via proxy).

---

# 64. Mapeamento com requisitos profissionais

O TaskFlow deve permitir demonstrar:

| Requisito     | Demonstração         |
| ------------- | -------------------- |
| C#            | Backend              |
| .NET Core     | ASP.NET Core         |
| REST          | API                  |
| React         | Frontend             |
| TypeScript    | Frontend             |
| PostgreSQL    | Banco principal      |
| SQL           | EF Core              |
| MongoDB       | Auditoria futura     |
| Git           | Versionamento        |
| GitLab        | Repositório          |
| Docker        | Containers           |
| CI/CD         | GitLab Pipeline      |
| Linux         | Ambiente de execução |
| AWS           | Deploy futuro        |
| DigitalOcean  | Deploy alternativo   |
| Kubernetes    | Evolução             |
| Rancher       | Evolução             |
| Testing       | Unit + Integration   |
| Architecture  | Clean Architecture   |
| Observability | Logs + Metrics       |
| Security      | JWT + Authorization  |

---

# 65. Perguntas que o projeto deve permitir responder

O projeto deve ser construído de forma que seja possível explicar tecnicamente:

### Arquitetura

* Por que utilizar Clean Architecture?
* Por que separar Domain, Application e Infrastructure?
* Por que Controllers não devem conter regras de negócio?
* Como funciona Dependency Injection?
* Como evitar acoplamento?

### Banco de dados

* Por que PostgreSQL?
* Quando utilizaria MongoDB?
* Como funciona uma transação?
* Como funciona Entity Framework?
* Como funcionam migrations?
* Como lidar com concorrência?

### API

* O que caracteriza uma API REST?
* Como tratar erros?
* Como validar requests?
* Como implementar autenticação?
* Como implementar autorização?

### Docker

* Por que utilizar containers?
* Qual diferença entre imagem e container?
* O que é Docker Compose?
* Como persistir dados do PostgreSQL?
* Como configurar variáveis de ambiente?

### CI/CD

* Como funciona um pipeline?
* Qual diferença entre CI e CD?
* Como executar testes automaticamente?
* Como publicar uma imagem Docker?
* Como fazer deploy automático?

### Cloud

* Como faria o deploy?
* Como escalaria a API?
* Como faria backup?
* Como monitoraria a aplicação?
* Como lidaria com uma instância indisponível?

### Observabilidade

* Qual diferença entre logs e métricas?
* O que é health check?
* Como detectar aumento de erros?
* Como medir latência?

---

# 66. Decisões que devem ser evitadas

Não adicionar inicialmente:

* Kubernetes
* Rancher
* MongoDB
* AWS
* Microservices
* Kafka
* Redis
* RabbitMQ
* Service Mesh

sem que exista uma necessidade arquitetural.

O projeto deve começar como um **monólito modular bem estruturado**.

Isso é proposital.

Uma aplicação monolítica bem organizada é preferível a uma arquitetura distribuída artificialmente complexa.

---

# 67. Evolução arquitetural

A evolução planejada é:

```text
                MVP
                 │
                 ▼
       Modular Monolith
                 │
        ┌────────┴────────┐
        │                 │
        ▼                 ▼
   PostgreSQL        Docker
        │                 │
        └────────┬────────┘
                 ▼
              CI/CD
                 │
                 ▼
               Cloud
                 │
        ┌────────┴────────┐
        ▼                 ▼
 Observability         Scaling
        │                 │
        └────────┬────────┘
                 ▼
             Kubernetes
                 │
                 ▼
              Rancher
```

---

# 68. Filosofia arquitetural

O projeto não deve tentar demonstrar "quantas tecnologias consigo colocar em uma aplicação".

O objetivo é demonstrar:

> "Consigo escolher uma tecnologia, explicar por que ela existe e demonstrar sua implementação."

Por exemplo:

### PostgreSQL

Existe porque o domínio é relacional.

### MongoDB

Só será adicionado se houver uma necessidade clara de auditoria/eventos.

### Docker

Existe para padronizar execução.

### GitLab CI/CD

Existe para automatizar build, testes e deploy.

### Kubernetes

Só será utilizado quando houver necessidade de orquestração e escalabilidade.

### Prometheus/Grafana

Existem para resolver necessidades de observabilidade.

---

# 69. Estratégia de implementação

A implementação deverá seguir esta ordem:

```text
1. Criar Solution
        ↓
2. Criar Domain
        ↓
3. Criar Infrastructure
        ↓
4. Configurar PostgreSQL
        ↓
5. Criar Application
        ↓
6. Criar API
        ↓
7. Implementar CRUD
        ↓
8. Criar testes
        ↓
9. Criar React
        ↓
10. Integrar frontend/backend
        ↓
11. Implementar autenticação
        ↓
12. Dockerizar
        ↓
13. Criar GitLab CI/CD
        ↓
14. Deploy
        ↓
15. Observabilidade
        ↓
16. Evoluções
```

---

# 70. Regra para implementação com Claude

Ao implementar este projeto:

1. Não implementar todas as fases de uma vez.
2. Trabalhar por etapas pequenas.
3. Antes de alterar uma arquitetura existente, explicar o motivo.
4. Sempre informar quais arquivos serão criados ou alterados.
5. Fornecer comandos executáveis.
6. Não utilizar código fictício quando houver implementação real necessária.
7. Manter compatibilidade entre os projetos.
8. Evitar duplicação de código.
9. Criar testes junto das funcionalidades importantes.
10. Atualizar a documentação quando uma decisão arquitetural mudar.
11. Não adicionar dependências sem justificativa.
12. Não adicionar tecnologias que não estejam previstas sem explicar a necessidade.
13. Priorizar código funcional antes de abstrações sofisticadas.
14. Sempre considerar execução local via Docker.
15. Manter o projeto pronto para CI/CD.

---

# 71. Estado atual do projeto

## Backend

* [x] Solution criada
* [x] Domain
* [x] Application
* [x] Infrastructure
* [x] API
* [x] PostgreSQL
* [x] EF Core
* [x] Migrations
* [x] Projects CRUD
* [x] Tasks CRUD
* [x] Authentication
* [x] Authorization

## Frontend

* [x] React
* [x] TypeScript
* [x] Dashboard
* [x] Projects
* [x] Kanban
* [x] API integration
* [x] Authentication

## Tests

* [x] Unit Tests
* [x] Integration Tests

## DevOps

* [x] Docker
* [x] Docker Compose
* [ ] GitLab
* [ ] GitLab CI/CD
* [ ] Container Registry
* [ ] Deploy

## Cloud

* [ ] AWS / DigitalOcean
* [ ] HTTPS
* [ ] Domain
* [ ] Backup

## Observability

* [x] Health Checks
* [ ] Structured Logs
* [ ] Prometheus
* [ ] Grafana

## Advanced

* [ ] MongoDB
* [ ] Audit Events
* [ ] Kubernetes
* [ ] Rancher
* [ ] Horizontal Scaling

---

# 72. Primeiro objetivo de implementação

O primeiro objetivo é ter a seguinte estrutura funcionando:

```text
TaskFlow
│
├── ASP.NET Core API
│
├── Clean Architecture
│
├── PostgreSQL
│
├── Entity Framework Core
│
└── CRUD
    ├── Projects
    └── Tasks
```

Depois disso:

```text
React
   ↓
ASP.NET Core
   ↓
Application
   ↓
Domain
   ↓
Infrastructure
   ↓
PostgreSQL
```

Somente após essa etapa o projeto deverá avançar para Docker completo, testes, CI/CD e cloud.

---

# 73. Comando inicial esperado

A criação inicial da solution deverá resultar em:

```bash
mkdir TaskFlow

cd TaskFlow

mkdir backend

cd backend

dotnet new sln -n TaskFlow

dotnet new webapi -n TaskFlow.Api

dotnet new classlib -n TaskFlow.Application

dotnet new classlib -n TaskFlow.Domain

dotnet new classlib -n TaskFlow.Infrastructure
```

Adicionar os projetos:

```bash
dotnet sln add TaskFlow.Api
dotnet sln add TaskFlow.Application
dotnet sln add TaskFlow.Domain
dotnet sln add TaskFlow.Infrastructure
```

Referências:

```bash
dotnet add TaskFlow.Application reference TaskFlow.Domain

dotnet add TaskFlow.Infrastructure reference TaskFlow.Domain

dotnet add TaskFlow.Infrastructure reference TaskFlow.Application

dotnet add TaskFlow.Api reference TaskFlow.Application

dotnet add TaskFlow.Api reference TaskFlow.Infrastructure
```

---

# 74. Estado final desejado

A versão final do projeto deverá representar:

```text
                         INTERNET
                            │
                            ▼
                    ┌───────────────┐
                    │    Frontend   │
                    │ React + TS    │
                    └───────┬───────┘
                            │
                            ▼
                    ┌───────────────┐
                    │   API         │
                    │ ASP.NET Core  │
                    └───────┬───────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │    Application      │
                 │                     │
                 │ Use Cases           │
                 │ DTOs                │
                 │ Business Flow       │
                 └──────────┬──────────┘
                            │
                            ▼
                 ┌─────────────────────┐
                 │       Domain        │
                 │                     │
                 │ Entities            │
                 │ Rules               │
                 │ Value Objects       │
                 └──────────┬──────────┘
                            ▲
                            │
                 ┌──────────┴──────────┐
                 │   Infrastructure   │
                 │                     │
                 │ EF Core             │
                 │ PostgreSQL          │
                 │ Repositories        │
                 └─────────────────────┘

                         Docker
                           │
                           ▼
                     GitLab CI/CD
                           │
                           ▼
                      Cloud Deploy
                           │
                           ▼
                    Observability
                           │
                    ┌──────┴──────┐
                    ▼             ▼
                Prometheus     Grafana
```

---

# 75. Resultado esperado

Ao final, o TaskFlow deverá ser uma aplicação que possa ser apresentada como um projeto real de engenharia de software, demonstrando:

* Desenvolvimento full stack
* Arquitetura limpa
* API REST
* Banco relacional
* ORM
* Testes automatizados
* Containerização
* CI/CD
* Segurança
* Observabilidade
* Cloud
* Escalabilidade

A aplicação deve permanecer compreensível e justificável em uma entrevista técnica.

O foco não é quantidade de código ou quantidade de tecnologias.

O foco é demonstrar **capacidade de projetar, implementar, testar, empacotar, entregar e operar uma aplicação moderna**.
