FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app    
EXPOSE 80
EXPOSE 443
LABEL org.opencontainers.image.source=https://github.com/Mik1ll/Shizou

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Shizou.Blazor/Shizou.Blazor.csproj", "Shizou.Blazor/"]
COPY ["Shizou.Data/Shizou.Data.csproj", "Shizou.Data/"]
COPY ["Shizou.Server/Shizou.Server.csproj", "Shizou.Server/"]
RUN dotnet restore "Shizou.Blazor/Shizou.Blazor.csproj"
COPY . .
WORKDIR "/src/Shizou.Blazor"
RUN dotnet build "Shizou.Blazor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Shizou.Blazor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Shizou.Blazor.dll"]
