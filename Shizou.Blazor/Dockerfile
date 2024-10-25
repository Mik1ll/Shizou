FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
EXPOSE 80
EXPOSE 443
USER 0:0
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS publish
ARG TARGETARCH

RUN dotnet tool install Microsoft.Web.LibraryManager.Cli --tool-path /dotnet-tools

RUN apt-get -q -y update && apt-get -q -y install jq moreutils busybox xz-utils

ADD --link --checksum=sha256:6689420D7073C57D9FAF19764E2A92F53C84D3EA66BE402FD75E5419E2F0B38F https://cdn.anidb.net/client/avdump3/avdump3_8293_stable.zip /download/avdump3.zip
ADD --link /download/AVDump3.zip /AVDump3/

WORKDIR /src/dotnet-runtime-6
RUN <<EOF
case ${TARGETARCH} in
  amd64) curl -sL https://download.visualstudio.microsoft.com/download/pr/a80ab89d-9f64-47b9-bba5-907a4cdaf457/c5714a6e605ef86293a5145d8ea72f39/dotnet-runtime-6.0.30-linux-x64.tar.gz | tar -xz ./shared
    ;;
  arm64) curl -sL https://download.visualstudio.microsoft.com/download/pr/27a21bdd-cad5-4f5a-b7a3-86942632a745/3d7aba7c0cfe0c28342a8f83b65e72b9/dotnet-runtime-6.0.30-linux-arm64.tar.gz | tar -xz ./shared    
    ;;
  *) echo Architecture not supported
    exit 1
    ;;
esac
EOF

WORKDIR /src/ffmpeg
RUN curl -sL https://johnvansickle.com/ffmpeg/releases/ffmpeg-7.0-${TARGETARCH}-static.tar.xz | tar -xJ --strip-components=1

WORKDIR /src
COPY NuGet.Config Shizou.sln ./
COPY RHashWrapper RHashWrapper/
# Required MSBuild property due to bug https://github.com/dotnet/sdk/issues/10335
RUN dotnet pack RHashWrapper/RHashWrapper.csproj -p:GeneratePackageOnBuild=false
COPY Shizou.Data/Shizou.Data.csproj Shizou.Data/
COPY Shizou.Server/Shizou.Server.csproj Shizou.Server/
COPY Shizou.Blazor/Shizou.Blazor.csproj Shizou.Blazor/libman.json Shizou.Blazor/
# Libman requires json in working directory
WORKDIR Shizou.Blazor
RUN /dotnet-tools/libman restore >/dev/null
RUN dotnet restore Shizou.Blazor.csproj -a $TARGETARCH
COPY Shizou.Data ../Shizou.Data/
COPY Shizou.Server ../Shizou.Server/
COPY Shizou.Blazor ../Shizou.Blazor/
RUN dotnet publish Shizou.Blazor.csproj --no-restore -c Release -a $TARGETARCH --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /src/ffmpeg/ffprobe /src/ffmpeg/ffmpeg ./
COPY --from=publish --link /AVDump3 AVDump3/
COPY --from=publish /src/dotnet-runtime-6 /usr/share/dotnet/
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "Shizou.Blazor.dll"]
