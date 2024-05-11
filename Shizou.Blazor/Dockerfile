FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
EXPOSE 80
EXPOSE 443
USER 0:0
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS publish
ARG TARGETARCH

RUN apt-get -q -y update && apt-get -q -y install make gcc libssl-dev jq moreutils busybox xz-utils

WORKDIR "/src/RHash"
RUN wget -qO- "https://github.com/rhash/RHash/archive/refs/tags/v1.4.4.tar.gz" | tar -xz --strip-components=1 && \
    ./configure --disable-gettext --enable-openssl=runtime --enable-lib-shared && \
    make lib-shared

WORKDIR "/src/AVDump3"
RUN wget -qO- "https://cdn.anidb.net/client/avdump3/avdump3_8293_stable.zip" | busybox unzip -oq - && \
    jq '.runtimeOptions.framework.rollForward = "Major"' "AVDump3CL.runtimeconfig.json" | sponge "AVDump3CL.runtimeconfig.json"

WORKDIR "/src/ffmpeg"
RUN wget -qO- "https://johnvansickle.com/ffmpeg/releases/ffmpeg-6.1-amd64-static.tar.xz" | tar -xJ --strip-components=1

WORKDIR "/src"
COPY "Shizou.Data/Shizou.Data.csproj" "Shizou.Data/"
COPY "Shizou.Server/Shizou.Server.csproj" "Shizou.Server/"
COPY "Shizou.Blazor/Shizou.Blazor.csproj" "Shizou.Blazor/"
RUN dotnet restore "Shizou.Blazor/Shizou.Blazor.csproj" -a $TARGETARCH
COPY "." "."
WORKDIR "/src/Shizou.Blazor"
RUN dotnet publish "Shizou.Blazor.csproj" -c Release -a $TARGETARCH --no-restore -o "/app/publish"

RUN cp "/src/RHash/librhash/librhash.so.1.4.4" "/app/publish/RHash/librhash.so"
RUN cp -r "/src/AVDump3" "/app/publish/"
RUN cp "/src/ffmpeg/ffprobe" "/src/ffmpeg/ffmpeg" "/app/publish/"

FROM base AS final
WORKDIR "/app"
COPY --from=publish "/app/publish" "."
ENTRYPOINT ["./Shizou.Blazor"]
