# Test Assignment: Partner Commission System

## Context

Develop a distributed system (a set of microservices) for accruing and paying
out partner commissions. The system works with a hierarchy of users and a
stream of events that generate commission rewards.

## Domain objects

| Entity | Description |
|---|---|
| User | Identified by `ExternalId`. Has a single internal wallet in conventional units |
| Partner link | A user may reference another user (be their partner) or have no reference |
| Event | Arrives per user. Contains the event's `ExternalId` and `Profit` (may be positive or negative) |
| Commission | Accrued only for events with positive `Profit`. Calculated for every user in the partner chain above the event owner. The wallet receives commissions, not events |

## Commission accrual rules

For every event with positive `Profit`, an amount is accrued at each level of
the partner chain upwards from the event owner, according to one of two schemas:

- Level 1 — the direct partner (the one who directly invited the user)
- Level 2 — the partner's partner
- Level 3 — the next one up the chain, and so on

| Schema | Formula for level L |
|---|---|
| Linear | `L × Profit / 100` |
| Fibonacci | `F(L) × Profit / 100`, where `F(L)` is a Fibonacci number |

Storage requirement: every accrued commission record must store `schema_type` —
the schema it was calculated with.

## Functional requirements

### User tree management

- Add a user
- Set a partner link (who references whom)
- Get the tree (branch up/down) for a given user

### Events and commissions

- Accept profit/loss events for users
- Automatically calculate commissions for the whole partner chain using the
  current schema
- Store the accrual history (every commission stores `schema_type`)

### Wallets

- Get a user's current balance (the sum of paid-out commissions)
- Get the payout history (which commissions were paid and when)
- Periodic payout of accrued commissions to the wallet

### Viewing events and commissions

- Get a user's event list (without commission details)
- Get detailed information for a single event with the list of all commissions
  accrued for it (to which partner, how much, by which schema, paid or not)

### Administration

- Switching the accrual schema (Linear ↔ Fibonacci). Switching affects only
  new calculations; old commissions are not recalculated

## Non-functional requirements

| Category | Requirement |
|---|---|
| Architecture | Microservice-based. The solution must consist of several interacting services |
| Scalability | The system must scale horizontally |
| Fault tolerance | The system must handle failures of individual components correctly (retry, circuit breaker, graceful shutdown) |
| Observability | Logging, metrics, health checks |
| Data | Guarantee of no double accruals (idempotency) |

## Deliverables

1. Source code in C# (.NET 8+)
2. `docker-compose.yml` for running all services and dependencies locally
3. README with launch instructions and a description of the architectural
   decisions taken
4. Unit tests for the key business logic (commission calculation)

## Evaluation criteria

| Area | What is evaluated |
|---|---|
| Correctness | Calculations are correct for both schemas, the tree works correctly, only commissions reach the wallet |
| Architecture | Service boundaries, protocol choices, state handling, schema stored in every commission |
| Reliability | Idempotency, error handling, graceful shutdown |
| Code | Readability, DI, asynchrony, tests |
| Infrastructure | Docker, DB/broker setup, health checks |

## Allowed simplifications

- Instead of Kafka, asynchronous communication may use HTTP + Outbox (or a
  simplified in-memory queue)
- Instead of distributed transactions — background processes with retry and
  idempotency
- Limit the tree depth (e.g. 10 levels)
