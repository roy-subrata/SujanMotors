from pydantic import BaseModel, Field


def _empty_cart_items() -> list['CartItem']:
    return []


class CartItem(BaseModel):
    part_id: str
    part_number: str
    name: str
    qty: int
    unit_price: float
    unit_id: str | None = None


class SessionState(BaseModel):
    session_id: str
    active_warehouse: str = ''
    active_warehouse_id: str = ''
    auth_user_identifier: str = ''
    auth_roles: list[str] = Field(default_factory=list)
    selected_customer_id: str | None = None
    selected_customer_code: str | None = None
    selected_customer_name: str | None = None
    cart_items: list[CartItem] = Field(default_factory=_empty_cart_items)
