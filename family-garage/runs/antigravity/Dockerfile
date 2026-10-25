# FamilyGarage image. One image for every database provider that was scaffolded - `Provider=Sqlite|SqlServer|PostgreSql`
# is chosen at runtime. Build from this folder:
#   docker build -t kandra-familygarage .
#   docker buildx build --platform linux/amd64,linux/arm64 --build-arg VERSION=1.0.0 ...
# Packages restore anonymously from the public feed in nuget.config; no token is needed.
# The compose files in this folder build this image for you (docker compose -f compose.sqlite.yml up -d --build).

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
# Overrides the exact Kandra Core pin in Directory.Build.props (leave empty to build with the pin).
ARG KANDRA_CORE_VERSION=
ARG VERSION=0.0.0
WORKDIR /src
COPY . .
RUN arch="$([ "$TARGETARCH" = "arm64" ] && echo arm64 || echo x64)" \
 && dotnet publish src/FamilyGarage.WebApi/FamilyGarage.WebApi.csproj -c Release -a "$arch" --no-self-contained -o /app \
      ${KANDRA_CORE_VERSION:+-p:KandraCoreVersion=$KANDRA_CORE_VERSION} \
      -p:InformationalVersion=$VERSION -p:UseAppHost=false
# The Kandra engine's licence texts travel with it: the agreement (EULA 4.2: pass it on when you ship the engine) and the
# third-party notices, taken from the engine package that was just restored, so they always match the pinned version.
RUN mkdir /licenses \
 && for v in /root/.nuget/packages/kandra.api.lib/*/; do cp "$v"EULA.md "$v"EULA.uk.md "$v"THIRD-PARTY-NOTICES.md /licenses/; done

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG VERSION=0.0.0
LABEL org.opencontainers.image.title="kandra-familygarage" \
      org.opencontainers.image.version="$VERSION"
# Add your own labels here: org.opencontainers.image.source, .vendor, .licenses (the licence of your configuration;
# /licenses already holds the Kandra engine's agreement and third-party notices).

# Data folder (database file for SQLite, uploaded files, signing key, backups): Kandra:DataPath.
# Owned by the non-root `app` user (uid 1654 in the official image).
RUN mkdir /data && chown app:app /data
ENV Kandra__DataPath=/data \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_gcServer=0
VOLUME /data

WORKDIR /app
COPY --from=build /app .
COPY --from=build /licenses /licenses
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "FamilyGarage.WebApi.dll"]
