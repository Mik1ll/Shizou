FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
EXPOSE 8080
EXPOSE 8443
# Should be 1654
USER $APP_UID
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS publish
ARG TARGETARCH

RUN dotnet tool install Microsoft.Web.LibraryManager.Cli --tool-path /dotnet-tools

RUN apt-get update && apt-get install -y --no-install-recommends xz-utils unzip

ADD --link --checksum=sha256:6689420d7073c57d9faf19764e2a92f53c84d3ea66be402fd75e5419e2f0b38f https://cdn.anidb.net/client/avdump3/avdump3_8293_stable.zip /AVDump3.zip
RUN unzip AVDump3.zip -d /AVDump3 \
    && rm AVDump3.zip

# Release list here https://github.com/dotnet/core/blob/main/release-notes/6.0/releases.json
RUN dotnet_base='https://download.visualstudio.microsoft.com/download/pr/' \
    && case ${TARGETARCH} in \
      amd64) dotnet_url="${dotnet_base}79e3d66e-14b8-4c20-9816-37c0c0964c8c/98ed84be388dfa1a7db279e9beefbee8/dotnet-runtime-6.0.35-linux-x64.tar.gz" \
        && dotnet_sha512='d8d10d600fb664336949576f8ec0534dbffd573f754b9e741f20812221fafcac5f509a7e1ab44e9e63fc31a7b5dbcb19e4ec1930ffd29312212dc7454977090e' ;; \
      arm64) dotnet_url="${dotnet_base}8f344652-6b7e-4136-b6ca-c1a46d998835/e00bad479ac747a8ddc90e7d006aaa52/dotnet-runtime-6.0.35-linux-arm64.tar.gz" \
        && dotnet_sha512='945e24f9c2d677e65fddaa06cafe8d518ee599ce98883b60fd9d734320fa2f3e1ccbfb46ea26ee925e319fb5430c2e18d64269fdae96030169c4b6d3d811ea77' ;; \
      *) echo 'Architecture not supported' ; exit 1 ;; \
    esac \
    && curl -fSL -o dotnet.tar.gz "$dotnet_url" \
    && echo "$dotnet_sha512 dotnet.tar.gz" | sha512sum -c - \
    && mkdir /dotnet-runtime-6 \
    && tar -oxzf dotnet.tar.gz -C /dotnet-runtime-6 ./shared/Microsoft.NETCore.App \
    && rm dotnet.tar.gz

RUN case ${TARGETARCH} in \
      amd64) ffmpeg_md5='7fa72b652e19bf84c9461e332ea1cdf3' ;; \
      arm64) ffmpeg_md5='807afe21601db0a73e426121c7d636ea' ;; \
      *) echo 'Architecture not supported' ; exit 1 ;; \
    esac \
    && curl -fSL -o ffmpeg.tar.xz "https://johnvansickle.com/ffmpeg/releases/ffmpeg-7.0.2-${TARGETARCH}-static.tar.xz" \
    && echo "$ffmpeg_md5 ffmpeg.tar.xz" | md5sum -c - \
    && mkdir /ffmpeg \
    && tar -oxJf ffmpeg.tar.xz --strip-components=1 -C /ffmpeg "ffmpeg-7.0.2-${TARGETARCH}-static/ffprobe" "ffmpeg-7.0.2-${TARGETARCH}-static/ffmpeg" \
    && rm ffmpeg.tar.xz

WORKDIR /src
COPY NuGet.Config Shizou.sln ./
COPY Shizou.Data/Shizou.Data.csproj Shizou.Data/
COPY Shizou.Server/Shizou.Server.csproj Shizou.Server/
COPY Shizou.Blazor/Shizou.Blazor.csproj Shizou.Blazor/libman.json Shizou.Blazor/
# Libman requires json in working directory
WORKDIR /src/Shizou.Blazor
RUN /dotnet-tools/libman restore >/dev/null
RUN dotnet restore Shizou.Blazor.csproj -a $TARGETARCH
COPY Shizou.Data ../Shizou.Data/
COPY Shizou.Server ../Shizou.Server/
COPY Shizou.Blazor ../Shizou.Blazor/
RUN dotnet publish Shizou.Blazor.csproj --no-restore -c Release -a $TARGETARCH --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish --link /ffmpeg ./
COPY --from=publish --link /AVDump3 AVDump3/
COPY --from=publish --link /dotnet-runtime-6/shared /usr/share/dotnet/shared/
COPY --from=publish --link /app/publish ./
ENTRYPOINT ["dotnet", "Shizou.Blazor.dll"]
