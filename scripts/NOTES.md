# Dev notes

Day-to-day reference for working on the code. For what the system does and why, see the root `README.md`.

There are two ways to run the system:

| Mode | When | Postgres | Services |
|---|---|---|---|
| **Docker Compose** | check the whole system, demo, reviewers | container `postgres`, host port `5433` | containers, ports `8081` / `8082` / `8083` |
| **Local dev** | debugging, running from the IDE | container `pc-postgres`, host port `5432` | `dotnet run`, ports `5292` / `5168` / `5018` |

The two modes do not conflict and can run at the same time.

---

## Prerequisites

| Tool | Why | Check |
|---|---|---|
| .NET SDK 8.x | build, run, test | `dotnet --list-sdks` |
| Docker Desktop | Postgres, compose stack | `docker --version` |
| EF Core tools (optional) | add / remove migrations | `dotnet ef --version` |
| DBeaver (optional) | browse the databases | — |

```bash
dotnet tool install -g dotnet-ef --version 8.*
```

---

## Docker Compose

```bash
docker compose up --build
```

```bash
docker compose down
```

```bash
docker compose down -v
```

- `down -v` also removes the data volumes; use it to start from a clean state.
- `infra/postgres-init/` runs only when the Postgres volume is created, so new databases appear only after `down -v`.
- Swagger: `http://localhost:8081/swagger`, `:8082`, `:8083`. Prometheus: `http://localhost:9090`.
- Rebuild a single service after a code change:

```bash
docker compose up -d --build commissions
```

- Logs of one service, follow mode:

```bash
docker compose logs -f commissions
```

---

## Local dev

### 1. Postgres container (once per machine)

```bash
docker run -d --name pc-postgres -e POSTGRES_USER=app -e POSTGRES_PASSWORD=app -p 5432:5432 -v pc-pgdata:/var/lib/postgresql/data postgres:16-alpine
```

### 2. Databases (once)

Services apply their migrations on startup, but the database itself must already exist.

```bash
docker exec pc-postgres psql -U app -d postgres -c "CREATE DATABASE partners" -c "CREATE DATABASE commissions" -c "CREATE DATABASE wallets"
```

(`dotnet ef database update --project src/<Service>.Api` also creates a missing database.)

### 3. Run the services

```bash
dotnet run --project src/PartnerCommission.Partners.Api
```

```bash
dotnet run --project src/PartnerCommission.Wallets.Api
```

```bash
dotnet run --project src/PartnerCommission.Commissions.Api
```

Connection strings and service URLs for this mode are in each `appsettings.Development.json`.
Each API project has a `.http` file with ready-to-send requests.

### Container lifecycle

```bash
docker start pc-postgres
```

```bash
docker stop pc-postgres
```

```bash
docker rm -f pc-postgres && docker volume rm pc-pgdata
```

The container does not autostart after a reboot. The last command is a full reset (data is lost).

---

## Tests

```bash
dotnet test
```

- `PartnerCommission.Commissions.Domain.Tests` — schemas and the commission calculator.
- `PartnerCommission.Partners.Api.Tests` — partner tree rules (self-reference, cycle, depth).

If the services are running locally, `dotnet test` / `dotnet build` may fail with "file is locked":
stop the services, or build into another folder with `-o <dir>`.

---

## EF migrations

Commands run from the repository root. Migrations are applied automatically on service startup
(`MigrateWithLockAsync`), so after adding one just restart the service.

Add a migration:

```bash
dotnet ef migrations add <Name> --project src/PartnerCommission.Partners.Api
```

```bash
dotnet ef migrations add <Name> --project src/PartnerCommission.Commissions.Api
```

```bash
dotnet ef migrations add <Name> --project src/PartnerCommission.Wallets.Api
```

Check that the model and the migrations are in sync:

```bash
dotnet ef migrations has-pending-model-changes --project src/PartnerCommission.Commissions.Api
```

Undo the last migration (only if it is not applied anywhere yet):

```bash
dotnet ef migrations remove --project src/PartnerCommission.Commissions.Api
```

Review every generated migration before committing: it runs automatically on startup,
so an unexpected `DropColumn` or `DeleteData` is applied silently.

### Recreate a service schema from scratch (dev only)

Allowed only while the data is disposable and nobody else has applied the schema. Example for Commissions:

```bash
dotnet ef database drop --project src/PartnerCommission.Commissions.Api -f
```

```bash
dotnet ef migrations remove --project src/PartnerCommission.Commissions.Api
```

```bash
dotnet ef migrations add Initial --project src/PartnerCommission.Commissions.Api -o Data/Migrations
```

The default commission schema (`settings.commission_schema = Linear`) is inserted on startup if missing.

---

## Databases

psql, local dev:

```bash
docker exec -it pc-postgres psql -U app -d commissions
```

psql, compose:

```bash
docker compose exec postgres psql -U app -d commissions
```

Inside psql: `\l` databases, `\dt` tables, `\d profit_events` describe a table, `\q` quit.
Column names are PascalCase and must be quoted: `select "Status", count(*) from profit_events group by 1;`

DBeaver: PostgreSQL, host `127.0.0.1`, port `5432` (local dev) or `5433` (compose), database `postgres`,
user/password `app`/`app`. Enable "Show all databases" in the connection settings to see all three databases.

---

## Configuration

| Key | Service | Default |
|---|---|---|
| `ConnectionStrings:Db` | all | `appsettings.Development.json` / compose env |
| `Services:Partners`, `Services:Wallets` | Commissions | `appsettings.Development.json` / compose env |
| `Partners:MaxDepth` | Partners | 10 |
| `BackgroundJobs:ProfitEventProcessor:PollInterval` / `:BatchSize` | Commissions | `00:00:05` / 50 |
| `BackgroundJobs:OutboxDispatcher:PollInterval` / `:BatchSize` | Commissions | `00:00:05` / 50 |
| `BackgroundJobs:PayoutProcessor:PollInterval` / `:BatchSize` | Wallets | `00:00:15` / 50 |
| `Logging:Console:FormatterName` | all | `simple` (or `json`) |

Any key can be overridden with an environment variable, `:` becomes `__`
(for example `BackgroundJobs__PayoutProcessor__PollInterval=00:00:03`).
Invalid values stop the service on startup with a validation error.

---

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| Port 5432 is taken | a local PostgreSQL service is running; map `-p 5433:5432` for `pc-postgres` and change the port in `appsettings.Development.json` |
| Service fails on startup: database does not exist | create the databases (Local dev, step 2) or run `docker compose down -v` so the init script runs again |
| `docker compose build --no-cache` fails on `apt-get update` | no access to `deb.debian.org` from Docker; build without `--no-cache` to reuse the cached layer |
| Build fails with "file is locked" | the service is running from the same `bin` folder; stop it or build with `-o <dir>` |
| Background job does nothing on a second instance | expected: jobs are active/standby by advisory lock; enable Debug logs to see "skipping tick" |
| pgAdmin crashes with "access violation" | client-side bug on one dev machine; use DBeaver |
