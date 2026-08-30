import { APIRequestContext, expect } from '@playwright/test';

/** apiUrl the ng dev server proxies to (see proxy.conf.json). */
export const API_BASE = 'http://localhost:5001/api/v1';

export const ADMIN_CREDENTIALS = { username: 'admin', password: 'Admin@1990' };

/** Logs in via the real auth endpoint and returns a bearer token for direct API calls (test setup only — not for driving the UI). */
export async function getAdminToken(request: APIRequestContext): Promise<string> {
    const res = await request.post(`${API_BASE}/auth/login`, { data: ADMIN_CREDENTIALS });
    expect(res.ok(), `admin login failed: ${res.status()} ${await res.text()}`).toBeTruthy();
    const body = await res.json();
    return body.token as string;
}

export function authHeaders(token: string) {
    return { Authorization: `Bearer ${token}` };
}

/** Unique-ish suffix so re-runs don't collide on unique fields (SKU, phone, email, code). */
export function uniqueSuffix(): string {
    return Date.now().toString(36) + Math.random().toString(36).slice(2, 6);
}
