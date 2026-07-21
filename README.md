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

A `Dockerfile` and `.dockerignore` are included at the repo root:

```bash
docker build -t servigo-web .
docker run -p 8080:8080 -v servigo-data:/app/App_Data servigo-web
```

The volume mount keeps `App_Data/servigo.db` across container restarts.

### Option C — Fly.io, via GitHub (what this repo is set up for)

`fly.toml` and `.github/workflows/fly-deploy.yml` are included. One-time setup (needs
your own Fly account — sign-up may ask for a payment method as an anti-abuse check, even
though the resources this app needs fit in the free allowance):

1. Install the CLI and log in:
   ```bash
   curl -L https://fly.io/install.sh | sh    # or: iwr https://fly.io/install.ps1 -useb | iex   (Windows)
   flyctl auth login
   ```
2. From the repo root, launch the app (reuses `fly.toml`; it'll prompt you to confirm or
   change the app name if `servigo-2-0` is taken, and pick a region):
   ```bash
   flyctl launch --no-deploy
   ```
3. Create the persistent volume for the SQLite database (must match `fly.toml`'s mount):
   ```bash
   flyctl volumes create servigo_data --size 1
   ```
4. Deploy once manually to confirm it works:
   ```bash
   flyctl deploy
   ```
5. Wire up auto-deploy from GitHub: generate a deploy token —
   ```bash
   flyctl tokens create deploy
   ```
   then in the GitHub repo: **Settings → Secrets and variables → Actions → New repository
   secret**, name it `FLY_API_TOKEN`, paste the token. Every future push to `main` now
   auto-deploys via the included workflow.

This config gives you a **persistent volume out of the box** — no separate upgrade needed
for the database to survive restarts, unlike the Render free tier.

### Other PaaS options (Render, Railway, Azure App Service, etc.)

Any of these can run the included `Dockerfile` directly. The one thing to check on
whichever you pick: make sure `App_Data/` is on **persistent** storage, not an ephemeral
filesystem — otherwise the database resets on every redeploy/restart.

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
