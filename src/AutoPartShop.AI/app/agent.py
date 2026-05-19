from __future__ import annotations

import logging
from pathlib import Path
from typing import Any, cast

from langchain.agents import create_agent
from langchain_core.messages import SystemMessage
from langchain_openai import ChatOpenAI
from langgraph.checkpoint.memory import MemorySaver
from pydantic import SecretStr

from app.config import settings
from app.services.api_client import pop_request_bearer_token, push_request_bearer_token
from app.services.tools import build_tools

PROMPT_PATH = Path(__file__).resolve().parent / 'prompts' / 'system_prompt.txt'

_memory = MemorySaver()
logger = logging.getLogger(__name__)


def _load_prompt() -> str:
    try:
        return PROMPT_PATH.read_text(encoding='utf-8')
    except FileNotFoundError:
        return 'You are AutoPartShop.AI. Use tools for live business data and ask clarification when needed.'


def _llm() -> ChatOpenAI:
    if not settings.openai_api_key:
        raise RuntimeError('OPENAI_API_KEY is required for Chainlit chatbot runtime')

    return ChatOpenAI(
        model=settings.openai_model,
        temperature=settings.llm_temperature,
        max_tokens=settings.openai_max_tokens,
        base_url=settings.openai_base_url,
        api_key=SecretStr(settings.openai_api_key),
    )


_agent: Any = None


def _get_agent() -> Any:
    global _agent
    if _agent is None:
        _agent = create_agent(
            model=_llm(),
            tools=build_tools(),
            system_prompt=SystemMessage(content=_load_prompt()),
            checkpointer=_memory,
        )
    return _agent


def run_agent(message: str, session_id: str, bearer_token: str = '') -> str:
    try:
        agent = _get_agent()
        token_ref = push_request_bearer_token(bearer_token)
        try:
            result = agent.invoke(
                {'messages': [{'role': 'user', 'content': message}]},
                config={'configurable': {'thread_id': session_id}},
            )
        finally:
            pop_request_bearer_token(token_ref)
    except Exception as exc:
        logger.warning('Agent invocation failed: %s', type(exc).__name__)
        return (
            'I could not complete your request due to a temporary service issue. '
            'Please try again in a few seconds.'
        )

    if not isinstance(result, dict):
        return 'I could not generate a response. Please try again.'

    raw_messages = cast(Any, result).get('messages', [])
    if not isinstance(raw_messages, list):
        return 'I could not generate a response. Please try again.'
    messages = cast(list[Any], raw_messages)

    for item in reversed(messages):
        content = getattr(item, 'content', None)
        if isinstance(content, str) and content.strip():
            return content
    return 'I could not generate a response. Please try again.'
