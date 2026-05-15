# Quick Sale - Required API Endpoints

This document lists all the API endpoints that the Quick Sale component expects to work properly.

## 📋 **Required Backend Endpoints**

### 1. **Customer Endpoints**

#### Get Recent Customers
```http
GET /api/customer/recent?limit=50
```
**Response:**
```json
[
  {
    "id": "string",
    "customerCode": "string",
    "firstName": "string",
    "lastName": "string",
    "fullName": "string",
    "phone": "string",
    "email": "string",
    "city": "string"
  }
]
```

#### Search Customer by Phone
```http
GET /api/customer/search-by-phone?phone={phoneNumber}
```
**Response:**
```json
{
  "id": "string",
  "customerCode": "string",
  "firstName": "string",
  "lastName": "string",
  "fullName": "string",
  "phone": "string",
  "email": "string",
  "city": "string",
  "creditLimit": 0,
  "availableCredit": 0
}
```

### 2. **Part/Inventory Endpoints**

#### Get Active Parts
```http
GET /api/parts/active
```
**Response:**
```json
[
  {
    "id": "string",
    "name": "string",
    "partNumber": "string",
    "sku": "string",
    "categoryName": "string",
    "unitName": "string",
    "costPrice": 0,
    "sellingPrice": 0,
    "minimumStock": 0,
    "isActive": true
  }
]
```

### 3. **Stock Check Endpoint**

#### Check Stock Availability
```http
POST /api/stock/check
```
**Request Body:**
```json
{
  "partId": "string",
  "quantity": 0
}
```
**Response:**
```json
{
  "partId": "string",
  "available": true,
  "stockAvailable": 100,
  "warehouseLocation": "Warehouse A - Rack 5",
  "supplierName": "Acme Parts Co.",
  "message": "Stock available"
}
```

### 4. **Technician Endpoints**

#### Get All Technicians
```http
GET /api/technician
```
**Response:**
```json
[
  {
    "id": "string",
    "technicianCode": "string",
    "name": "string",
    "phone": "string",
    "email": "string",
    "specialization": "string",
    "status": "ACTIVE"
  }
]
```

### 5. **Quick Sale (Sales Order) Endpoint**

#### Create Quick Sale
```http
POST /api/salesorder/quick-sale
```
**Request Body:**
```json
{
  "customerId": "string",
  "customerName": "John Doe",
  "customerPhone": "1234567890",
  "customerEmail": "john@example.com",
  "technicianId": "string",
  "technicianName": "Mike Johnson",
  "technicianNotes": "Customer recommended by Mike",
  "paymentResponsibility": "CUSTOMER",
  "purchaseOrderId": "string",
  "autoCreatePO": false,
  "items": [
    {
      "partId": "string",
      "partName": "Brake Pad",
      "partNumber": "BP-001",
      "sku": "SKU001",
      "quantity": 2,
      "unitPrice": 50.00,
      "discount": 5,
      "stockAvailable": 100
    }
  ],
  "payments": [
    {
      "method": "CASH",
      "amount": 95.00,
      "reference": "",
      "notes": ""
    }
  ],
  "subtotal": 95.00,
  "discountAmount": 5.00,
  "vatAmount": 14.25,
  "vatPercentage": 15,
  "grandTotal": 109.25,
  "paidAmount": 95.00,
  "dueAmount": 14.25,
  "notes": "Rush order"
}
```

**Response:**
```json
{
  "id": "string",
  "invoiceNumber": "INV-001",
  "salesOrderId": "string",
  "salesOrderNumber": "SO-001",
  "customerId": "string",
  "customerName": "John Doe",
  "technicianId": "string",
  "technicianName": "Mike Johnson",
  "paymentResponsibility": "CUSTOMER",
  "subtotal": 95.00,
  "discountAmount": 5.00,
  "vatAmount": 14.25,
  "grandTotal": 109.25,
  "paidAmount": 95.00,
  "dueAmount": 14.25,
  "status": "COMPLETED",
  "createdAt": "2025-12-10T10:30:00Z"
}
```

### 6. **Code Generation Endpoint**

#### Generate Invoice Number
```http
GET /api/code-generate/invoice
```
**Response:**
```json
{
  "code": "INV-2025-001"
}
```

## 🔧 **Implementation Notes**

### Payment Methods Enum
```csharp
public enum PaymentMethod
{
    CASH,
    MOBILE_BANKING,
    CARD,
    DUE,
    PART_PAY
}
```

### Payment Responsibility Enum
```csharp
public enum PaymentResponsibility
{
    CUSTOMER,
    TECHNICIAN_TEMPORARY
}
```

## 🎯 **Quick Sale Business Logic**

1. **Customer Validation**: Check if customer exists, validate credit limit if payment is due
2. **Stock Validation**: Verify all items have sufficient stock before processing
3. **Pricing**: Apply discounts, calculate VAT based on configuration
4. **Payment Processing**:
   - If `CUSTOMER`: Record payment against customer account
   - If `TECHNICIAN_TEMPORARY`: Create temporary advance record for technician
5. **Auto PO Creation**: If `autoCreatePO` is true, create a purchase order for restocking
6. **Transaction**: Create Sales Order + Invoice + Payment records in a single transaction
7. **Stock Adjustment**: Deduct sold quantities from stock levels
8. **Notifications**: Send invoice to customer (email/SMS if configured)

## 📊 **Data Flow**

```
Quick Sale Component
    ↓
1. Load: Customers, Parts, Technicians (on init)
    ↓
2. User adds items to cart
    ↓
3. Check stock availability (real-time)
    ↓
4. Select payment method & customer
    ↓
5. Submit Quick Sale
    ↓
Backend: Create SalesOrder + Invoice + Payments + Update Stock
    ↓
6. Return invoice number & confirmation
    ↓
7. Clear cart & show success
```

## ✅ **Current Implementation Status**

### Frontend (Angular) ✅
- [x] Quick Sale Component
- [x] Customer auto-fill by phone
- [x] Part search with autocomplete
- [x] Cart management with calculations
- [x] Technician selection
- [x] Payment methods support
- [x] Real-time calculations (subtotal, VAT, discount, due)
- [x] Auto-save draft functionality
- [x] Keyboard shortcuts (Ctrl+S, Ctrl+Enter, Ctrl+N)
- [x] Quick action toolbar
- [x] Responsive design

### Backend (C# API) ⚠️
- [ ] `/api/customer/recent` - Needs implementation
- [ ] `/api/customer/search-by-phone` - Needs implementation
- [ ] `/api/parts/active` - May need implementation
- [ ] `/api/stock/check` - Needs implementation
- [ ] `/api/salesorder/quick-sale` - **Main endpoint - needs implementation**
- [ ] `/api/code-generate/invoice` - May already exist
- [ ] `/api/technician` - Check if exists

## 🚀 **Next Steps**

1. Implement the backend API endpoints listed above
2. Test the Quick Sale flow end-to-end
3. Add print invoice functionality
4. Add customer payment history dialog
5. Add stock search dialog
6. Add real-time stock updates via SignalR (optional)
7. Add barcode scanner support (optional)

## 📝 **Notes**

- All monetary values should be in decimal with 2 decimal places
- Date/time should be in ISO 8601 format
- Status values should be uppercase enums
- API base URL: `http://localhost:5292/api`
