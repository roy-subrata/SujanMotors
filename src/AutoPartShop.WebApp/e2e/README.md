# E2E tests (Playwright)

Covers: login, product create/edit/search, supplier creation, customer creation
(+ duplicate-phone rejection), the full purchase→stock→sale→stock business flow with a
database-level assertion, permission/role enforcement, invoices, reports, warranty, and
barcode label printing.

## Prerequisites

Two servers, already running, **before** you invoke Playwright — it does not start or
manage them:

```bash
# 1. SQL Server (if not already up)
docker compose -f deployment/docker-compose.yml up -d autopartshop.db

# 2. API — Development env, port 5001, uses AutoPartShopDevDb (NOT the prod DB the
#    docker-compose "autopartshop.api" container serves — keep this separate so the
#    suite's writes never touch real data)
ASPNETCORE_ENVIRONMENT=Development dotnet run --project ../../src/AutoPartShop.Api

# 3. Angular dev server — port 4301 (4200 is normally taken by the docker web container)
npm run start -- --port 4301 --proxy-config proxy.conf.json
```

The business-flow spec (`05-business-flow.spec.ts`) also shells out to
`docker exec autopartshop.db sqlcmd ...` to confirm stock levels directly against the
database, so the `autopartshop.db` container specifically must be the one running (not a
different SQL Server instance).

## Running

```bash
npm run e2e            # headless, once
npm run e2e:ui         # interactive UI mode
npm run e2e:report     # open the last HTML report
```

`auth.setup.ts` runs first (via the `setup` project in `playwright.config.ts`), logs in
as `admin`/`Admin@1990` through the real login form, and saves the session to
`e2e/.auth/admin.json` for every other spec to reuse — specs don't each pay the login
cost. `01-login.spec.ts` and `06-permissions.spec.ts` opt back out of that saved session
(`test.use({ storageState: { cookies: [], origins: [] } })`) since they're specifically
testing unauthenticated/differently-privileged access.

`06-permissions.spec.ts` depends on a limited-privilege user that must exist first:

- username `e2elimiteduser`, password `TestPass123!`, role `User` only.
- Created once via Admin Settings → Users → Add User, then Assign Roles → `User`.

If that user doesn't exist, only the permissions spec fails — every other spec is
independent.

## Notes on flakiness sources found while writing this suite

- Several PrimeNG fields (the date picker, the POS payment amount field) parse
  **keystrokes** as they arrive — Playwright's `.fill()` sets the DOM value directly and
  skips that parsing, leaving the field looking correct but the form control invalid.
  Use `.click()` + `.pressSequentially()` for those; plain `.fill()` is fine for ordinary
  text/number inputs.
- Several "auto-generated code" placeholders (`p-select`/`p-autocomplete` wrapper
  components) render their placeholder text on both the wrapper element and the real
  `<input>` inside it — `getByPlaceholder` matches both. Use `.last()`.
- Several page headers render twice (once in the fixed topbar, once in the page's own
  header) — `getByRole('heading', { name })` needs `.first()`.
- The Parts list search box only searches on Enter or the search button — typing alone
  (`.fill()`) does not trigger it, unlike the Stock Management search box, which does.
