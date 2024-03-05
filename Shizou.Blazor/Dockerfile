FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou
RUN apk update
RUN apk add rhash-libs=1.4.4-r0 ffmpeg

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS publish
ARG TARGETARCH
WORKDIR /src
COPY ["Shizou.Blazor/Shizou.Blazor.csproj", "Shizou.Blazor/"]
COPY ["Shizou.Data/Shizou.Data.csproj", "Shizou.Data/"]
COPY ["Shizou.Server/Shizou.Server.csproj", "Shizou.Server/"]
RUN dotnet restore "Shizou.Blazor/Shizou.Blazor.csproj" -a $TARGETARCH
COPY . .
WORKDIR "/src/Shizou.Blazor"
RUN dotnet publish "Shizou.Blazor.csproj" -c Release -a $TARGETARCH --no-restore -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN ln -fs /usr/lib/librhash.so.1.4.4 /app/RHash/librhash.so
ENTRYPOINT ["./Shizou.Blazor"]
