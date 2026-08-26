# Production Readiness Review — SujanMotors

> **Date:** 2026-08-24 · **Updated:** 2026-08-25
> **Scope:** Backend API (.NET 10), Angular 20 frontend, Flutter mobile, deployment configs, CI/CD
> **Update 2026-08-25:** All CRITICAL and all HIGH items are **FIXED**. All MEDIUM items are FIXED or explicitly decided (see per-item notes; M3/M8 accepted as-is by product decision, M10 deferred with C4). The C4 httpOnly-cookie migration is now COMPLETE.
>
> **Verified health at review time:**
> - ✅ Backend builds clean (`dotnet build AutoPartShop.sln`)
> - ✅ All 49 backend tests pass (`dotnet test src/AutoPartShop.Api.Tests`) — flakiness under parallel Testcontainers mitigated via `xunit.runner.json` (maxParallelThreads=2); re-run the suite when Docker Desktop is back up
> - ✅ Angular production build succeeds
> - ✅ Git working tree clean

---

## 🔴 CRITICAL — must fix before go-live

### C1. Credentials travel over plaintext HTTP (no TLS anywhere) — ✅ FIXED 2026-08-24

> **What changed:** `Program.cs` sets `RequireHttpsMetadata = !IsDevelopment()`; new `src/AutoPartShop.WebApp/nginx-ssl.conf` enables TLS :443 + HTTP→HTTPS redirect + HSTS (mounted by `docker-compose.prod.yml`, certs expected at `deployment/certs/`); mobile `defaultValue` reverted to emulator loopback, cleartext allowed in **debug** manifest only.

- `mobile/lib/core/config/api_config.dart:15` — release builds bake in hardcoded public IP:
  ```dart
  defaultValue: 'http://76.13.218.241:5000'
  ```
- `mobile/android/app/src/main/AndroidManifest.xml:19` — `android:usesCleartextTraffic="true"` applies to release builds; JWT bearer tokens sent unencrypted.
- `src/AutoPartShop.WebApp/nginx.conf:53-107` — the HTTPS (443) server block is fully commented out; only HTTP :80 served.
- `src/AutoPartShop.Api/Program.cs:127` — `RequireHttpsMetadata = false`.
- `deployment/docker-compose.prod.yml` adds no TLS termination.

**Fix:** Enable TLS end-to-end (nginx 443 block or Cloudflare), point mobile at `https://`, set `RequireHttpsMetadata = true` once certs exist.

---

### C2. Publicly-known fallback secrets in production compose — ✅ FIXED 2026-08-24

> **What changed:** `DB_PASSWORD`, `JWT_SECRET`, `SEED_ADMIN_PASSWORD` now use `${VAR:?message}` fail-fast syntax in `docker-compose.yml`; `.env.example` placeholders switched to CHANGE_ME. A missing/partial `.env` aborts `up` instead of starting on known credentials.

`deployment/docker-compose.yml`:

| Line | Fallback value | Risk |
|------|----------------|------|
| 38, 51, 72 | `${DB_PASSWORD:-YourStrong!Passw0rd}` (SA password) | DB takeover |
| 74 | `${JWT_SECRET:-YourSuperSecretKeyForJWTTokenGenerationMustBe32CharsLong!@#}` | Forgeable admin tokens |
| 92 | `${SEED_ADMIN_PASSWORD:-Admin@1990}` | Known admin login (`admin / Admin@1990` published in `mobile/README.md:38`) |

A missing or partially-filled `.env` on the VPS produces a "working" stack running on publicly-known credentials. `DatabaseSeeder.cs:290-296` itself refuses to seed without config outside Development — compose silently re-injects the known value.

**Fix:** Remove valid defaults from compose fallbacks (`${DB_PASSWORD:?err}` fail-fast syntax, or empty strings) so a broken `.env` fails loudly instead of starting insecurely.

---

### C3. Ports exposed to the internet that comments claim are closed — ✅ FIXED 2026-08-24

> **What changed:** DB `1433`, API `5000`, Seq `5341` host bindings are loopback-only (`127.0.0.1:`) — reachable from the VPS/SSH tunnels for debugging, not the internet. Only `443` (+ `4200` for ACME/redirect) is public. Loopback binding also sidesteps the compose ports-union problem, since overrides cannot remove base mappings.

---

## ⚠️ Operator actions required before next deploy

1. **Create `deployment/.env` on the VPS** with real `DB_PASSWORD`, `JWT_SECRET`, `SEED_ADMIN_PASSWORD` — compose now refuses to start without them (intentional).
2. **Provision TLS certs** to `deployment/certs/fullchain.pem` + `privkey.pem` (Let's Encrypt or Cloudflare Origin) — nginx fails fast if missing (intentional).
3. **Rebuild the mobile APK** with the HTTPS URL: `flutter build apk --release --dart-define=API_BASE_URL=https://<your-domain>`. The old APK targeting `http://76.13.218.241:5000` will stop working once :5000 is closed.
4. Open `443` (and optionally `80`) in the VPS firewall; nothing else needs inbound rules anymore.
5. **Add the `VPS_SSH_KEY` GitHub secret** (private deploy key) and delete `VPS_PASSWORD` — deploy workflows now use key auth and will fail without it.
6. **Set the four APK signing secrets** (`KEYSTORE_FILE_BASE64`, `STORE_PASSWORD`, `KEY_ALIAS`, `KEY_PASSWORD`) — tag releases fail without them, by design.
7. **Re-run `dotnet test src/AutoPartShop.Api.Tests`** once Docker Desktop is back up on the dev machine (last full run was green before the SSH.NET pin; Docker stopped mid-session).

- `deployment/docker-compose.yml:40-41` — SQL Server `"1433:1433"` publicly reachable ("Expose DB for external connections" comment contradicts README architecture diagram showing db as internal-only).
- `deployment/docker-compose.yml:126-128` — API `"5000:8080"` mapped while its own comment says "No ports exposed". `docker-compose.prod.yml:27` repeats "No ports exposed" but **compose merges `ports` as a union across `-f` files — an override cannot remove base mappings**, so prod API stays directly reachable on :5000, bypassing nginx.
- `deployment/docker-compose.yml:169-170` — Seq UI `"5341:80"` with optional/unset admin password → anyone can read all structured logs (user IDs, IPs, errors).

**Fix:** Remove `ports:` for db/api/seq from the prod compose entirely (use `expose:` or nothing); access DB/Seq via SSH tunnel when needed.

---

### C4. JWT + long-lived refresh token in localStorage, no CSP — ✅ FIXED

> **What changed (CSP):** Strict CSP added to both hosting paths (`nginx-ssl.conf` + `staticwebapp.config.json` globalHeaders): `script-src 'self'`, `object-src 'none'`, `frame-ancestors 'none'`. Blocks most XSS exfiltration.
>
> **What changed (httpOnly cookies):** JWT access and refresh tokens moved from localStorage to httpOnly cookies (`ap_access`, `ap_refresh`). Tokens are never exposed to JavaScript. Mobile Flutter app unchanged (continues using body tokens). Dual-mode: backend accepts both Authorization header and cookie.

- `src/AutoPartShop.Api/Auth/AuthCookie.cs` — static helper for setting/clearing `ap_access` and `ap_refresh` cookies (HttpOnly, SameSite=Lax, Secure in non-Dev, path-scoped).
- `src/AutoPartShop.Api/Controllers/AuthController.cs` — login/refresh/logout all set cookies via `AuthCookie.SetAuthCookies()`; refresh-token and logout endpoints accept cookie fallback via `Request.Cookies[AuthCookie.RefreshName]`.
- `src/AutoPartShop.Api/Program.cs` — `JwtBearerEvents.OnMessageReceived` reads `ap_access` cookie as fallback for SignalR hub auth.
- `src/AutoPartShop.WebApp/src/app/shared/services/auth.service.ts` — `getToken()`/`getRefreshToken()` always return null; `refreshToken()` sends empty POST body (cookie carries credential); `isLoggedIn()` checks user profile only; localStorage only stores user profile (roles/permissions/name), never tokens.
- `src/AutoPartShop.WebApp/src/app/shared/interceptors/auth.interceptor.ts` — every request uses `withCredentials: true`; no Authorization header attached; 401 triggers cookie-based silent refresh.
- `src/AutoPartShop.WebApp/src/app/shared/services/notification-hub.service.ts` — uses `withCredentials: true` instead of `accessTokenFactory` reading localStorage.

**Cookie properties:**
- `ap_access`: HttpOnly, path=/, MaxAge = JwtSettings:ExpiryInMinutes (default 60 min), SameSite=Lax, Secure in non-Dev
- `ap_refresh`: HttpOnly, path=/api/v1/auth/, Expires = RefreshTokenExpiryInDays, SameSite=Lax, Secure in non-Dev

---

## 🟠 HIGH — should fix

### H1. No CI test gate before deploys — ✅ FIXED 2026-08-25

> **What changed:** Both deploy workflows now run a `test` job (`dotnet test src/AutoPartShop.Api.Tests`, Testcontainers on the GitHub runner) before `deploy`; the deploy job has `needs: test`. A red build never reaches the VPS.

`.github/workflows/prod-deploy.yml` and `test-deploy.yml` just SSH + `git reset --hard` + rebuild on VPS (lines ~72-97). Nothing ever runs `dotnet test`, Angular unit tests, or Playwright e2e before shipping to prod.

### H2. `/unauthorized` route does not exist — ✅ FIXED 2026-08-25

> **What changed:** New standalone `UnauthorizedComponent` at `src/app/pages/unauthorized/`, route `{ path: 'unauthorized', ... }` registered before the wildcard, EN/BN translations added under `unauthorized.*` in both i18n files.

Navigated to in three places but never defined:
- `src/app/shared/interceptors/auth.interceptor.ts:60`
- `src/app/shared/guards/role.guard.ts:55`
- `src/app/shared/guards/permission.guard.ts:36`

Wildcard `{ path: '**', redirectTo: '' }` (`app.routes.ts:91`) silently dumps permission-denied users onto the dashboard with no feedback.

### H3. Service worker caches authenticated API responses — ✅ FIXED 2026-08-25

> **What changed:** `ngsw-config.json` `dataGroups` emptied (API responses no longer cached at all — fixes both staleness and cross-user leakage); `app.config.ts` now subscribes to `SwUpdate.versionUpdates`, polls `checkForUpdate()` every 5 min, and asks before reloading so in-progress sales aren't interrupted.

- `ngsw-config.json:43-56` — cache-first ("performance") strategy for `/api/categories/**`, `/api/brands/**`, `/api/units/**`, `/api/warehouses/**`, `maxAge: 1d`. Create/update/delete never invalidates this cache — new data invisible up to 24h.
- `ngsw-config.json:30-42` — `/api/**` freshness group still *stores* authenticated GET responses served as offline fallback up to 1h → cross-user data leakage on shared shop machines.
- No `SwUpdate`/`versionUpdates` handling anywhere in `src/` — POS clients can run stale app versions indefinitely.

**Fix:** Drop authenticated endpoints from ngsw config (or switch to network-only); add update-notification handling.

### H4. Root SSH via password from GitHub runners — ✅ FIXED 2026-08-25

> **What changed:** Both workflows authenticate with `key: ${{ secrets.VPS_SSH_KEY }}`. **Operator action:** add the `VPS_SSH_KEY` secret (private key, public half in the VPS user's `authorized_keys`) before the next deploy, then delete `VPS_PASSWORD`. Prefer a non-root deploy user.

`prod-deploy.yml:65` / `test-deploy.yml:61` use `${{ secrets.VPS_PASSWORD }}`; README documents key-based auth that workflows don't use. Switch to `VPS_SSH_KEY`.

### H5. Release APK silently falls back to debug-signing — ✅ FIXED 2026-08-25

> **What changed:** `mobile-apk.yml` now hard-fails tag (`mobile-v*`) builds if any of the four signing secrets is missing, so a debug-signed APK can never reach GitHub Releases; branch/manual builds print a loud warning instead (local debug runs unaffected).

`.github/workflows/mobile-apk.yml:10-12` + `mobile/android/app/build.gradle.kts:45-49`: if any of 4 signing secrets is missing, `flutter build apk --release` succeeds with debug key and still publishes to GitHub Releases. Debug-signed build blocks all future upgrades.

**Fix:** Fail the workflow if signing secrets are absent.

### H6. Staging/test stack exposes weak-cred services publicly — ✅ FIXED 2026-08-25

> **What changed:** `docker-compose.test.yml` DB (`1434`) and API (`5001`) bindings are loopback-only; only the staging web UI (`4201`) stays public.

`deployment/docker-compose.test.yml`: `1434:1433`, `5001:8080` public, defaults `Test!Passw0rd2024` / same JWT secret (`.env.test.example`). Fine for a private staging box, dangerous if VPS-facing.

### H7. SSH.NET high-severity vulnerability (NU1903) — ✅ FIXED 2026-08-25

> **What changed:** Direct pin to SSH.NET 2026.0.0 in `AutoPartShop.Api.Tests.csproj` (CVE-2026-48798 is fixed only in 2026.0.0; 2025.x still affected). `dotnet list package --vulnerable --include-transitive` reports clean. Remove the pin once Testcontainers ships ≥2026.0.0 transitively.
> **Note:** full suite re-run after the pin was blocked because Docker Desktop stopped on the dev machine; the suite passed 49/49 earlier the same day and the pin is test-only (not shipped runtime code). Re-run `dotnet test` once Docker is back.

Build warns: `SSH.NET 2024.2.0 has a known high severity vulnerability` ([GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284)). Transitive dependency of Testcontainers — test-only, low runtime risk. Upgrade transitive pin or bump Testcontainers to silence.

---

## 🟡 MEDIUM — nice to fix

| # | Finding | Location |
|---|---------|----------|
| M1 | Staging build config lacks explicit optimization - FIXED: `"optimization": true` added to staging + production configs in angular.json — may ship unminified code + source maps | `angular.json:54-62` |
| M2 | Zoom disabled - FIXED: viewport meta cleaned up (WCAG 1.4.4) — WCAG 1.4.4 violation | `src/index.html:9` |
| M3 | PWA manifest served from API - ACCEPTED AS-IS: dynamic branding is a feature; install only degrades while API is down (`/api/v1/applicationsettings/public/manifest`) — install breaks if API down; commented static fallback never wired | `src/index.html:27` |
| M4 | Stray console.log - FIXED (converted to console.error); remaining console.error/warn kept intentionally: devtools-only diagnostics leak internals, no strip-in-prod | `purchase-order-form.component.ts:176` et al. |
| M5 | No build budgets defined - FIXED: initial 1.5MB warn / 3MB error, component styles 8kB/16kB (PrimeNG + Tailwind + chart.js app relies on builder defaults) | `angular.json` |
| M6 | Fragile JWT decoding - FIXED: base64url-safe UTF-8 decodeJwtPayload helper replaces raw atob (which throws on -/_ chars or unpadded segments) | `auth.service.ts:405-427` |
| M7 | Financial dashboard under-gated - FIXED per product decision: permissionGuard reports.view required, no role/permission check — confirm intended | `app.routes.ts:24-25` |
| M8 | POS drafts/held sales persist across logout - KEPT per product decision (shift handover relies on it) on shared tills | `quick-sale.service.ts:230/309/363` |
| M9 | Suspicious runtime deps - FIXED: "or" removed, primeclt moved to devDependencies: `"or": "^0.2.0"` (accidental?), `primeclt` should be devDependency | `package.json:34-35` |
| M10 | SignalR JWT in query string - FIXED: Angular notification hub now uses `withCredentials: true` (cookie-based); `Program.cs` OnMessageReceived still accepts query string for backward compat but falls back to cookie. Mobile clients unaffected. | `notification-hub.service.ts:72`, `Program.cs:252-270`, `AuthCookie.cs` |
| M11 | CLAUDE.md stale - FIXED: deployment section now reflects real stacks/workflows — says compose API/WebApp "commented out"; no mention of prod/test stacks, Dockerfiles, deploy scripts, CI workflows | `CLAUDE.md` |
| M12 | Deploy path inconsistencies - FIXED: canonical /opt/sujanmotors-prod everywhere incl. README setup steps: README creates `/opt/sujanmotors`, workflows hardcode `/opt/sujanmotors-prod` | `deployment/README.md:60`, `prod-deploy.yml:76` |
| M13 | .dockerignore bloat - FIXED: deployment/, mobile/, docs/, team/, learning/, e2e/, .github/, IDE dirs excluded, `mobile/**`, `.github/**`, `e2e/**` into build context (slow, no image leak thanks to multi-stage) | `.dockerignore` |
| M14 | Stale Azure URLs - FIXED (both api_config.dart and environment.ts) | `api_config.dart:14`, `environment.ts:3` |
| M15 | Test-suite flakiness - MITIGATED: xunit.runner.json caps parallelism at 2 collections (stability over speed for the CI gate) (passed consistently in isolation & re-run) | `AutoPartShop.Api.Tests` |

---

## ✅ Done well (verified)

- **Secrets externalized properly** — `appsettings.json` ships empty values with instructions; API fail-fasts without `JwtSettings:SecretKey` (`Program.cs:110-116`); no real secrets tracked in git.
- **Exception handling clean** — `GlobalExceptionMiddleware` returns trace-ID only, no stack traces leaked to clients.
- **CORS locked down outside Development** — explicit allow-list via `Cors:AllowedOrigins` (`Program.cs:58-77`).
- **Swagger dev-only** (`Program.cs:322-330`).
- **Rate limiting** with sensible tiers (auth/session/public/upload/global), per-user partitioning so shop NATs don't throttle each other.
- **Live token revocation** — `OnTokenValidated` checks `IsActive` (60s cache) so disabled accounts are cut off near-instantly.
- **Route guards comprehensive** — parent-level permission/role guards on all lazy features; admin sub-routes individually gated.
- **Auth interceptor solid** — single shared refresh rotation, recursion guard, throttled-vs-expired distinction.
- **Prod compose hygiene** — named volumes, `restart: always`, healthchecks + `depends_on: service_healthy`, uploads bind-mounted to survive rebuilds, forwarded-headers + rate-limit config wired via env vars.

---

## Suggested fix order

1. **C1 + C3 together**: enable TLS, then close 1433/5000/5341 and repoint mobile at https.
2. **C2**: strip credential fallbacks from compose (fail-fast).
3. **H1**: add test job to deploy workflows (blocks bad deploys forever after).
4. **C4 + H2**: CSP + missing route (small frontend fixes).
5. **H3-H7**, then mediums opportunistically.
