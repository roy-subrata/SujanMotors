# Learning: Building the Database Backup & Restore Feature

> A software-engineering case study of the backup feature on branch `feature/database-backup`
> (July 2026). This document is not API reference — it explains the **thinking**: how the
> feature was scoped, why it was designed this way, what went wrong during implementation,
> and what you should take away if you build something like this yourself.

---

## 1. The requirement, and turning it into decisions

The request was one sentence:

> "Implement database backup using a background job with resilience. Admin configures backup
> time from the UI. Restore if needed. Upload to Google Drive or cloud."

A sentence like this hides several **architecture-changing decisions**. The first engineering
step was not writing code — it was finding the questions whose answers change the design:

| Question | Why it changes everything | Decision made |
|---|---|---|
| Where does production SQL Server run? | Azure SQL **does not support** `BACKUP DATABASE` at all — you'd need BACPAC export instead. Self-hosted supports native `.bak`. | Self-hosted SQL Server 2025 in Docker (dev + Hostinger VPS) → native `BACKUP`/`RESTORE` |
| How should Google Drive authenticate? | A server can't do an interactive OAuth login. | Service account: admin shares a Drive folder with the SA's email; JSON key via env/secret |
| What does "restore" actually mean? | Restoring over a live DB kicks every user out and loses everything after the backup point. That's a product decision, not a technical one. | Full restore over the live DB, with typed confirmation + automatic safety backup first |

**Takeaway #1 — interrogate the requirement before designing.** One wrong assumption here
(e.g. "we're on Azure SQL") would have invalidated the entire implementation. The cheapest
time to discover a constraint is before you write anything.

---

## 2. Explore before you plan: reuse beats invention

Before designing, the codebase was surveyed for patterns this feature could ride on:

- **Background job template** — `ReorderAlertService` (a `BackgroundService` with a
  resilient loop and `IServiceScopeFactory` for scoped services inside a singleton).
- **Admin-editable settings** — the generic `ApplicationSettings` key/value table with a
  `Category` column, already exposed via API and consumed by the Angular settings pages.
  The backup schedule became category `BACKUP` — **zero new settings endpoints needed**.
- **Authorization** — the `[HasPermission("...")]` policy pattern; one new constant
  `backups.manage` and the whole controller is protected (Admin bypasses automatically).
- **Paged lists** — `PagedResult<T>` on the backend, and the frontend data-page design
  system (`app-page-container` / `app-page-header` / `app-data-pagination`).

**Takeaway #2 — a feature that reuses house patterns is cheaper to build, review, and
maintain.** Most of this feature's surface area (settings UI, auth, pagination, job loop)
was "assembled" rather than invented. The only genuinely new code is the backup pipeline
itself.

---

## 3. Where each piece lives (Clean Architecture)

Dependency rule of this repo: `Api → Application → Domain`; `Infrastructure → Domain + Application`.

```
Domain/          BackupRecord (entity + state machine), IBackupRecordRepository
Application/     IBackupService, IBackupStorage, DTOs      ← contracts only
Infrastructure/  SqlServerBackupService, GoogleDriveBackupStorage,
                 BackupCoordinator, BackupRecordRepository ← all real machinery
Api/             BackupsController, BackupSchedulerService ← HTTP + hosting concerns
```

Why bother with `IBackupStorage` as an interface when there's only one implementation?

1. **Swappability** — Azure Blob or S3 later means one new class, zero pipeline changes.
2. **Testability** — the pipeline can be tested with a fake storage.
3. **Dependency direction** — the pipeline logic must not know about Google SDK types.

**Takeaway #3 — abstract at the seams that are likely to change** (cloud provider), not
everywhere. There is no `ISqlBackupExecutor` interface, because "which database engine" is
not a realistic axis of change for this app.

---

## 4. The entity as a state machine

Every run — scheduled, manual, or pre-restore — is one `BackupRecord` row:

```
Pending ──► Running ──► Succeeded        backup + upload OK (or upload skipped)
                    ├─► UploadFailed     local .bak GOOD, only the cloud upload failed
                    └─► Failed           the backup itself failed
```

Two deliberate choices:

- **`UploadFailed` ≠ `Failed`.** A valid local backup whose upload failed is still a
  restorable backup. Collapsing these into one "failed" state would make the system throw
  away good backups. The entity exposes `IsRestorable => Succeeded || UploadFailed`, and
  download/restore/retention all key off that single property.
- **The entity owns its transitions.** Private setters + `MarkRunning()/MarkSucceeded()/
  MarkFailed()/Prune()` methods mean no service can write an inconsistent state
  (e.g. `Succeeded` with an `ErrorMessage`). This is the same DDD factory/mutator style as
  the rest of the Domain layer.

**Takeaway #4 — model operational reality in your states.** The states you need are the
states your *failure modes* produce, not just "success/failure".

---

## 5. The backup pipeline — and the bug that redesigned it

### The naïve design (what was planned)

Mount a host folder into the SQL container, and:

```sql
BACKUP DATABASE [Db] TO DISK = N'/var/opt/mssql/backups/x.bak' WITH CHECKSUM, COMPRESSION
```

API reads the file from the same folder on its side. Simple. **It failed on the very first
end-to-end test:**

```
The operating system returned the error '31 (A device attached to the system is not
functioning.)' while attempting 'DiskChangeFileSize' on '/var/opt/mssql/backups/....bak'.
```

### The investigation (this is the part to learn from)

Instead of guessing, each hypothesis was isolated with the smallest possible experiment:

| Experiment | Result | What it eliminated |
|---|---|---|
| Backup **without COMPRESSION** | same error | "it's the compression pre-allocation" |
| Backup with **trace flag 3042** (disables compressed pre-allocation) | same error | the documented workaround |
| `truncate -s 100M file` on the mount from inside the container | **works** | "the filesystem can't resize files at all" |
| Backup to a **container-local** dir (`/var/opt/mssql/...`) | **works** | "SQL Server backup is broken" |
| Copy a valid `.bak` onto the mount, then `RESTORE VERIFYONLY` from it | **works** | "SQL can't touch the mount at all" |

Conclusion: SQL Server's backup writer uses low-level file operations (O_DIRECT-style I/O +
its own file-resize path) that Docker Desktop's **Windows bind-mount filesystem doesn't
support** — but plain **reads work fine**. This is a platform limitation, not a bug in our
code. Production (Linux VPS, real ext4 bind mount) would have worked — but dev must work too.

### The redesigned pipeline

Stage where SQL can always write, then pull the bytes **over the SQL connection itself**:

```
1. record → Running
2. SELECT SERVERPROPERTY('InstanceDefaultBackupPath')          -- SQL's own dir, e.g. /var/opt/mssql/data
3. BACKUP DATABASE [Db] TO DISK = @staging
       WITH INIT, FORMAT, CHECKSUM, COMPRESSION
4. RESTORE VERIFYONLY FROM DISK = @staging WITH CHECKSUM       -- never trust an unverified backup
5. SELECT BulkColumn FROM OPENROWSET(BULK @staging, SINGLE_BLOB)
       → streamed (SequentialAccess + GetStream) into Backup:Directory on the API side
6. finally: EXEC master.sys.xp_delete_files @staging           -- clean the staged copy
7. upload to Google Drive (retry 5s/15s/45s) → Succeeded / UploadFailed
8. retention pass
```

Restore goes the **opposite direction** using the operation that *does* work everywhere:
the API places the `.bak` in the shared folder, and SQL Server **reads** it from there.

**Takeaway #5 — when something fails mysteriously, shrink the experiment.** Five tiny
sqlcmd/shell probes located the exact broken operation and the exact working ones, and the
working ones dictated the new design.

**Takeaway #6 — prefer one code path that works everywhere over a fast path that works
somewhere.** Staging + pull is slightly slower than a direct write, but dev-on-Windows,
dev-in-Docker, and prod-on-Linux all run the identical pipeline. Environment-specific
branches are where bugs hide.

### Small details in that pipeline that matter

- **`CHECKSUM` + `RESTORE VERIFYONLY`** — a backup is never marked Succeeded unless SQL
  Server re-read and validated it. An unverifiable backup is worse than none: it's false
  confidence you discover during a disaster.
- **`CommandTimeout = 0`** on every backup/restore command. The 30-second ADO.NET default
  would kill any real backup and this failure would only appear once the DB grew — in
  production.
- **Streams, never `byte[]`** — both the OPENROWSET pull and the Drive upload use
  streaming (`CommandBehavior.SequentialAccess`, `FileStream`), so memory stays flat no
  matter how big the database gets.
- **The DB name cannot be a SQL parameter** (T-SQL doesn't allow `BACKUP DATABASE @name`),
  so it is read from the connection string, validated with `^[A-Za-z0-9_]+$`, and
  bracket-wrapped. The file *path* CAN be a parameter, so it is one. Know exactly which
  parts of dynamic SQL are parameterizable, and defend the parts that aren't.

---

## 6. Two views of one folder (path duality)

In dev, the API runs on Windows and SQL Server runs in a Linux container. The shared backup
folder therefore has two different spellings:

| Key | Used by | Dev value |
|---|---|---|
| `Backup:Directory` | API file IO (size, upload, download, restore staging) | `D:\Ai\SujanMotors\deployment\backups` |
| `Backup:SqlServerDirectory` | SQL Server (read-only, during RESTORE) | `/var/opt/mssql/backups` |

**Takeaway #7 — in containerized systems, a "path" is only meaningful relative to a
process.** Any feature where two processes exchange files needs the path configured once
per observer, and the code must never mix the views (T-SQL gets the SQL view; `File.*`
gets the API view).

---

## 7. The scheduler: polling beats sleeping (here)

The existing `ReorderAlertService` computes "sleep until 09:30 tomorrow" and reads its
config once from `appsettings.json`. The backup scheduler deliberately works differently,
because its schedule is **admin-editable at runtime**:

```
every 5 minutes:
    read BACKUP:ENABLED / LOCAL_TIME / TZ_OFFSET_MINUTES from the DB
    if enabled AND local-now >= configured time
       AND no Scheduled backup started since local midnight:
           run one backup
```

What this loop shape buys:

1. **Live configuration** — a UI change takes effect within ≤5 minutes, no restart. A
   sleep-until-target loop would sleep on stale settings for up to 24 hours.
2. **Self-healing** — the trigger condition is *"has one run today?"*, not *"is it exactly
   02:00?"*. If the app was down at 02:00, the backup fires at the next wake instead of
   silently skipping a day. Missing a backup silently is the worst failure mode this
   feature can have.
3. **Idempotence** — `HasScheduledRunSinceAsync(localMidnightUtc)` is the dedupe guard; the
   completed-run marker lives in the DB, so restarts can't cause double runs.
4. **Resilience** — every cycle is wrapped in a catch-all; a failed run is recorded as a
   `Failed` BackupRecord and the loop continues. `OperationCanceledException` is caught
   separately so shutdown stays clean.

It also repairs state on startup: any record still `Pending`/`Running` is an orphan from a
crash and gets marked `Failed("Interrupted by an application restart")` — otherwise the UI
would show a backup "Running" forever.

**Takeaway #8 — pick the loop shape from the requirements, not from habit.**
Sleep-until-target is right for fixed config; poll-and-check is right for runtime-editable
config + catch-up semantics. And in any long-lived background loop: *the loop must be
un-killable; the work inside it must be allowed to fail.*

---

## 8. Concurrency: one tiny singleton instead of a framework

Two backups at once, or a restore during a backup, would be corruption. The guard is
`BackupCoordinator` — a singleton with ~20 lines:

```csharp
if (!coordinator.TryBegin("backup"))  →  409 Conflict / mark record Failed
try { ... } finally { coordinator.End(); }
```

Both entry points (scheduler and controller) go through it, and `CurrentOperation` is
exposed via `GET /status` so the UI can disable buttons while anything runs.

Note what was **not** used: no Hangfire/Quartz, no distributed lock, no queue. This app is
a single API instance; a `SemaphoreSlim`-style claim is the correct amount of machinery.
(If the API ever scales to multiple instances, this becomes a DB-based lock — and the
interface stays the same.)

**Takeaway #9 — solve the concurrency you actually have.** Mutual exclusion within one
process is a 20-line class. Reaching for a job framework here would add dependencies,
config, and failure modes for zero benefit.

---

## 9. Restore: engineering for the most dangerous button in the app

Restore replaces every byte of live data. The design assumes the admin *will* eventually
click it by mistake, at the worst time. Defenses, in order:

```
UI     : dialog lists consequences; admin must literally type "RESTORE"
API    : rejects anything except confirmation == "RESTORE" (never trust the client)
Step 1 : ensure the .bak exists locally (re-download from Drive if it was pruned)
Step 2 : SAFETY BACKUP of the current state (TriggerType=PreRestore)
         → if it fails, ABORT. Never destroy data you cannot get back.
Step 3 : ClearAllPools()                       ← drop pooled app connections
Step 4 : on a dedicated master connection (Pooling=false):
             ALTER DATABASE [Db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE
             RESTORE DATABASE [Db] FROM DISK = @path WITH REPLACE, CHECKSUM
             ALTER DATABASE [Db] SET MULTI_USER   ← in a finally block
Step 5 : ClearAllPools() + dbContext.Database.MigrateAsync()
         ← an older backup may carry an older schema; roll it forward NOW,
           don't wait for the next app restart
Step 6 : reconcile the history table (below)
```

Failure-path thinking baked in:

- `SET MULTI_USER` runs in `finally`, so a failed restore doesn't leave the DB locked; if
  even that fails, a critical log states the exact manual `sqlcmd` recovery command.
- Restore of an old backup + immediate `MigrateAsync` handles **schema drift** — the
  scenario everyone forgets until a restored app crashes on a missing column.

### The self-referential bug (found only by running it)

`BackupRecords` lives *inside* the database being restored. The `.bak` snapshot was taken
while its own record still said **Running**, and the safety-backup record didn't exist yet.
So the first real restore test produced: history showing the restored backup stuck at
"Running" forever, and the safety backup invisible despite its file sitting on disk.

Fix: both entities are still tracked in memory with their final values, so after the
restore the service writes them back — an `UPDATE` for the restored backup's row (it exists
in the snapshot, frozen in Running), a re-`INSERT` for the safety record (it doesn't exist
in the snapshot at all). Best-effort, log-only on failure: reconciliation cosmetics must
never fail a successful restore.

**Takeaway #10 — any state your feature stores *inside* the thing it snapshots/restores
will lie to you afterwards.** Backup systems, migration tools, and replication all hit this
class of self-reference. And: this bug is invisible to unit tests — only a real end-to-end
restore exposed it. *Exercise the dangerous path for real before shipping it.*

### A retention race found by review

The safety backup runs retention like any backup. But mid-restore, that pass could prune
**the very backup being restored** (if it's the oldest retained one and retention is full)
— deleting its local *and* Drive copy between validation and the actual `RESTORE`. Fix:
retention is skipped for `PreRestore` runs; the next regular backup prunes instead.

**Takeaway #11 — re-examine every "background hygiene" task in the context of every
foreground operation it can interleave with.** Retention is safe next to a backup and
dangerous in the middle of a restore.

---

## 10. Cloud upload: service account + graceful degradation

- **Why a service account?** Servers can't do interactive OAuth. The admin shares a Drive
  folder with the SA's email (Editor); the SA uploads with its own key. The UI's
  drive-status endpoint returns that email precisely so the admin knows what to share —
  turning a config chore into a guided step.
- **`SupportsAllDrives = true`** on every request, so a Google Workspace Shared Drive works
  unchanged (also sidesteps SA storage-quota issues on personal Drives).
- **Scope `DriveFile`** — the SA can only touch files it created. Least privilege.
- **Retry**: 3 attempts, 5s/15s/45s backoff, hand-rolled (~10 lines). The codebase has no
  Polly, and consistency with house style beat adding a dependency for one loop.
- **Degradation**: if all retries fail → `UploadFailed`, local file kept, scheduler
  unaffected. If no folder is configured → upload silently skipped, backups stay
  local-only. Cloud upload is an *enhancement* of the backup, not a gate on it.

**Takeaway #12 — order your failure domains.** The local backup is the product; the upload
is a bonus. Never let the failure of a secondary step destroy or mask the success of the
primary one.

---

## 11. Configuration: split by *who changes it*

| Kind | Lives in | Examples | Why |
|---|---|---|---|
| Business schedule (admin-owned, changes at runtime) | `ApplicationSettings` table, category `BACKUP` | enabled, time, retention, Drive folder ID | Editable from UI; scheduler re-reads every cycle |
| Machine facts + secrets (operator-owned) | `appsettings.json` / env vars | the two directory paths, SA key JSON | Describe the *deployment*, not the business — and secrets must never be editable from a web page |

**Takeaway #13 — "configuration" is not one bucket.** Sorting each knob by *who changes it
and how often* tells you where it belongs. A secret in an admin-editable table is a
security smell; a business schedule in appsettings.json forces redeploys for policy
changes.

---

## 12. Verification: what "done" meant

No automated test project exists in this repo, so verification was a live end-to-end drive
of every path — which is exactly what caught the two biggest bugs (error 31, history
reconciliation):

1. Manual backup → `.bak` on host, record `Succeeded`, staging cleaned, correct size.
2. Scheduled backup → enabled settings via the DB, watched the 5-minute poll fire.
3. Restore → inserted a marker row *after* a backup, restored, proved the marker gone,
   DB back `ONLINE/MULTI_USER`, app still serving, history reconciled.
4. Download → streamed bytes byte-identical in size to the record.
5. Failure paths → interrupted-run cleanup at startup; drive-status with no credentials.

**Takeaway #14 — verification is part of implementation, not an afterthought.** The plan
written before coding already contained this checklist; half the design lessons in this
document came from executing it.

---

## 13. Summary: the takeaways in one list

1. **Interrogate requirements first** — one environment assumption (Azure SQL vs self-hosted) flips the whole design.
2. **Survey and reuse house patterns** before inventing anything.
3. **Abstract at the seams likely to change** (cloud storage), not everywhere.
4. **Design entity states around failure modes** (`UploadFailed` ≠ `Failed`).
5. **Debug by shrinking the experiment** — isolate the exact broken operation.
6. **One code path for all environments** beats per-environment fast paths.
7. **Paths are per-process in containerized systems** — configure each observer's view.
8. **Choose background-loop shape from requirements** (poll + catch-up vs sleep-until); loops must be un-killable, work inside may fail.
9. **Match concurrency machinery to actual concurrency** (20-line coordinator, not a job framework).
10. **State stored inside the restored thing lies after restore** — reconcile it; test destructive paths for real.
11. **Audit background hygiene tasks against every operation they can interleave with** (retention vs restore).
12. **Order failure domains** — never let a secondary step (upload) gate the primary one (backup).
13. **Split config by ownership** — admin/runtime vs operator/machine vs secret.
14. **Verification is implementation** — drive the real flows; that's where the real bugs were.

---

## 14. Topics to study (with the code that demonstrates each)

**.NET / backend**
- `BackgroundService` & `IHostedService` lifecycles; `IServiceScopeFactory` (why a singleton service must create scopes to use scoped services) — `Api/Services/BackupSchedulerService.cs`
- ADO.NET beyond EF: `SqlConnection`, `CommandTimeout`, connection pooling, `ClearAllPools`, `CommandBehavior.SequentialAccess` streaming — `Infrastructure/Services/Backup/SqlServerBackupService.cs`
- EF Core migrations at runtime (`Database.MigrateAsync`) and change-tracker behavior (identity resolution, setting `EntityState.Added`) — restore path + `ReconcileHistoryAfterRestoreAsync`
- Clean Architecture dependency rules; interface placement — `Application/Interfaces/IBackupStorage.cs`

**SQL Server**
- `BACKUP DATABASE` options: `INIT`, `FORMAT`, `CHECKSUM`, `COMPRESSION`; `RESTORE VERIFYONLY`; `RESTORE ... WITH REPLACE`
- `SINGLE_USER WITH ROLLBACK IMMEDIATE` / `MULTI_USER`; what happens to open connections
- `OPENROWSET(BULK ..., SINGLE_BLOB)`; `SERVERPROPERTY('InstanceDefaultBackupPath')`; `xp_delete_files`
- Which parts of T-SQL are parameterizable (paths yes, identifiers no) and how to defend identifiers

**Docker / infrastructure**
- Bind mounts vs named volumes; Docker Desktop file-sharing limitations (O_DIRECT, the error-31 class of failures)
- Container users & bind-mount ownership (mssql = UID 10001; the prod `chown` requirement in `deployment/docker-compose.yml`)
- Compose base + override files (`docker-compose.yml` / `.dev.yml` / `.prod.yml`)

**Distributed-systems thinking (small scale)**
- Idempotence & dedupe markers (the "one scheduled run per local day" guard)
- Graceful degradation & failure-domain ordering (backup vs upload)
- Mutual exclusion within a process (`BackupCoordinator`); when you'd upgrade to a distributed lock
- Timezone handling with explicit UTC offsets (`TZ_OFFSET_MINUTES`, local-midnight-to-UTC conversion)

**Google APIs**
- Service accounts vs OAuth user consent; domain sharing model; Drive scopes (`DriveFile`); resumable/streaming uploads; `SupportsAllDrives`

**Product thinking for dangerous operations**
- Typed confirmations, server-side re-validation, safety backups, consequence-listing dialogs — `BackupsController.Restore` + `backups.component.ts` restore dialog
