from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file='.env', env_file_encoding='utf-8', extra='ignore')

    app_name: str = Field(default='AutoPartShop.AI', alias='APP_NAME')
    app_env: str = Field(default='production', alias='APP_ENV')
    app_port: int = Field(default=8088, alias='APP_PORT')
    log_level: str = Field(default='INFO', alias='LOG_LEVEL')

    openai_api_key: str = Field(default='', alias='OPENAI_API_KEY')
    openai_base_url: str | None = Field(default=None, alias='OPENAI_BASE_URL')
    openai_model: str = Field(default='gpt-4.1-mini', alias='OPENAI_MODEL')
    openai_max_tokens: int = Field(default=2048, alias='OPENAI_MAX_TOKENS')
    llm_temperature: float = Field(default=0.1, alias='LLM_TEMPERATURE')

    autopartshop_api_base_url: str = Field(default='http://autopartshop.api:8080', alias='AUTOPARTSHOP_API_BASE_URL')
    autopartshop_api_timeout_seconds: int = Field(default=30, alias='AUTOPARTSHOP_API_TIMEOUT_SECONDS')
    autopartshop_api_bearer_token: str = Field(default='', alias='AUTOPARTSHOP_API_BEARER_TOKEN')

    qdrant_url: str = Field(default='http://qdrant:6333', alias='QDRANT_URL')
    qdrant_collection: str = Field(default='autopartshop_customer_memory', alias='QDRANT_COLLECTION')
    qdrant_vector_size: int = Field(default=1536, alias='QDRANT_VECTOR_SIZE')

    default_warehouse: str = Field(default='A', alias='DEFAULT_WAREHOUSE')
    low_stock_threshold: int = Field(default=5, alias='LOW_STOCK_THRESHOLD')


settings = Settings()
