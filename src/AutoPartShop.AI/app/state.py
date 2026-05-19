from __future__ import annotations

from collections.abc import MutableMapping

from app.models import SessionState


class SessionStore:
    def __init__(self) -> None:
        self._sessions: MutableMapping[str, SessionState] = {}

    def get_or_create(self, session_id: str, default_warehouse: str) -> SessionState:
        session = self._sessions.get(session_id)
        if session is None:
            session = SessionState(session_id=session_id, active_warehouse=default_warehouse)
            self._sessions[session_id] = session
        return session


session_store = SessionStore()
