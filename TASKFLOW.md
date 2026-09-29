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
* `Status`: TaskStatus
* `Priority`: TaskPriority
* `DueDate`: DateTime?
* `ProjectId`: Guid
* `AssignedToId`: Guid?
* `CreatedAt`: DateTime

---

# 12. Status das tarefas

Criar o enum:

```text
TaskStatus
```

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

task.ChangeStatus(TaskStatus.InProgress);

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

Estrutura:

```text
TaskFlow.Application/
│
├── Projects/
│   ├── CreateProject/
│   ├── GetProject/
│   ├── GetProjects/
│   ├── UpdateProject/
│   └── DeleteProject/
│
├── Tasks/
│   ├── CreateTask/
│   ├── GetTask/
│   ├── GetTasks/
│   ├── UpdateTask/
│   ├── DeleteTask/
│   ├── ChangeTaskStatus/
│   └── AssignTask/
│
└── Users/
    ├── CreateUser/
    └── GetUser/
```

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

Posteriormente o health check poderá verificar:

* API
* PostgreSQL
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

Posteriormente adicionar:

```text
taskflow-api
taskflow-frontend
```

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

* [ ] Criar User
* [ ] Criar Project
* [ ] Criar TaskItem
* [ ] Criar TaskStatus
* [ ] Criar TaskPriority
* [ ] Implementar regras básicas
* [ ] Criar relacionamentos

---

# Fase 3 — Infrastructure

* [ ] Instalar EF Core
* [ ] Criar DbContext
* [ ] Configurar entidades
* [ ] Configurar PostgreSQL
* [ ] Criar migrations
* [ ] Criar banco
* [ ] Testar persistência

---

# Fase 4 — Application

* [ ] Criar DTOs
* [ ] Criar CreateProject
* [ ] Criar GetProjects
* [ ] Criar GetProject
* [ ] Criar UpdateProject
* [ ] Criar DeleteProject
* [ ] Criar CreateTask
* [ ] Criar GetTasks
* [ ] Criar UpdateTask
* [ ] Criar DeleteTask
* [ ] Criar ChangeTaskStatus
* [ ] Criar AssignTask

---

# Fase 5 — API

* [ ] Criar ProjectsController
* [ ] Criar TasksController
* [ ] Configurar Dependency Injection
* [ ] Configurar Swagger
* [ ] Configurar middleware
* [ ] Configurar validation
* [ ] Criar health check
* [ ] Testar endpoints

---

# Fase 6 — Frontend

* [ ] Criar React + TypeScript
* [ ] Configurar Vite
* [ ] Criar layout
* [ ] Criar Dashboard
* [ ] Criar Projects
* [ ] Criar Project Details
* [ ] Criar Task Card
* [ ] Criar Kanban
* [ ] Integrar API
* [ ] Implementar loading states
* [ ] Implementar error states

---

# Fase 7 — Authentication

* [ ] Criar User
* [ ] Criar Register
* [ ] Criar Login
* [ ] Password hashing
* [ ] JWT
* [ ] Authentication middleware
* [ ] Authorization
* [ ] Proteger endpoints

---

# Fase 8 — Tests

* [ ] Criar Unit Tests
* [ ] Criar Integration Tests
* [ ] Testar Projects
* [ ] Testar Tasks
* [ ] Testar Authentication
* [ ] Testar validações
* [ ] Testar autorização

---

# Fase 9 — Docker

* [ ] Dockerfile backend
* [ ] Dockerfile frontend
* [ ] PostgreSQL container
* [ ] Docker Compose
* [ ] Networks
* [ ] Volumes
* [ ] Environment variables
* [ ] Testar aplicação completa via Docker

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

* [ ] PostgreSQL estiver funcionando via Docker
* [ ] API ASP.NET Core estiver funcionando
* [ ] Entity Framework estiver configurado
* [ ] CRUD de projetos estiver funcionando
* [ ] CRUD de tarefas estiver funcionando
* [ ] Alteração de status estiver funcionando
* [ ] React estiver funcionando
* [ ] React estiver consumindo a API
* [ ] Dashboard estiver funcionando
* [ ] Kanban estiver funcionando
* [ ] Testes básicos estiverem implementados
* [ ] Docker Compose executar a aplicação
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
* [ ] Domain
* [ ] Application
* [ ] Infrastructure
* [ ] API
* [ ] PostgreSQL
* [ ] EF Core
* [ ] Migrations
* [ ] Projects CRUD
* [ ] Tasks CRUD
* [ ] Authentication
* [ ] Authorization

## Frontend

* [ ] React
* [ ] TypeScript
* [ ] Dashboard
* [ ] Projects
* [ ] Kanban
* [ ] API integration
* [ ] Authentication

## Tests

* [ ] Unit Tests
* [ ] Integration Tests

## DevOps

* [ ] Docker
* [ ] Docker Compose
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

* [ ] Health Checks
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
