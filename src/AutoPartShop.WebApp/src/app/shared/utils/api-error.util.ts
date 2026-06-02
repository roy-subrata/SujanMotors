import { HttpErrorResponse } from '@angular/common/http';

/**
 * Canonical structured error shape returned by the API (see Api/Common/ApiError.cs).
 * `detail` is the primary message; `message` is a backward-compatibility alias.
 */
export interface ApiError {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  message?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/**
 * Extract a human-readable message from any error the API may return, regardless of
 * shape. Handles the structured ApiError (`detail`/`message`), legacy ad-hoc
 * `{ message }` objects, bare string bodies, and validation `errors` dictionaries.
 *
 * Prefer this over reading `err.error.message` directly so callers stay resilient
 * as endpoints migrate to the structured ApiError contract.
 *
 * @param err      The caught error (typically an HttpErrorResponse).
 * @param fallback Message to use when nothing usable can be extracted.
 */
export function extractApiError(err: unknown, fallback = 'Something went wrong. Please try again.'): string {
  const body = err instanceof HttpErrorResponse ? err.error : (err as { error?: unknown })?.error ?? err;

  // Bare string body, e.g. StatusCode(500, "message").
  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  const apiError = body as ApiError | null | undefined;
  if (apiError && typeof apiError === 'object') {
    // Flatten a validation errors dictionary first — it carries the most specific text.
    if (apiError.errors) {
      const flattened = Object.values(apiError.errors).flat().filter(Boolean);
      if (flattened.length > 0) {
        return flattened.join(' ');
      }
    }
    if (apiError.detail?.trim()) {
      return apiError.detail;
    }
    if (apiError.message?.trim()) {
      return apiError.message;
    }
  }

  // Angular surfaces a generic message on network/parse failures.
  if (err instanceof HttpErrorResponse && err.message?.trim()) {
    return err.message;
  }

  return fallback;
}
