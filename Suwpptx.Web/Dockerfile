# Build context is the repo root: docker build -f Suwpptx.Web/Dockerfile -t suwpptx-web:local .
FROM docker.io/library/node:24.11-alpine3.22 AS builder
ENV COREPACK_ENABLE_DOWNLOAD_PROMPT=0
WORKDIR /app
COPY Suwpptx.Web/package.json Suwpptx.Web/pnpm-lock.yaml Suwpptx.Web/pnpm-workspace.yaml ./
RUN corepack enable && pnpm install --frozen-lockfile
COPY Suwpptx.Web/ ./
RUN pnpm build

FROM docker.io/nginxinc/nginx-unprivileged:1.29.3-alpine
ENV NGINX_ENVSUBST_FILTER="^(API_UPSTREAM|NGINX_LOCAL_RESOLVERS)$"
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
ENV API_UPSTREAM="http://server:6310"
COPY --from=builder /app/dist /usr/share/nginx/html
COPY Suwpptx.Web/nginx.conf.template /etc/nginx/templates/default.conf.template
EXPOSE 8080
