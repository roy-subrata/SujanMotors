# Development Workflow — Human + AI

## Purpose

This document defines how a feature or fix moves from idea to production in
this repo, and — equally important — **what context the engineer must give the
LLM at each step**. An AI agent is only as good as the context it starts with;
this workflow exists to make that context repeatable instead of ad-hoc.

---

# 1. The Golden Rule

> **Spec first, prompt second.**

Never start an agent on a non-trivial task from a chat message alone. Write
(or update) a feature spec from `team/templates/feature-spec.md` first. A chat
prompt describes what you *want*; a spec describes what the system must *do*,
what it must *not* do, and how you will *know it works*.

Trivial tasks (typo, rename, one-line fix) skip the spec — but still state the
expected behavior and how to verify it.

---

# 2. Workflow Overview

```
Requirement
    │  write feature spec (templates/feature-spec.md)
    ▼
Plan            ← agent proposes; engineer approves BEFORE code is written
    │  branch: feature/<name> or fix/<name>, from main
    ▼
Implement       ← smallest vertical slices; build/tests green after each
    ▼
Verify          ← run the actual flow, not just the compiler
    ▼
Review          ← code review (human + AI reviewer)
    ▼
PR → dev → test (auto-deploys :4201) → main (auto-deploys :4200)
```

Branch naming: `feature/<kebab-name>`, `fix/<kebab-name>`, `chore/<kebab-name>`.

---

# 3. Step 1 — Understand Before Planning

Before any plan, the engineer (or the agent, instructed to do so) must locate:

| Question | Where to look |
|---|---|
| Does a similar feature already exist? | Closest existing feature — reuse its patterns |
| Which entities are involved? | `src/AutoPartShop.Domain/Entities/` |
| Which endpoints exist already? | `src/AutoPartShop.Api/Controllers/` |
| What are the UI patterns? | Existing pages in `src/AutoPartShop.WebApp/src/app/features/` |
| Is the mobile app affected? | `mobile/lib/features/` |
| What are the standards? | `team/standards/*.md` |

**Anti-pattern**: letting an agent invent a new pattern when the codebase
already has one. Always name a reference implementation in the spec
("follow the pattern of `features/inventory/brands`").

---

# 4. Step 2 — Plan and Get It Approved

Ask the agent for a plan **before** it writes code. A good plan lists:

- Files to create/modify, grouped by layer (Domain → Application →
  Infrastructure → Api → WebApp → mobile)
- Database changes (new EF migration? backfill needed? destructive?)
- API contract changes (new endpoints, changed DTOs — flag anything breaking)
- Permission/authorization impact (seeded permissions, `HasPermission` policies)
- What is explicitly **out of scope**

The engineer reviews the plan against the spec. Catching a wrong direction at
plan time costs minutes; at review time it costs the whole implementation.

---

# 5. Step 3 — Implement in Vertical Slices

Work in slices that each end in a **verifiable state**:

```
Slice 1: Domain entity + migration          → dotnet build passes
Slice 2: Application service + API endpoint → endpoint testable via Swagger
Slice 3: Frontend page/dialog               → flow works end-to-end
Slice 4: Mobile (if in scope)               → flutter analyze + manual check
```

Rules during implementation:

- **All sales/stock/payment writes go through a transaction** — follow the
  `CreateExecutionStrategy` + `BeginTransactionAsync` pattern used in
  `SalesOrderController`.
- Money amounts are `decimal`. Stock movements go through lots (FIFO).
- A cross-channel rule (web POS, mobile, ecommerce) belongs in the **API**,
  never duplicated per client. If a client computes something the server also
  computes, the server value wins.
- Match surrounding code style; do not reformat files you aren't changing.

---

# 6. Step 4 — Verify Like a User, Not Like a Compiler

Minimum gates before a PR:

| Layer | Gate |
|---|---|
| Backend | `dotnet build AutoPartShop.sln` + `dotnet test src/AutoPartShop.Api.Tests` |
| Frontend | `npx ng build` in `src/AutoPartShop.WebApp` |
| Mobile | `flutter analyze` in `mobile/` |
| Behavior | Drive the actual flow once (POS sale, report, import, …) |

"It compiles" is not verification. For anything touching money, stock, or
permissions, verify the **numbers** end-to-end (e.g., discount shows correctly
on the sales list *and* the invoice *and* the receipt).

---

# 7. Step 5 — Review and Merge

- Every PR gets a review; use the AI code-reviewer for a first pass, a human
  for the final one.
- One concern per PR. A bug fix and a refactor are two PRs.
- Commit messages: `feat:`, `fix:`, `chore:`, `refactor:` prefixes; imperative
  mood; body explains *why*, not *what*.
- Flow: PR into `dev` (or `test` for release candidates) → `test` branch
  auto-deploys to the test stack → merge to `main` deploys production.

---

# 8. What the LLM Must Be Told (Context Checklist)

When starting an agent on a task, provide — or point it at — all of these:

- [ ] The **feature spec** (or, for a bug, reproduction + expected behavior)
- [ ] A **reference implementation** in this repo to imitate
- [ ] The relevant **standards files** (`team/standards/...`)
- [ ] Any **hard constraints** (no breaking API changes, no destructive
      migration, deadline scope cuts)
- [ ] What is **out of scope** — agents over-deliver without this
- [ ] How to **verify** (which flow to run, what numbers to check)

And keep durable knowledge durable: when a decision is made in chat
("variants always come back as an array"), record it in the spec or a
standards file. Chat context evaporates; documents persist.

---

# 9. Definition of Done

- [ ] Behavior matches the spec's acceptance criteria
- [ ] All build/test gates in §6 pass
- [ ] The flow was exercised manually end-to-end
- [ ] No secrets, no `console.log`/`Console.WriteLine` debugging left behind
- [ ] Migration reviewed (no accidental drops; backfill included if needed)
- [ ] Docs updated: the feature spec reflects what was actually built;
      `docs/` updated if the feature is user-facing
- [ ] PR describes the change, its risk, and how it was verified
