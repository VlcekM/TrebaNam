# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# 1) Klient - staticky SvelteKit export (adapter-static -> build/)
# ---------------------------------------------------------------------------
FROM node:24-alpine AS client
WORKDIR /src/client

COPY TrebaNam.Client/package.json TrebaNam.Client/package-lock.json ./
RUN npm ci

COPY TrebaNam.Client/ ./
RUN npm run build

# ---------------------------------------------------------------------------
# 2) API - publish .NET 10
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src

COPY TrebaNam.API/TrebaNam.API.csproj TrebaNam.API/
RUN dotnet restore TrebaNam.API/TrebaNam.API.csproj

COPY TrebaNam.API/ TrebaNam.API/
RUN dotnet publish TrebaNam.API/TrebaNam.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------
# 3) GSSAPI - chiseled image nema apt, kniznice pre Npgsql beriem odtialto
# ---------------------------------------------------------------------------
FROM ubuntu:noble AS krb5
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && mkdir /krb5 \
    && cp -L /usr/lib/*-linux-gnu/libgssapi_krb5.so.2 \
             /usr/lib/*-linux-gnu/libkrb5.so.3 \
             /usr/lib/*-linux-gnu/libk5crypto.so.3 \
             /usr/lib/*-linux-gnu/libcom_err.so.2 \
             /usr/lib/*-linux-gnu/libkrb5support.so.0 \
             /usr/lib/*-linux-gnu/libkeyutils.so.1 \
             /krb5/

# ---------------------------------------------------------------------------
# 4) Runtime - API servuje aj predgenerovaneho klienta z wwwroot
#
# Chiseled (distroless) obraz: ziadny shell ani package manager, bezi pod
# neprivilegovanym uid. Variant "-extra" obsahuje tzdata - bez neho by
# TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava") spadlo.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app

COPY --from=krb5 /krb5/ /usr/local/lib/
ENV LD_LIBRARY_PATH=/usr/local/lib

COPY --from=api /app/publish ./
COPY --from=client /src/client/build ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "TrebaNam.API.dll"]
