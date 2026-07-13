# Feature Spec — <Feature Name>

<!--
HOW TO USE THIS TEMPLATE
- Copy this file to team/specs/<feature-name>.md (create the folder if needed)
  and fill it in BEFORE any code is written.
- Sections marked (required) must always be filled. The rest depend on the
  requirement — delete sections that don't apply rather than leaving them
  empty; an empty section tells the LLM nothing, a deleted one tells it the
  area is not affected.
- Write for a reader with zero chat history. The spec must stand alone.
- After implementation, update this spec to match what was actually built.
-->

| | |
|---|---|
| **Status** | Draft / Approved / In development / Done |
| **Branch** | `feature/<kebab-name>` |
| **Owner** | <engineer name> |
| **Date** | <YYYY-MM-DD> |

## 1. Problem / Goal (required)

<!-- 2–5 sentences. What user problem does this solve? Why now?
     State the business rule in plain words, e.g.:
     "Salespeople give cart-level discounts at the POS, but the discount only
      appears on the invoice, not the sales order, so daily sales reports
      overstate revenue." -->

## 2. Scope (required)

**In scope:**
- <bullet list of what will be built>

**Out of scope:** <!-- this section prevents the LLM from over-delivering -->
- <explicitly excluded things, e.g. "online ecommerce checkout", "mobile app">

## 3. Actors & Channels (required)

<!-- Who uses this, and from where? Check all that apply — each channel is a
     client that must be updated or explicitly excluded. -->
- [ ] Web POS (salesperson)
- [ ] Web back-office (admin/manager)
- [ ] Mobile app (Flutter)
- [ ] Ecommerce storefront (customer)
- [ ] Background job / scheduled task
- [ ] External API consumer

## 4. Functional Requirements (required)

<!-- Numbered, testable statements. One behavior per line.
     "FR-3: A walk-in customer cannot complete a sale with a due balance." -->
- FR-1: …
- FR-2: …

## 5. Business Rules & Edge Cases

<!-- The rules an LLM cannot guess: rounding, limits, who wins on conflict,
     what happens at zero/negative/duplicate, currency, permissions.
     These are the highest-value lines in the whole spec. -->
- Rule: …
- Edge case: … → expected behavior: …

## 6. Data Model Changes

<!-- New/changed entities and fields. Note nullability, defaults, and whether
     existing rows need a backfill. Say "No schema changes" if none. -->
| Entity | Change | Notes |
|---|---|---|
| `<Entity>` | add `<Field> (decimal, default 0)` | backfill: … |

Migration: `<MigrationName>` — destructive? yes/no

## 7. API Contract

<!-- New or changed endpoints, following team/standards/api.md.
     Include request/response DTO shapes for anything new. Flag breaking
     changes loudly. Say "No API changes" if none. -->
```
POST /api/v1/<resource>
Request:  { ... }
Response: { "data": { ... } }
Errors:   400 <when>, 404 <when>, 409 <when>
```

Authorization: `<permission name / role / [AllowAnonymous]>`

## 8. UI / UX

<!-- Per affected screen: what the user sees and does. Reference an existing
     page as the pattern: "list page follows the data-page design system,
     like features/inventory/brands". Attach mockups to docs/ if any. -->
- Screen: … — behavior: …

## 9. Reference Implementation (required)

<!-- Name the closest existing feature the agent should imitate, with paths.
     This single line saves more rework than any other section. -->
- Follow the pattern of: `<path to similar feature>`

## 10. Non-Functional Requirements

<!-- Only what actually applies: performance targets, volume expectations,
     concurrency (two cashiers at once?), auditability, i18n (EN/BN). -->

## 11. Acceptance Criteria (required)

<!-- Concrete pass/fail checks a tester (or agent) can run. Include the
     exact numbers where money or stock is involved. -->
- [ ] Given …, when …, then …
- [ ] `dotnet build` + `dotnet test` + `ng build` pass

## 12. Open Questions

<!-- Anything unresolved. An agent must ASK about these, not guess.
     Move answered questions into the sections above. -->
- Q: … → A (pending): …
