from __future__ import annotations

import asyncio
import io
import logging
import sys
import uuid
from pathlib import Path

import chainlit as cl
import httpx
import speech_recognition as sr
from chainlit.user import User
from pydub import AudioSegment

# Make `app.*` imports work when Chainlit executes this file directly.
PROJECT_ROOT = Path(__file__).resolve().parent.parent
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from app.agent import run_agent
from app.config import settings
from app.state import session_store

_recognizer = sr.Recognizer()
_audio_buffers: dict[str, bytearray] = {}
_audio_mimes: dict[str, str] = {}
_session_locks: dict[str, asyncio.Lock] = {}
DEFAULT_STT_LANGUAGE = 'bn-BD'
logger = logging.getLogger(__name__)


@cl.password_auth_callback
async def auth_callback(username: str, password: str) -> User | None:
    """Authenticate Chainlit user against backend AuthController login endpoint."""
    try:
        login_url = f"{settings.autopartshop_api_base_url.rstrip('/')}/api/Auth/login"
        async with httpx.AsyncClient(timeout=settings.autopartshop_api_timeout_seconds) as client:
            response = await client.post(
                login_url,
                json={'username': username, 'password': password},
            )

        if response.status_code != 200:
            return None

        payload = response.json()
        roles = payload.get('roles') if isinstance(payload.get('roles'), list) else []
        permissions = payload.get('permissions') if isinstance(payload.get('permissions'), list) else []
        token = str(payload.get('token') or '')

        return User(
            identifier=str(payload.get('username') or username),
            display_name=str(payload.get('fullName') or username),
            metadata={
                'roles': roles,
                'permissions': permissions,
                'access_token': token,
                'email': str(payload.get('email') or ''),
            },
        )
    except Exception:
        return None


def _get_session_lock(session_id: str) -> asyncio.Lock:
    lock = _session_locks.get(session_id)
    if lock is None:
        lock = asyncio.Lock()
        _session_locks[session_id] = lock
    return lock


def _user_access_token() -> str:
    user = cl.user_session.get('user')
    if not isinstance(user, User):
        return ''
    metadata = user.metadata if isinstance(user.metadata, dict) else {}
    token = metadata.get('access_token')
    return str(token) if isinstance(token, str) else ''


def _preferred_session_id() -> str:
    user = cl.user_session.get('user')
    if isinstance(user, User):
        identifier = str(user.identifier or '').strip().lower()
        if identifier:
            return f"user:{identifier}"
    return str(uuid.uuid4())


def _candidate_formats(mime_type: str) -> list[str | None]:
    mime = (mime_type or '').lower()
    formats: list[str | None] = []

    if 'webm' in mime:
        formats.extend(['webm', 'matroska'])
    if 'ogg' in mime or 'opus' in mime:
        formats.extend(['ogg', 'opus'])
    if 'wav' in mime or 'wave' in mime or 'pcm' in mime:
        formats.append('wav')
    if 'mp4' in mime or 'm4a' in mime:
        formats.extend(['mp4', 'm4a'])
    if 'mpeg' in mime or 'mp3' in mime:
        formats.append('mp3')

    # Final auto-detection fallback
    formats.append(None)

    # De-duplicate while preserving order
    deduped: list[str | None] = []
    for fmt in formats:
        if fmt not in deduped:
            deduped.append(fmt)
    return deduped


def _speech_to_text(audio_bytes: bytes, mime_type: str, language: str = DEFAULT_STT_LANGUAGE) -> str:
    """Convert raw audio bytes to text using Google Speech Recognition (free, no API key needed)."""
    normalized_mime = (mime_type or '').lower()

    # Chainlit can stream raw PCM16 bytes (not a container format). Decode directly.
    if 'pcm16' in normalized_mime or normalized_mime in {'pcm', 'audio/pcm', 'audio/raw'}:
        audio_data = sr.AudioData(audio_bytes, sample_rate=24000, sample_width=2)
        try:
            return _recognizer.recognize_google(audio_data, language=language)
        except sr.UnknownValueError:
            return _recognizer.recognize_google(audio_data, language='en-US')

    last_error: Exception | None = None
    segment: AudioSegment | None = None

    for fmt in _candidate_formats(normalized_mime):
        audio_io = io.BytesIO(audio_bytes)
        try:
            if fmt is None:
                segment = AudioSegment.from_file(audio_io)
            else:
                segment = AudioSegment.from_file(audio_io, format=fmt)
            break
        except Exception as exc:
            last_error = exc

    if segment is None:
        message = f'Unable to decode recorded audio (mime={mime_type}).'
        if last_error is not None:
            message = f'{message} Decoder error: {last_error}'
        raise RuntimeError(message)

    wav_io = io.BytesIO()
    segment.export(wav_io, format='wav')
    wav_io.seek(0)

    with sr.AudioFile(wav_io) as source:
        audio_data = _recognizer.record(source)

    try:
        return _recognizer.recognize_google(audio_data, language=language)
    except sr.UnknownValueError:
        return _recognizer.recognize_google(audio_data, language='en-US')


@cl.on_chat_start
async def on_chat_start() -> None:
    session_id = _preferred_session_id()
    cl.user_session.set('session_id', session_id)
    _audio_buffers[session_id] = bytearray()
    _audio_mimes[session_id] = 'audio/webm'
    cl.user_session.set('stt_language', DEFAULT_STT_LANGUAGE)

    user = cl.user_session.get('user')
    session = session_store.get_or_create(session_id, '')
    if isinstance(user, User):
        session.auth_user_identifier = user.identifier
        roles = user.metadata.get('roles') if isinstance(user.metadata, dict) else []
        session.auth_roles = [str(r) for r in roles if isinstance(r, str)]

    greeting = (
        'আসসালামু আলাইকুম। AutoPartShop.AI-তে আপনাকে স্বাগতম। আমি আপনার সেলস অ্যাসিস্ট্যান্ট। '
        'আমি প্রোডাক্ট, স্টক, কাস্টমার, সেলস এবং পেমেন্ট বিষয়ে সহায়তা করতে পারি। '
        'আপনি টাইপ করতে পারেন অথবা মাইক্রোফোন বাটন ব্যবহার করে কথা বলতে পারেন। '
        'বলুন, আপনি কী করতে চান।'
    )

    await cl.Message(content=greeting).send()


@cl.on_audio_start
async def on_audio_start() -> bool:
    session_id = cl.user_session.get('session_id')
    if not session_id:
        session_id = _preferred_session_id()
        cl.user_session.set('session_id', session_id)
    _audio_buffers[session_id] = bytearray()
    _audio_mimes[session_id] = 'audio/webm'
    return True


@cl.on_audio_chunk
async def on_audio_chunk(chunk: cl.InputAudioChunk) -> None:
    session_id = cl.user_session.get('session_id')
    if not session_id:
        return

    if session_id not in _audio_buffers:
        _audio_buffers[session_id] = bytearray()

    _audio_buffers[session_id].extend(chunk.data)
    if chunk.mimeType:
        _audio_mimes[session_id] = chunk.mimeType


@cl.on_audio_end
async def on_audio_end() -> None:
    session_id = cl.user_session.get('session_id')
    if not session_id:
        session_id = _preferred_session_id()
        cl.user_session.set('session_id', session_id)

    audio_bytes = bytes(_audio_buffers.get(session_id, bytearray()))
    mime_type = _audio_mimes.get(session_id, 'audio/webm')

    # Clean up session audio buffers after capture ends.
    _audio_buffers.pop(session_id, None)
    _audio_mimes.pop(session_id, None)

    if not audio_bytes:
        await cl.Message(content='No audio received. Please try again.').send()
        return

    # Step 1: Speech -> Text
    try:
        stt_language: str = cl.user_session.get('stt_language') or DEFAULT_STT_LANGUAGE
        recognized_text = await cl.make_async(_speech_to_text)(audio_bytes, mime_type, stt_language)
    except sr.UnknownValueError:
        await cl.Message(
            content="Sorry, I couldn't understand the audio. Please speak clearly and try again."
        ).send()
        return
    except sr.RequestError as exc:
        logger.warning('Speech recognition service error: %s', type(exc).__name__)
        await cl.Message(
            content=(
                'Voice service is temporarily unavailable right now. '
                'Please try again in a moment or type your message.'
            )
        ).send()
        return

    # Show recognized text so user knows what was heard
    await cl.Message(content=f'You said: {recognized_text}').send()

    # Step 2: Text -> LLM
    try:
        async with _get_session_lock(session_id):
            reply = await cl.make_async(run_agent)(recognized_text, session_id, _user_access_token())
    except Exception as exc:
        logger.warning('Failed to process user audio request: %s', type(exc).__name__)
        await cl.Message(
            content=(
                'I could not complete your request right now because a service call failed. '
                'Please try again in a few seconds.'
            )
        ).send()
        return

    # Step 3: Show text response
    await cl.Message(content=reply).send()

@cl.on_message
async def on_message(message: cl.Message) -> None:
    session_id = cl.user_session.get('session_id')
    if not session_id:
        session_id = _preferred_session_id()
        cl.user_session.set('session_id', session_id)

    try:
        async with _get_session_lock(session_id):
            reply = await cl.make_async(run_agent)(message.content, session_id, _user_access_token())
        await cl.Message(content=reply).send()
    except Exception as exc:
        logger.warning('Failed to process user message: %s', type(exc).__name__)
        await cl.Message(
            content=(
                'I could not complete your request right now because a service call failed. '
                'Please try again in a few seconds.'
            )
        ).send()
