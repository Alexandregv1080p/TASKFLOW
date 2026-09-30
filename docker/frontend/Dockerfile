# Build context: raiz do repositório (docker compose já usa ".").

FROM node:20-alpine AS build
WORKDIR /app

COPY frontend/taskflow-web/package.json frontend/taskflow-web/package-lock.json ./
RUN npm ci

COPY frontend/taskflow-web/ ./
RUN npm run build

# Nginx sem root servindo os arquivos estáticos e fazendo proxy de /api para a API.
FROM nginxinc/nginx-unprivileged:1.27-alpine
COPY docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
