<!--
Sync Impact Report
===================
Version change: (unratified template) → 1.0.0
Rationale: Initial ratification. Project had only an unfilled placeholder scaffold;
this is the first concrete constitution, hence MAJOR version 1.0.0.

Modified principles: N/A (initial adoption, no prior named principles existed)

Added sections:
- I. Clear API/Frontend Separation (NON-NEGOTIABLE)
- II. Domain-Driven Design for the API
- III. Test-First for Business Logic (NON-NEGOTIABLE)
- IV. Frontend Technology Standards
- V. Repository & Folder Structure
- Technology Stack (Section 2)
- Development Workflow & Quality Gates (Section 3)
- Governance

Removed sections: none

Deferred / TODO placeholders: none — all template tokens resolved from user-supplied input.

Templates requiring follow-up review (not modified by this command; read constitution at runtime):
- .specify/templates/plan-template.md — verify Constitution Check section reflects
  DDD layering and API/frontend separation gates.
- .specify/templates/spec-template.md — no direct dependency detected.
- .specify/templates/tasks-template.md — verify task categorization supports
  separate api/ and web/ workstreams and mandatory unit-test tasks for business logic.
-->

# TaladPOS Constitution

## Core Principles

### I. Clear API/Frontend Separation (NON-NEGOTIABLE)
The system MUST be built as two independently deployable applications: a backend API and a
frontend web application. The frontend MUST communicate with the backend exclusively through
the REST API — it MUST NOT access the database, ORM, or any backend-internal module directly,
whether in server components, API routes, or build-time code. The API MUST NOT contain any
frontend rendering or presentation logic. This boundary MUST be enforced at the repository/folder
level (see Principle V), not merely by convention.
**Rationale**: A hard boundary between API and frontend keeps the two deployable and scalable
independently, prevents accidental coupling to database internals, and keeps the REST contract
as the single source of truth for integration.

### II. Domain-Driven Design for the API
The API MUST be implemented in .NET Core (ASP.NET Core Web API) and MUST follow Domain-Driven
Design: business rules and invariants live in a Domain layer independent of persistence and
transport concerns; use-case orchestration lives in an Application layer; persistence,
messaging, and other external integrations live in an Infrastructure layer; HTTP concerns live
in a Presentation/API layer. Entity Framework Core MUST be the ORM, and PostgreSQL MUST be the
system of record. Domain entities and value objects MUST NOT depend on EF Core types or
ASP.NET Core types.
**Rationale**: DDD layering keeps business logic testable and stable while infrastructure
choices (EF Core, PostgreSQL) evolve independently, and prevents persistence concerns from
leaking into core business rules.

### III. Test-First for Business Logic (NON-NEGOTIABLE)
Every unit of business logic in the Domain and Application layers MUST have automated unit
test coverage. Unit tests MUST NOT require a live database, network, or file system — persistence
and external dependencies MUST be abstracted so business logic can be tested in isolation.
Pull requests that add or change business logic without corresponding unit tests MUST be
rejected.
**Rationale**: Business logic is the highest-risk, highest-change-frequency part of the system;
isolated unit tests catch regressions early and document intended behavior without the cost and
flakiness of integration tests.

### IV. Frontend Technology Standards
The frontend MUST be implemented in Next.js styled with Tailwind CSS. All server state MUST be
retrieved and mutated through calls to the backend REST API. The frontend MUST NOT embed
database drivers, ORM clients, or direct database connection strings. Environment configuration
for the frontend MUST only ever reference the API's base URL and public, non-secret values.
**Rationale**: Constraining the frontend to a single stack and a single integration path (REST)
keeps the client thin, keeps secrets and data-access logic on the server side of the API
boundary, and simplifies onboarding.

### V. Repository & Folder Structure
The codebase MUST maintain a clear top-level separation between the API and the frontend: an
`api/` folder (or repository) containing the entire .NET Core solution, and a `web/` folder (or
repository) containing the entire Next.js application. Neither side MUST import source code,
types, or build artifacts from the other; the only permitted contract between them is the REST
API (and any generated API client/types built from that contract).
**Rationale**: Physical separation makes the architectural boundary from Principle I impossible
to casually violate, and allows the two applications to have independent build pipelines,
dependency versions, and deployment lifecycles.

## Technology Stack

- **Backend**: .NET Core (ASP.NET Core Web API), Entity Framework Core, PostgreSQL.
- **Frontend**: Next.js, Tailwind CSS.
- **Integration**: REST API over HTTP(S) as the only channel between frontend and backend;
  request/response payloads MUST use JSON.
- Any deviation from this stack (e.g., introducing a new database engine, ORM, or frontend
  framework) MUST be proposed as a constitution amendment before implementation.

## Development Workflow & Quality Gates

- Every pull request touching `api/` domain or application code MUST include unit tests for the
  business logic it adds or changes; CI MUST run these tests and block merge on failure.
- Every pull request MUST be reviewed for compliance with the DDD layering (Principle II) and
  the API/frontend separation (Principles I and V) before merge.
- Any pull request that introduces direct database access from `web/`, or business logic outside
  the Domain/Application layers of `api/`, MUST be rejected or corrected before merge.

## Governance

This constitution supersedes all other project practices and conventions where a conflict
exists. Amendments require: (1) a documented rationale for the change, (2) an explicit version
bump following semantic versioning — MAJOR for backward-incompatible principle removals or
redefinitions, MINOR for new principles or materially expanded guidance, PATCH for wording or
clarification fixes — and (3) an update to the Sync Impact Report at the top of this file.
All pull requests and code reviews MUST verify compliance with this constitution; any
complexity or deviation from it MUST be explicitly justified in the pull request description.

**Version**: 1.0.0 | **Ratified**: 2026-09-10 | **Last Amended**: 2026-09-10
