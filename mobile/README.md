# Auto Parts Shop — Staff Mobile App

A Flutter staff/internal app for the Auto Parts Shop. Phase 1 covers **login**, **product
search**, **stock check**, and **barcode scanning**, consuming the existing .NET API
(`src/AutoPartShop.Api`). It is the foundation for a future mobile POS.

## Tech stack
- Flutter (stable, Dart 3) + Material 3
- `flutter_riverpod` — state/DI
- `dio` — HTTP client (auth + error interceptors)
- `go_router` — navigation with an auth redirect guard
- `flutter_secure_storage` — JWT storage
- `mobile_scanner` — camera barcode/QR scanning

Models are hand-written immutable classes with tolerant `fromJson` parsers (no codegen step).

## Configuration
The API base URL is injected at build/run time via `--dart-define`:

| Target | Value |
| --- | --- |
| Android emulator | `http://10.0.2.2:5001` (default) |
| iOS simulator | `http://localhost:5001` |
| Physical device | `http://<your-machine-LAN-IP>:5001` |

All endpoints are consumed under the `/api/v1` prefix (added automatically).

## Running
```bash
# 1. Start the API (from repo root) — listens on http://localhost:5001, Swagger at /docs
#    dotnet run --project src/AutoPartShop.Api

# 2. From this folder:
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5001
```

Sign in with a seeded staff account, e.g. `admin` / `Admin@1990`.

## Tests & analysis
```bash
flutter analyze
flutter test
```

## Project layout
```
lib/
  core/        config (API base URL), network (dio + interceptors + AppException),
               router (go_router + auth guard), storage (secure token store)
  features/    auth, products (search + detail), stock, scanner
  shared/      models (DTOs + paged envelope), widgets, formatting helpers
```

## Endpoints used (phase 1)
- `POST /auth/login` — staff JWT login
- `GET  /products?search=&page=&pageSize=` — paginated product search
- `GET  /products/{id}` — product detail (variants + pricing)
- `GET  /products/by-code?code=` — barcode/SKU/part-number lookup
- `GET  /stock/levels/part/{partId}` — per-warehouse stock levels
- `POST /stock/check` — quantity availability probe

## Notes for production
- The dev build allows cleartext HTTP (`usesCleartextTraffic` on Android,
  `NSAllowsArbitraryLoads` on iOS) so it can reach the local `http://` API. **Use HTTPS in
  production** and remove/tighten those flags.
- Native HTTP requests don't send an `Origin` header, so the API's CORS allow-list doesn't block
  the app. A Flutter **web** build would need the web origin added to `Cors:AllowedOrigins`.
