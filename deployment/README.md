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
