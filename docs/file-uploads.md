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

**VPS deployment:** bind-mount `RootPath` into the container (e.g. `-v /srv/autopartshop/uploads:/app/App_Data/uploads`)
so files survive rebuilds, and include that folder in the backup strategy alongside the database.
`src/AutoPartShop.Api/App_Data/` is gitignored — uploads must never be committed.

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
| POST | `/` | Any logged-in user | Multipart upload: `file`, optional `ownerType` (e.g. `PRODUCT`, `EMPLOYEE`) + `ownerId`. Returns `{ id, url, fileName, contentType, sizeBytes, kind, isPublic }` |
| GET | `/{id}/content` | Public files: none; documents: JWT | Streams the bytes |
| GET | `/?ownerType=&ownerId=` | Logged-in | List a record's attachments |
| DELETE | `/{id}` | Admin, Manager | Deletes record + blob |

### Product media — `/api/v1/products/{partId}/media`

Gallery rows live in `ProductMedia` (URL-based: uploaded file URLs or external, e.g. YouTube).

| Method | Route | Permission | Purpose |
|---|---|---|---|
| GET | `/` | `inventory.view` | Gallery ordered by `sortOrder` |
| POST | `/` | `inventory.edit` | Add `{ url, mediaType, altText?, fileName?, isPrimary?, variantId? }`; first item auto-becomes primary |
| PUT | `/{mediaId}` | `inventory.edit` | Update fields |
| PATCH | `/{mediaId}/primary` | `inventory.edit` | Make primary (clears others) |
| PUT | `/order` | `inventory.edit` | `{ orderedIds: [] }` — sortOrder by position |
| DELETE | `/{mediaId}` | `inventory.edit` | Removes row **and** the uploaded blob when the URL is ours |

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
