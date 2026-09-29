# Dev notes — local environment guide

Full guide to get the development environment running on a fresh machine,
plus a quick command reference for daily work.

---

## 0. New machine setup (from zero to running)

### Prerequisites (install once)

| Tool | Why | Check |
|---|---|---|
| .NET SDK 8.x | build & run services | `dotnet --list-sdks` |
| Docker Desktop | Postgres container (later: whole system via compose) | `docker --version` |
| Git | obviously | `git --version` |
| DBeaver (optional) | browse the databases | — |
| Git Bash (comes with Git on Windows) | run `.sh` scripts | — |

### Steps

```bash
git clone <repo-url> && cd partner-commission
```

1) EF tooling (once per machine):

```bash
dotnet tool install -g dotnet-ef --version 8.*
```

(restart the terminal afterwards so PATH picks it up)

2) Start Postgres in Docker and create the service databases:

```bash
./scripts/dev-db-up.sh
```

(manual one-liner alternative — see "Start Postgres" below)

3) Build and apply migrations:

```bash
dotnet build
```

```bash
dotnet ef database update --project src/PartnerCommission.Partners.Api
```

```bash
dotnet ef database update --project src/PartnerCommission.Commissions.Api
```

```bash
dotnet ef database update --project src/PartnerCommission.Wallets.Api
```

4) Verify: run a service and check health (port — see launchSettings.json
of the service):

```bash
dotnet run --project src/PartnerCommission.Partners.Api
```

`/health/live` → 200 always; `/health/ready` → 200 with Postgres up,
503 after `docker stop pc-postgres`. If that holds — the environment works.

---

## Start Postgres container (manual one-liner)

```bash
docker run -d --name pc-postgres -e POSTGRES_USER=app -e POSTGRES_PASSWORD=app -p 5432:5432 -v pc-pgdata:/var/lib/postgresql/data postgres:16-alpine
```

Notes:
- `dotnet ef database update` creates the target database itself if it does
  not exist — no manual `CREATE DATABASE` needed for the EF path.
  The SQL in `../infra/postgres-init/` is for docker-compose later
  (auto-applied on first volume initialization) and as explicit documentation
  of which databases the system needs.
- Port 5432 already taken (local PostgreSQL service is a common culprit on
  Windows): map another one, e.g. `-p 5433:5432`, and change the port in
  every appsettings.json connection string.

## Container lifecycle

```bash
docker start pc-postgres      # after reboot (container does not autostart)
docker stop pc-postgres       # stop; data survives in the pc-pgdata volume
docker logs pc-postgres       # if something looks wrong
docker rm -f pc-postgres && docker volume rm pc-pgdata   # full reset
```

## EF migrations

All commands run from the repo root (where the .sln is) — `--project` paths
are relative to it.

One-time per project (already done, listed for completeness):

```bash
dotnet add src/PartnerCommission.Partners.Api package Microsoft.EntityFrameworkCore.Design --version 8.0.11
```

Add a new migration after changing entities (`-o Data/Migrations` keeps
migrations next to the DbContext; only matters for the FIRST migration of a
project — later ones follow the existing folder automatically):

```bash
dotnet ef migrations add <Name> --project src/PartnerCommission.Partners.Api -o Data/Migrations
```

```bash
dotnet ef migrations add <Name> --project src/PartnerCommission.Commissions.Api -o Data/Migrations
```

```bash
dotnet ef migrations add <Name> --project src/PartnerCommission.Wallets.Api -o Data/Migrations
```

Undo the LAST migration if it is not applied to the database yet:

```bash
dotnet ef migrations remove --project src/PartnerCommission.Partners.Api
```

### Recreate a service database from scratch (dev-only!)

After changing entities pre-release it is fine to rebuild the single Initial
migration instead of stacking fixup migrations. Legal ONLY while nobody else
has applied the schema and the data is disposable. Example for Commissions:

```bash
dotnet ef database drop --project src/PartnerCommission.Commissions.Api -f
```

```bash
dotnet ef migrations remove --project src/PartnerCommission.Commissions.Api
```

```bash
dotnet ef migrations add Initial --project src/PartnerCommission.Commissions.Api -o Data/Migrations
```

```bash
dotnet ef database update --project src/PartnerCommission.Commissions.Api
```

Notes: other services' databases are untouched; review the regenerated
migration (indexes, maxlength, precision); startup seeding (current_schema)
re-inserts itself on the next run — that is what "insert if missing" is for.

Apply migrations (creates the database itself if missing; re-running is a
no-op):

```bash
dotnet ef database update --project src/PartnerCommission.Partners.Api
```

```bash
dotnet ef database update --project src/PartnerCommission.Commissions.Api
```

```bash
dotnet ef database update --project src/PartnerCommission.Wallets.Api
```

## Poke around the database (psql)

```bash
docker exec -it pc-postgres psql -U app -d partners
```

Inside psql: `\l` — list databases, `\dt` — list tables, `\d users` — describe
table, `\q` — quit.

## GUI client (DBeaver)

Connection: PostgreSQL, host `127.0.0.1`, port `5432`, database `postgres`,
user/password `app`/`app`. Enable Edit Connection -> PostgreSQL ->
"Show all databases" to see partners/commissions/wallets in the tree.
Tables live under `<database> -> Schemas -> public -> Tables`.
(pgAdmin crashed with "access violation" on one dev machine — client-side
bug, DBeaver is used instead.)
