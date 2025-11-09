# Development Deployment Guide

This document explains how to set up and run the WEBP Shop solution for local development from scratch.

## 1. Prerequisites

Install the following tools before cloning the repository:

- [Git](https://git-scm.com/downloads)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (or Docker Engine + Docker Compose)
- [.NET SDK 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Optional (required only for the hybrid app): .NET MAUI workloads. Install them with `dotnet workload install maui` on the platforms you plan to target.

> **Note:** For MAUI development on Windows you need Visual Studio 2022 17.9+ with the ".NET Multi-platform App UI development" workload. On macOS you need the latest Visual Studio for Mac or the MAUI VS Code extension and the corresponding platform SDKs.

## 2. Clone the repository

```bash
git clone https://github.com/<your-org>/WEBP-Shop.git
cd WEBP-Shop
```

Replace `<your-org>` with the actual GitHub organization or user.

## 3. Start the infrastructure

The solution relies on a SQL Server instance. Start it with Docker Compose:

```bash
docker compose up -d
```

The database container exposes port `1433` and uses the credentials defined in `docker-compose.yaml`.

To verify the container is healthy:

```bash
docker compose ps
```

## 4. Restore and build the solution

Run the following commands from the repository root:

```bash
dotnet restore PWebShop.sln
dotnet build PWebShop.sln
```

## 5. Apply database schema and seed data

The API project ensures the database is created and seeds sample data on startup via Entity Framework Core. Launch the API once to provision the schema:

```bash
dotnet run --project PWebShop.Api
```

Keep the API running while you work on the frontend applications. If you only want to initialize the database, wait until the log indicates the web host has started and then stop it with `Ctrl+C`.

## 6. Run the applications

Open separate terminals for each application you want to run.

### 6.1 Public Web (Blazor WebAssembly)

```bash
dotnet run --project PWebShop.Web
```

The development server listens on `https://localhost:7216` by default (check the console output for the actual URL).

### 6.2 Administration App (Blazor Server)

```bash
dotnet run --project PWebShop.Admin
```

The default launch URL is `https://localhost:7180`.

### 6.3 Hybrid App (MAUI)

Ensure the MAUI workloads are installed (see prerequisites). To run on Android from the command line:

```bash
dotnet build PWebShop.Hybrid/PWebShop.Hybrid.csproj -t:Run -f net8.0-android
```

For other targets, replace `net8.0-android` with the desired framework (e.g., `net8.0-ios`, `net8.0-maccatalyst`, or `net8.0-windows10.0.19041.0`) and make sure the corresponding SDKs/emulators are installed.

## 7. Stopping services and cleaning up

To stop the running .NET applications, press `Ctrl+C` in each terminal. To shut down the database container:

```bash
docker compose down
```

If you want to remove the persistent SQL Server volume, append `--volumes` to the command above.

## 8. Troubleshooting tips

- If the API cannot reach SQL Server, ensure the container is running and that port `1433` is not blocked. You can test the connection with `docker exec -it pwebshop-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Development_s3rv3r!" -C`.
- When switching branches, rerun `dotnet restore` if new dependencies were introduced.
- Delete the `bin` and `obj` folders (`dotnet clean PWebShop.sln`) if you encounter build issues after pulling new changes.

You are now ready to develop and test the WEBP Shop solution locally.
