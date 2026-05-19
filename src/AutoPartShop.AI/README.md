# AutoPartShop.AI (Python, uv, Chainlit, LangChain, LangGraph, Qdrant)

## Run with uv (local)

1. Create and sync environment:

```bash
uv venv
uv sync
```

2. Configure environment:

```bash
cp .env.example .env
```

3. Start chatbot UI:

```bash
uv run chainlit run app/chainlit_app.py --host 0.0.0.0 --port 8088
```

## Run with Docker Compose

From `deployment`:

```bash
docker compose up --build autopartshop.ai qdrant
```

The chatbot UI is available at `http://localhost:8088`.
