FROM node:22-bookworm-slim AS frontend-build
WORKDIR /src/frontend
COPY src/frontend/package.json src/frontend/pnpm-lock.yaml src/frontend/pnpm-workspace.yaml ./
RUN corepack enable && corepack prepare pnpm@10.18.0 --activate && pnpm install --frozen-lockfile
COPY src/frontend/ ./
RUN pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0-bookworm-slim AS backend-build
WORKDIR /src
COPY src/backend/ ./backend/
RUN dotnet restore backend/SalesManagement.sln
RUN dotnet publish backend/SalesManagement.Api/SalesManagement.Api.csproj -c Release --no-restore -o /publish/api \
    && dotnet publish backend/SalesManagement.Setup/SalesManagement.Setup.csproj -c Release --no-restore -o /publish/setup

FROM mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=backend-build /publish/api/ /app/
COPY --from=backend-build /publish/setup/ /setup/
COPY --from=frontend-build /src/frontend/dist/ /app/wwwroot/
COPY deploy/render/start.sh /usr/local/bin/switch-korea-start
RUN chmod 0555 /usr/local/bin/switch-korea-start
USER $APP_UID
ENTRYPOINT ["/usr/local/bin/switch-korea-start"]
