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
* Cada usuário vê apenas os projetos de que participa, e as tarefas desses projetos; dashboard também é por usuário. Recurso sem acesso responde **404** (não revela que existe).

**Compartilhamento de projetos** (papéis `ProjectOwner` e `ProjectMember` previstos acima):

| Ação | Dono | Membro |
| --- | --- | --- |
| Ver o projeto, o quadro e as pessoas | sim | sim |
| Criar, editar, mover e excluir tarefas; comentar | sim | sim |
| Editar ou excluir o projeto | sim | não (403) |
| Adicionar pessoas | sim | não (403) |
| Remover pessoas | qualquer membro | só a si mesmo (sair) |

```http
GET    /api/projects/{id}/members            → [{ id, name, isOwner }]  (dono primeiro, sem e-mails)
POST   /api/projects/{id}/members            { "email": "..." }  → 201
DELETE /api/projects/{id}/members/{userId}   → 204
```

* A regra de acesso fica em um único lugar (`ProjectAccess`, na Application) e é usada por projetos, tarefas, comentários, histórico e dashboard.
* 404 x 403: quem não participa recebe 404; membro tentando ação de dono recebe 403, porque já sabe que o projeto existe.
* Adicionar é direto, pelo e-mail de quem já tem conta (sem convite por e-mail, que exigiria serviço de envio). Trade-off conhecido: a resposta "nenhuma conta com esse e-mail" permite a um usuário autenticado descobrir se um e-mail está cadastrado; convites pendentes resolveriam isso.
* O dono não pode ser removido nem trocado. Membro repetido é barrado pela chave composta `(ProjectId, UserId)` e responde 409.
* Só pessoas do projeto podem ser responsáveis por tarefas. Ao remover alguém (ou a pessoa sair), as tarefas dela naquele projeto ficam sem responsável, com registro no histórico, na mesma transação.
* O antigo `GET /api/users` (todos os usuários) foi removido: a lista de responsáveis vem das pessoas do projeto.
* O papel global `Admin` continua sem caso de uso e não foi criado.
* Limitação: no histórico, ações de quem já saiu do projeto aparecem como "Alguém", pois os nomes vêm da lista atual de pessoas.

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

Implementado: arrastar e soltar nativo (HTML5, sem biblioteca). Ao arrastar um card, só as colunas vizinhas (movimento permitido pelo domínio) viram destino e ficam destacadas; as demais ficam apagadas e recusam o card. Os botões de seta continuam no card: são o caminho pelo teclado e em telas de toque, onde o drag-and-drop nativo não dispara.

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

> **Decisão (Fase 10): o CI/CD foi implementado com GitHub Actions**, porque o repositório está hospedado no GitHub. Os conceitos são os mesmos (pipeline, jobs, serviços, artefatos, registry); o arquivo é `.github/workflows/ci.yml` e o registry é o GitHub Container Registry (`ghcr.io`). O exemplo em GitLab abaixo foi mantido como referência.

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

> **Decisão (Fase 13): a auditoria das tarefas foi implementada no PostgreSQL**, na tabela `TaskEvents`, sem MongoDB.
>
> * **Motivo:** o evento é gravado no mesmo `SaveChanges` (mesma transação) da alteração. Ou os dois são gravados, ou nenhum: nunca existe mudança sem registro nem registro sem mudança. Com um segundo banco isso exigiria um mecanismo de consistência (outbox ou similar) que o volume atual não justifica.
> * **O que registra:** `Created`, `TitleChanged`, `DescriptionChanged`, `PriorityChanged`, `DueDateChanged`, `StatusChanged`, `AssigneeChanged`, com valor antigo, valor novo, autor e data. Só gera evento quando o valor realmente muda. A descrição registra apenas que mudou (o texto pode ser longo e o atual já está na tarefa).
> * **Onde aparece:** `GET /api/tasks/{id}/history` (mais recente primeiro, restrito ao dono do projeto) e a seção "Atividade" no detalhe da tarefa.
> * **Ciclo de vida:** o histórico pertence à tarefa e é excluído com ela; a exclusão em si fica nos logs estruturados. Se o autor for removido, o evento permanece sem autor.
> * **Quando o MongoDB passaria a fazer sentido:** auditoria de alto volume e formato variável (eventos de vários módulos, payloads livres) que não precise de consistência transacional com os dados principais. Login e operações de projeto hoje ficam nos logs estruturados (Fase 12).

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

# Fase 10 — CI/CD (GitHub Actions no lugar do GitLab, ver seção 46)

* [x] Repositório (GitHub)
* [x] Configurar pipeline (`.github/workflows/ci.yml`)
* [x] Build
* [x] Unit tests
* [x] Integration tests (PostgreSQL 16 como serviço do job)
* [x] Docker build
* [x] Container Registry (GitHub Container Registry, em push na `main`)
* [ ] Deploy (depende da Fase 11: escolha da cloud)

Pipeline (roda em push na `main`, em pull requests e manualmente):

```text
backend  (restore → build Release → 52 testes, resultados .trx como artefato) ─┐
                                                                                ├─► docker
frontend (npm ci → lint → tsc + vite build) ───────────────────────────────────┘
docker:  compose build → sobe a stack → smoke test pelo nginx
         (/health, rota do SPA, 401 sem token, cadastro + acesso com token)
         → em push na main: publica taskflow-api e taskflow-web em ghcr.io
           com as tags latest e SHA curto do commit (seção 48)
```

* Execuções antigas do mesmo branch são canceladas quando chega um push novo.
* O workflow é validado com `actionlint`.
* O token de publicação é o `GITHUB_TOKEN` do próprio job (permissão `packages: write` só no job docker); não há segredo manual a configurar.

---

# Fase 11 — Cloud

> **Adiada (decisão de 30/09/2026).** Hospedagem exige conta e cartão em um provedor; nenhuma fase seguinte depende dela, então o projeto seguiu para a observabilidade, que roda inteira em Docker local. O que já existe e será reaproveitado no deploy: imagens publicadas no `ghcr.io` pelo CI, `docker-compose.yml` pronto para uma VM e configuração 100% por variáveis de ambiente. Faltará: provisionar a VM, HTTPS (proxy reverso com certificado), job de deploy no GitHub Actions e backup do PostgreSQL.

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

* [x] Structured Logging
* [x] Metrics
* [x] Prometheus
* [x] Grafana
* [x] Health Checks (Fase 5)
* [x] Dashboards
* [x] Alertas (regras no Prometheus; entrega por Alertmanager fica para quando houver quem receba)

Como usar: `docker compose --profile observability up -d` → Grafana em `http://localhost:3001` (usuário `admin`, senha `GRAFANA_PASSWORD` do `.env`) e Prometheus em `http://localhost:9090`. Sem o perfil, o `up` normal não sobe os dois.

Decisões:

* **Logs:** logger nativo do .NET, sem biblioteca extra. Fora de Development, uma linha JSON por evento com `Timestamp` UTC, nível, categoria, `TraceId`/`SpanId` da requisição e cada propriedade do template como campo (`ProjectId`, `UserId`...). Eventos de negócio: `UserRegistered`, `LoginSucceeded`, `LoginFailed` (sem o e-mail tentado: não grava dado pessoal nem pista de enumeração), `ProjectCreated`, `ProjectDeleted`, `TaskCreated`, `TaskStatusChanged`. Erros não tratados já eram logados pelo `GlobalExceptionHandler`.
* **Métricas de negócio na Application** com `System.Diagnostics.Metrics` (biblioteca padrão), sem a camada conhecer o Prometheus: `taskflow_projects_created`, `taskflow_tasks_created`, `taskflow_tasks_completed`, `taskflow_logins{result}`. A API exporta com `prometheus-net`, junto com as métricas HTTP (contagem, status e latência por endpoint), do processo (.NET, memória) e do pool de conexões do Npgsql.
* **`/metrics` em porta interna:** no Docker a API escuta também na 9090 e o endpoint só responde nela (`RequireHost`). Essa porta não é publicada no host nem passa pelo nginx; só o Prometheus, na rede `backend`, a alcança. Na porta pública o caminho não existe.
* **Redes:** Grafana só fala com o Prometheus (rede `monitoring`). As portas do Prometheus e do Grafana no host ficam presas a `127.0.0.1`, porque o Prometheus não tem autenticação.
* **Grafana provisionado por arquivo** (`docker/grafana`): fonte de dados e dashboard "TaskFlow" versionados no Git, sem configuração manual. Painéis: API no ar, requisições/s, erros 5xx, latência p50/p95/p99, requisições por status e por endpoint, memória, conexões com o PostgreSQL, projetos/tarefas criados e concluídos, logins por resultado.
* **Alertas** (`docker/prometheus/alerts.yml`): `ApiFora`, `TaxaDeErrosAlta` (>5% de 5xx), `LatenciaAlta` (p95 > 1 s) e `MuitasFalhasDeLogin` (possível força bruta).
* Detalhe de implementação: o `prometheus-net` só começa a escutar `System.Diagnostics.Metrics` no primeiro scrape, perdendo o que foi medido antes; a escuta é iniciada explicitamente na subida da API. Os contadores de negócio contam desde a última inicialização da API.

---

# Fase 13 — Evolução

* [ ] MongoDB (não adotado por ora; ver decisão na seção 52)
* [x] Audit Events (histórico de alterações da tarefa no PostgreSQL, seção 52)
* [x] Drag-and-drop no Kanban (seção 37)
* [x] Comentários nas tarefas (além da spec original)
* [x] Compartilhamento de projetos com papéis de dono e membro (seção 34)
* [x] Página "Para você" e diretório de projetos, no visual do Jira (além da spec original)
* [x] Metas, dashboard em painéis e barra lateral personalizável (além da spec original)
* [x] Espaços (nível acima dos projetos) com galeria de modelos (além da spec original)
* [x] Quadro em tempo real com SignalR (além da spec original)
* [x] Notificações e menções com @, com aviso em tempo real (além da spec original)
* [x] Busca global com Ctrl+K (além da spec original)
* [x] Testes de ponta a ponta com Playwright, também no CI (além da spec original)
* [x] App Android companheiro (Kotlin + Compose), com tempo real e notificações (além da spec original)
* [x] Tipos de tarefa por projeto, responsável na criação e subtarefas (além da spec original)

### Para você (página inicial)

```http
GET    /api/for-you?filter=Recommended|Assigned|Starred|Worked|Viewed
       → { recommendedProjects, assignedCount, items: [{ taskId, projectId, projectName, number, title, status, priority, assigneeName, starred, date }] }
PUT    /api/tasks/{id}/star      marca com estrela      DELETE /api/tasks/{id}/star   desmarca
POST   /api/tasks/{id}/view      registra que abri a tarefa → 204
```

| Aba | O que mostra | `date` usada no agrupamento |
| --- | --- | --- |
| Recomendado | Tarefas abertas atribuídas a mim + abertas em que trabalhei | Última atividade |
| Atribuído a mim | Tarefas abertas atribuídas a mim (com contador) | Última movimentação da tarefa |
| Com estrela | Tarefas que marquei | Quando marquei |
| Trabalhado | Tarefas que alterei ou comentei (inclusive concluídas) | Minha última ação |
| Visto | Tarefas que abri | Quando abri |

* O frontend agrupa por data: Hoje, Ontem, Nesta semana, No último mês, Há mais de um mês.
* "Trabalhado" é derivado do histórico (`TaskEvents`) e dos comentários, sem tabela nova. Estrela e "visto" são marcações pessoais (`TaskStars`, `TaskViews`, chave composta usuário + tarefa), idempotentes, e não entram no histórico da tarefa.
* "Espaços recomendados" são os 3 projetos ativos mais recentes a que tenho acesso.
* Só entram tarefas de projetos ativos a que tenho acesso. Limite de 50 itens por aba, sem paginação.
* `TaskResponse` ganhou `starred` (se **eu** marquei).

### Diretório de projetos

```http
GET    /api/projects?scope=All|Mine|Archived&search=trecho
PATCH  /api/projects/{id}/status     { "status": "OnTrack|AtRisk|OffTrack|Paused|Completed" }
POST   /api/projects/{id}/archive    POST /api/projects/{id}/restore
```

* **Abas:** Todos os projetos (ativos em que sou dono ou membro), Meus projetos (ativos que criei) e Arquivados.
* **Busca** por trecho do nome, sem diferenciar maiúsculas, feita no banco (`lower(name) LIKE`, sem função específica do PostgreSQL na Application). No frontend a busca vive na URL (`?search=`), com espera de 250 ms entre digitar e consultar; a busca da barra superior leva ao diretório já filtrado.
* **Status do projeto** (No prazo, Em risco, Atrasado, Pausado, Concluído): informado pelo dono, com a data da última atualização (`StatusUpdatedAt`) exibida no diretório.
* **Arquivar:** o projeto sai das listas, do dashboard e do "Para você" e fica **somente leitura** (criar, alterar, mover, comentar e excluir tarefas respondem 400); nada é apagado e o dono pode restaurar. Estrela e "visto" continuam funcionando, por serem pessoais. Status e arquivamento são ações só do dono (403 para membros).
* **Sem projetos:** ao entrar em Projetos sem ter nenhum, abre o modal "Você ainda não tem projetos para mostrar", com "Criar projeto" e "Mais sobre como funcionam os projetos", que mostra o texto de ajuda (o que é um projeto, o que se faz nele, pessoas, status e diretório). O mesmo texto fica sempre acessível pelo botão "Como funcionam os projetos".

### Espaços

Um **espaço** fica um nível acima dos projetos: agrupa os projetos de uma mesma área ou time (ex.: "Marketing", "Engenharia").

```text
Espaço ──► Projeto ──► Tarefa
           (um projeto pode ficar sem espaço)
```

```http
GET    /api/spaces                 → espaços que criei ou que têm projeto a que tenho acesso
GET    /api/spaces/templates       → galeria de modelos
GET    /api/spaces/{id}            → espaço + projetos dele que consigo ver
POST   /api/spaces                 { "name", "description", "templateKey" }  → 201
PUT    /api/spaces/{id}            { "name", "description" }
DELETE /api/spaces/{id}            → 204
GET    /api/projects?spaceId=...   → diretório filtrado por espaço
```

`POST`/`PUT /api/projects` aceitam `spaceId` (nulo = sem espaço). Como o `PUT` substitui o projeto inteiro, omitir `spaceId` tira o projeto do espaço.

* **Projeto sem espaço continua válido** (`Project.SpaceId` é opcional): os projetos anteriores à funcionalidade seguem funcionando e podem ser movidos depois, pelo formulário de edição.
* **Acesso:**

| Ação | Dono do espaço | Quem participa de um projeto do espaço | Demais |
| --- | --- | --- | --- |
| Ver o espaço | sim, com todos os projetos | sim, só com os projetos a que tem acesso | não (404) |
| Editar ou excluir o espaço | sim | não (403) | não (404) |
| Colocar projeto no espaço | sim (projetos dele) | não | não |

* Não há "membros de espaço": o compartilhamento continua sendo por projeto. Um projeto só entra em espaço do próprio dono, então um espaço só contém projetos de quem o criou.
* **Excluir um espaço não apaga os projetos:** a FK usa `SET NULL` e eles voltam a ficar sem espaço.
* **Modelos:** catálogo fixo em código (`SpaceTemplateCatalog`): Espaço em branco, Kanban, Scrum, Solicitações de desenvolvimento, Roadmap de produto, Campanha de marketing, Integração de novas pessoas e Organização pessoal. Criar a partir de um modelo gera o espaço, os projetos e as tarefas iniciais (numeradas, com criador e evento de criação no histórico) em uma única transação. Um teste garante que todo texto do catálogo cabe nas regras do domínio.
* **Frontend:** `/spaces` (lista), `/spaces/new` (galeria no formato da tela de modelos do Jira: "Feitos para você" e categorias à esquerda, cartões à direita com a prévia dos projetos que serão criados) e `/spaces/:id` (projetos do espaço, "Novo projeto" já com o espaço escolhido). O menu ganhou a seção Espaços, com os recentes e o botão "+". O diretório de projetos ganhou a coluna Espaço, e o projeto dentro de um espaço volta para ele pelo link de retorno.
* **Para você:** "Espaços recomendados" mostra os 3 espaços mais recentes; quem ainda só tem projetos avulsos vê "Projetos recomendados".

### Metas

```http
GET    /api/goals
POST   /api/goals          { "name", "description", "targetDate", "status", "projectIds": [] }  → 201
PUT    /api/goals/{id}     (mesmo corpo; troca o conjunto de projetos ligados)
DELETE /api/goals/{id}     → 204
```

* Uma meta tem nome, descrição, data alvo (só a data) e situação, na mesma escala do status de projeto (No prazo, Em risco, Atrasado, Pausado, Concluído).
* **Progresso calculado:** a meta pode ser ligada a projetos (`GoalProjects`); `taskCount`/`doneCount` somam as tarefas desses projetos, contadas no banco. Sem projetos ligados, a meta existe com progresso zerado.
* **Pessoais:** cada usuário só vê e altera as próprias metas (404 para as de outros). Só dá para ligar projetos a que o usuário tem acesso; se ele perder o acesso a um projeto ligado, o projeto deixa de contar na meta.
* Excluir uma meta não altera os projetos; excluir um projeto desfaz a ligação e mantém a meta.
* No frontend (`/goals`): lista com situação, progresso, data alvo ("Venceu em..." quando passou sem estar concluída) e os projetos ligados; formulário de criar/editar com seleção de projetos.

### Dashboards

A seção "Dashboards" do menu tem o **Dashboard padrão** (`/dashboard`), em painéis no formato do dashboard do Jira: Introdução, Projetos (com o líder de cada um), Resumo das tarefas (por status e prioridade) e Atribuído a mim (tabela Tipo/Chave/Resumo/Prioridade, 15 por página, "1-15 de N"). O botão "Atualizar" recarrega tudo e cada painel mostra a hora da última atualização. Não exigiu backend novo: usa `/api/dashboard`, `/api/projects` e `/api/for-you?filter=Assigned`. Dashboards personalizados (vários, com painéis escolhidos pelo usuário) não foram implementados.

### Personalizar a barra lateral

O botão "Personalizar barra lateral" abre um diálogo com um interruptor por seção: Recentes, Com estrela, Projetos, Metas e Dashboards. "Para você" é fixa, para o menu nunca ficar vazio. A mudança vale na hora; esconder uma seção só a tira do menu, a página continua acessível pelo endereço. A preferência é de interface e fica no navegador (`localStorage`, uma chave por usuário): não acompanha a pessoa em outro aparelho. Passaria para a API se isso virasse necessidade.

"Recentes" e "Com estrela" são atalhos para as abas Visto e Com estrela da página Para você, cuja aba agora vive na URL (`/?filter=Viewed`).

### Tempo real (SignalR)

Quem está com o quadro de um projeto aberto vê na hora o que outra pessoa faz nele: card criado, movido, editado ou excluído, comentário, responsável, pessoas do projeto, status e arquivamento. O indicador "Ao vivo" aparece ao lado do progresso enquanto a conexão está ativa.

```text
WS /hubs/board          (JWT obrigatório)
  → JoinProject(projectId)                 entra no grupo do projeto (dono ou membro; senão erro)
  ← BoardChanged { projectId, taskId }     algo mudou no projeto
```

* **O aviso não leva dados.** O servidor só diz "mudou"; o cliente recarrega pela API REST, que já aplica as regras de acesso. Não existe um segundo caminho de leitura para proteger nem um segundo formato de dados para manter.
* **Camadas:** os services chamam `ILiveNotifier` (Application) depois do `SaveChanges`; a implementação com `IHubContext` fica na API. Falhar ao avisar só gera log: a alteração já foi gravada.
* **Autenticação:** mesmo JWT do REST. No WebSocket o navegador não envia o header `Authorization`, então o token vai na query (`access_token`), aceita só em `/hubs`; o nginx não registra essas URLs no log de acesso.
* **Frontend:** hook `useLive` (uma conexão por aba, compartilhada com o sino de notificações, com reconexão automática; ao reconectar entra de novo no grupo e recarrega). O modal da tarefa recarrega a atividade sem perder o que a pessoa está digitando. Sem conexão, o quadro funciona como antes, só não atualiza sozinho.
* **Limites conhecidos:** grupos em memória servem para uma instância da API (várias réplicas pedem backplane Redis); quem é removido do projeto continua no grupo até fechar a página, recebendo só ids (o recarregamento dele responde 404); quem fez a alteração também recebe o aviso e faz uma leitura a mais; a conexão aberta não é derrubada quando o token expira.
* **Testes:** `BoardHubTests` (aviso chega a quem entrou no quadro, quem não é do projeto não entra, hub exige autenticação).

### Notificações e menções

O sino da barra superior avisa quando outra pessoa faz algo que diz respeito a você. O contador de não lidas sobe na hora, sem recarregar a página.

| Tipo | Quando | Quem recebe |
| --- | --- | --- |
| `Assigned` | uma tarefa é atribuída | o novo responsável |
| `Mentioned` | um comentário traz `@Nome` | cada pessoa do projeto mencionada |
| `Commented` | um comentário é feito na tarefa | responsável e criador, se não foram mencionados |

```http
GET  /api/notifications            → { "unreadCount": 2, "items": [ ...30 mais recentes ] }
POST /api/notifications/{id}/read  → 204
POST /api/notifications/read-all   → 204
```

* **Ninguém é avisado do que ele mesmo fez.** Quem é mencionado recebe só a menção, não os dois avisos.
* **Mesma transação:** as notificações entram no `SaveChanges` da ação que as gera (`TaskService`); ou as duas coisas são gravadas, ou nenhuma.
* **Menção:** o texto do comentário contém `@` + nome de alguém do projeto, sem diferenciar maiúsculas. No formulário, digitar `@` sugere as pessoas do projeto (Enter ou Tab escolhe a primeira) e o nome aparece destacado no comentário.
* **Acesso:** cada pessoa só vê as próprias notificações, e só de projetos a que ainda tem acesso; a de outra pessoa responde 404. Excluir a tarefa apaga as notificações dela.
* **Tempo real:** toda conexão do hub entra no grupo da própria pessoa (`user:{id}`) e recebe o evento `Notified`, sem dados; o sino recarrega pela API.
* **Frontend:** o painel é um popover nativo (`popover`), que fecha com Esc ou clique fora. Clicar em um aviso abre a tarefa e marca como lido.
* **Limites conhecidos:** a menção casa pelo nome, então duas pessoas em que um nome contém o outro ("Ana" e "Ana Paula") são avisadas juntas; a sugestão de `@` só funciona no fim do texto; a lista traz as 30 mais recentes, sem paginação nem expiração; não há aviso por e-mail.
* **Testes:** `NotificationTests` (atribuição, menção e comentário, leitura, isolamento entre usuários, saída do projeto e chegada em tempo real).

### Busca global (Ctrl+K)

`Ctrl+K` (`⌘K` no Mac), ou o campo "Buscar" da barra superior, abre uma paleta que procura tarefas, projetos e espaços de uma vez. O teclado faz tudo: as setas escolhem o resultado, Enter abre e Esc fecha.

```http
GET /api/search?q=revis   → { "tasks": [...8], "projects": [...5], "spaces": [...5] }
```

* **O que casa:** um trecho do título da tarefa ou do nome do projeto ou espaço, sem diferenciar maiúsculas. Um número ("12", "#12") ou uma chave ("WEB-12") também acha a tarefa pelo número. A sigla é derivada do nome do projeto e não fica no banco, então a API casa o número e a paleta filtra pela sigla.
* **Ordem:** tarefas abertas antes das concluídas, depois as mais recentes; projetos ativos antes dos arquivados.
* **Acesso:** só o que a pessoa já pode ver (`AccessibleProjects` e `AccessibleSpaces`, agora em `ProjectAccess`). Projeto de outra pessoa não aparece.
* **Custo:** termo com menos de 2 letras (e sem número) não consulta a API, e a paleta espera 150 ms de pausa na digitação. Resposta de um termo antigo é descartada.
* **Acessibilidade:** padrão combobox + listbox (`aria-activedescendant`); o foco fica no campo enquanto as setas movem a opção ativa.
* **Limites conhecidos:** a busca não ignora acentos ("revisao" não acha "Revisão"); é `LIKE '%termo%'`, que varre as tabelas (com volume real, índice trigram `pg_trgm` ou full-text do PostgreSQL); a descrição da tarefa não entra na busca. A busca do diretório de projetos continua existindo na própria página.
* **Testes:** `SearchTests` (tarefas, projetos e espaços; busca por número e chave; termo curto; nada de projetos de outras pessoas).

### Testes de ponta a ponta (Playwright)

Quatro testes em `e2e/` usam o app como uma pessoa usaria, num Chromium de verdade, contra a stack do Docker Compose (nginx, API e PostgreSQL):

| Teste | Fluxo |
| --- | --- |
| `auth.spec.ts` | página protegida leva ao login; cadastro, sair, senha errada e login |
| `board.spec.ts` | criar projeto e tarefa, editar no detalhe, comentar, mover pelo botão e arrastando até Concluído, recarregar |
| `collaboration.spec.ts` | duas pessoas em contextos separados: card movido por uma aparece para a outra; menção `@` acende o sino e o aviso abre a tarefa |
| `search.spec.ts` | Ctrl+K, busca pela chave (`PDC-2`) e pelo nome, Enter abre a tarefa |

* **Preparação pela API:** cada teste cadastra os próprios usuários (e-mails únicos) e cria os dados pela API; a tela fica só para o que está sendo verificado. Por isso rodam em paralelo e contra um banco com dados.
* **Seletores por papel e nome acessível** (`getByRole`, `getByLabel`), os mesmos que um leitor de tela usa; uma regressão de acessibilidade também quebra o teste.
* **CI:** rodam no job Docker logo após o smoke test, contra as imagens recém-construídas; se falharem, o relatório com trace e screenshots é publicado como artefato.
* **Estabilidade:** 20 execuções seguidas (`--repeat-each=5`) sem falha.
* **Fora do escopo:** só Chromium (Firefox e WebKit ficam para quando houver motivo) e sem teste visual por screenshot.

### Tipos de tarefa, responsável na criação e subtarefas

Pedido do usuário: dizer por que o card existe (bug, integração, desenvolvimento...), com quem coordena o projeto decidindo quantos tipos houver; o card já nascer com responsável; e tarefas com subtarefas.

**Tipos de tarefa (por projeto).** Cada projeto tem os próprios tipos, com nome e cor. Todo projeto novo (e os que já existiam, pela migration) nasce com Desenvolvimento, Bug e Integração. As colunas do quadro continuam A fazer / Em andamento / Concluído: tipo e status são coisas diferentes.

```http
GET    /api/projects/{id}/task-types          (quem está no projeto)
POST   /api/projects/{id}/task-types          { "name": "Melhoria", "color": "#22A06B" }   (só o dono) → 201
PUT    /api/task-types/{id}                   { "name", "color" }   (só o dono)
DELETE /api/task-types/{id}                   (só o dono) → 204; as tarefas desse tipo ficam sem tipo
PATCH  /api/tasks/{id}/type                   { "typeId": "..." | null }   (quem está no projeto)
```

* Nome único no projeto, sem diferenciar maiúsculas (409); cor em `#RRGGBB` (400). Membro tentando gerenciar: 403. Tipo de outro projeto: 404.
* Trocar o tipo entra no histórico (`TypeChanged`) e avisa o quadro em tempo real.
* No site: botão "Tipos" no cabeçalho do projeto (só o dono) abre o diálogo para criar, renomear, mudar a cor e excluir; o card mostra o selo colorido; o detalhe tem o campo "Tipo".

**Responsável na criação.** `POST /api/projects/{id}/tasks` aceita `typeId`, `parentId` e `assignedToId`. O formulário "Nova tarefa" ganhou Tipo (padrão: o primeiro do projeto, ou "Sem tipo") e Responsável com "Atribuir a mim". Atribuir na criação gera o mesmo histórico e a mesma notificação de uma atribuição feita depois.

**Subtarefas.** Uma subtarefa é uma tarefa comum do mesmo projeto com `parentId`, então aparece no quadro e tem status, responsável, comentários e histórico próprios.

* Um nível só, como no Jira: subtarefa não tem subtarefas. Pai de outro projeto ou subtarefa como pai: 400.
* Tarefa que já existe vira subtarefa (ou volta a ser comum) pelo campo "Tarefa-pai" do detalhe: `PATCH /api/tasks/{id}/parent { "parentId": "..." | null }`. Quem já tem subtarefas não vira subtarefa (400, e o campo fica desabilitado). A mudança entra no histórico (`ParentChanged`).
* Excluir a tarefa-pai exclui as subtarefas (FK em cascata).
* No detalhe: seção "Subtarefas" com progresso ("1 de 3 concluídas"), lista com status e responsável, e "Criar subtarefa" pelo título (herda o tipo do pai). A subtarefa mostra o pai no caminho (Projeto / WEB-1 / WEB-4).
* No card: "1/3" para quem tem subtarefas e "↳ WEB-1" para quem é subtarefa.

* **Testes:** `TaskTypeAndSubtaskTests` (domínio: cor, tipos padrão, regras de subtarefa) e `TaskTypeTests` (API: padrões, permissões, conflito de nome, tipo trocado no histórico, tipo excluído, responsável e tipo de fora do projeto, subtarefas e cascata), mais o E2E `types-subtasks.spec.ts`.
* **Fora do escopo por ora:** colunas configuráveis por projeto.

### App Android

App companheiro em `mobile/taskflow-android`, feito para o uso do dia a dia no celular, não para repetir o site inteiro. Usa a mesma API e o mesmo hub de tempo real; o backend não mudou.

| Tela | O que faz |
| --- | --- |
| Login | entrar ou criar conta |
| Para você | abas Recomendado, Atribuído a mim e Com estrela |
| Projetos | lista com espaço e progresso |
| Quadro | uma coluna por vez em abas (A fazer, Em andamento, Concluído), mover pelos botões do card, selo do tipo, "1/2 subtarefas" e "↳ WEB-1", indicador "Ao vivo" |
| Tarefa | status, responsável ("Atribuir a mim"), tipo, tarefa-pai, subtarefas com progresso e criação de subtarefa pelo título, descrição e comentários com sugestão de `@` e menções destacadas |
| Notificações | lista com não lidas, contador na barra inferior, toque abre a tarefa |
| Tipos de tarefa | só para o dono (botão "Tipos" no quadro): criar, renomear, trocar a cor e excluir; a cor vem de uma paleta de 8 cores, porque o Android não tem seletor de cor nativo (uma cor escolhida no site e fora da paleta é mantida) |

* **Stack:** Kotlin, Jetpack Compose (Material 3), Navigation Compose, Retrofit + OkHttp + kotlinx.serialization, cliente SignalR para Java. MVVM com injeção de dependência manual (três objetos na `Application`), sem framework.
* **Tempo real:** uma conexão por app, aberta enquanto há sessão e refeita a cada 5 s se cair. Quadro e tarefa entram no grupo do projeto (contagem de quem está olhando, para sair só quando a última tela fecha). Os avisos não trazem dados: as telas recarregam pela API, como no site.
* **Sessão:** o token (JWT de 60 min) fica em SharedPreferences privadas, fora de backup (`allowBackup=false`). Um 401 encerra a sessão e volta ao login.
* **Rede:** release só HTTPS; o build debug libera HTTP para falar com o compose local. O endereço vem de `taskflow.apiUrl` em `gradle.properties` (padrão `http://10.0.2.2:3200/`, o localhost visto pelo emulador).
* **Mesmas regras do site:** a sigla do projeto e a chave da tarefa (`projectKey`), os nomes de status, as transições de uma coluna por vez e a regra de menção foram portados e têm testes com os mesmos casos.
* **Testes:** 13 testes JUnit (incluindo cor do tipo, paleta, mudança de tarefa-pai, criação de subtarefa e gerenciamento de tipos): `FormatTest` (sigla, transições, menções, iniciais) e `TaskFlowRepositoryTest` com MockWebServer (login guarda a sessão e manda o token, senha errada, token vencido encerra a sessão, mensagens de erro, corpo do PATCH). No CI, o job Android roda testes, lint e gera o APK de debug como artefato.
* **Verificação manual:** rodado no emulador (Pixel 6a, API 35) contra a stack local: login, Para você, quadro, card movido por outra pessoa aparecendo sozinho, comentário com menção que notifica a colega, e notificação nova chegando ao vivo.
* **Fora do escopo por ora:** criar projetos e tarefas soltas pelo app (subtarefas sim), espaços, metas, dashboards e busca; push notification com o app fechado (pede Firebase/FCM); conexão desligada em segundo plano (hoje fica aberta enquanto o processo vive).

### Layout

Barra superior fixa (marca, busca global Ctrl+K, botão "Criar", usuário) + menu lateral (Para você, Recentes, Com estrela, Espaços com os recentes, Projetos com os recentes, Metas, Dashboards). Rotas: `/` Para você, `/spaces` espaços, `/spaces/new` modelos, `/spaces/:id` espaço, `/projects` diretório, `/projects/:id` quadro, `/projects/:id/tasks/:taskId` detalhe da tarefa, `/goals` metas, `/dashboard` dashboard padrão. Paleta: fundo branco, neutros frios e azul como único acento; ícones de projeto com cor derivada do nome.

Comentários:

```http
GET    /api/tasks/{id}/comments     (mais recente primeiro)
POST   /api/tasks/{id}/comments     { "body": "..." }   → 201
DELETE /api/comments/{id}           → 204
```

* Texto de 1 a 2000 caracteres (validado no domínio). Acesso igual ao da tarefa: só em projetos do usuário.
* Só o autor exclui o próprio comentário; comentário de outra pessoa responde 404. Comentários são excluídos junto com a tarefa.
* No frontend ficam na seção "Atividade" do detalhe da tarefa, em uma única linha do tempo com o histórico de alterações (como a aba "Tudo" do Jira). Ctrl+Enter envia; Esc limpa o rascunho sem fechar o modal. O texto é renderizado como texto puro (sem HTML), preservando quebras de linha.
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
* [x] CI executar build e testes (GitHub Actions, ver seção 46)

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
* [x] GitHub (no lugar do GitLab)
* [x] GitHub Actions CI (no lugar do GitLab CI/CD)
* [x] Container Registry (ghcr.io)
* [ ] Deploy

## Cloud

* [ ] AWS / DigitalOcean
* [ ] HTTPS
* [ ] Domain
* [ ] Backup

## Observability

* [x] Health Checks
* [x] Structured Logs
* [x] Prometheus
* [x] Grafana

## Advanced

* [ ] MongoDB
* [x] Audit Events
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
