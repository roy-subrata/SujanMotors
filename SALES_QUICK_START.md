# Sales Feature - Quick Start Guide

## 🚀 Get Started in 3 Steps

### Step 1: Start the Backend
```bash
cd src/AutoPartShop.AppHost
dotnet run
```
API will run on: `http://localhost:5292`

### Step 2: Start the Frontend
```bash
cd src/AutoPartShop.WebApp
npm install  # First time only
npm start
```
App will run on: `http://localhost:4200`

### Step 3: Access Sales Features
Navigate to: **`http://localhost:4200/sales/sales-orders`**

---

## 📍 Available Routes

| Feature | URL | Description |
|---------|-----|-------------|
| **Sales Orders** | `/sales/sales-orders` | Manage customer orders |
| **Create Order** | `/sales/sales-orders/create` | Create new sales order |
| **Invoices** | `/sales/invoices` | Invoice management |
| **Sales Returns** | `/sales/sales-returns` | Handle returns |
| **Payments** | `/sales/customer-payments` | Track payments |
| **Customers** | `/sales/customers` | Customer management |

---

## 🎯 Quick Workflow

### Create Your First Sales Order

1. Go to **Sales Orders** → Click **"+ Create Sales Order"**

2. Fill in customer info:
   ```
   Customer Name: John Doe
   Email: john@example.com
   Phone: +1-555-0123
   City: New York
   Delivery Date: [Select a date]
   ```

3. Add line items:
   - Click **"+ Add Line"**
   - Enter: Part ID, Quantity, Unit Price
   - See real-time total calculation

4. Click **"Create Order"**

5. Success! Your order is in **DRAFT** status

### Confirm an Order

1. Find your order in the list
2. Click **"Confirm"** button
3. Order status → **CONFIRMED**
4. Ready for fulfillment!

---

## 🔍 Features at a Glance

### Sales Orders ✅
- ✅ Create, edit, view, delete
- ✅ Multi-line items
- ✅ Real-time calculations
- ✅ Status workflow
- ✅ Search & filter
- ✅ Pagination

### Invoices ✅
- ✅ Generate from orders
- ✅ Issue invoices
- ✅ Record payments
- ✅ Track outstanding amounts

### Customers ✅
- ✅ Full CRUD operations
- ✅ Credit limit tracking
- ✅ Status management
- ✅ Search functionality

### Payments ✅
- ✅ Record customer payments
- ✅ Settlement tracking
- ✅ Payment reconciliation

### Returns ✅
- ✅ Create return requests
- ✅ Approval workflow
- ✅ Refund management

---

## 🎨 UI Highlights

- **Modern Design** - Tailwind CSS styling
- **Responsive** - Works on all devices
- **Color-Coded Status** - Visual status indicators
- **Real-Time Validation** - Form error handling
- **Professional Tables** - Sortable, searchable, paginated

---

## 📊 API Endpoints

### Sales Orders
```
GET    /api/salesorder              # List all
GET    /api/salesorder/list         # Paginated
POST   /api/salesorder              # Create
PUT    /api/salesorder/{id}         # Update
PATCH  /api/salesorder/{id}/confirm # Confirm
DELETE /api/salesorder/{id}         # Delete
```

### Customers
```
GET    /api/customer              # List all
GET    /api/customer/list         # Paginated
POST   /api/customer              # Create
PUT    /api/customer/{id}         # Update
PATCH  /api/customer/{id}/activate # Status changes
```

### Payments
```
GET  /api/customerpayment/{id}                   # Get payment
GET  /api/customerpayment/customer/{customerId}  # By customer
POST /api/customerpayment                        # Record payment
```

Full API documentation: `http://localhost:5292/docs` (Swagger)

---

## 🛠️ Troubleshooting

### Backend won't start?
```bash
# Clean and rebuild
dotnet clean
dotnet build
dotnet run
```

### Frontend errors?
```bash
# Clear node modules and reinstall
rm -rf node_modules package-lock.json
npm install
npm start
```

### CORS errors?
- Backend already configured for `http://localhost:4200`
- Check API URL in service files matches backend port

---

## 📚 Next Steps

1. **Explore the UI** - Try creating orders, customers
2. **Test Workflows** - Create → Confirm → Invoice
3. **Customize** - Add your branding, modify forms
4. **Extend** - Add more features as needed

See **[SALES_FEATURE_IMPLEMENTATION.md](SALES_FEATURE_IMPLEMENTATION.md)** for complete documentation.

---

## 💡 Pro Tips

- Use **search** to quickly find orders/customers
- **Filter by status** to see active vs completed orders
- **Pagination** handles large datasets efficiently
- **Confirm orders** to move them through the workflow
- Check **Swagger docs** for complete API reference

---

## ✨ You're All Set!

Your enterprise-grade sales management system is ready to use. Start creating orders, managing customers, and tracking payments!

**Happy Selling! 🎉**
