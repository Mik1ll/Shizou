FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
EXPOSE 80
EXPOSE 443
USER 0:0
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS publish
ARG TARGETARCH

RUN dotnet tool install Microsoft.Web.LibraryManager.Cli --tool-path /dotnet-tools

RUN apt-get -q -y update && apt-get -q -y install jq moreutils busybox xz-utils

WORKDIR /src/AVDump3
RUN curl -sL https://cdn.anidb.net/client/avdump3/avdump3_8293_stable.zip | busybox unzip -oq - && \
    jq '.runtimeOptions.framework.rollForward = "Major"' "AVDump3CL.runtimeconfig.json" | sponge AVDump3CL.runtimeconfig.json

WORKDIR /src/ffmpeg
RUN curl -sL https://johnvansickle.com/ffmpeg/releases/ffmpeg-6.1-amd64-static.tar.xz | tar -xJ --strip-components=1

WORKDIR /src
COPY NuGet.Config Shizou.sln ./
COPY RHashWrapper/RHashWrapper.csproj RHashWrapper/
RUN dotnet pack RHashWrapper/RHashWrapper.csproj
COPY Shizou.Data/Shizou.Data.csproj Shizou.Data/
COPY Shizou.Server/Shizou.Server.csproj Shizou.Server/
COPY Shizou.Blazor/Shizou.Blazor.csproj Shizou.Blazor/libman.json Shizou.Blazor/
WORKDIR /src/Shizou.Blazor
RUN dotnet restore Shizou.Blazor.csproj -a $TARGETARCH
RUN /dotnet-tools/libman restore
COPY Shizou.Data /src/Shizou.Data/
COPY Shizou.Server /src/Shizou.Server/
COPY Shizou.Blazor /src/Shizou.Blazor/
RUN dotnet publish -c Release -a $TARGETARCH --no-self-contained --no-restore -o /app/publish
RUN mkdir /ShizouBin; mv /app/publish/Shizou.* /ShizouBin/

FROM base AS final
WORKDIR /app
COPY --from=publish /src/ffmpeg/ffprobe /src/ffmpeg/ffmpeg ./
COPY --from=publish /src/AVDump3 AVDump3/
COPY --from=publish /app/publish ./
COPY --from=publish /ShizouBin ./
ENTRYPOINT ["dotnet", "Shizou.Blazor.dll"]
