# Deployment

Production runs via Docker Compose:

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build -d
```

The web container (nginx) is the only public-facing service; it reverse-proxies
`/api` and `/hubs` to the API, which is not exposed directly.

## Required environment variables (production)

Set these in the environment / `.env` (see `.env.example`) — **never** commit real
values to `appsettings.json`:

| Variable | Purpose |
| --- | --- |
| `ConnectionStrings__AutoPartDb` | SQL Server connection string |
| `JwtSettings__SecretKey` | JWT signing key (rotate if ever exposed) |
| `Cors__AllowedOrigins__0`, `__1`, … | **Required** — allow-list of frontend origins (scheme+host+port). Defaults to `[]`, which blocks every cross-origin call in prod. |
| `Twilio__AccountSid`, `Twilio__AuthToken`, `Twilio__*` | SMS / WhatsApp |
| `sms__apiKey` | SMS provider key |
| `Seed__AdminPassword` | **Required** to bootstrap the first admin outside Development |
| `Seed__DemoUsers` | Leave `false` (default outside Development) — no demo logins in prod |
| `ASPNETCORE_ENVIRONMENT` | `Production` (set by `docker-compose.prod.yml`) |

Swagger is disabled automatically when `ASPNETCORE_ENVIRONMENT=Production`.

## TLS / HTTPS  ⚠️ required before go-live

The API runs with `RequireHttpsMetadata=false` and the in-app HTTPS redirect
disabled. That is **only safe behind TLS termination** — JWT bearer tokens travel
in the clear over plain HTTP. The shipped `nginx.conf` listens on port 80 only, so
out of the box there is **no TLS**. Pick one before exposing the app publicly:

1. **Upstream TLS** (recommended for cloud): terminate TLS at a load balancer /
   reverse proxy (Azure App Gateway, Cloudflare, AWS ALB, …) and forward plain
   HTTP to the web container's port 80. Ensure the proxy sends
   `X-Forwarded-Proto: https` and adds the HSTS header.

2. **TLS in the nginx container**: follow the commented `443` server block in
   `src/AutoPartShop.WebApp/nginx.conf` — mount certificates, uncomment the block
   (it includes HSTS), publish port 443, and switch the port-80 block to a
   `301 https://…` redirect so nothing is served over plain HTTP.

Verify after deploy: `https://<host>` serves the app, `http://<host>` redirects to
HTTPS, and the response carries `Strict-Transport-Security`.

## Database migrations

Migrations apply automatically on API startup (`DatabaseSeeder.MigrateAsync`), so
no manual `dotnet ef database update` step is needed in production.

---

# Azure deployment (App Service + Static Web Apps + Docker Hub + MonsterASP MSSQL)

Split deployment:

- **API** → Azure **App Service for Containers**, image built from
  `src/AutoPartShop.Api/Dockerfile` and stored on **Docker Hub** (`docker.io/subrata23/autopartshop-api`).
- **Frontend** → Azure **Static Web Apps** (Angular build output `dist/smauto/browser`).
- **Database** → **MonsterASP.NET free MSSQL** (external SQL Server, SQL auth over the internet).

Both App Service (`*.azurewebsites.net`) and Static Web Apps are HTTPS by default, so no
manual TLS/cert setup is required for the default hostnames.

CI/CD is GitHub Actions: `.github/workflows/api-deploy.yml` (API → Docker Hub → App Service) and
`.github/workflows/swa-deploy.yml` (frontend). Edit the placeholder values at the top of
`api-deploy.yml` (`IMAGE`, `WEBAPP_NAME`, `RESOURCE_GROUP`) to match what you provision.

## 1. Provision Azure resources (`az` CLI, one-time)

```bash
RG=sujanmotors-rg
LOC=southeastasia
API_APP=sujanmotors-api          # globally unique
PLAN=sujanmotors-plan
SWA=sujanmotors-web

az group create -n $RG -l $LOC

# App Service plan (Linux) + Web App for Containers (placeholder image first)
az appservice plan create -n $PLAN -g $RG --is-linux --sku B1
az webapp create -n $API_APP -g $RG -p $PLAN \
  --deployment-container-image-name mcr.microsoft.com/dotnet/aspnet:10.0

# Point the Web App at the Docker Hub image. If you make the Docker Hub repo PUBLIC, no creds
# are needed; for a PRIVATE repo supply a Docker Hub access token:
az webapp config container set -n $API_APP -g $RG \
  --container-image-name subrata23/autopartshop-api:latest \
  --container-registry-url https://index.docker.io/v1/ \
  --container-registry-user subrata23 \
  --container-registry-password <DOCKERHUB_TOKEN>

# WebSockets (SignalR) + Always On
az webapp config set -n $API_APP -g $RG --web-sockets-enabled true --always-on true

# Static Web App (capture the deployment token for the GitHub secret)
az staticwebapp create -n $SWA -g $RG -l $LOC
az staticwebapp secrets list -n $SWA -g $RG --query "properties.apiKey" -o tsv   # → AZURE_STATIC_WEB_APPS_API_TOKEN
```

## 2. Database — MonsterASP.NET free MSSQL

Create a free MSSQL database in the MonsterASP.NET control panel and copy its connection
string (server host like `dbXXXX.databaseasp.net`, a database name, SQL user + password).
No Azure SQL resource is provisioned. The App Service reaches it outbound over the internet.

> **Semantic search is disabled on this DB.** MonsterASP free MSSQL is SQL Server 2019/2022,
> which lacks the `vector` type. The `AddProductEmbedding` migration is now version-aware: it
> **only** creates the `ProductEmbeddings` (vector) table on SQL Server 2025+ (ProductMajorVersion
> ≥ 17) and is skipped otherwise, so startup migrations succeed. Keep `Embedding__BaseUrl` blank
> (the default) → product search uses keyword matching. If you later move to a SQL 2025 host, the
> table is created automatically and semantic search can be enabled by setting `Embedding__BaseUrl`.

## 3. App Service application settings (runtime env vars)

```bash
az webapp config appsettings set -n $API_APP -g $RG --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  WEBSITES_PORT=8080 \
  ConnectionStrings__AutoPartDb="Server=<dbXXXX.databaseasp.net>;Database=<db-name>;User Id=<db-user>;Password=<db-password>;Encrypt=true;TrustServerCertificate=true;" \
  JwtSettings__SecretKey="<ROTATED_32B+_KEY>" \
  Cors__AllowedOrigins__0="https://<SWA-default-hostname>" \
  Twilio__AccountSid="<rotated>" Twilio__AuthToken="<rotated>" Twilio__SmsMsgServiceSid="<rotated>" \
  sms__apiKey="<rotated>" \
  Seed__AdminPassword="<first-admin-password>" \
  Seed__DemoUsers=false
```

Use the exact MonsterASP connection string they give you (the format above is typical;
`TrustServerCertificate=true` is usually required). Get the SWA hostname with
`az staticwebapp show -n $SWA -g $RG --query defaultHostname -o tsv` and use it
(with `https://`, no trailing slash) for `Cors__AllowedOrigins__0`.

## 4. GitHub repo secrets

| Secret | Source |
| --- | --- |
| `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` | App registration with an OIDC federated credential for this repo; role: Contributor on `$RG` (used to update the Web App) |
| `DOCKERHUB_USERNAME` | Docker Hub username/namespace (`subrata23`) |
| `DOCKERHUB_TOKEN` | Docker Hub access token (Account Settings → Security → New Access Token). Used both to push the image and by App Service to pull it. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | SWA deployment token (command above) |

## 5. Frontend API URL

Set `apiUrl` in `src/AutoPartShop.WebApp/src/environments/environment.prod.ts` to
`https://<API_APP>.azurewebsites.net/api` before the first frontend deploy — the production
build bakes it in, and the SignalR hub URL is derived from it.

## Notes & caveats

- **Single instance at launch:** migrations auto-apply on startup; keep the plan at 1 instance
  initially and back up the DB before deploys. Scaling the API to >1 instance also requires
  **Azure SignalR Service** (or ARR affinity) for real-time notifications.
- **MonsterASP free tier limits:** small max DB size and connection limits, and the DB may idle
  out — fine for a pilot, plan to move to a paid/managed DB as data grows.
- **Cross-provider latency:** API on Azure + DB on MonsterASP means each query crosses the
  internet; acceptable for a small shop but noticeably slower than a co-located DB.
- **Secrets:** set rotated Twilio/SMS/JWT/DB values only as App Service settings — never back in
  `appsettings.json`.
