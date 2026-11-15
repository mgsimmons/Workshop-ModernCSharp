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

- **Input limits.** Enforced by the domain (`DomainLimits`):
  - names up to 100 characters, superpowers up to 200, cities up to 100, descriptions up to 2000
  - starting reputation from -100 to 100
  - report cost from 0 to 1,000,000,000, and injury from 0 to 1,000,000
  - Violations return `400` with `request.invalid`.
- **Future incidents.** An incident may not occur more than one day after the server's current time
  (for clock skew). Violations return `400` with `incident.invalid`.

## Time

Timestamps come from an injected `TimeProvider`, so tests can control them. The domain layer never
reads the clock; commands pass the current time in. Each command reads the clock before it changes
anything, so a clock failure cannot leave a partial change behind.

## Atomicity

A command either applies completely or changes nothing. Closing an incident validates every
hero's assessment before recording any of them. Assignment and report checks also run before any
state changes.

## Concurrency

The in-memory store takes one lock for every command, save, and query. Reads return snapshot
arrays, reused until the next new object is saved, and API responses are built inside the lock, so
they never show a half-applied change.

## Errors

Unexpected failures return a generic `500` with the code `server.error`. The exception message is
logged, not returned.

## Testing

- Run everything: `dotnet test WWSHA.slnx` (110 tests: 31 domain, 25 persistence, 54 API).
- Measure coverage: `dotnet test WWSHA.slnx --collect:"XPlat Code Coverage"`. Line coverage is 100%
  and branch coverage is about 90%. The uncovered branches are mostly compiler-generated null
  handling inside LINQ lambdas.
- `InternalsVisibleTo` lets the domain tests exercise defensive guards (for example, adding a hero
  to a closed assignment) that the public API cannot reach.

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
