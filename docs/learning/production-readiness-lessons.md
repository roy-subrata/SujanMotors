












































# Production Readiness Lessons Learned

> What we fixed, why it mattered, and what every developer should internalize.

---

## 1. Secrets Must Never Live in Code

### What happened
`docker-compose.yml` had `DB_PASSWORD`, `JWT_SECRET`, and `SEED_ADMIN_PASSWORD` hardcoded as plain text. Anyone with repo access (or a leaked git history) had production credentials.

### Why it matters
- Git history is permanent. Even if you fix it in the next commit, the secret stays in every prior commit.
- CI/CD systems, Docker Hub, and code review tools all expose `git history`.
- A single leaked DB password = full data breach.

### What to do
- Use `${VAR:?message}` in docker-compose — Docker refuses to start if the env var is unset.
- Provide a `.env.example` with `CHANGE_ME` placeholders. Never commit `.env`.
- Secrets belong in: environment variables, Docker secrets, or a vault (HashiCorp Vault, AWS SSM). Never in source code.

---

## 2. Bind to Loopback, Not 0.0.0.0

### What happened
All Docker host port bindings were `0.0.0.0:1433`, `0.0.0.0:5000`, etc. — exposed to every network interface on the host.

### Why it matters
- A VPS with `0.0.0.0:1433` means SQL Server is accessible from the public internet.
- `0.0.0.0:5000` means the API is reachable without nginx/TLS.
- Scanners constantly scan every port on every public IP.

### What to do
- Always bind to `127.0.0.1:PORT` for internal services (DB, API, Seq).
- Only the TLS terminator (nginx, port 443) should be public.
- Use `docker-compose` networks for inter-container traffic — host ports are for external access only.

```yaml
# Bad
ports:
  - "1433:1433"

# Good
ports:
  - "127.0.0.1:1433:1433"
```

---

## 3. Require HTTPS in Production

### What happened
`RequireHttpsMetadata` was set to `false` unconditionally, including production. The API accepted plain HTTP JWT tokens.

### Why it matters
- JWT tokens sent over HTTP are visible to anyone on the network (MITM).
- A stolen JWT = impersonation. A stolen refresh token = persistent session.
- TLS is not optional for any non-development environment.

### What to do
- Set `RequireHttpsMetadata = !builder.Environment.IsDevelopment()`.
- Provide an nginx config with TLS termination (`ssl_certificate`, `ssl_certificate_key`).
- Use Let's Encrypt or a cloud provider's cert manager. Self-signed certs are for dev only.

---

## 4. Security Headers Are Not Optional

### What happened
No CSP, no HSTS, no X-Content-Type-Options, no X-Frame-Options anywhere in the app.

### Why it matters
Without headers:
- **CSP missing** — any XSS can exfiltrate data to an attacker's server.
- **HSTS missing** — browsers can be downgraded to HTTP via MITM.
- **X-Frame-Options missing** — your login page can be framed (clickjacking).
- **X-Content-Type-Options missing** — browsers may MIME-sniff responses (upload XSS).

### What to do
Add to nginx (or your reverse proxy):

```nginx
add_header Content-Security-Policy "script-src 'self'; object-src 'none'; frame-ancestors 'none'" always;
add_header Strict-Transport-Security "max-age=63072000; includeSubDomains" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
```

---

## 5. Tokens in localStorage Are Vulnerable

### What happened
JWT access and refresh tokens were stored in `localStorage` — accessible to any JavaScript on the page.

### Why it matters
- Any XSS vulnerability (even a third-party script) can read `localStorage` and steal both tokens.
- The refresh token grants full session renewal — stealing it gives persistent access.
- `localStorage` persists across browser tabs and sessions.

### What to do
- Store tokens in httpOnly cookies — JavaScript cannot read them.
- Use `SameSite=Lax` (prevents CSRF on cross-origin form submissions).
- Set `Secure` flag (cookies only sent over HTTPS).
- Use `Path` scoping — refresh cookie should only be sent to auth endpoints.

```csharp
response.Cookies.Append("ap_access", token, new CookieOptions
{
    HttpOnly = true,
    Secure = !isDevelopment,
    SameSite = SameSiteMode.Lax,
    Path = "/",
    MaxAge = TimeSpan.FromMinutes(60)
});
```

### Why not SameSite=Strict?
Strict blocks cookies on top-level navigations (e.g., clicking a link from an email to your app). The user would see an unauthenticated state on first load. Lax is the safe default for SPAs.

---

## 6. Dual-Mode Auth for Mixed Clients

### What happened
Web SPA and Flutter mobile app needed different auth mechanisms — cookies for browsers, body tokens for mobile.

### Why it matters
- Flutter/Dio doesn't support httpOnly cookies natively.
- Web browsers handle cookies automatically but can't read httpOnly values.
- A single auth endpoint must serve both without breaking either.

### What to do
- Accept both `Authorization: Bearer <token>` header AND `Cookie: ap_access=<token>` on every endpoint.
- Web SPA: send `withCredentials: true`, never attach Authorization header.
- Mobile: send tokens in body/response, attach Authorization header via Dio interceptor.
- The backend `OnMessageReceived` event should check: query string (SignalR) → header → cookie, in that priority order.

---

## 7. CSRF Protection Without Explicit Tokens

### What happened
With cookies instead of Authorization headers, cross-site request forgery becomes a concern.

### Why it matters
If a user visits a malicious site while logged in, the browser will automatically attach cookies to requests made to your API. A form submission from the malicious site could trigger unintended actions.

### Why we're okay without explicit CSRF tokens
- **SameSite=Lax** blocks cookies on cross-origin POST/PUT/DELETE requests (only GET navigations are allowed).
- All state-changing operations use POST/PUT/DELETE — they're protected by SameSite.
- The API has no GET endpoints that modify state (idempotent reads only).
- For extra safety, the refresh cookie uses `Path=/api/v1/auth/` — it's only sent to auth endpoints, not to business endpoints.

If your app uses forms that POST cross-origin, add explicit CSRF tokens (e.g., ASP.NET Core's `[ValidateAntiForgeryToken]`).

---

## 8. Rate Limiting Is Layered, Not Single

### What happened
Login, refresh, and other endpoints have different rate limit policies.

### Why it matters
- **Login**: strict (brute-force protection). 5 failed attempts = lockout.
- **Refresh**: moderate. The credential is a 256-bit random token, not a guessable password. But a whole shop shares one IP, so bursting is normal.
- **Register/Change-password**: generous. Authenticated operations shouldn't be throttled for legitimate batch operations.

### What to do
- Don't use one global rate limit for everything.
- Use `[EnableRateLimiting("policy-name")]` per endpoint.
- Partition by user (authenticated) or IP (anonymous).
- A single IP limit would throttle all cashiers behind the shop's NAT.

---

## 9. Silent Token Refresh Must Be Coordinated

### What happened
Multiple concurrent requests could all get 401s and all try to refresh simultaneously, wasting the single-use refresh token.

### Why it matters
Refresh tokens are single-use. If 5 requests all fire refreshes at once, only the first succeeds. The rest waste the token (reuse detection kills the session).

### What to do
- Share one in-flight refresh observable across concurrent callers.
- Use `shareReplay(1)` to multicast the result.
- Only one actual HTTP request is made; others wait for the result.
- On failure (throttled/5xx), don't clear the session — retry next time.

```typescript
// Single flight pattern
if (this.refreshInFlight$) {
    return this.refreshInFlight$; // others share this
}
this.refreshInFlight$ = this.http.post(...).pipe(
    shareReplay(1),
    finalize(() => { this.refreshInFlight$ = null; })
);
```

---

## 10. Session Restore on Page Load

### What happened
After the cookie migration, `localStorage` no longer holds tokens. Session state must be restored differently.

### Why it matters
- On page reload, the browser still has the httpOnly cookies (if not expired).
- But JavaScript can't read them to check if the session is valid.
- The app needs to show the UI (menus, permissions) immediately without waiting for an API call.

### What to do
- Store **user profile data** (name, roles, permissions) in `localStorage` — it's non-sensitive UX data.
- Store **tokens** in httpOnly cookies — invisible to JavaScript.
- On page load: read user profile from `localStorage`, render UI optimistically.
- First API call: if 401, interceptor silently refreshes via cookie. If refresh fails → clear profile, redirect to login.
- The `isLoggedIn()` check becomes: "do we have a stored user profile?" (not "do we have a valid token?").

This gives instant UI on reload while keeping tokens secure.

---

## 11. CI Must Gate Deploys on Tests

### What happened
Deploy workflows ran tests and deploys in parallel — a failing test wouldn't block deployment.

### Why it matters
- A broken test + auto-deploy = broken production.
- Tests are the safety net. If they fail, deployment must stop.

### What to do
```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps: [dotnet test ...]
  
  deploy:
    needs: [test]  # <-- blocks until test passes
    if: github.ref == 'refs/heads/main'
    steps: [deploy ...]
```

---

## 12. SSH Keys Over Passwords

### What happened
CI/CD workflows used `VPS_PASSWORD` for SSH access. Password-based SSH is vulnerable to brute-force.

### Why it matters
- SSH keys are cryptographically strong (2048+ bit RSA or Ed25519).
- Passwords can be brute-forced, especially if the VPS has a public IP.
- GitHub Actions secrets are secure, but the key itself should be strong.

### What to do
- Generate a dedicated deploy key: `ssh-keygen -t ed25519 -C "deploy@sujanmotors"`.
- Store the private key as `VPS_SSH_KEY` secret.
- Add the public key to `~/.ssh/authorized_keys` on the VPS.
- Never use passwords for automated deploys.

---

## 13. Docker .dockerignore Matters

### What happened
`.dockerignore` was minimal — Docker images included test files, CI configs, documentation, and other non-runtime artifacts.

### Why it matters
- Larger images = longer builds, more bandwidth, larger attack surface.
- Test files in production images can leak test credentials or endpoints.
- Every file in the image is a potential attack vector.

### What to do
```dockerignore
# Everything except what the build needs
.git
.github
.vscode
docs
mobile
e2e
*.md
docker-compose*.yml
```

---

## 14. Fail-Fast vs Fail-Safe Configuration

### What happened
`docker-compose.yml` used plain `DB_PASSWORD: password` — if the real password wasn't set, the app started with the default and silently connected with wrong credentials.

### Why it matters
- Fail-safe (default works) hides misconfiguration until it's too late.
- Fail-fast (crash on startup) surfaces problems immediately.

### What to do
```yaml
# Bad: fails silently
DB_PASSWORD: password

# Good: fails loudly
DB_PASSWORD: ${DB_PASSWORD:?Set DB_PASSWORD in .env}
```

The `:?` syntax makes Docker Compose refuse to start if the variable is unset.

---

## 15. Portable Error Messages Don't Leak Internals

### What happened
Customer creation returned a full internal error message including the existing phone number.

### Why it matters
- Error messages should help the caller, not reveal database internals.
- Exposing specific field values in errors enables enumeration attacks.
- "Phone number already exists" tells an attacker that exact number is in the system.

### What to do
- Return generic messages: "A record with this information already exists."
- Log the specifics server-side (for debugging), not in the HTTP response.
- Use structured logging: `_logger.LogWarning("Duplicate phone {Phone}", phone)`.

---

## 16. Budget Thresholds in Build Configs

### What happened
Angular build budgets had no `maximumWarning` or `maximumError` — bundles could grow without anyone noticing.

### Why it matters
- A 5MB JavaScript bundle on a mobile network = 10+ second load time.
- Without budgets, gradual bloat goes unnoticed until users complain.
- Budgets enforce size discipline at build time, not runtime.

### What to do
```json
"budgets": [
  { "type": "initial", "maximumWarning": "2.5 MB", "maximumError": "4 MB" },
  { "type": "anyComponentStyle", "maximumWarning": "24 kB", "maximumError": "48 kB" }
]
```

---

## 17. PWA Cache Must Not Stale API Data

### What happened
`ngsw-config.json` had `dataGroups` caching API responses — stale data could be served after deploy.

### Why it matters
- API responses are dynamic (inventory, prices, sales). Caching them serves stale data.
- A user might see outdated stock levels or prices.
- Caching `/api/**` defeats the purpose of a real-time business system.

### What to do
```json
"dataGroups": [] // Don't cache API responses
```

Only cache static assets (`/assets/**`). API data freshness > cache performance.

---

## 18. Role-Based Protection Should Be in Code, Not Just Seed Data

### What happened
Admin role was protected only by not seeding extra Admin accounts — the registration endpoint had no policy check.

### Why it matters
- Seed exclusivity is a runtime assumption, not a security boundary.
- If someone calls `/register` with `defaultRole: "Admin"`, the seeder exclusivity doesn't help.
- Code-level authorization is the only reliable protection.

### What to do
```csharp
[Authorize(Roles = "Admin")]
[HttpPost("register")]
public async Task<IActionResult> Register(...)
```

---

## 19. Service Worker Must Not Cache Auth Responses

### What happened
`ngsw-config.json` had `performance` caching for API routes — login/refresh responses could be cached and replayed.

### Why it matters
- A cached login response means a stale token is replayed.
- A cached refresh response means a spent token is replayed (server rejects it as reuse).
- This causes phantom 401s and mysterious session failures.

### What to do
- Remove all API routes from `ngsw-config.json` `dataGroups`.
- Add `SwUpdate` check in `app.component.ts` to prompt users when a new version is available.
- Let the browser's HTTP cache (with proper `Cache-Control` headers) handle static assets.

---

## 20. Build Should Fail on Warnings

### What happened
Angular budgets had `maximumWarning` but no `maximumError` — warnings were silently ignored.

### Why it matters
- Warnings that never become errors are noise that developers learn to ignore.
- A budget that only warns is not a budget — it's a suggestion.

### What to do
Set both `maximumWarning` and `maximumError`. The build should fail if the error threshold is exceeded. This forces a conscious decision to either optimize or raise the limit.

---

## Summary Checklist

Before deploying any web application to production, verify:

- [ ] No secrets in source code (use env vars / vault)
- [ ] All ports bound to loopback (except TLS terminator)
- [ ] HTTPS enforced (HSTS, TLS termination, RequireHttpsMetadata)
- [ ] Security headers present (CSP, X-Frame-Options, X-Content-Type-Options)
- [ ] Tokens in httpOnly cookies, not localStorage
- [ ] Rate limiting applied per-endpoint with appropriate policies
- [ ] CI gates deployment on passing tests
- [ ] SSH keys for automated deploys (no passwords)
- [ ] Docker images are minimal (.dockerignore)
- [ ] Fail-fast configuration (crash on missing secrets)
- [ ] Error messages don't leak internals
- [ ] Build budgets enforced (warning + error)
- [ ] Service worker doesn't cache auth or API responses
- [ ] Role protection in code, not just seed data
