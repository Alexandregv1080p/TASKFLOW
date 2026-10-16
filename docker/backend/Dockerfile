# Build context: raiz do repositório (docker compose já usa ".").

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Só os .csproj primeiro: o restore fica em cache enquanto as dependências não mudarem.
COPY backend/TaskFlow.Api/TaskFlow.Api.csproj backend/TaskFlow.Api/
COPY backend/TaskFlow.Application/TaskFlow.Application.csproj backend/TaskFlow.Application/
COPY backend/TaskFlow.Domain/TaskFlow.Domain.csproj backend/TaskFlow.Domain/
COPY backend/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj backend/TaskFlow.Infrastructure/
RUN dotnet restore backend/TaskFlow.Api/TaskFlow.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime enxuto (sem SDK), rodando como usuário não-root da própria imagem.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Pasta dos anexos (montada como volume no compose) pertencendo ao usuário da aplicação: um volume novo
# herda dono e permissões desta pasta, e a API grava nela sem rodar como root.
RUN mkdir -p /app/data && chown $APP_UID /app/data

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]
