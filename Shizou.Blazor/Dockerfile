FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /app    
EXPOSE 80
EXPOSE 443
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS publish
WORKDIR /src
COPY ["Shizou.Blazor/Shizou.Blazor.csproj", "Shizou.Blazor/"]
COPY ["Shizou.Data/Shizou.Data.csproj", "Shizou.Data/"]
COPY ["Shizou.Server/Shizou.Server.csproj", "Shizou.Server/"]
RUN dotnet restore "Shizou.Blazor/Shizou.Blazor.csproj" --runtime alpine-x64
COPY . .
WORKDIR "/src/Shizou.Blazor"
RUN dotnet publish "Shizou.Blazor.csproj" -c Release -o /app/publish --no-restore --no-self-contained --runtime alpine-x64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./Shizou.Blazor"]
