FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SERVIGO.Web.csproj .
RUN dotnet restore SERVIGO.Web.csproj
COPY . .
RUN dotnet publish SERVIGO.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /app/App_Data
EXPOSE 8080
# Render (and most PaaS hosts) inject $PORT at runtime; fall back to 8080 for local `docker run`.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet SERVIGO.Web.dll"]
