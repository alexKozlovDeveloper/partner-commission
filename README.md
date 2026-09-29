# Partner Commission

A distributed system that accrues and pays out partner commissions.
Three .NET 8 microservices on PostgreSQL: users form a partner hierarchy, profit events arrive per user,
and every positive profit accrues commissions for the whole partner chain above the event owner.
Commissions are paid out to internal wallets periodically.

---

## Quick start

Requirements: Docker Desktop (Docker Compose v2).

```bash
docker compose up --build
```

All services become `healthy` in about 10 seconds. Databases are created by `infra/postgres-init`,
schemas are applied by each service on startup (EF Core migrations).

| Component   | URL                                   |
|-------------|---------------------------------------|
| Partners    | http://localhost:8081/swagger         |
| Commissions | http://localhost:8082/swagger         |
| Wallets     | http://localhost:8083/swagger         |
| Prometheus  | http://localhost:9090                 |
| PostgreSQL  | `localhost:5433`, user/password `app` |

Every service also exposes `/health/live`, `/health/ready` and `/metrics`.

```bash
docker compose down        # stop, keep data
docker compose down -v     # stop and wipe data
```

> `infra/postgres-init` runs only when the Postgres volume is created for the first time.
> If the volume already exists, run `docker compose down -v` to recreate the databases.

Log format is switched with `Logging__Console__FormatterName` in `docker-compose.yml`:
`simple` (readable in `docker compose logs`) or `json` (for a log collector).

### Running without Docker

Start PostgreSQL on `localhost:5432` (user/password `app`) with databases `partners`, `commissions`, `wallets`,
then run the three `*.Api` projects. Ports are defined in each project's `launchSettings.json`
(Partners `5292`, Commissions `5168`, Wallets `5018`). Each project has a `.http` file with ready-to-send requests.

### Tests

```bash
dotnet test
```

Unit tests cover the commission calculation (both schemas, rounding, edge cases) and the partner tree rules
(self-reference, cycles, depth limit).

---

## Try it in 5 minutes

```bash
P=http://localhost:8081; C=http://localhost:8082; W=http://localhost:8083
J='Content-Type: application/json'

# 1. Users and the partner chain: u1 <- u2 <- u3
curl -X POST $P/users -H "$J" -d '{"externalId":"u1"}'
curl -X POST $P/users -H "$J" -d '{"externalId":"u2"}'
curl -X POST $P/users -H "$J" -d '{"externalId":"u3"}'
curl -X PUT  $P/users/u2/partner -H "$J" -d '{"partnerExternalId":"u1"}'
curl -X PUT  $P/users/u3/partner -H "$J" -d '{"partnerExternalId":"u2"}'

# 2. Profit event of u3 (Linear schema by default)  -> 202
curl -X POST $C/users/u3/profit-events -H "$J" -d '{"eventExternalId":"evt-1","profit":100}'

# 3. After ~15 s (payout interval): u2 got 1% (level 1), u1 got 2% (level 2), both paid
curl $C/users/u3/profit-events/evt-1
curl $W/users/u1/wallet          # {"balance":2.0000}
curl $W/users/u2/wallet          # {"balance":1.0000}
curl $W/users/u1/wallet/payouts

# 4. Switch the schema - affects only new events
curl -X PUT $C/admin/commission-schema -H "$J" -d '{"schemaType":"Fibonacci"}'
curl -X POST $C/users/u3/profit-events -H "$J" -d '{"eventExternalId":"evt-2","profit":100}'

# 5. Idempotency and tree rules
curl -X POST $C/users/u3/profit-events -H "$J" -d '{"eventExternalId":"evt-1","profit":100}'   # 200, duplicate
curl -X PUT  $P/users/u1/partner -H "$J" -d '{"partnerExternalId":"u3"}'                        # 409, cycle
```

---

## Architecture

```
                         +---------------------------+
   client  ------------> |         Partners          |  users, partner links, tree
                         |  db: partners             |
                         +-------------^-------------+
                                       | GET /internal/users/{id}/ancestors
                                       | (sync HTTP, retry + circuit breaker)
                         +-------------+-------------+
   client  ------------> |        Commissions        |  profit events, commissions,
                         |  db: commissions          |  current schema, outbox
                         +-----+---------------+-----+
                               |               |
        POST /internal/        |               | POST /internal/commissions/payments:query
        commissions (outbox)   |               | (payment status for event details)
                               v               v
                         +-----+---------------+-----+
   client  ------------> |          Wallets          |  accrued commissions, periodic payout,
                         |  db: wallets              |  balances, payout history
                         +---------------------------+

   Prometheus  --- scrapes /metrics of all three services every 5 s
```

| Service     | Owns                                              | Background jobs                          |
|-------------|---------------------------------------------------|------------------------------------------|
| Partners    | `users` (adjacency list: `ParentId`)              | -                                        |
| Commissions | `profit_events`, `commissions`, `settings`, `outbox_messages` | profit event processor, outbox dispatcher |
| Wallets     | `wallet_entries`, `wallets`                        | payout processor                         |

Each service has its own database; services never read each other's tables.
Shared infrastructure (logging, health checks, metrics, error handling, migrations, advisory locks, paging)
lives in `PartnerCommission.Shared`; inter-service DTOs live in `PartnerCommission.Contracts`.

### Life of a profit event

```
POST /users/{id}/profit-events
  -> profit_events: Received  (current schema is captured here)          202 Accepted
        |
        | ProfitEventProcessor, every 5 s
        v
  Profit <= 0 ............................................ Processed, no commissions
  user unknown in Partners ............................... Unresolved, re-checked every 30 s
  otherwise: ancestors from Partners -> calculator
        one transaction: commissions + outbox messages + Processed
        |
        | OutboxDispatcher, every 5 s
        v
  Wallets: wallet_entries (Pending)   -- idempotent by CommissionId
        |
        | PayoutProcessor, every 15 s
        v
  one transaction per user: entries -> Paid, wallets.balance += sum
```

Transient failures (Partners/Wallets down, DB hiccups) are retried forever with exponential backoff
(2, 4, 8 ... up to 300 s). The attempt counter and the last error are stored on the row (`Attempts`, `LastError`).

### API

**Partners**

| Method | Path | Result |
|---|---|---|
| POST | `/users` | 201 created, 200 already exists (same id) |
| GET | `/users/{externalId}` | user with its partner |
| GET | `/users?page=&pageSize=` | paged list |
| PUT | `/users/{externalId}/partner` | 204 linked / unchanged; 400 self; 409 cycle or depth exceeded |
| DELETE | `/users/{externalId}/partner` | 204 |
| GET | `/users/{externalId}/tree` | ancestors and the whole subtree |
| GET | `/internal/users/{externalId}/ancestors` | partner chain for Commissions |

**Commissions**

| Method | Path | Result |
|---|---|---|
| POST | `/users/{externalId}/profit-events` | 202 accepted; 200 duplicate; 409 same id for another user or another profit |
| GET | `/users/{externalId}/profit-events?page=&pageSize=` | user's events without commissions |
| GET | `/users/{externalId}/profit-events/{eventExternalId}` | event with commissions: beneficiary, level, amount, schema, payment status |
| GET | `/admin/commission-schema` | current schema |
| PUT | `/admin/commission-schema` | switch `Linear` / `Fibonacci` |

**Wallets**

| Method | Path | Result |
|---|---|---|
| GET | `/users/{externalId}/wallet` | balance (sum of paid commissions) |
| GET | `/users/{externalId}/wallet/payouts?page=&pageSize=` | paid commissions, newest first |
| POST | `/internal/commissions` | accrued commission from the outbox; 204 for new and duplicate |
| POST | `/internal/commissions/payments:query` | payment status for a list of commission ids |

Errors are returned as RFC 7807 `ProblemDetails` with a `traceId`.
Paged responses: `{ items, page, pageSize, totalCount }`, `pageSize` 1..100 (default 20).

---

## Decisions

**Communication.** Synchronous HTTP for reads that need an immediate answer (ancestors, payment status);
a transactional outbox for Commissions -> Wallets. The task allows HTTP + outbox instead of a broker;
it keeps the stack small and still gives at-least-once delivery without distributed transactions.

**Idempotency.** Every write is protected by a database constraint, not only by application checks:
`profit_events.EventExternalId` unique, `commissions (ProfitEventId, Level)` unique,
`wallet_entries.CommissionId` primary key, `users.ExternalId` unique.
Each write follows the same pattern: check (fast path) -> insert -> catch unique violation (race) -> treat as duplicate.
A duplicate with different data (same event id for another user / another profit, same commission id
with another amount) is a `409`, not a silent success.

**Schema per commission.** The current schema is read from `settings` when an event is received and stored on the event;
every commission copies it into `SchemaType`. Switching the schema affects only events received afterwards,
and re-processing after a failure always gives the same result. The default schema is seeded on startup
with `INSERT ... ON CONFLICT DO NOTHING` (not `HasData`, so migrations never overwrite a value changed at runtime).

**Calculation.** Level L gets `L x Profit / 100` (Linear) or `F(L) x Profit / 100` (Fibonacci, `F(1)=1, F(2)=1, F(3)=2 ...`).
Amounts are rounded to 4 decimals with `MidpointRounding.AwayFromZero`; amounts that round to zero are not stored.
Only commissions go to wallets, never the profit itself.

**Partner tree.** Stored as an adjacency list (`ParentId`) and read with recursive CTEs - one query per operation.
The depth limit is an invariant of the tree itself: no user may have more than `Partners:MaxDepth` (10) ancestors.
Linking a partner is rejected if it creates a cycle, a self-reference, or makes any node of the user's subtree deeper than the limit.
Tree changes are serialized with a transaction-level advisory lock, so two concurrent links cannot create a cycle together.
Closure tables or `ltree` read faster but make moving a subtree expensive; with a depth of 10 the CTE approach is sufficient.

**Payouts.** Accrued commissions arrive in Wallets as `Pending` and do not affect the balance.
The payout job moves them to `Paid` and adds the sum to the balance in one transaction per user.
The payment status shown in event details is fetched from Wallets on request (Wallets is the source of truth);
if Wallets is unavailable, the status is `Unknown` and the rest of the details are still returned.

**Scalability.** API instances are stateless and can be scaled freely.
Background jobs run active/standby: each tick takes a PostgreSQL advisory lock (`pg_try_advisory_lock`),
other instances skip the tick; if the leader dies, the lock is released with its connection.
Migrations on startup are also serialized with an advisory lock (EF Core 8 has no migration lock).

**Fault tolerance.** Outgoing HTTP clients use `AddStandardResilienceHandler` (retries, circuit breaker, timeouts).
Background work is never lost: status changes and side effects are committed in one transaction,
so an interrupted item is simply picked up again. `HostOptions.ShutdownTimeout` is 10 s and
`stop_grace_period` in compose is 35 s, so containers stop gracefully (verified: exit code 0).

**Observability.**
- Logs carry `TraceId`/`SpanId`. HTTP calls propagate `traceparent`, so a request is traced across services.
  Background jobs start their own activity per item (`ProcessProfitEvent`, `DispatchOutboxMessage`, `PayoutUser`),
  so their calls to Partners and Wallets share one `TraceId` as well. Items are additionally tagged via log scopes
  (`EventExternalId`, `OutboxMessageId`, `UserExternalId`).
- Every error response contains `traceId` for searching the logs.
- Prometheus metrics (see below), `/health/live` (process is up) and `/health/ready` (database reachable).

---

## Metrics

All services expose `/metrics`; Prometheus scrapes them every 5 s (`infra/prometheus/prometheus.yml`).

### Business metrics

| Service | Metric | Type | Labels |
|---|---|---|---|
| Partners | `partnercommission_users_created_total` | counter | `result`: created, duplicate |
| Partners | `partnercommission_partner_links_total` | counter | `result`: linked, unchanged, self, cycle, depth_exceeded |
| Commissions | `partnercommission_profit_events_received_total` | counter | `result`: accepted, duplicate |
| Commissions | `partnercommission_profit_events_processed_total` | counter | `outcome`: processed, unresolved, retry |
| Commissions | `partnercommission_profit_event_processing_seconds` | histogram | - |
| Commissions | `partnercommission_commissions_accrued_total` | counter | `schema`: Linear, Fibonacci |
| Commissions | `partnercommission_commissions_accrued_amount_total` | counter | `schema`: Linear, Fibonacci |
| Commissions | `partnercommission_outbox_messages_sent_total` | counter | `result`: sent, failed |
| Commissions | `partnercommission_outbox_pending` | gauge | - |
| Commissions | `partnercommission_profit_events_pending` | gauge | - |
| Wallets | `partnercommission_commissions_received_total` | counter | `result`: created, duplicate, conflict |
| Wallets | `partnercommission_payouts_total` | counter | - |
| Wallets | `partnercommission_payout_amount_total` | counter | - |
| Wallets | `partnercommission_payout_run_seconds` | histogram | - |
| Wallets | `partnercommission_wallet_entries_pending` | gauge | - |

Counters of accrued and paid amounts are incremented only after the transaction is committed.
The `*_pending` gauges are updated by the instance that currently holds the job lock, so aggregate them with `max()`.

### Built-in metrics (prometheus-net)

| Metric | Source |
|---|---|
| `http_requests_received_total`, `http_request_duration_seconds`, `http_requests_in_progress` | incoming HTTP, labels `code`, `method`, `controller`, `action`, `endpoint` |
| `httpclient_requests_sent_total`, `httpclient_request_duration_seconds`, `httpclient_requests_in_progress` | outgoing HTTP from Commissions, labels `client`, `host`, `method`, `code`; every retry attempt is measured separately |
| `aspnetcore_healthcheck_status` | health check result (0/1), label `name` |
| `process_*`, `dotnet_*` | CPU, memory, GC, thread pool |

### Useful queries

```promql
# commissions accrued per schema
sum by (schema) (partnercommission_commissions_accrued_amount_total)

# deliveries waiting for Wallets - grows when Wallets is down
max(partnercommission_outbox_pending)

# processing outcomes per second - growing "retry" means failures
sum by (outcome) (rate(partnercommission_profit_events_processed_total[1m]))

# how neighbours respond to Commissions
sum by (host, code) (rate(httpclient_requests_sent_total[1m]))

# p95 of profit event processing
histogram_quantile(0.95, rate(partnercommission_profit_event_processing_seconds_bucket[5m]))
```

To see recovery in action: `docker compose stop wallets`, send a few events, watch `outbox_pending` grow,
then `docker compose start wallets` and watch it drop to 0.

---

## Configuration

| Setting | Where | Default |
|---|---|---|
| `ConnectionStrings:Db` | each service | `appsettings.Development.json`, overridden in compose |
| `Services:Partners`, `Services:Wallets` | Commissions | `http://partners:8080/`, `http://wallets:8080/` in compose |
| `Partners:MaxDepth` | Partners | 10 (allowed 1..25) |
| Polling interval / batch size | background jobs | events 5 s, outbox 5 s, payouts 15 s; batch 50 |
| Retry backoff | events, outbox | 2^attempt s, max 300 s; unresolved events every 30 s |

---

## Limitations and next steps

- **Background throughput.** Jobs run on one active instance. For horizontal scaling of processing, events would go
  to Kafka partitioned by `UserExternalId` (consumer group, one partition per consumer) or be claimed with `FOR UPDATE SKIP LOCKED`.
- **Tracing across the outbox.** Event processing and outbox delivery are separate traces, correlated by `EventExternalId`;
  storing `traceparent` in the outbox row would join them into one trace. OpenTelemetry export (Jaeger/Tempo) is not set up.
- **Internal endpoints** (`/internal/*`) are not authenticated; in production they would be closed at the gateway or use mTLS / API keys.
- **Paging** is offset-based; for event and payout feeds keyset paging by `(CreatedAtUtc, Id)` would avoid duplicates between pages.
- **Migrations** run on service startup; in production a dedicated migrator job (or EF migration bundle) would be used.
- **Poison items** are retried forever with backoff and are visible through `Attempts`/`LastError`; a dead-letter queue and alerting would be added.
- **Wide subtrees** are returned in full by `/users/{id}/tree` (depth is bounded, width is not).
- **Tests** are unit tests; integration tests with Testcontainers (idempotency, outage recovery) are the next step.

---

## Repository layout

```
src/
  PartnerCommission.Partners.Api        users, partner links, tree
  PartnerCommission.Commissions.Api     profit events, commissions, outbox, schema settings
  PartnerCommission.Commissions.Domain  commission schemas and calculator (no dependencies)
  PartnerCommission.Wallets.Api         wallet entries, payouts, balances
  PartnerCommission.Contracts           DTOs shared between services
  PartnerCommission.Shared              service defaults, logging, metrics, errors, migrations, locks, paging
tests/
  PartnerCommission.Commissions.Domain.Tests
  PartnerCommission.Partners.Api.Tests
infra/
  postgres-init/                        creates the three databases
  prometheus/                           scrape configuration
docker-compose.yml
```
