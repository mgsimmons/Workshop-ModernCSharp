# WWSHA implementation notes (myFinal branch)

These notes describe how this implementation behaves where the lab manual (`README.md`) is silent or
has been extended. The lab manual itself is unchanged.

## Projects

| Project | Purpose |
|---|---|
| `WWSHA.Domain` | Domain types and business rules. No I/O, no clock. |
| `WWSHA.Persistence` | Interfaces, the locked in-memory store, and commands. |
| `WWSHA.Api` | Minimal API, DTOs, and error mapping. |
| `*.Tests` | xUnit tests for each layer. Run `dotnet test WWSHA.slnx`. |

## Business rules

- **Dispatch.** Heroes are assigned to waiting (Opened) incidents, oldest first (by `occurredAt`,
  then id). This happens when an incident is filed, when a hero is registered, and when an
  incident is closed. A waiting incident is never left Opened while a hero is free.
- **Assignment size.** One hero is dispatched automatically. More heroes (up to three per
  assignment) are added explicitly with `POST /api/incidents/{id}/heroes`.
- **Reports.** Accepted while an incident is Opened or InProgress, and rejected once it is Closed.
  Reports must name the incident's city (case-insensitive). A report filed while Opened is
  attached to the assignment when the first hero arrives.
- **Closing.** Only an InProgress incident can be closed. Closing assesses every assigned hero:
  reputation changes by 5 minus the number of reports on the incident, and the hero becomes
  available again.
- **Report fields.** Beyond the lab's description, reports carry `severity` (Minor, Moderate, Severe),
  `cost` (zero or more), and `injury` (zero or more).

## Time

Timestamps come from an injected `TimeProvider`, so tests can control them. The domain layer never
reads the clock; commands pass the current time in.

## Concurrency

The in-memory store takes one lock for every command, save, and query. Reads return snapshot
copies, and API responses are built inside the lock, so they never show a half-applied change.

## API conventions

- Enums are sent and received as names (`"InProgress"`), not numbers.
- Errors are RFC 7807 problem responses with a stable `code` field:
  - `404`: `entity.not_found`
  - `409`: `hero.unavailable`, `assignment.full`, `assignment.duplicate`, `incident.closed`,
    `incident.state`
  - `400`: `request.invalid`, `report.invalid`, `assessment.invalid`, `domain.invalid`
- `GET /api/health` returns `{"status":"ok"}`.
- OpenAPI is served at `/openapi/v1.json` in the Development environment.
- `WWSHA.Api/WWSHA.Api.http` contains runnable requests (VS Code REST Client).

## Breaking changes from the first version

- `POST /api/incidents/{id}/reports` now requires `severity`, `cost`, and `injury`.
- Report, assignment, and assessment responses include new fields (`severity`, `cost`, `injury`,
  `createdAt`, `closedAt`).
- Filing an incident, registering a hero, or closing an incident can now assign heroes to waiting
  incidents, so responses may show `InProgress` where the first version showed `Opened`.
