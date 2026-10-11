# syntax=docker/dockerfile:1

# ---- build -------------------------------------------------------------------------------
# global.json pins SDK 10.0.400 with rollForward latestFeature, so any newer 10.0.x SDK works.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore from the project files alone first, so this layer is cached until a dependency changes.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ChefLan.Domain/ChefLan.Domain.csproj               src/ChefLan.Domain/
COPY src/ChefLan.Commons/ChefLan.Commons.csproj             src/ChefLan.Commons/
COPY src/ChefLan.Logic/ChefLan.Logic.csproj                 src/ChefLan.Logic/
COPY src/ChefLan.DB/ChefLan.DB.csproj                       src/ChefLan.DB/
COPY src/ChefLan.Infrastructure/ChefLan.Infrastructure.csproj src/ChefLan.Infrastructure/
COPY src/ChefLan.Courses.API/ChefLan.Courses.API.csproj     src/ChefLan.Courses.API/
RUN dotnet restore src/ChefLan.Courses.API/ChefLan.Courses.API.csproj

COPY src/ src/
RUN dotnet publish src/ChefLan.Courses.API/ChefLan.Courses.API.csproj \
      -c Release --no-restore -o /app /p:UseAppHost=false

# ---- runtime -----------------------------------------------------------------------------
# The Debian-based aspnet image includes ICU, which this app relies on (see CLAUDE.md, Slugs).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Non-root. nginx terminates TLS, so the container speaks plain HTTP on 8080.
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=1
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChefLan.Courses.API.dll"]
