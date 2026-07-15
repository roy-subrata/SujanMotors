# Learning: How the File Upload Feature Was Designed & Built

*A walkthrough of the thinking behind `feature/file-uploads` — the idea, the decisions, the trade-offs, and what you can take away as a software engineer.*

---

## 1. The Problem

> "Upload images or videos for products, employees, and other files — to any free cloud service or local storage. I will have a Hostinger VPS."

Three requirements hide inside this one sentence:

1. **Multiple file types** — images, videos, documents — with very different sizes and risks.
2. **Multiple owners** — products, employees, "other" (i.e., the list will grow).
3. **Uncertain storage destination** — local now, maybe cloud later. This uncertainty is the single most important design input.

**Lesson: requirements that say "X now, maybe Y later" are telling you where to put an abstraction.**

---

## 2. Step Zero: Read Before You Write

Before designing anything, I searched the codebase. This changed the plan significantly:

| Discovery | Impact on the plan |
|---|---|
| `ProductMedia` entity already existed (URL, mediaType, sortOrder, isPrimary, variantId) | Don't invent a new `ProductImage` table — the data model was already there, only the *upload* and *API* were missing |
| The Angular `product-media-manager` component ran on `MOCK_MEDIA` | The gap was precisely: no upload endpoint, no media CRUD API |
| `ProductImportController` already handled `IFormFile` | Copy its validation/error style (`ApiError.Validation`, `[RequestSizeLimit]`) for consistency |
| Every entity inherits `AuditableEntity`; repos follow a soft-delete pattern | New entities must follow the same conventions or the codebase becomes inconsistent |

**Lesson: the first hour of a feature belongs to `grep`, not to code.** The best design for "add product images" turned out to be "finish the half-built media system," which you can only know by reading. Duplicating an existing concept (a second media table) is one of the most expensive mistakes in a codebase — it forever confuses every future reader.

---

## 3. The Core Design Decision: An Interface Between "What" and "Where"

The storage question (local disk? Cloudflare R2? Google Drive? MinIO?) could have stalled the feature. Instead it was **dissolved** by one interface:

```
Application layer (stable):        IFileStorageService
                                    ├── SaveAsync(stream, key, contentType)
                                    ├── OpenReadAsync(key)
                                    └── DeleteAsync(key)

Infrastructure layer (swappable):  LocalDiskFileStorage   (today)
                                   S3FileStorage           (tomorrow: R2 / MinIO / B2 — no caller changes)
```

This is the **Dependency Inversion Principle** doing real work, and it mirrors this codebase's existing Clean Architecture: interfaces live in `Application`, implementations in `Infrastructure`, and controllers depend only on the interface.

Why local disk *first*?
- It's free and the VPS has 50–200 GB NVMe — adequate for a single-shop system.
- Zero external dependencies, zero credentials to manage, zero new failure modes.
- Because of the interface, "migrate to R2" later is one new class + one config value — **the cheap option now doesn't block the better option later.**

**Lesson: you don't need to pick the final storage on day one; you need to make the choice cheap to change.** Abstractions are insurance — buy them exactly where requirements are volatile (storage backend), not everywhere (that's over-engineering).

Related decision: kept the interface *minimal* (3 methods). No `GetPublicUrl()`, no metadata queries — URL building is the API's concern, metadata lives in the database. A fat interface would leak backend details and make the S3 version harder.

---

## 4. Why Files Are Served Through the API (Not as Static Files)

The obvious approach — dump files in `wwwroot` and let the web server serve them — was rejected. Every file is served via `GET /api/v1/files/{id}/content` instead. Reasons:

1. **The URL outlives the storage backend.** URLs stored in the database (`ProductMedia.Url`, `Employee.PhotoUrl`) must keep working after moving to R2. If URLs pointed at disk paths, migration breaks every stored link. With API URLs, only the code behind the endpoint changes.
2. **Access control needs code.** Static file middleware can't say "images public, documents authenticated."
3. **The database is the source of truth.** Serving by StoredFile *id* (not filename) means renames, dedup, and metadata all stay in SQL.

The trade-off: every download passes through .NET instead of the kernel's static file path. At this system's scale that's irrelevant; at CDN scale you'd flip to presigned URLs — *which the same interface can support later*.

**Lesson: URLs are a public API. Design them to survive infrastructure changes, because they get persisted in databases, browser caches, and other people's systems.**

---

## 5. The Authentication Problem Nobody Notices Until It Bites: `<img>` Can't Send a JWT

The system uses JWT bearer tokens in the `Authorization` header. But browsers load `<img src="...">` and `<video src="...">` with plain GET requests — **no header, no token**. If images required auth, every product photo in the Angular app would 401.

The solution is a deliberate two-tier policy:

| File kind | Access | Why it's safe / necessary |
|---|---|---|
| Images & videos | **Public**, no auth | URL contains a random GUID (~122 bits of entropy) — unguessable, like a Google Drive "anyone with the link" share. Product images must be public anyway (e-commerce storefront). |
| Documents (PDF, NID scans, contracts) | **JWT required** | Sensitive. The frontend fetches them with `HttpClient` (which *can* attach the header) and hands the blob to the browser. |

This is called a **capability URL** — the URL itself is the credential. It's a well-known, honest compromise; the alternative (cookie auth, signed URLs) has its own costs.

**Lesson: authentication schemes interact with *how browsers load resources*. "Just protect everything with JWT" fails for media tags — you must decide per-resource-type, explicitly, and write the decision down.**

---

## 6. Upload Security: Never Trust Anything the Client Sends

An upload endpoint is a classic attack surface. Every defense in `Api/Services/UploadRules.cs` and `FilesController` exists for a specific attack:

| Attack | Defense |
|---|---|
| Upload `shell.aspx` / `.exe` | **Extension allow-list** (deny-by-default: if the extension isn't in the table, it's rejected) |
| Rename `malware.exe` → `photo.png` | **Magic-byte sniffing** — the first bytes of the file must match the format's signature (`FF D8 FF` for JPEG, `%PDF` for PDF…). The client's `Content-Type` header is *never* trusted; the server picks the content type from its own table. |
| 10 GB upload = disk/memory exhaustion | **Size limits per kind** (5 MB image / 100 MB video / 10 MB document) + `[RequestSizeLimit]` so Kestrel aborts the request early |
| `fileName = "../../appsettings.json"` (path traversal) | File names are **discarded entirely** — the storage key is a server-generated GUID. Belt-and-braces: `LocalDiskFileStorage.ResolvePath` still verifies the resolved path stays under the root. |
| Guessing other users' files | GUID keys/ids are unguessable |
| Serving user HTML/SVG that runs scripts (stored XSS) | Those extensions simply aren't in the allow-list |

**Lesson: enumerate attacks first, then map one defense to each. "Validation" is not one thing — extension, content, size, and path are four separate checks, and skipping any one of them is a real CVE class.**

---

## 7. Data Model: A Ledger and a View

Two tables could have been one, but they answer different questions:

- **`StoredFiles`** — the *ledger*: what bytes exist, where (`StorageKey`), what they are (`Kind`, `ContentType`, size), who they belong to (`OwnerType`/`OwnerId`), and whether they're public. It knows nothing about galleries.
- **`ProductMedia`** — the *presentation*: sort order, primary flag, alt text, variant link. Its `Url` may point at a stored file **or** an external URL (YouTube). It knows nothing about disks.

Loose coupling between them (a URL string, not a foreign key) was a deliberate trade-off: the gallery must support external URLs, so a nullable FK would only cover half the rows anyway. The cost is that deletion needs the URL parsed back to a file id (`TryParseStoredFileId`) — accepted, and handled in exactly one place.

The generic `OwnerType/OwnerId` pair (a **polymorphic association**) is what makes "employee other files" free: no `EmployeeDocument` table needed — upload with `ownerType=EMPLOYEE`, list by owner. New owner types cost zero migrations.

**Lesson: separate "what exists" from "how it's displayed." And when a feature says "…and other things too," reach for a generic owner tag instead of one link table per owner.**

---

## 8. Small Decisions That Show Engineering Judgment

These are the details that separate a working feature from a robust one:

1. **Atomic writes** — `LocalDiskFileStorage` writes to `file.tmp` then `File.Move`s to the final name. A crash mid-upload can never leave a half-written blob at a valid key. (*Topic: write-rename atomicity, crash consistency.*)
2. **Compensating cleanup** — if the DB insert fails *after* the blob was saved, the `catch` deletes the blob. There's no distributed transaction between disk and SQL, so you order the operations and compensate. (*Topic: sagas / compensating actions.*)
3. **Cascade with intent** — deleting a `ProductMedia` row also deletes its uploaded blob (parsed from the URL), preventing silent orphan accumulation on disk.
4. **Immutable caching** — keys are never reused, so public files are served with `Cache-Control: public, max-age=31536000, immutable`. Content-addressed-ish URLs make aggressive caching *safe*.
5. **Range requests** — `enableRangeProcessing: true` for video, otherwise `<video>` seeking downloads the whole file.
6. **First image auto-primary** — a tiny domain rule (`existing.Count == 0 → isPrimary`) that guarantees every listed product has a thumbnail without asking the frontend to remember.
7. **Convention adherence** — soft deletes, `AuditableEntity`, `ApiResponse<T>`/`ApiError`, permission policies (`inventory.view`/`inventory.edit`), repository-per-aggregate. None of this was *my* preference; it was *the codebase's* preference. Consistency beats personal style.
8. **`.gitignore` the upload root** — user data must never enter version control.

---

## 9. Verification: Prove It, Don't Assume It

"It compiles" is not verification. The feature was smoke-tested end-to-end against the running API:

- upload a real PNG → 200, correct metadata
- fetch it **anonymously** → 200, right bytes, `Cache-Control: immutable`, `Accept-Ranges: bytes`
- upload a text file renamed `.png` → **400 "content does not match extension"** (negative test!)
- upload without a token → **401**
- attach to a part → auto-primary; delete the row → the blob 404s (cascade proven)
- protected PDF: anonymous → **401**, with token → **200**

Negative tests matter as much as happy paths — a security control that was never observed rejecting anything is a hypothesis, not a control.

Bonus: verification surfaced a **pre-existing bug** (`DELETE /api/v1/products/{id}` 500s on the price-history FK). Exercising a system teaches you things reading it never will.

**Lesson: for every feature, write down the 5–8 behaviors that must be true, then *observe* each one — including the ones that must fail.**

---

## 10. The Implementation Order (and Why)

```
1. Explore        grep/read: what exists? (found ProductMedia, mocked UI, conventions)
2. Domain         StoredFile entity + repository interfaces        ← no dependencies
3. Application    IFileStorageService interface + DTOs             ← depends on nothing concrete
4. Infrastructure LocalDiskFileStorage, repositories, EF config    ← implements 2 & 3
5. API            UploadRules, FilesController, ProductMediaController
6. Config/DI      appsettings section, Dependency.cs registrations
7. Migration      one migration for all schema changes
8. Verify         build + live smoke test (happy + negative paths)
9. Document       docs/file-uploads.md; commit with a story-telling message
```

Inside-out (domain → API) matches the dependency direction of Clean Architecture: each layer only ever references things that already exist. You never write a controller against an interface you haven't designed.

---

## 11. Key Takeaways (the short list)

1. **Read the codebase before designing** — the right feature was "finish the existing media system," discoverable only by searching.
2. **Put abstractions exactly where requirements are volatile** — one small interface turned "which cloud?" from a blocker into a config value.
3. **URLs are persisted API contracts** — serve through stable, id-based endpoints so storage can move without breaking stored links.
4. **Browsers constrain auth design** — `<img>` can't send JWTs; public-with-unguessable-URL vs. authenticated-blob-fetch is a *decision*, make it explicitly.
5. **Upload security is a checklist, not a feeling** — extension allow-list, magic bytes, size caps, server-generated names, traversal guard. One defense per attack.
6. **Separate the ledger from the view** — file records vs. gallery rows; polymorphic owner tags for "attachments on anything."
7. **No transactions across disk + DB** — order operations so failures are recoverable, and compensate (temp-file rename, cleanup-on-failure, cascade deletes).
8. **Follow the house style** — audit fields, soft deletes, error shapes, permissions. Consistency is a feature.
9. **Verify by observing behavior**, including rejections — and expect to find unrelated bugs when you do.

---

## 12. Topics to Study (mapped to where you can see them in this repo)

| Topic | Where it's visible here |
|---|---|
| Dependency Inversion / Ports & Adapters (Hexagonal Architecture) | `Application/Interfaces/IFileStorageService.cs` vs `Infrastructure/Services/Storage/LocalDiskFileStorage.cs` |
| Clean Architecture layering | Domain → Application → Infrastructure → Api flow of this feature |
| File upload security (OWASP File Upload Cheat Sheet) | `Api/Services/UploadRules.cs`, `FilesController.Upload` |
| Magic bytes / file signatures | `UploadRules.MatchesSignatureAsync` |
| Path traversal attacks | `LocalDiskFileStorage.ResolvePath` |
| Capability URLs & security-by-unguessability | public GUID content URLs |
| HTTP caching (`Cache-Control`, immutability) | `FilesController.GetContent` |
| HTTP range requests / partial content (206) | `enableRangeProcessing` for video |
| Multipart/form-data & request size limits in ASP.NET Core | `[RequestSizeLimit]`, `[RequestFormLimits]` on the upload action |
| Atomic file operations & crash consistency | temp-write-then-rename in `LocalDiskFileStorage.SaveAsync` |
| Compensating actions (mini-saga) | blob cleanup in `FilesController.Upload`'s catch block |
| Polymorphic associations | `StoredFile.OwnerType` + `OwnerId` |
| Soft deletes & audit columns | `AuditableEntity`, `Isdeleted` filtering in repositories |
| EF Core migrations & entity configuration | `StoredFileConfiguration`, `AddStoredFilesAndEmployeePhoto` migration |
| S3-compatible object storage (R2, MinIO, B2) | the planned second `IFileStorageService` implementation |
| Presigned URLs (the "next scale step") | not implemented — read about it as the alternative to API-streamed serving |

---

*Next step in this feature's story: wire the Angular `product-media-manager` to the real API — a good exercise in consuming exactly the contract designed here.*
