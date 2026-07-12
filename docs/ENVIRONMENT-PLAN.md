# Environment Setup Plan — Dev / Test / Prod

## Current State (Problems)

| Tier | Issue |
|------|-------|
| **API** | Only `appsettings.Development.json` exists. No Staging/Production files. Real Twilio creds + SMS API key hardcoded and committed. |
| **Web** | Only `environment.ts` + `environment.prod.ts`. No staging config. API URL baked at build time — changing requires full rebuild. |
| **Mobile** | Single `api_config.dart` with hardcoded IP `http://10.106.230.55:5001`. No environment profiles. CI builds APK without passing `--dart-define`, so prod APK hits a local LAN IP. |
| **Infra** | Docker Compose files have fallback default secrets. `appsettings.Development.json` is tracked in git despite being in `.gitignore`. |

---

## Phase 1 — API Environment Files

**Create 3 appsettings files with no secrets (all from env vars):**

| File | Purpose | DB | Secrets |
|------|---------|-----|---------|
| `appsettings.Development.json` | Local dev | `localhost,1433` (Docker SQL) | Env vars or local user-secrets |
| `appsettings.Staging.json` | Test/staging env | Staging Azure SQL / separate DB | Azure Key Vault / env vars |
| `appsettings.Production.json` | Production | Production Azure SQL | Azure Key Vault / env vars |

Each file overrides: `ConnectionStrings`, `JwtSettings`, `CORS:AllowedOrigins`, `Twilio`, `SMTP`, `SMS`, `Embedding`.

**Remove hardcoded secrets** from `appsettings.Development.json` — use `dotnet user-secrets` for local dev instead.

**Update `Program.cs`** — no changes needed (already reads env-specific files via `AddJsonFile(appsettings.{ENV}.json)`).

**Update Docker Compose files** — all three (`docker-compose.dev.yml`, `docker-compose.test.yml`, `docker-compose.yml`) reference `.env` files, never embed secrets.

---

## Phase 2 — Angular Web Environment Files

**Create 3 environment files:**

| File | `production` | `apiUrl` |
|------|-------------|----------|
| `environment.ts` | `false` | `http://localhost:5001/api` |
| `environment.staging.ts` | `false` | `https://sujanmotors-api-staging.azurewebsites.net/api` |
| `environment.prod.ts` | `true` | `https://sujanmotors-api-gtetffcscjg3cyfe.southeastasia-01.azurewebsites.net/api` |

**Update `angular.json`** — add `staging` configuration with `fileReplacements` pointing to `environment.staging.ts`.

**Update `proxy.conf.json`** — rename to `proxy.conf.dev.json`, create `proxy.conf.test.json` for staging API.

**Result:** `ng build` = dev, `ng build --configuration staging` = test, `ng build --configuration production` = prod.

---

## Phase 3 — Mobile (Flutter) Environment Profiles

**Replace single `api_config.dart` with 3 flavor configs:**

| Flavor | `baseUrl` | Build Command |
|--------|-----------|---------------|
| `dev` | `http://10.0.2.2:5001` (Android emulator) | `flutter run --flavor dev` |
| `test` | `https://sujanmotors-api-staging.azurewebsites.net` | `flutter build apk --flavor test` |
| `prod` | `https://sujanmotors-api-gtetffcscjg3cyfe.southeastasia-01.azurewebsites.net` | `flutter build apk --flavor prod` |

**Approach:** Use `--dart-define-from-file` (Flutter 3.x) with per-flavor JSON files:

```
mobile/
  config/
    dev.json    → { "API_BASE_URL": "http://10.0.2.2:5001" }
    test.json   → { "API_BASE_URL": "https://sujanmotors-api-staging.azurewebsites.net" }
    prod.json   → { "API_BASE_URL": "https://sujanmotors-api-gtetffcscjg3cyfe.southeastasia-01.azurewebsites.net" }
```

`api_config.dart` already reads from `String.fromEnvironment('API_BASE_URL')` — no code change needed beyond updating the default.

**Update Android/iOS signing** — separate keystores or signing configs for dev vs prod.

---

## Phase 4 — Infrastructure & Secrets

**Secrets management pattern (all 3 envs):**

| Secret | Dev | Test | Prod |
|--------|-----|------|------|
| DB password | `.env` (local) | Azure Key Vault | Azure Key Vault |
| JWT secret | `dotnet user-secrets` | Azure Key Vault | Azure Key Vault |
| Twilio creds | Not needed | Azure Key Vault | Azure Key Vault |
| API URL (web) | `environment.ts` (compile) | `environment.staging.ts` | `environment.prod.ts` |
| API URL (mobile) | `config/dev.json` | `config/test.json` | `config/prod.json` |

**Update `.gitignore`** — ensure `appsettings.Development.json` is untracked (force-remove from index), and `mobile/config/*.json` are tracked but have no real secrets.

**Docker Compose per env:**
- `docker-compose.dev.yml` — API on `:5292`, SQL on `:1433`, Web on `:4200`
- `docker-compose.test.yml` — API on `:5293`, SQL on `:1434`, Web on `:4201`
- `docker-compose.prod.yml` — production overrides (restart: always, no exposed SQL)

---

## Phase 5 — CI/CD Pipelines

| Pipeline | Trigger | Env | Action |
|----------|---------|-----|--------|
| `api-deploy.yml` | push to `main` | prod | Build Docker → push → deploy to Azure App Service |
| `swa-deploy.yml` | push to `main` | prod | `ng build --configuration production` → deploy to SWA |
| `mobile-apk.yml` | push to `main` or `mobile-v*` | prod | `flutter build apk --dart-define-from-file=config/prod.json` |
| **NEW:** `api-deploy-test.yml` | push to `develop` or PR to `main` | test | Build → deploy to test Azure App Service |
| **NEW:** `swa-deploy-test.yml` | push to `develop` or PR to `main` | test | `ng build --configuration staging` → deploy to test SWA |
| **NEW:** `mobile-apk-test.yml` | manual dispatch | test | `flutter build apk --dart-define-from-file=config/test.json` |

---

## Phase 6 — Quick Reference

### Developer daily workflow

```bash
# Terminal 1: SQL Server
docker compose -f deployment/docker-compose.dev.yml up autopartshop.db

# Terminal 2: API
cd src/AutoPartShop.Api
dotnet run --launch-profile http

# Terminal 3: Web
cd src/AutoPartShop.WebApp
npm start  # proxies to localhost:5001

# Mobile (emulator)
cd mobile
flutter run --flavor dev --dart-define-from-file=config/dev.json
```

### Deploy to test

```bash
git push origin develop
# CI auto-deploys API + Web to test endpoints
# Mobile: manual trigger in GitHub Actions with --dart-define-from-file=config/test.json
```

### Deploy to prod

```bash
git push origin main
# CI auto-deploys all three
```

---

## File Changes Summary

| Component | Files to Create/Modify | Count |
|-----------|----------------------|-------|
| **API** | Create `appsettings.Staging.json`, `appsettings.Production.json`; rewrite `appsettings.Development.json` (no secrets); update `.gitignore`; remove tracked secrets | ~5 |
| **Web** | Create `environment.staging.ts`; update `environment.prod.ts`, `angular.json`, proxy files | ~5 |
| **Mobile** | Create `config/dev.json`, `config/test.json`, `config/prod.json`; update `api_config.dart` default; update `build.gradle.kts` for flavors | ~6 |
| **Infra** | Update 3 docker-compose files; create/update `.env.example` per env; update CI workflows | ~8 |
| **Total** | | **~24 files** |
