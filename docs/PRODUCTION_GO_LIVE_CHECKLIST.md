# Production Go-Live Checklist — SujanMotors

> **Purpose:** step-by-step runbook to take the current codebase live on the Hostinger VPS.
> All CRITICAL/HIGH code-level findings from `docs/PRODUCTION_READINESS_REVIEW.md` are already
> fixed (2026-08-25) — what's left is **operator setup**, not code changes. Work through this
> top to bottom; each step lists the exact com3mands.

---

## 0. Prerequisites

- [ ] You have root/sudo SSH access to the Hostinger VPS
- [ ] You own a domain (or subdomain) you can point at the VPS
- [ ] `dotnet test src/AutoPartShop.Api.Tests` passes locally (49/49) and `git status` is clean
- [ ] You're on `main` for prod, `test` branch exists for staging

---

## 1. VPS Initial Setup (one-time)

**Why manual, when we have CI/CD?** `prod-deploy.yml` / `test-deploy.yml` only *update* an
already-running stack — the script literally does
`cd /opt/sujanmotors-prod || { echo "Directory not found! Run initial setup first."; exit 1; }`
and then `git fetch` + `docker compose down` + `up --build`. It never creates the directory,
never clones the repo, and never creates `.env`. Those things have to exist *before* CI/CD's
first run, so this step (and the first `.env` in step 3, and the first manual `up` in step 6)
is a **one-time bootstrap you do by hand**. After this, every future deploy is just a `git push`
to `main`/`test` — you won't run `docker compose up` manually again unless you're debugging.

```bash
ssh root@<YOUR_VPS_IP>

# Docker + Compose
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Deployment directories
sudo mkdir -p /opt/sujanmotors-prod /opt/sujanmotors-test
sudo chown $USER:$USER /opt/sujanmotors-prod /opt/sujanmotors-test

# Clone repo into each
cd /opt/sujanmotors-prod && git clone <YOUR_REPO_URL> . && git checkout main
cd /opt/sujanmotors-test && git clone <YOUR_REPO_URL> . && git checkout test
```

---

## 2. Domain + TLS

**Why:** Every request — including login — carries a JWT and now httpOnly session cookies.
Over plain HTTP, anyone on the same network (café wifi, a compromised router, an ISP) can read
those in transit and impersonate the user. TLS is what makes "httpOnly cookie" actually mean
something: the `Secure` flag on the cookie refuses to even send it over HTTP, so without a cert
the app effectively can't authenticate at all in prod. This is also why `RequireHttpsMetadata`
is now `true` outside Development — the API itself refuses to trust tokens presented over HTTP.

Pick one:

### Option A — Cloudflare (recommended, easiest)
1. Add your domain to Cloudflare (free plan works)
2. Point the domain's A record at the VPS IP
3. Enable the Cloudflare proxy (orange cloud)
4. SSL/TLS mode → **Full (Strict)**
5. Enable **Always Use HTTPS**

### Option B — Let's Encrypt on the VPS
```bash
sudo apt install certbot
sudo certbot certonly --standalone -d yourdomain.com
```
Certs need to land at `deployment/certs/fullchain.pem` and `deployment/certs/privkey.pem` —
`nginx-ssl.conf` fails fast if they're missing. Set up a renewal cron/systemd timer
(`certbot renew`) since Let's Encrypt certs expire every 90 days.

- [ ] Domain resolves to VPS IP
- [ ] Cert files present (or Cloudflare Full-Strict active)

---

## 3. Secrets — `deployment/.env`

**Why:** `docker-compose.yml` used to ship with working fallback values
(`YourStrong!Passw0rd`, a hardcoded JWT signing key, `Admin@1990`) baked into the file itself.
Those values are in git history forever — anyone with repo access, or anyone who ever cloned it,
effectively has the keys to any deployment that didn't override them. A DB password leak is a
full data breach; a leaked JWT secret means anyone can forge an admin token for *any* account
without even needing a password. Fail-fast (`${VAR:?...}`) turns "silently running on known
credentials" into "compose refuses to start" — a loud, obvious failure beats a quiet
vulnerability every time. Compose now **fails to start** without these — this is intentional,
don't add fallback defaults back in.

```bash
cd /opt/sujanmotors-prod
cp deployment/.env.prod.example deployment/.env
nano deployment/.env
```

Fill in real values — see `deployment/.env.example` for the full list:

| Variable | Notes |
|---|---|
| `DB_PASSWORD` | Strong, unique. Generate: `openssl rand -base64 32` |
| `JWT_SECRET` | 64+ random chars. Generate: `openssl rand -base64 64` |
| `SEED_ADMIN_PASSWORD` | Not `Admin@1990` (that value is published in `mobile/README.md`) |
| `JWT_EXPIRY_MINUTES` / `JWT_REFRESH_DAYS` | Defaults 60 / 7 are fine |
| `TWILIO_*` | Only if SMS/WhatsApp notifications are wanted |
| `SMTP_*` | Only if email notifications are wanted |
| `SHOP_TZ_OFFSET_MINUTES` | 360 = UTC+6 (Bangladesh) — leave as-is unless the shop is elsewhere |
| `RATE_LIMIT_*` | Defaults are sane; `RATE_LIMIT_ENABLED=false` is the kill switch |
| `FORWARDED_HOP_COUNT` | `1` for nginx alone, `2` if Cloudflare proxy sits in front |

Repeat for `/opt/sujanmotors-test/deployment/.env` with `.env.test.example` and *different* secrets.

- [ ] `deployment/.env` created on VPS with real, unique secrets (prod ≠ test)
- [ ] No secret reused between prod/test/dev
- [ ] `.env` is **not** committed (already gitignored — verify with `git status`)

---

## 4. Firewall

**Why:** the compose files bind DB (`1433`), API (`5000`/`5001`), and Seq (`5341`) to
`127.0.0.1:` instead of `0.0.0.0:` — but that's a second layer, not the only one. Port scanners
constantly probe every public IP on every port; if the VPS firewall itself allows those ports
through, a misconfigured compose override or a future change to `ports:` could re-expose them
without anyone noticing. The firewall is the backstop: even if something in Docker regresses,
nothing but nginx on 443 is reachable from the internet. Only nginx needs to be public — DB/API/Seq
bindings are already loopback-only in compose, but don't rely on that alone.

```bash
sudo ufw allow 22/tcp     # SSH
sudo ufw allow 443/tcp    # HTTPS
sudo ufw allow 4200/tcp   # ACME/redirect (prod) — optional, only if not behind Cloudflare
sudo ufw allow 4201/tcp   # staging web, if VPS-facing
sudo ufw enable
```

- [ ] No firewall rule opens 1433 (DB), 5000/5001 (API), or 5341 (Seq) to the internet

---

## 5. GitHub Repo Configuration

**Why:** the deploy workflows used to SSH into the VPS using a `VPS_PASSWORD` secret — password
auth over SSH is brute-forceable and, unlike a key, a password can be guessed without needing to
steal anything first. An SSH key pair is cryptographically strong and the private half never
leaves GitHub's encrypted secret store. Same logic for APK signing secrets: without them, a tag
build used to silently fall back to **debug signing** and still publish to GitHub Releases — a
debug-signed APK can never receive a signed upgrade later, so users would be stuck reinstalling
from scratch. The workflow now hard-fails instead of shipping a broken upgrade path.

**Settings → Secrets and variables → Actions:**

| Secret | Value |
|---|---|
| `VPS_HOST` | VPS IP |
| `VPS_USER` | SSH username |
| `VPS_SSH_KEY` | Private deploy key (see below) — **not** `VPS_PASSWORD`, delete that secret if present |

Generate a dedicated deploy key:
```bash
ssh-keygen -t ed25519 -f github-actions-deploy -N ""
ssh-copy-id -i github-actions-deploy.pub <user>@<VPS_IP>
cat github-actions-deploy   # paste into VPS_SSH_KEY secret
```

For mobile release builds (`.github/workflows/mobile-apk.yml`, tag builds only), also add:

| Secret | Purpose |
|---|---|
| `KEYSTORE_FILE_BASE64` | `base64 -w0 your-release.keystore` |
| `STORE_PASSWORD` | Keystore password |
| `KEY_ALIAS` | Signing key alias |
| `KEY_PASSWORD` | Signing key password |

Tag builds now hard-fail if any of the four are missing (no silent debug-signed fallback).

- [ ] `VPS_SSH_KEY` set, `VPS_PASSWORD` removed
- [ ] Deploy key's public half is in the VPS user's `~/.ssh/authorized_keys`
- [ ] APK signing secrets set (if shipping tagged mobile releases)

---

## 6. First Build & Start

**Why:** this is the step that proves everything above actually works together — a `.env` with
a typo, a missing cert file, or a firewall rule that's too tight all surface here as a container
stuck in `restarting` rather than in production three weeks from now when a customer can't check
out. Cheaper to find out now, with nobody depending on the system yet. It's also the **only**
time you run `up --build` by hand for a fresh environment — CI/CD's deploy script only does
`down` + `up --build` on a stack that's already running (see step 1); it has no code path to
bring one up from nothing.

```bash
cd /opt/sujanmotors-prod
docker compose -p sujanmotors-prod --env-file deployment/.env \
  -f deployment/docker-compose.yml -f deployment/docker-compose.prod.yml up --build -d

cd /opt/sujanmotors-test
docker compose -p sujanmotors-test --env-file deployment/.env \
  -f deployment/docker-compose.test.yml up --build -d
```

Verify:
```bash
docker compose -p sujanmotors-prod ps
docker compose -p sujanmotors-prod logs -f autopartshop.api
curl -I https://yourdomain.com/api/health
```

- [ ] All containers healthy (`docker compose ps` shows `healthy`, not `restarting`)
- [ ] `https://yourdomain.com` loads over TLS with a valid cert
- [ ] Login works and admin password is **not** the seed default

---

## 7. Mobile App

**Why:** unlike the web app, which reads its API URL at runtime, the Flutter release APK
**compiles the API base URL into the binary**. The old APKs point at a plaintext
`http://<IP>:5000` — once that port is closed behind the firewall in step 4, those APKs stop
working entirely, with no way to fix it except reinstalling a new build. Anyone still running an
old APK after go-live will silently lose connectivity.

The release APK bakes in the API base URL at build time.

```bash
flutter build apk --release --dart-define=API_BASE_URL=https://yourdomain.com
```

- [ ] Old APKs pointed at `http://<old-IP>:5000` are retired/not distributed further
- [ ] New APK built against the HTTPS domain, tested against prod before wide distribution
- [ ] If publishing via tag (`mobile-vX.Y.Z`), confirm the four signing secrets produced a release-signed build (check the workflow run, not just that it succeeded)

---

## 8. CI Gate Sanity Check

**Why:** the workflows used to run tests and deploy as parallel/independent steps — a failing
test didn't stop the deploy, it just showed red *next to* a production rollout that happened
anyway. `needs: test` makes the deploy job wait on the test job's outcome, so a broken build
literally cannot reach the VPS. Trust but verify: a YAML typo (`needs:` pointing at the wrong
job name, or missing entirely) is an easy mistake that silently reverts you to the old
unsafe behavior, so prove it blocks once rather than assuming the file is correct.

Both `prod-deploy.yml` and `test-deploy.yml` now run backend tests before deploying
(`needs: test`). Confirm this is actually true before trusting it:

- [ ] Push a trivial commit to `test` branch, watch the Actions run — the `test` job must complete before `deploy` starts
- [ ] Intentionally break a test locally once (throwaway branch) to confirm a red test blocks deploy — revert after confirming

---

## 9. Post-Deploy Verification

**Why:** each check here maps to one specific fixed vulnerability, and each is something that
can silently regress on a future change without breaking any test. A missing header, a cookie
without `Secure`, or a container accidentally bound to `0.0.0.0` all *look* like the app is
working fine to an end user — the failure is invisible unless you specifically check for it.

- [ ] `curl -I https://yourdomain.com` returns security headers: `Content-Security-Policy`, `Strict-Transport-Security`, `X-Frame-Options`, `X-Content-Type-Options`
- [ ] Auth cookies (`ap_access`, `ap_refresh`) show `HttpOnly` and `Secure` in browser devtools → Application → Cookies
- [ ] `docker ps` shows DB/API/Seq bound to `127.0.0.1:*`, not `0.0.0.0:*`
- [ ] Log into the app, refresh the page — session persists (cookie-based restore working)
- [ ] Trigger a permission-denied action (non-admin hitting an admin route) — lands on `/unauthorized`, not a blank dashboard
- [ ] SignalR notifications connect (check browser console / notification bell)

---

## 10. Backups

**Why:** everything above protects the system from being *broken into* — this protects it from
being *lost*. A bad migration, a `docker volume prune` typo, a disk failure, or ransomware can
destroy the live database in seconds; without a backup that's every sale, customer, and stock
record gone permanently. Go-live is the point of maximum risk for this because it's also when
people start actually depending on the data existing tomorrow.

No automated backup is wired into the compose stack yet — do this manually until the
`DatabaseBackup` feature (see project memory) is merged and scheduled.

```bash
docker exec autopartshop.db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$DB_PASSWORD" \
  -Q "BACKUP DATABASE [AutoPartShopDb] TO DISK = '/var/opt/mssql/backup.bak'"
docker cp autopartshop.db:/var/opt/mssql/backup.bak ./backup-$(date +%Y%m%d).bak
```

- [ ] At least one manual backup taken and copied off the VPS before go-live
- [ ] A recurring backup (cron + off-box copy, e.g. to Google Drive/S3) is scheduled

---

## 11. Ongoing Monitoring

**Why:** a shop-facing system fails quietly by default — a full disk or an OOM-killed container
shows up to the cashier as "the app is slow" or "checkout won't load," not as an alert. Knowing
where to look *before* something breaks is the difference between a five-minute fix and an
afternoon of guessing. Seq is loopback-only for the same reason the DB/API are (step 4) —
structured logs contain user IDs, IPs, and error detail that shouldn't be internet-reachable —
so tunneling in is the intended access pattern, not a workaround.

```bash
# Logs
docker compose -p sujanmotors-prod logs -f autopartshop.api

# Resource usage
docker stats

# Disk
df -h && docker system df
```

Seq UI (`http://127.0.0.1:5341` via SSH tunnel: `ssh -L 5341:127.0.0.1:5341 <user>@<VPS_IP>`) for
structured log search — it's loopback-only by design, tunnel in rather than exposing it.

- [ ] Know how to tail logs and check container health without re-reading this doc
- [ ] Disk usage checked — SQL Server + Docker images can fill a small VPS fast

---

## Reference

- Deployment architecture & troubleshooting: `deployment/README.md`
- Full security audit + fix history: `docs/PRODUCTION_READINESS_REVIEW.md`
- Root-cause lessons behind each fix: `docs/learning/production-readiness-lessons.md`
- Env var reference: `deployment/.env.example`, `deployment/.env.prod.example`, `deployment/.env.test.example`
