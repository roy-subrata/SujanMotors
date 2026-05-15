from __future__ import annotations

import uuid
from typing import Any

from langchain_core.tools import StructuredTool

from app.models import CartItem
from app.services.api_client import api_client
from app.services.memory import memory_service
from app.state import session_store


def _embed_stub(text: str, size: int) -> list[float]:
    vector = [0.0] * size
    for i, ch in enumerate(text[:size]):
        vector[i] = (ord(ch) % 255) / 255.0
    return vector


def _normalize_search_json(raw: Any) -> dict[str, Any]:
    if isinstance(raw, dict):
        return raw
    return {'result': raw}


def _extract_items(payload: Any) -> list[dict[str, Any]]:
    if isinstance(payload, list):
        return [x for x in payload if isinstance(x, dict)]
    if not isinstance(payload, dict):
        return []

    candidates = [payload.get('data'), payload.get('items'), payload.get('result')]
    for candidate in candidates:
        if isinstance(candidate, list):
            return [x for x in candidate if isinstance(x, dict)]
    return []


def _is_guid(value: str) -> bool:
    try:
        uuid.UUID(str(value))
        return True
    except Exception:
        return False


def _clean(value: Any) -> str:
    return str(value or '').strip()


def _display_customer_name(customer: dict[str, Any]) -> str:
    full_name = _clean(customer.get('fullName'))
    if full_name:
        return full_name

    first_name = _clean(customer.get('firstName'))
    last_name = _clean(customer.get('lastName'))
    combined = f"{first_name} {last_name}".strip()
    if combined:
        return combined

    code = _clean(customer.get('customerCode'))
    if code:
        return code
    return 'Customer'


def _memory_namespace_for_session(session_id: str) -> str:
    if not session_id:
        return 'global'

    session = session_store.get_or_create(session_id, '')
    role = session.auth_roles[0] if session.auth_roles else 'anonymous'
    user = session.auth_user_identifier or 'unknown'
    return f"{role.lower()}:{user}"


def _is_privileged_memory_role(session_id: str) -> bool:
    if not session_id:
        return False
    session = session_store.get_or_create(session_id, '')
    roles = [str(r).strip().lower() for r in session.auth_roles if isinstance(r, str)]
    return 'admin' in roles or 'system' in roles


def _resolve_customer(customer_query: str) -> dict[str, Any]:
    query = customer_query.strip()
    if not query:
        raise RuntimeError('Customer name/code/ID is required')

    if _is_guid(query):
        return api_client.get(f'/api/Customer/{query}')

    try:
        by_code = api_client.get(f'/api/Customer/code/{query}')
        if isinstance(by_code, dict) and by_code.get('id'):
            return by_code
    except Exception:
        pass

    found = search_customer(query, page_number=1, page_size=20)
    items = _extract_items(found)
    if not items:
        raise RuntimeError(f'Customer not found: {query}')

    q = query.lower()
    for item in items:
        full_name = str(item.get('fullName') or '').lower()
        code = str(item.get('customerCode') or '').lower()
        phone = str(item.get('phone') or '').lower()
        if q in {full_name, code, phone}:
            return item
    return items[0]


def _resolve_part(part_query: str) -> dict[str, Any]:
    query = part_query.strip()
    if not query:
        raise RuntimeError('Product search text/part number/ID is required')

    if _is_guid(query):
        return api_client.get(f'/api/Parts/public/{query}')

    result = search_products(query, page_number=1, page_size=20)
    items = _extract_items(result)
    if not items:
        raise RuntimeError(f'Product not found: {query}')

    q = query.lower()
    for item in items:
        part_number = str(item.get('partNumber') or '').lower()
        sku = str(item.get('sku') or '').lower()
        name = str(item.get('name') or '').lower()
        if q in {part_number, sku} or q == name:
            return item
    return items[0]


def search_products(
    search_text: str,
    page_number: int = 1,
    page_size: int = 10,
) -> dict[str, Any]:
    payload = {
        'search': search_text,
        'pageNumber': page_number,
        'pageSize': page_size,
        'isActive': True,
    }
    data = api_client.post('/api/Parts/public/list', payload)
    return _normalize_search_json(data)


def get_warehouse_stock(
    warehouse_code: str = '',
    availability: str = 'all',
    page_number: int = 1,
    page_size: int = 20,
) -> dict[str, Any]:
    if availability.lower() == 'low_stock':
        data = api_client.get('/api/Stock/levels/low-stock')
        return _normalize_search_json(data)

    if warehouse_code:
        warehouse = api_client.get(f'/api/Warehouses/code/{warehouse_code}')
        warehouse_id = warehouse.get('id')
        if not warehouse_id:
            raise RuntimeError(f'Warehouse code not found: {warehouse_code}')
        data = api_client.get(f'/api/Stock/levels/warehouse/{warehouse_id}')
        return {'warehouse': warehouse, 'stock': data}

    payload = {'pageNumber': page_number, 'pageSize': page_size}
    data = api_client.post('/api/Stock/levels/list', payload)
    return _normalize_search_json(data)


def list_warehouses(page_number: int = 1, page_size: int = 50) -> dict[str, Any]:
    payload = {'pageNumber': page_number, 'pageSize': page_size}
    data = api_client.post('/api/Warehouses/list', payload)
    return _normalize_search_json(data)


def search_customer(query: str = '', page_number: int = 1, page_size: int = 10) -> dict[str, Any]:
    try:
        phone_result = api_client.get('/api/Customer/search-by-phone', {'phone': query})
        if isinstance(phone_result, dict) and phone_result.get('id'):
            return {'data': [phone_result], 'totalCount': 1}
    except Exception:
        pass

    payload = {'search': query, 'pageNumber': page_number, 'pageSize': page_size}
    data = api_client.post('/api/Customer/list', payload)
    return _normalize_search_json(data)


def create_customer(
    name: str,
    phone: str,
    email: str = '',
    city: str = '',
    country: str = 'Bangladesh',
) -> dict[str, Any]:
    parts = [p for p in name.strip().split(' ') if p]
    if not parts:
        raise RuntimeError('Customer name is required')

    first_name = parts[0]
    last_name = ' '.join(parts[1:]) if len(parts) > 1 else '.'

    code_response = api_client.get('/api/code-generate/customer')
    customer_code = code_response.get('customerCode')
    if not customer_code:
        raise RuntimeError('Unable to generate customer code')

    payload = {
        'customerCode': customer_code,
        'firstName': first_name,
        'lastName': last_name,
        'phone': phone,
        'email': email,
        'city': city,
        'country': country,
        'customerType': 'Regular',
    }
    return api_client.post('/api/Customer', payload)


def select_customer_for_session(session_id: str, customer_id: str) -> dict[str, Any]:
    customer = api_client.get(f'/api/Customer/{customer_id}')
    session = session_store.get_or_create(session_id, '')
    session.selected_customer_id = customer.get('id')
    session.selected_customer_code = customer.get('customerCode')
    session.selected_customer_name = customer.get('fullName') or f"{customer.get('firstName', '')} {customer.get('lastName', '')}".strip()
    return {'session': session.model_dump(), 'customer': customer}


def add_to_cart(
    session_id: str,
    part_id: str,
    part_number: str,
    name: str,
    unit_price: float,
    quantity: int = 1,
    unit_id: str = '',
) -> dict[str, Any]:
    if quantity < 1:
        raise RuntimeError('Quantity must be at least 1')

    session = session_store.get_or_create(session_id, '')
    session.cart_items.append(
        CartItem(
            part_id=part_id,
            part_number=part_number,
            name=name,
            qty=quantity,
            unit_price=unit_price,
            unit_id=unit_id or None,
        )
    )
    return {'session': session.model_dump()}


def set_active_warehouse(session_id: str, warehouse_code: str) -> dict[str, Any]:
    code_or_id = warehouse_code.strip()
    if not code_or_id:
        raise RuntimeError('Warehouse code or ID is required')

    try:
        if _is_guid(code_or_id):
            warehouse = api_client.get(f'/api/Warehouses/{code_or_id}')
        else:
            warehouse = api_client.get(f'/api/Warehouses/code/{code_or_id}')
    except Exception as exc:
        raise RuntimeError(f'Warehouse not found: {code_or_id}') from exc

    warehouse_id = warehouse.get('id', '')
    normalized_code = warehouse.get('code') or warehouse.get('warehouseCode') or code_or_id
    session = session_store.get_or_create(session_id, normalized_code)
    session.active_warehouse = normalized_code
    session.active_warehouse_id = warehouse_id
    return {'session': session.model_dump(), 'warehouse': warehouse}


def create_sale(
    session_id: str,
    customer_id: str = '',
    warehouse_id: str = '',
    notes: str = '',
    currency: str = 'BDT',
) -> dict[str, Any]:
    session = session_store.get_or_create(session_id, '')
    if not session.cart_items:
        raise RuntimeError('Cart is empty')

    # Resolve customer from arguments/session and make sure required customer context is populated.
    resolved_customer_id = _clean(customer_id) or _clean(session.selected_customer_id)
    customer: dict[str, Any]
    if resolved_customer_id:
        customer = api_client.get(f'/api/Customer/{resolved_customer_id}')
    elif session.selected_customer_code:
        customer = api_client.get(f'/api/Customer/code/{session.selected_customer_code}')
        resolved_customer_id = _clean(customer.get('id'))
    else:
        raise RuntimeError('No customer selected. Please search and select a customer first.')

    resolved_customer_id = _clean(customer.get('id')) or resolved_customer_id
    if not resolved_customer_id:
        raise RuntimeError('Selected customer is invalid. Please re-select a customer.')

    # Keep session customer context fully populated for downstream tools/memory.
    session.selected_customer_id = resolved_customer_id
    session.selected_customer_code = _clean(customer.get('customerCode')) or session.selected_customer_code
    session.selected_customer_name = _display_customer_name(customer)

    # Resolve warehouse from arguments/session and ensure ID is available.
    resolved_warehouse_id = _clean(warehouse_id) or _clean(session.active_warehouse_id)
    if not resolved_warehouse_id:
        active_code = _clean(session.active_warehouse)
        if active_code:
            warehouse = api_client.get(f'/api/Warehouses/code/{active_code}')
            resolved_warehouse_id = _clean(warehouse.get('id'))
            session.active_warehouse_id = resolved_warehouse_id
            session.active_warehouse = _clean(warehouse.get('code') or warehouse.get('warehouseCode')) or active_code
    if not resolved_warehouse_id:
        raise RuntimeError('No warehouse selected. Please set the active warehouse first.')

    # Validate cart lines before API call so required fields are always populated.
    lines: list[dict[str, Any]] = []
    for item in session.cart_items:
        if not _clean(item.part_id):
            raise RuntimeError('Cart contains an item without part ID. Please remove and add it again.')
        if item.qty < 1:
            raise RuntimeError('Cart contains an item with invalid quantity. Quantity must be at least 1.')
        if item.unit_price < 0:
            raise RuntimeError('Cart contains an item with invalid unit price.')

        lines.append(
            {
                'partId': item.part_id,
                'unitId': item.unit_id,
                'quantity': item.qty,
                'unitPrice': item.unit_price,
                'discount': 0,
            }
        )

    today = __import__('datetime').datetime.utcnow().strftime('%Y-%m-%dT00:00:00Z')
    customer_name = _display_customer_name(customer)
    customer_email = _clean(customer.get('email'))
    customer_phone = _clean(customer.get('phone'))
    customer_city = _clean(customer.get('city'))

    payload = {
        'customerId': resolved_customer_id,
        'warehouseId': resolved_warehouse_id,
        'customerName': customer_name,
        'customerEmail': customer_email,
        'customerPhone': customer_phone,
        'customerCity': customer_city,
        'deliveryDate': today,
        'notes': notes,
        'currency': currency,
        'discount': 0,
        'lines': lines,
    }

    order = api_client.post('/api/SalesOrder', payload)

    if session.selected_customer_code:
        net_total = sum(item.qty * item.unit_price for item in session.cart_items)
        namespace = _memory_namespace_for_session(session_id)
        memory_service.save_preference(
            session.selected_customer_code,
            f"Bought {len(session.cart_items)} items; net {net_total:.2f}",
            _embed_stub(f"{session.selected_customer_code}:{net_total:.2f}", 1536),
            namespace=namespace,
        )

    session.cart_items = []
    return {'order': order, 'session': session.model_dump()}


def get_customer_payment_summary(customer_id: str) -> dict[str, Any]:
    return api_client.get(f'/api/CustomerPayment/customer/{customer_id}/summary')


def get_customer_recent_memory(customer_code: str, session_id: str, limit: int = 5) -> dict[str, Any]:
    code = customer_code.strip()
    if not code:
        raise RuntimeError('Customer code is required')

    privileged = _is_privileged_memory_role(session_id)
    namespace = '*' if privileged else _memory_namespace_for_session(session_id)
    notes = memory_service.recent_preferences(code, limit=limit, namespace=namespace)
    return {
        'customerCode': code,
        'namespace': namespace,
        'accessScope': 'all' if privileged else 'scoped',
        'notes': notes,
    }


def get_product_pricing_and_stock(part_query: str, warehouse_code: str = '') -> dict[str, Any]:
    product = _resolve_part(part_query)
    part_id = product.get('id')
    if not part_id:
        raise RuntimeError('Resolved product does not have an ID')

    data: dict[str, Any] = {'product': product}

    if warehouse_code.strip():
        warehouse = api_client.get(f'/api/Warehouses/code/{warehouse_code.strip()}')
        warehouse_id = warehouse.get('id')
        if not warehouse_id:
            raise RuntimeError(f'Warehouse code not found: {warehouse_code}')
        stock = api_client.get(f'/api/Stock/levels/part/{part_id}/warehouse/{warehouse_id}')
        data['warehouse'] = warehouse
        data['stock'] = stock
    else:
        stock = api_client.get(f'/api/Stock/levels/part/{part_id}')
        data['stock'] = stock

    try:
        data['compatibleVehicles'] = api_client.get(f'/api/Parts/{part_id}/compatible-vehicles')
    except Exception:
        pass

    return data


def get_customer_sales_context(
    customer_query: str,
    from_date: str = '',
    to_date: str = '',
    page_number: int = 1,
    page_size: int = 20,
) -> dict[str, Any]:
    customer = _resolve_customer(customer_query)
    customer_id = customer.get('id')
    if not customer_id:
        raise RuntimeError('Resolved customer does not have an ID')

    orders = api_client.get(f'/api/SalesOrder/customer/{customer_id}')
    payment_summary = api_client.get(f'/api/CustomerPayment/customer/{customer_id}/summary')
    account_summary = api_client.post(
        f'/api/customer-account-summary/{customer_id}',
        {
            'fromDate': from_date or None,
            'toDate': to_date or None,
            'pageNumber': page_number,
            'pageSize': page_size,
        },
    )

    return {
        'customer': customer,
        'orders': orders,
        'paymentSummary': payment_summary,
        'accountSummary': account_summary,
    }


def list_customer_payments(
    customer_query: str,
    search_query: str = '',
    status: str = '',
    from_date: str = '',
    to_date: str = '',
    page_number: int = 1,
    page_size: int = 20,
) -> dict[str, Any]:
    customer = _resolve_customer(customer_query)
    customer_id = customer.get('id')
    if not customer_id:
        raise RuntimeError('Resolved customer does not have an ID')

    payload = {
        'search': search_query,
        'customerId': customer_id,
        'status': status or None,
        'fromDate': from_date or None,
        'toDate': to_date or None,
        'pageNumber': page_number,
        'pageSize': page_size,
    }
    payments = api_client.post('/api/CustomerPayment/list', payload)

    try:
        advances = api_client.get(f'/api/CustomerPayment/customer/{customer_id}/available-advances')
    except Exception:
        advances = []

    return {'customer': customer, 'payments': payments, 'availableAdvances': advances}


def list_sales_orders(
    search_query: str = '',
    status: str = '',
    from_date: str = '',
    to_date: str = '',
    page_number: int = 1,
    page_size: int = 20,
) -> dict[str, Any]:
    payload = {
        'status': status or None,
        'fromDate': from_date or None,
        'toDate': to_date or None,
        'pageNumber': page_number,
        'pageSize': page_size,
        'search': search_query,
    }
    data = api_client.post('/api/SalesOrder/list', payload)
    return _normalize_search_json(data)


def get_sales_order_by_number(so_number: str) -> dict[str, Any]:
    if not so_number.strip():
        raise RuntimeError('Sales order number is required')
    return api_client.get(f'/api/SalesOrder/number/{so_number.strip()}')


def get_customer_due_alert(customer_query: str) -> dict[str, Any]:
    customer = _resolve_customer(customer_query)
    customer_id = customer.get('id')
    if not customer_id:
        raise RuntimeError('Resolved customer does not have an ID')

    payment_summary = api_client.get(f'/api/CustomerPayment/customer/{customer_id}/summary')
    account_summary = api_client.post(
        f'/api/customer-account-summary/{customer_id}',
        {
            'fromDate': None,
            'toDate': None,
            'pageNumber': 1,
            'pageSize': 20,
        },
    )

    overdue_all = api_client.get('/api/SalesOrder/overdue')
    overdue_items = _extract_items(overdue_all)
    overdue_for_customer = [
        item
        for item in overdue_items
        if str(item.get('customerId') or '').lower() == str(customer_id).lower()
    ]

    return {
        'customer': customer,
        'paymentSummary': payment_summary,
        'accountSummary': account_summary,
        'overdueOrders': overdue_for_customer,
        'overdueOrderCount': len(overdue_for_customer),
    }


def get_warehouse_location_and_stock(
    warehouse_code: str,
    include_low_stock: bool = True,
    page_number: int = 1,
    page_size: int = 50,
) -> dict[str, Any]:
    warehouse = api_client.get(f'/api/Warehouses/code/{warehouse_code}')
    warehouse_id = warehouse.get('id')
    if not warehouse_id:
        raise RuntimeError(f'Warehouse code not found: {warehouse_code}')

    stock = api_client.get(f'/api/Stock/levels/warehouse/{warehouse_id}')
    response: dict[str, Any] = {'warehouse': warehouse, 'stock': stock}

    if include_low_stock:
        low_stock = api_client.get('/api/Stock/levels/low-stock')
        low_items = _extract_items(low_stock)
        scoped = [
            item
            for item in low_items
            if str(item.get('warehouseId') or '').lower() == str(warehouse_id).lower()
        ]
        response['lowStockInWarehouse'] = scoped

    response['paging'] = {'pageNumber': page_number, 'pageSize': page_size}
    return response


def record_payment(
    customer_id: str,
    amount: float,
    payment_method: str = 'CASH',
    transaction_number: str = '',
    reference_number: str = '',
) -> dict[str, Any]:
    resolved_customer_id = _clean(customer_id)
    if not resolved_customer_id:
        raise RuntimeError('Customer ID is required to record payment')

    # Validate customer first so API calls fail early with clear message.
    customer = api_client.get(f'/api/Customer/{resolved_customer_id}')
    if not _clean(customer.get('id')):
        raise RuntimeError('Customer not found for payment')

    if amount <= 0:
        raise RuntimeError('Amount must be greater than 0')

    generated_txn = f"TXN-{__import__('datetime').datetime.utcnow().strftime('%Y%m%d%H%M%S')}-{str(uuid.uuid4())[:8]}"

    payload = {
        'customerId': resolved_customer_id,
        'amount': amount,
        'paymentMethod': payment_method,
        'transactionNumber': _clean(transaction_number) or generated_txn,
        'referenceNumber': reference_number,
        'paymentDate': __import__('datetime').datetime.utcnow().isoformat() + 'Z',
    }
    return api_client.post('/api/CustomerPayment', payload)


def get_session_state(session_id: str) -> dict[str, Any]:
    session = session_store.get_or_create(session_id, '')
    return session.model_dump()


def build_tools() -> list[StructuredTool]:
    return [
        StructuredTool.from_function(
            search_products,
            description='Search parts catalog by text with pagination.',
        ),
        StructuredTool.from_function(
            list_warehouses,
            description='List all warehouses. Call this when user asks to see all warehouses.',
        ),
        StructuredTool.from_function(
            get_warehouse_stock,
            description='Get stock levels, low-stock alerts, or warehouse-specific stock.',
        ),
        StructuredTool.from_function(
            search_customer,
            description='Find customers by name, phone, or code. Use empty query to list all customers.',
        ),
        StructuredTool.from_function(
            create_customer,
            description='Create a new customer with generated customer code.',
        ),
        StructuredTool.from_function(
            select_customer_for_session,
            description='Set selected customer context for an active chat session.',
        ),
        StructuredTool.from_function(
            add_to_cart,
            description='Add a part item to the session cart.',
        ),
        StructuredTool.from_function(
            set_active_warehouse,
            description='Set the active warehouse code for the session.',
        ),
        StructuredTool.from_function(
            create_sale,
            description='Create a sales order from session cart items for a customer.',
        ),
        StructuredTool.from_function(
            get_customer_payment_summary,
            description='Fetch payment summary and due information for a customer.',
        ),
        StructuredTool.from_function(
            get_customer_recent_memory,
            description='Get recent role-scoped memory notes for a customer code within current session.',
        ),
        StructuredTool.from_function(
            get_product_pricing_and_stock,
            description='Get product details with price plus stock data, optionally scoped to a warehouse code.',
        ),
        StructuredTool.from_function(
            get_customer_sales_context,
            description='Get customer profile, sales orders, payment summary, and account summary in one call.',
        ),
        StructuredTool.from_function(
            list_customer_payments,
            description='List a customer payment transactions with optional status/date filters and available advances.',
        ),
        StructuredTool.from_function(
            list_sales_orders,
            description='Search/list sales orders using paging with optional search text, status, and date-range filters.',
        ),
        StructuredTool.from_function(
            get_sales_order_by_number,
            description='Get a sales order directly by sales order number.',
        ),
        StructuredTool.from_function(
            get_customer_due_alert,
            description='Get customer due/overdue context from payment summary, account summary, and overdue sales orders.',
        ),
        StructuredTool.from_function(
            get_warehouse_location_and_stock,
            description='Get warehouse details (including location fields) and stock levels, with optional low-stock subset.',
        ),
        StructuredTool.from_function(
            record_payment,
            description='Record a customer payment transaction.',
        ),
        StructuredTool.from_function(
            get_session_state,
            description='Return the current AI session state including cart and customer context.',
        ),
    ]
