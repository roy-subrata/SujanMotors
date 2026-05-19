from __future__ import annotations

import contextvars
from typing import Any

import httpx

from app.config import settings


_request_bearer_token: contextvars.ContextVar[str] = contextvars.ContextVar('request_bearer_token', default='')


class AutoPartShopApiClient:
    def __init__(self) -> None:
        self.base_url = settings.autopartshop_api_base_url.rstrip('/')
        self.timeout = settings.autopartshop_api_timeout_seconds

    def _headers(self) -> dict[str, str]:
        headers = {'Content-Type': 'application/json'}
        token = _request_bearer_token.get() or settings.autopartshop_api_bearer_token
        if token:
            headers['Authorization'] = f"Bearer {token}"
        return headers

    def get(self, path: str, params: dict[str, Any] | None = None) -> Any:
        with httpx.Client(timeout=self.timeout, headers=self._headers()) as client:
            response = client.get(f"{self.base_url}{path}", params=params)
            self._raise_for_status(response)
            return self._to_json(response)

    def post(self, path: str, payload: dict[str, Any] | None = None) -> Any:
        with httpx.Client(timeout=self.timeout, headers=self._headers()) as client:
            response = client.post(f"{self.base_url}{path}", json=payload or {})
            self._raise_for_status(response)
            return self._to_json(response)

    def _to_json(self, response: httpx.Response) -> Any:
        if not response.content:
            return {}
        return response.json()

    def _raise_for_status(self, response: httpx.Response) -> None:
        try:
            response.raise_for_status()
        except httpx.HTTPStatusError as exc:
            message = self._safe_error_message(exc.response)
            raise RuntimeError(message) from exc

    @staticmethod
    def _safe_error_message(response: httpx.Response) -> str:
        try:
            payload = response.json()
            if isinstance(payload, dict) and payload.get('message'):
                return str(payload['message'])
            return str(payload)
        except Exception:
            return response.text or f"HTTP {response.status_code}"


api_client = AutoPartShopApiClient()


def push_request_bearer_token(token: str) -> contextvars.Token:
    return _request_bearer_token.set(token or '')


def pop_request_bearer_token(token_ref: contextvars.Token) -> None:
    _request_bearer_token.reset(token_ref)
