FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Api/SistemaAranceles.Api.csproj", "src/Api/"]
COPY ["src/Application/SistemaAranceles.Application.csproj", "src/Application/"]
COPY ["src/Domain/SistemaAranceles.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/SistemaAranceles.Infrastructure.csproj", "src/Infrastructure/"]

RUN dotnet restore "src/Api/SistemaAranceles.Api.csproj"

COPY . .
RUN dotnet publish "src/Api/SistemaAranceles.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080
COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "dotnet SistemaAranceles.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
