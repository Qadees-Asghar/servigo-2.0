# SERVIGO — Web Edition

A browser-based rewrite of the SERVIGO desktop app (originally C# WinForms + SQL Server).
This version is an ASP.NET Core MVC web app backed by **SQLite** — a single database
file with no server process, no SQL Server Management Studio, and no Visual Studio required.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — that's it. No SQL Server, no SSMS, no IDE.

Check it's installed:

```bash
dotnet --version
```

## Run it locally

From this folder:

```bash
dotnet run
```

Then open **http://localhost:5188** in a browser.

The first time it runs, it automatically creates `App_Data/servigo.db` (SQLite) and
the full schema, and seeds a default admin account:

- **Email:** `admin@servigo.com`
- **User ID:** `SRV-00001`
- **Password:** `Admin@123`

Change that password (or delete the account and re-seed) before using this for anything real.

To reset all data, stop the app and delete `App_Data/servigo.db` — it will be recreated
empty (with a fresh admin account) on the next run.

## What changed from the desktop version

| | Desktop (original) | Web (this version) |
|---|---|---|
| UI | WinForms | ASP.NET Core MVC + Razor views |
| Database | SQL Server (needs SSMS/server) | SQLite (single file, zero setup) |
| Business logic | T-SQL stored procedures/triggers | Plain C# in the DAL layer |
| Auth | In-memory session singleton | ASP.NET Core cookie authentication |
| Testing | Run from Visual Studio | `dotnet run` + any browser |

All features carried over: customer booking flow, provider service/schedule management,
booking accept/reject/complete, ratings with live average recalculation, notifications,
feedback/reports, and the admin panel (users, providers, bookings, analytics, audit log).

## Deploying it

Because everything (app + database) is a single self-contained process, you can deploy
this almost anywhere that runs .NET — no separate database server to provision.

### Option A — Publish and run the binary

```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet SERVIGO.Web.dll --urls http://0.0.0.0:5000
```

Copy the `publish` folder to any server with the .NET runtime installed and run it there
(behind IIS, nginx, or Caddy as a reverse proxy, or directly).

### Option B — Docker

Add this `Dockerfile` (not included by default, since it depends on your hosting target):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
VOLUME /app/App_Data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SERVIGO.Web.dll"]
```

Mount a volume at `/app/App_Data` so the SQLite database survives container restarts.

### Option C — A PaaS with persistent disk (Render, Fly.io, Railway, Azure App Service, etc.)

Any of these can run a published .NET app directly. The one thing to check: make sure
`App_Data/` is on **persistent** storage, not an ephemeral filesystem — otherwise the
database resets on every redeploy/restart. Most of these platforms offer a small
persistent volume/disk you can mount at that path.

### Connection string

The SQLite path is configured in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=App_Data/servigo.db"
}
```

Override it with an environment variable if needed:

```bash
ConnectionStrings__DefaultConnection="Data Source=/data/servigo.db" dotnet SERVIGO.Web.dll
```

## Project layout

```
Controllers/    Account, Customer, Provider, Admin
Views/          Razor views per controller, dark-themed shared layout + sidebars
Models/         Domain models + view models
DAL/            Data access (Microsoft.Data.Sqlite), one class per feature area
Data/           Db.cs (connection/query helpers) + Schema.cs (auto-created schema)
Helpers/        Password hashing (BCrypt), input validation, auth claims helpers
wwwroot/        Vendored Bootstrap 5 + custom dark theme CSS (no CDN dependency)
```
