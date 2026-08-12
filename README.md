# SERVIGO/Web Edition

This version is an ASP.NET Core MVC web app backed by **SQLite**.
## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — that's it. No SQL Server, no SSMS, no IDE.
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
