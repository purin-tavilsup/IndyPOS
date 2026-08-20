# Build context is the REPOSITORY ROOT, not this directory:
#   docker build -f src/IndyPOS.CloudApi/Dockerfile -t indypos-cloudapi:local .
# CloudApi references Application (-> Domain) and ServiceDefaults, and inherits
# Directory.Build.props, none of which are reachable from src/IndyPOS.CloudApi/.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer: project files only, so editing a .cs file does not re-restore.
COPY Directory.Build.props ./
COPY src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj src/IndyPOS.CloudApi/
COPY src/IndyPOS.Application/IndyPOS.Application.csproj src/IndyPOS.Application/
COPY src/IndyPOS.Domain/IndyPOS.Domain.csproj src/IndyPOS.Domain/
COPY src/IndyPOS.ServiceDefaults/IndyPOS.ServiceDefaults.csproj src/IndyPOS.ServiceDefaults/
RUN dotnet restore src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj

COPY src/IndyPOS.CloudApi/ src/IndyPOS.CloudApi/
COPY src/IndyPOS.Application/ src/IndyPOS.Application/
COPY src/IndyPOS.Domain/ src/IndyPOS.Domain/
COPY src/IndyPOS.ServiceDefaults/ src/IndyPOS.ServiceDefaults/
RUN dotnet publish src/IndyPOS.CloudApi/IndyPOS.CloudApi.csproj \
      -c Release -o /app/publish --no-restore

# Debian, not Alpine: this system converts timestamps to and from Asia/Bangkok and handles Thai
# product names, and the Alpine images ship neither ICU nor tzdata without extra packages.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# curl is the probe binary for HEALTHCHECK and for compose's service_healthy condition. The aspnet
# image ships neither curl nor wget, which is also why the chiseled variants are unusable here.
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Non-root. The aspnet images define APP_UID for exactly this.
USER $APP_UID

# /health/live, not /health/ready: it is the cheap probe by design, and it is mapped exactly once
# (see defect I0-C in the spec).
HEALTHCHECK --interval=15s --timeout=3s --start-period=20s --retries=3 \
  CMD curl -fsS http://localhost:8080/health/live || exit 1

# Entrypoint without the command, so "migrate" can be passed as an argument by the compose one-shot
# rather than needing a second image.
ENTRYPOINT ["dotnet", "IndyPOS.CloudApi.dll"]
