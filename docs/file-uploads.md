# File Uploads (Product Media, Employee Photos & Documents)

Uploaded binaries (images, videos, documents) are stored behind the `IFileStorageService`
abstraction and tracked in the `StoredFiles` table. The only URL surface is the API
(`/api/v1/files/{id}/content`), so the storage backend can change without breaking links.

## Storage backends

| Provider | Status | Notes |
|---|---|---|
| `Local` (default) | Implemented | Blobs on disk under `FileStorage:Local:RootPath` (default `{ContentRoot}/App_Data/uploads`) |
| S3-compatible (Cloudflare R2 / MinIO / B2) | Planned | Add an `IFileStorageService` implementation; no caller changes needed |

### Configuration (`appsettings.json`)

```jsonc
"FileStorage": {
  "Provider": "Local",
  "PublicBaseUrl": "",          // optional absolute prefix for returned URLs (e.g. "https://api.example.com")
  "Local": { "RootPath": "" }   // blank = {ContentRoot}/App_Data/uploads
}
```

**VPS deployment:** the upload root **must** be bind-mounted, or every rebuild/recreate of the
API container destroys the blobs while the `StoredFiles` / `ProductMedia` rows survive and start
returning 404s. All three compose files mount it:

```yaml
volumes:
  - ./uploads:/app/App_Data/uploads   # docker-compose.yml (prod) and .dev.yml
  - ./uploads-test:/app/App_Data/uploads   # docker-compose.test.yml
```

Include that folder in the backup strategy alongside the database — the DB backup job does not
cover it. `src/AutoPartShop.Api/App_Data/` and `deployment/uploads*/` are gitignored — uploads
must never be committed.

## Upload rules

Kind is inferred from the file extension; the client's content type is never trusted and
magic bytes are verified (see `Api/Services/UploadRules.cs`).

| Kind | Extensions | Max size | Access |
|---|---|---|---|
| IMAGE | jpg, jpeg, png, gif, webp | 5 MB | Public (anonymous GET) |
| VIDEO | mp4, mov, webm | 100 MB | Public, range requests supported (`<video>` seeking) |
| DOCUMENT | pdf, doc(x), xls(x), csv, txt | 10 MB | Authenticated only (fetch as blob with JWT) |

Public files are served with `Cache-Control: public, max-age=31536000, immutable`
(keys are never reused). URLs contain an unguessable GUID.

## Endpoints

### Files — `/api/v1/files`

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/` | Any logged-in user | Multipart upload: `file`, optional `ownerType` (e.g. `PRODUCT`, `EMPLOYEE`) + `ownerId`. Returns `{ id, url, fileName, contentType, sizeBytes, kind, isPublic }`. Throttled by the `upload` rate-limit tier (default 30/min per IP) |
| GET | `/{id}/content` | Public files: none; documents: JWT | Streams the bytes |
| GET | `/?ownerType=&ownerId=` | Logged-in | List a record's attachments |
| DELETE | `/{id}` | Admin/Manager: any file. Others: only files they uploaded (`CreatedBy`) | Deletes record + blob |

Upload and attach are two calls, so a client that uploads successfully but then fails to attach
must `DELETE /api/v1/files/{id}` to avoid orphaning the blob — that is why the uploader can
delete their own file. The web media manager, the employee photo form, and the mobile product
uploader all do this.

### Product media — `/api/v1/products/{partId}/media`

Gallery rows live in `ProductMedia` (URL-based: uploaded file URLs or external, e.g. YouTube).

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/` | `inventory.view` | Gallery ordered by `sortOrder` |
| POST | `/` | `inventory.edit` | Add `{ url, mediaType, altText?, fileName?, isPrimary?, variantId? }`; first item auto-becomes primary. `variantId` must belong to this part |
| PUT | `/{mediaId}` | `inventory.edit` | Update fields (same `variantId` rule) |
| PATCH | `/{mediaId}/primary` | `inventory.edit` | Make primary (clears others) |
| PUT | `/order` | `inventory.edit` | `{ orderedIds: [] }` — sortOrder by position; all ids validated before anything is written |
| DELETE | `/{mediaId}` | `inventory.edit` | Removes row **and** the uploaded blob when the URL is ours; deleting the primary promotes the next item in display order |

A part that has media always has exactly one primary: the first item added is promoted
automatically, and deleting the primary promotes its successor.

### Employee photo

`PUT /api/v1/employees/{id}/photo` (Admin/Manager) with `{ "photoUrl": "..." }` — upload the
image first, then pass the returned URL; `null` clears it. `photoUrl` is included in employee
responses. Other employee files (NID scans, contracts): upload with `ownerType=EMPLOYEE` +
`ownerId` and list via the files owner query.

## Typical frontend flow

1. `POST /api/v1/files` (multipart) → get `url`.
2. Attach it: `POST /api/v1/products/{partId}/media` or `PUT /api/v1/employees/{id}/photo`.
3. Render: image/video URLs go straight into `<img>` / `<video>` (prefix with the API origin);
   documents are downloaded via `HttpClient` with the auth interceptor and saved as a blob.
