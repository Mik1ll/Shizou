FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
EXPOSE 80
EXPOSE 443
USER 0:0
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS publish
ARG TARGETARCH

WORKDIR "/src"
RUN apt-get -y update
RUN apt-get -y install make gcc libssl-dev jq moreutils unzip

RUN wget https://github.com/rhash/RHash/archive/refs/tags/v1.4.4.tar.gz
RUN tar -xzvf "v1.4.4.tar.gz"
WORKDIR "RHash-1.4.4"
RUN ./configure --disable-gettext --enable-openssl-runtime --enable-lib-shared
RUN make lib-shared

WORKDIR "/src"
RUN wget https://cdn.anidb.net/client/avdump3/avdump3_8293_stable.zip
RUN unzip "avdump3_8293_stable.zip" -d "AVDump-8293"
# Need to add roll forward because avdump is built targetting old .net version
RUN jq '.runtimeOptions.framework.rollForward = "Major"' "AVDump-8293/AVDump3CL.runtimeconfig.json" | sponge "AVDump-8293/AVDump3CL.runtimeconfig.json"

RUN wget https://johnvansickle.com/ffmpeg/releases/ffmpeg-6.1-amd64-static.tar.xz && \
tar -xvJf ffmpeg-6.1-amd64-static.tar.xz && \
mv ffmpeg-6.1-amd64-static/ffmpeg ffmpeg-6.1-amd64-static/ffprobe ./

COPY "Shizou.Blazor/Shizou.Blazor.csproj" "Shizou.Blazor/"
COPY "Shizou.Data/Shizou.Data.csproj" "Shizou.Data/"
COPY "Shizou.Server/Shizou.Server.csproj" "Shizou.Server/"
RUN dotnet restore "Shizou.Blazor/Shizou.Blazor.csproj" -a $TARGETARCH
COPY "." "."
WORKDIR "/src/Shizou.Blazor"
RUN dotnet publish "Shizou.Blazor.csproj" -c Release -a $TARGETARCH --no-restore -o "/app/publish"

FROM base AS final
WORKDIR "/app"
COPY --from=publish "/app/publish" "."
COPY --from=publish "/src/RHash-1.4.4/librhash/librhash.so.1.4.4" "./RHash/librhash.so"
COPY --from=publish "/src/AVDump-8293" "/app/AVDump3"
COPY --from=publish "/src/ffprobe" "/app/ffprobe"
COPY --from=publish "/src/ffmpeg" "/app/ffmpeg"
ENTRYPOINT ["./Shizou.Blazor"]
