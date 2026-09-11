---
name: "feature-logic-docs"
description: "Traces how a TaladPOS feature actually works end-to-end (screen -> API -> business logic -> database) by reading the real source code, then writes the findings as Markdown documentation under docs/logic/ with Mermaid high-level, flow, and sequence diagrams. Use this whenever the user asks to explain/document how a screen, flow, or calculation works (e.g. 'อธิบายการทำงานของหน้า...', 'explain the logic of...', 'how does checkout/void/discount/membership work', 'document this flow', 'สร้างเอกสารอธิบาย logic'), even if they don't explicitly mention diagrams or docs/logic — asking 'how does X work' in this repo means 'produce the docs/logic write-up', not just a spoken explanation."
argument-hint: "ชื่อฟีเจอร์/หน้าจอ/flow ที่ต้องการให้อธิบาย เช่น 'หน้าขายสินค้าและโปรโมชั่น', 'ระบบสมาชิก', 'stock management', 'sale void flow'"
user-invocable: true
---

## What this produces

A small set of Markdown files under `docs/logic/` that let someone who has never opened this
codebase understand a feature by reading docs instead of grepping five projects. Every claim in
the output must trace back to code you actually read in this session — never describe behavior
from memory of "how POS systems usually work" or from the spec docs alone without confirming the
code still matches them.

The last time this was done by hand (for the Sales + Promotion/Discount feature), the output was
5 files: a root index, a high-level architecture doc, a screen/flow doc, a business-logic doc with
sequence diagrams, and a data-model doc. Use that as your template for structure and depth, not as
a fixed checklist — a smaller feature (e.g. "login flow") may collapse into 2-3 files, and that's
fine. The point is coverage of the four layers below, not a fixed file count.

## Step 1 — Pin down the scope

If the user names a screen or flow clearly ("หน้าขายสินค้า", "void flow", "member signup"), start
research immediately — don't stall on clarifying questions for something you can figure out by
reading the router. If the ask is genuinely ambiguous (e.g. "explain the reports" when there are
three different report screens), ask one short question before spending a research budget in the
wrong place.

## Step 2 — Trace the stack, layer by layer

TaladPOS is two apps talking over REST/JSON: `web/` (Next.js App Router) and `api/` (ASP.NET Core,
DDD-layered: Api → Application → Domain → Infrastructure), backed by PostgreSQL via EF Core. Read
`references/codebase-map.md` now if you haven't already this session — it has the concrete folder
paths, naming conventions, and where each layer's queries/writes live, so you spend your research
budget reading actual logic instead of rediscovering the project layout from scratch.

Work top-down, and **read the real files, don't infer from names**:

1. **Screen** — find the route under `web/src/app/`, the page component, and every child
   component it renders. Note which pieces of state exist and which are derived vs. fetched.
2. **API client** — for every backend call the screen makes, find its typed wrapper under
   `web/src/lib/api/*.ts`. These usually carry a comment pointing at a `contracts/*.md` file —
   that file is a design doc, not necessarily current behavior, so treat it as a lead to verify
   against the controller, not a citation to copy blind.
3. **Controller → Application → Domain** — follow each endpoint from
   `api/src/TaladPOS.Api/Controllers/**` into its use case / query in
   `api/src/TaladPOS.Application/**`, then into whatever `api/src/TaladPOS.Domain/**` entities or
   shared services it calls. Business rules (validation, calculations, state transitions) live in
   Domain — that's where the actual "logic" the user is asking about usually is, not the
   controller.
4. **Database** — repositories under `api/src/TaladPOS.Infrastructure/Persistence/Repositories/`
   show you exactly which tables and columns are read/written and how (LINQ query shape tells you
   filters, joins, and ordering). Reading the repository + the Domain entity's properties is
   normally enough to write an accurate data-model section — you don't need a running database.
   If one happens to be running (check `docker ps` for a postgres container), a quick `\dt` /
   `SELECT` is a nice sanity check but treat it as optional confirmation, not a required step.

Along the way, watch for FR-XXX / research.md / spec.md references in code comments — this
codebase cites its own requirements inline. When you find one, quote or paraphrase it rather than
re-deriving the "why" yourself; it's a primary source you'd otherwise be guessing at.

If something you observe while reading looks like a bug or an inconsistency with the intended
behavior (e.g. a UI value that doesn't refresh after a mutation, a race condition, an edge case the
code doesn't handle) — note it plainly in the doc as an observation. Don't silently smooth it over,
and don't fix it unless asked; documenting reality accurately is the job here.

## Step 3 — Write the docs

Write in Thai for prose (this project's users work in Thai), keep code identifiers, file paths,
and technical terms (API, endpoint, table names, etc.) in their original form — same convention as
the rest of this repo's documentation and chat responses.

**File layout:** put each documented topic in its own folder so multiple invocations of this skill
never collide or overwrite each other:

```
docs/logic/
├── README.md                  <- master index: one line per topic folder, created if missing,
│                                  appended to (not overwritten) if it already exists
└── <topic-slug>/
    ├── README.md               <- this topic's index + summary
    ├── 01-high-level.md        <- architecture / component diagram for this topic
    ├── 02-<screen-or-flow>.md  <- screen functions, flow diagram, API table
    ├── 03-<logic-name>.md      <- core business-logic detail + sequence diagram(s)
    └── 04-data-model.md        <- ER diagram + table-by-table read/write notes
```

Exception: if `docs/logic/` already contains flat files at the root from before this convention
existed, leave them where they are — don't move or renumber someone else's existing docs. Just add
your new topic as a subfolder and link it from (or create) the root `README.md`.

Skip a file if it would be near-empty for this topic (e.g. a topic with no new tables doesn't need
its own `04-data-model.md` — link to the existing data-model doc for a related topic instead if one
exists).

### Diagram requirements

Use Mermaid fenced code blocks (` ```mermaid `) — GitHub and VS Code render these natively, no
extra tooling needed. Every topic write-up should include, at minimum:

- **One high-level/architecture diagram** (`graph TB` or similar) showing which layers/components
  are involved — can be a light copy of an existing one if the topic doesn't introduce new
  components, but still show where this particular topic's slice sits.
- **One flow diagram** (`flowchart TD`) of the user-facing journey — what a person actually clicks
  through, including branches for error paths and edge cases, not just the happy path.
- **One sequence diagram per distinct backend transaction** (`sequenceDiagram`) showing
  participant-by-participant calls from UI → API client → Controller → UseCase → Repository → DB.
  If a topic has multiple meaningfully different transactions (e.g. "create" vs "void"), give each
  its own sequence diagram rather than cramming both into one.
- **An ER diagram or table** (`erDiagram` or a Markdown table) for any database tables genuinely
  read or written by this topic, noting which code path touches each one.

### Tables to include

Alongside the diagrams, include plain Markdown tables for anything list-shaped and reference-y:
which API endpoints get called and by what client function, which components make up the screen,
which database columns matter and why. These are what someone skims first — diagrams are for
understanding flow, tables are for looking something up quickly.

## Step 4 — Sanity-check before finishing

Re-read what you wrote against the files you actually opened: does every formula, every API path,
every table/column name match what's in the code right now (not what a spec doc *says* it should
be)? If you're not sure about a claim, go re-read the source rather than hedge it with "should" or
"likely" in the doc — the whole point of this skill is that the docs are grounded in the real
implementation.
