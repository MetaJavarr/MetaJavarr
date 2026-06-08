FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim

LABEL org.opencontainers.image.title="MetaJavarr"
LABEL org.opencontainers.image.description="MetaJavarr is a Radarr fork for JAV libraries backed by MetaTube metadata."
LABEL org.opencontainers.image.source="https://github.com/MetaJavarr/MetaJavarr"
LABEL org.opencontainers.image.licenses="GPL-3.0"

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    XDG_CONFIG_HOME=/config \
    TMPDIR=/tmp

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        libicu72 \
        tzdata \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app/metajavarr

COPY _artifacts/linux-x64/net8.0/Radarr/ ./

RUN chmod +x /app/metajavarr/Radarr

USER 1000:1000

VOLUME ["/config", "/downloads", "/data/movies"]
EXPOSE 7878

ENTRYPOINT ["/app/metajavarr/Radarr", "-nobrowser", "-data=/config"]
