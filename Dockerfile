FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
EXPOSE 8080
EXPOSE 8443
USER 1654
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM scratch AS shared-dl
ADD --link --checksum=sha256:6689420d7073c57d9faf19764e2a92f53c84d3ea66be402fd75e5419e2f0b38f \
    ["https://cdn.anidb.net/client/avdump3/avdump3_8293_stable.zip", "/AVDump3.zip"]

FROM shared-dl AS amd64-dl
ADD --link --checksum=sha256:c7222162d145e2c3e64b3440d962c54a549b1d903abdf70deb74fe61cae74db8 \
    ["https://download.visualstudio.microsoft.com/download/pr/79e3d66e-14b8-4c20-9816-37c0c0964c8c/98ed84be388dfa1a7db279e9beefbee8/dotnet-runtime-6.0.35-linux-x64.tar.gz", "dotnet.tar.gz"]
ADD --link --checksum=sha256:abda8d77ce8309141f83ab8edf0596834087c52467f6badf376a6a2a4c87cf67 \
    ["https://johnvansickle.com/ffmpeg/releases/ffmpeg-7.0.2-amd64-static.tar.xz", "ffmpeg.tar.xz"]

FROM shared-dl AS arm64-dl
ADD --link --checksum=sha256:e88ff831cc6ee7535db7eb534ae4fe4e5f795e3cf9b914b5b874b29105c204d6 \
    ["https://download.visualstudio.microsoft.com/download/pr/8f344652-6b7e-4136-b6ca-c1a46d998835/e00bad479ac747a8ddc90e7d006aaa52/dotnet-runtime-6.0.35-linux-arm64.tar.gz", "dotnet.tar.gz"]
ADD --link --checksum=sha256:f4149bb2b0784e30e99bdda85471c9b5930d3402014e934a5098b41d0f7201b1 \
    ["https://johnvansickle.com/ffmpeg/releases/ffmpeg-7.0.2-arm64-static.tar.xz", "ffmpeg.tar.xz"]

FROM ${TARGETARCH}-dl AS dl

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS publish
ARG TARGETARCH

RUN apt-get update && apt-get install -y --no-install-recommends xz-utils unzip
RUN --mount=from=dl,dst=/dl mkdir /dotnet-runtime-6 /ffmpeg \
&& unzip /dl/AVDump3.zip -d /AVDump3 \
&& tar -oxzf /dl/dotnet.tar.gz -C /dotnet-runtime-6 ./shared/Microsoft.NETCore.App \
&& tar -oxJf /dl/ffmpeg.tar.xz --strip-components=1 --wildcards -C /ffmpeg "*/ffprobe" "*/ffmpeg"

WORKDIR /src/Shizou.Blazor
COPY Shizou.Blazor/.config/dotnet-tools.json .config/
RUN dotnet tool restore

WORKDIR /src
COPY NuGet.Config Shizou.sln ./
COPY Shizou.Data/Shizou.Data.csproj Shizou.Data/
COPY Shizou.Server/Shizou.Server.csproj Shizou.Server/
COPY Shizou.Blazor/Shizou.Blazor.csproj Shizou.Blazor/libman.json Shizou.Blazor/
# Libman requires json in working directory
WORKDIR /src/Shizou.Blazor
RUN dotnet libman restore >/dev/null && dotnet restore Shizou.Blazor.csproj -a $TARGETARCH
COPY Shizou.Data ../Shizou.Data/
COPY Shizou.Server ../Shizou.Server/
COPY Shizou.Blazor ../Shizou.Blazor/
RUN dotnet publish Shizou.Blazor.csproj --no-restore -c Release -a $TARGETARCH --no-self-contained -o /app/publish

WORKDIR /src/Shizou.HealthChecker
RUN --mount=source=Shizou.HealthChecker,dst=/src/Shizou.HealthChecker \
    dotnet publish --no-restore -c Release -a $TARGETARCH --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish --link /ffmpeg ./
COPY --from=publish --link /AVDump3 AVDump3/
COPY --from=publish --link /dotnet-runtime-6 /usr/share/dotnet/
COPY --from=publish --link /app/publish ./
HEALTHCHECK CMD ["dotnet", "/app/Shizou.HealthChecker.dll", "https://localhost:8443/healthz"]
ENTRYPOINT ["dotnet", "/app/Shizou.Blazor.dll"]
