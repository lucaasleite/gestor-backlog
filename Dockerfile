# Stage 1: build do Angular (gera os arquivos direto em backend/GestorDeBacklogs.Api/wwwroot)
FROM node:20-alpine AS frontend-build
WORKDIR /src
COPY frontend/gestor-backlogs-ui/package*.json frontend/gestor-backlogs-ui/
RUN cd frontend/gestor-backlogs-ui && npm ci
COPY frontend/gestor-backlogs-ui frontend/gestor-backlogs-ui
RUN mkdir -p backend/GestorDeBacklogs.Api/wwwroot
RUN cd frontend/gestor-backlogs-ui && npm run build

# Stage 2: build/publish da API (.NET), com o wwwroot gerado acima embutido
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY backend/GestorDeBacklogs.sln backend/
COPY backend/GestorDeBacklogs.Api/GestorDeBacklogs.Api.csproj backend/GestorDeBacklogs.Api/
COPY backend/GestorDeBacklogs.Api.Tests/GestorDeBacklogs.Api.Tests.csproj backend/GestorDeBacklogs.Api.Tests/
RUN dotnet restore backend/GestorDeBacklogs.sln
COPY backend/ backend/
COPY --from=frontend-build /src/backend/GestorDeBacklogs.Api/wwwroot backend/GestorDeBacklogs.Api/wwwroot
RUN dotnet publish backend/GestorDeBacklogs.Api/GestorDeBacklogs.Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: imagem final, só o runtime do ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=backend-build /app/publish .

# Necessário pro modo de autenticação "Azure CLI": AzureCliCredential chama o binário "az"
# pra pegar um token a partir da sessão de "az login" feita no host (montada em /root/.azure
# via volume no docker-compose.yml).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates apt-transport-https gnupg lsb-release \
    && curl -sL https://aka.ms/InstallAzureCLIDeb | bash \
    && apt-get purge -y --auto-remove gnupg lsb-release \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "GestorDeBacklogs.Api.dll"]
