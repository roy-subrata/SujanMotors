# Claude Prompt: Unified Checkout System (E‑commerce + POS + COD)

You are an assistant helping implement a **checkout system** that works for both online e‑commerce and in‑store POS.

## 🎯 Goal
Design a unified checkout flow where:
- Online customers can checkout with **full payment** or **cash on delivery (COD)**.
- POS customers can checkout with **full payment**, or if authorized by a salesperson, **due/credit payment**.
- POS customers may also receive **discounts** applied by the salesperson.
- System must handle **order status** and **invoice status** separately.
- Checkout requires **login authentication** (customer login for online checkout, salesperson login for POS checkout).

---

## 🧩 Flow

### 1. Detect Channel
- Online → self‑checkout (customer login required).
- POS → salesperson-assisted checkout (salesperson login required).

### 2. Payment Options
- Online → Full Payment, COD.
- POS → Full Payment, Due/Credit (salesperson required), Discounts.

### 3. Order vs. Invoice Status
- **Order Status** → lifecycle of product (Pending → Confirmed → Shipped → Completed → Cancelled).
- **Invoice Status** → lifecycle of payment (Draft → Issued → Paid → Unpaid → Due → Partially Paid → Cancelled).

### 4. Sequence Rules
- **Online Checkout**
  - Customer must log in before checkout.
  - Payment step comes first.
  - If payment succeeds → Order Confirmed → Invoice Issued → Paid.
  - If COD → Order Confirmed → Invoice Issued → Unpaid (until delivery).
- **POS Checkout**
  - Salesperson must log in before creating order.
  - Order Confirmed first (salesperson creates order).
  - Then payment step:
    - Full → Invoice Issued → Paid.
    - Due/Credit → Invoice Issued → Due.
    - Partial → Invoice Issued → Partially Paid.
    - Discount → applied before invoice calculation.
  - Salesperson ID logged for accountability.

### 5. COD Flow
- Customer selects COD online.
- Order Confirmed → Invoice Issued → Unpaid.
- At delivery, cash collected → Invoice updated to Paid.
- Notification sent at both stages:
  - At checkout → “Order confirmed, payment due on delivery.”
  - At delivery → “Payment received, invoice settled.”

### 6. Safeguards
- Online checkout cannot access due/credit/discount options.
- POS checkout blocks due/credit/discount unless salesperson confirms.
- Audit trail for all due/credit/discount transactions.

### 7. Business Rules
- If customer does checkout on their own → **customer login required** before checkout and order confirmation.
- If salesperson does checkout on behalf of customer → **salesperson login required** before proceeding with customer order and sale.

---

## 📦 Output
Generate backend logic and API endpoints that:
- Handle channel detection.
- Enforce login requirement (customer login for online, salesperson login for POS).
- Enforce payment rules.
- Apply discounts in POS checkout.
- Record orders consistently.
- Update inventory in real time.
- Track outstanding balances for POS due/credit cases.
- Maintain audit logs for salesperson actions.
- Support COD flow with invoice status transition (Issued → Paid at delivery).
- Send notifications (email/SMS) after order confirmation and after COD payment collection.
