# TaladPOS

A single-store POS web application: cashiers sell products from an image-based catalog (search by
name/barcode, cart, checkout) with automatic stock deduction, membership lookup, and
promotion/member discounts; admins manage the product catalog, promotions, and view sales history
and reports.

Delivered as two independently deployable applications, per the project constitution:

- **`api/`** — ASP.NET Core Web API, DDD-layered (`Domain` / `Application` / `Infrastructure` /
  `Api`), backed by PostgreSQL via EF Core.
- **`web/`** — Next.js (App Router) + Tailwind CSS frontend, talking to `api/` exclusively over
  REST/JSON, using PrimeReact (unstyled, Tailwind-themed) for `DataTable`/`Dialog`/`Button`.

Full feature documentation — spec, architecture decisions, data model, and API contracts — lives
under [`specs/001-pos-system/`](specs/001-pos-system/). The step-by-step acceptance walkthrough is
[`specs/001-pos-system/quickstart.md`](specs/001-pos-system/quickstart.md); this README covers
getting both apps running locally.

## Prerequisites

- .NET 10 SDK
- Node.js 20 LTS
- A reachable PostgreSQL 16 instance (e.g. `docker run -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16`)

## 1. Database

Set `ConnectionStrings:DefaultConnection` in `api/src/TaladPOS.Api/appsettings.Development.json`
to point at your PostgreSQL instance, then apply migrations from `api/`:

```bash
cd api
dotnet ef database update --project src/TaladPOS.Infrastructure --startup-project src/TaladPOS.Api
```

This creates the schema and seeds one bootstrap Admin account (`admin` / `Admin@12345` —
change immediately after first login).

## 2. API

```bash
cd api
dotnet run --project src/TaladPOS.Api
```

Runs at `http://localhost:5260` by default (see `src/TaladPOS.Api/Properties/launchSettings.json`).
`web/`'s allowed CORS origins are configured under `Cors:AllowedOrigins` in
`appsettings.Development.json`.

## 3. Frontend

```bash
cd web
npm install
npm run dev
```

Runs at `http://localhost:3000` by default. Set `NEXT_PUBLIC_API_BASE_URL` (see
`web/.env.local.example`) if the API isn't at its default address.

## Tests

```bash
# api/ — Domain and Application unit tests (mandatory, Constitution Principle III) and API
# integration tests (require a reachable PostgreSQL instance; each run resets its own database)
cd api
dotnet test

# web/ — build, lint, and type-check (no test framework mandated for the frontend;
# correctness is validated via quickstart.md)
cd web
npm run build
```

## Validating a full walkthrough

Once both apps are running against a live database, follow
[`specs/001-pos-system/quickstart.md`](specs/001-pos-system/quickstart.md) for the full
acceptance walkthrough covering login, stock setup, checkout, membership, promotions, void, and
reports.
