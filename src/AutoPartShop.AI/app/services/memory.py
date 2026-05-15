from __future__ import annotations

from typing import Any

from qdrant_client import QdrantClient
from qdrant_client.http import models as qmodels

from app.config import settings


class QdrantMemoryService:
    def __init__(self) -> None:
        self.client = QdrantClient(url=settings.qdrant_url)
        self.collection = settings.qdrant_collection
        self._fallback: dict[str, list[str]] = {}
        self._enabled = True
        try:
            self._ensure_collection()
        except Exception:
            self._enabled = False

    def _ensure_collection(self) -> None:
        collections = self.client.get_collections().collections
        exists = any(c.name == self.collection for c in collections)
        if exists:
            return

        self.client.create_collection(
            collection_name=self.collection,
            vectors_config=qmodels.VectorParams(
                size=settings.qdrant_vector_size,
                distance=qmodels.Distance.COSINE,
            ),
        )

    @staticmethod
    def _fallback_key(customer_code: str, namespace: str) -> str:
        return f"{namespace}:{customer_code}"

    def save_preference(
        self,
        customer_code: str,
        note: str,
        vector: list[float],
        namespace: str = 'global',
    ) -> None:
        if not self._enabled:
            key = self._fallback_key(customer_code, namespace)
            self._fallback.setdefault(key, []).append(note)
            return

        point_id = abs(hash(f"{namespace}:{customer_code}:{note}")) % (10**12)
        self.client.upsert(
            collection_name=self.collection,
            points=[
                qmodels.PointStruct(
                    id=point_id,
                    vector=vector,
                    payload={
                        "namespace": namespace,
                        "customer_code": customer_code,
                        "note": note,
                    },
                )
            ],
        )

    def recent_preferences(self, customer_code: str, limit: int = 5, namespace: str = 'global') -> list[str]:
        if not self._enabled:
            if namespace == '*':
                notes: list[str] = []
                suffix = f":{customer_code}"
                for key, values in self._fallback.items():
                    if key.endswith(suffix):
                        notes.extend(values)
                return notes[-limit:]

            key = self._fallback_key(customer_code, namespace)
            return self._fallback.get(key, [])[-limit:]

        filters = [
            qmodels.FieldCondition(
                key="customer_code",
                match=qmodels.MatchValue(value=customer_code),
            )
        ]
        if namespace != '*':
            filters.insert(
                0,
                qmodels.FieldCondition(
                    key="namespace",
                    match=qmodels.MatchValue(value=namespace),
                ),
            )

        points, _ = self.client.scroll(
            collection_name=self.collection,
            limit=limit,
            with_payload=True,
            with_vectors=False,
            scroll_filter=qmodels.Filter(
                must=filters
            ),
        )

        notes: list[str] = []
        for point in points:
            payload: dict[str, Any] = point.payload or {}
            note = payload.get("note")
            if isinstance(note, str) and note:
                notes.append(note)
        return notes


memory_service = QdrantMemoryService()
