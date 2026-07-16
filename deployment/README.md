# =============================================================================
# Deployment Guide — Hostinger VPS (Docker Compose)
# =============================================================================

This project deploys as Docker containers on a Hostinger VPS using Docker Compose.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Hostinger VPS                              │
│                                                                 │
│  ┌─── PRODUCTION (/opt/sujanmotors/) ────────────────────────┐  │
│  │  web:4200 ← nginx (public)                                │  │
│  │    └── /api → autopartshop.api:8080 (internal)            │  │
│  │    └── /hubs → autopartshop.api:8080 (internal)           │  │
│  │  db:1433 (internal)                                        │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌─── TEST (/opt/sujanmotors-test/) ─────────────────────────┐  │
│  │  web:4201 ← nginx                                         │  │
│  │    └── /api → autopartshop.api.test:8080 (internal)       │  │
│  │  db:1433 (internal, isolated volume)                       │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Branch Workflow

```
feature/* ──merge──▶ dev (no deploy, developer tests locally)
                       │
                  merge to test
                       │
                       ▼
              test-deploy.yml triggers
              → SSH into VPS, rebuild test stack
              → Available at http://<VPS_IP>:4201
                       │
                  merge to main
                       │
                       ▼
              prod-deploy.yml triggers
              → SSH into VPS, rebuild prod stack
              → Available at http://<VPS_IP>:4200
```

## 1. VPS Initial Setup (one-time)

```bash
# SSH into your Hostinger VPS
ssh root@<YOUR_VPS_IP>

# Install Docker and Docker Compose (if not installed)
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Create deployment directories
sudo mkdir -p /opt/sujanmotors /opt/sujanmotors-test
sudo chown $USER:$USER /opt/sujanmotors /opt/sujanmotors-test

# Clone the repository into each directory
cd /opt/sujanmotors
git clone <YOUR_REPO_URL> .
git checkout main

cd /opt/sujanmotors-test
git clone <YOUR_REPO_URL> .
git checkout test

# Create .env files with real secrets
cd /opt/sujanmotors
cp deployment/.env.prod.example deployment/.env
nano deployment/.env    # Edit with real production secrets

cd /opt/sujanmotors-test
cp deployment/.env.test.example deployment/.env
nano deployment/.env    # Edit with real test secrets

# Initial build and start
cd /opt/sujanmotors-prod
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml -f deployment/docker-compose.prod.yml up --build -d

cd /opt/sujanmotors-test
docker compose -p sujanmotors-test --env-file deployment/.env -f deployment/docker-compose.test.yml up --build -d
```

## 2. GitHub Secrets Configuration

Go to your GitHub repo → **Settings** → **Secrets and variables** → **Actions** and add:

| Secret | Value | Used By |
|--------|-------|---------|
| `VPS_HOST` | Your VPS IP address (e.g. `123.45.67.89`) | Both workflows |
| `VPS_USER` | SSH username (e.g. `root`) | Both workflows |
| `VPS_SSH_KEY` | Private SSH key (ed25519 recommended) | Both workflows |

### Generating an SSH Key for GitHub Actions

```bash
# On your local machine (not the VPS)
ssh-keygen -t ed25519 -f github-actions-deploy -N ""
# Copy the PUBLIC key to the VPS
ssh-copy-id -i github-actions-deploy.pub root@<YOUR_VPS_IP>
# Copy the PRIVATE key content to GitHub Secret VPS_SSH_KEY
cat github-actions-deploy
```

## 3. Environment Variables

Each environment has its own `.env` file:

- **Production:** `/opt/sujanmotors/deployment/.env`
- **Test:** `/opt/sujanmotors-test/deployment/.env`

See `.env.prod.example` and `.env.test.example` for all available variables.

### Required Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `DB_PASSWORD` | SQL Server SA password | `StrongP@ssw0rd!` |
| `DB_NAME` | Database name | `AutoPartShopDb` |
| `JWT_SECRET` | JWT token signing key (32+ chars) | `random-64-char-string...` |
| `JWT_EXPIRY_MINUTES` | Access token lifetime | `60` |
| `JWT_REFRESH_DAYS` | Refresh token lifetime | `7` |

## 4. TLS / HTTPS

**Before going live**, you MUST set up HTTPS. JWT tokens travel in the clear over plain HTTP.

### Option A: Cloudflare (Recommended — Easiest)

1. Add your domain to Cloudflare (free plan works)
2. Point the domain A record to your VPS IP
3. Enable Cloudflare proxy (orange cloud)
4. Set SSL mode to **Full (Strict)** in Cloudflare dashboard
5. Enable "Always Use HTTPS" in SSL/TLS settings

### Option B: Let's Encrypt on VPS

```bash
# Install Certbot
sudo apt install certbot

# Get certificate (stop nginx container first, or use standalone)
sudo certbot certonly --standalone -d yourdomain.com

# Mount certs in docker-compose.prod.yml:
#   volumes:
#     - /etc/letsencrypt:/etc/nginx/certs:ro
# And uncomment the 443 block in nginx.conf
```

## 5. Deployment

Use the provided scripts from the VPS:

```bash
# Deploy production
cd /opt/sujanmotors-prod
git pull origin main
./deployment/deploy-prod.sh

# Deploy test
cd /opt/sujanmotors-test
git pull origin test
./deployment/deploy-test.sh
```

The scripts handle: docker compose down → up --build -d → image cleanup.

### Manual commands

```bash
# View logs
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml logs -f

# Restart services
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml restart

# Stop services
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml down
```

## 6. Database

- Migrations run automatically on API startup (`DatabaseSeeder.MigrateAsync`)
- No manual `dotnet ef database update` needed
- Data persists in Docker named volumes (survives container restarts)

### Backup

```bash
# Backup production database
docker exec autopartshop.db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YOUR_PASSWORD' \
  -Q "BACKUP DATABASE [AutoPartShopDb] TO DISK = '/var/opt/mssql/backup.bak'"

# Copy backup from container to host
docker cp autopartshop.db:/var/opt/mssql/backup.bak ./backup-$(date +%Y%m%d).bak
```

## 7. Monitoring & Logs

```bash
# View all container logs (production)
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml logs -f

# View specific service logs
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml logs -f autopartshop.api
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml logs -f autopartshop.web

# Check container status
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml ps

# Check container resource usage
docker stats
```

## 8. Troubleshooting

### Containers keep restarting

```bash
# Check logs for errors
docker compose -p sujanmotors-prod --env-file deployment/.env -f deployment/docker-compose.yml logs autopartshop.api

# Common issues:
# - Wrong DB_PASSWORD in .env
# - SQL Server not ready (health check fails)
# - Missing environment variables
```

### Can't connect to API from browser

```bash
# Check if web container is running
docker ps | grep autopartshop.web

# Check nginx config inside container
docker exec autopartshop.web cat /etc/nginx/conf.d/default.conf

# Test API from within the VPS
curl http://localhost:4200/api/health
```

### Disk space issues

```bash
# Remove unused Docker images
docker image prune -a

# Remove unused volumes (⚠️ this deletes data!)
docker volume prune

# Check disk usage
df -h
docker system df
```

## Notes

- **Single VPS:** Both test and prod run on the same VPS with different ports/container names
- **No Docker Hub needed:** Images are built directly on the VPS (not pushed to a registry)
- **Database isolation:** Test and prod use separate Docker volumes (data never mixes)
- **Auto-restart:** Production containers restart automatically on crash or server reboot
- **Manual trigger:** Both workflows support `workflow_dispatch` for manual re-deploys from GitHub UI
