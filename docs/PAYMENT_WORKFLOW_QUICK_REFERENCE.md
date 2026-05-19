# Payment Workflow - Quick Reference Guide

## 🚀 What Changed

### Before
- All payments auto-confirmed regardless of method
- No differentiation between instant and deferred payments
- Limited payment management capabilities

### After (Enterprise Workflow)
- ✅ Smart auto-confirmation based on payment method
- ✅ Manual verification for risky payment methods
- ✅ Status-based action buttons
- ✅ Advanced filtering and reconciliation

---

## 📋 Payment Methods & Behavior

### Auto-Confirmed (Instant)
| Method | Behavior | When Balance Updates |
|--------|----------|---------------------|
| CASH | ✅ Auto-confirm | Immediately |
| UPI | ✅ Auto-confirm | Immediately |
| CARD | ✅ Auto-confirm | Immediately |
| CREDIT_CARD | ✅ Auto-confirm | Immediately |
| DEBIT_CARD | ✅ Auto-confirm | Immediately |

### Manual Confirmation Required
| Method | Behavior | When Balance Updates |
|--------|----------|---------------------|
| CHEQUE | ❌ Stay PENDING | After manual confirm |
| BANK_TRANSFER | ❌ Stay PENDING | After manual confirm |
| NEFT | ❌ Stay PENDING | After manual confirm |
| RTGS | ❌ Stay PENDING | After manual confirm |

---

## 🎯 Common Tasks

### 1. Record Cash Payment
```
1. Navigate to: Sales → Customer Payments
2. Click: "New Payment"
3. Fill form:
   - Customer: Select customer
   - Amount: Enter amount
   - Payment Method: CASH
   - Invoice: (optional) Select invoice
4. Click: "Submit"

Result: ✅ Auto-confirmed, balance updated immediately
```

### 2. Record Cheque Payment
```
1. Navigate to: Sales → Customer Payments
2. Click: "New Payment"
3. Fill form:
   - Customer: Select customer
   - Amount: Enter amount
   - Payment Method: CHEQUE
   - Transaction Number: Cheque number
   - Notes: Add clearing date estimate
4. Click: "Submit"

Result: ⏳ Stays PENDING, awaiting confirmation
```

### 3. Confirm Pending Payment
```
1. Navigate to: Sales → Customer Payments
2. Filter by Status: "Pending"
3. Locate payment
4. Click: Green "✓ Confirm" button
5. Confirm action

Result: ✅ Payment confirmed, balance updated
```

### 4. Reconcile Completed Payment
```
1. Navigate to: Sales → Customer Payments
2. Filter by Status: "Completed"
3. Locate payment (Reconciled: No)
4. Click: Blue "☑ Reconcile" button
5. Confirm action

Result: ✅ Payment marked as reconciled
```

### 5. Refund Payment
```
1. Navigate to: Sales → Customer Payments
2. Find completed, non-reconciled payment
3. Click: Orange "↺ Refund" button
4. Confirm refund

Result: ✅ Payment refunded, balance reversed
```

---

## 🎨 Payment List Features

### Filter Dropdown
```
[Filter by Status ▼]
├─ All Payments
├─ Pending ⚠️ (Action Required)
├─ Processing
├─ Completed ✅
├─ Failed ❌
├─ Cancelled
└─ Refunded
```

### Action Buttons by Status

**PENDING:**
- ✅ Confirm (Green)
- ❌ Cancel (Red)

**COMPLETED (Not Reconciled):**
- ☑ Reconcile (Blue)
- ↺ Refund (Orange)

**ALL:**
- ⋮ More Menu (Context menu)

---

## ⚡ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl + N | New Payment |
| F5 | Refresh List |

---

## 🔍 Status Badge Colors

| Status | Color | Meaning |
|--------|-------|---------|
| PENDING | 🟨 Yellow | Awaiting action |
| PROCESSING | 🔵 Blue | In progress |
| COMPLETED | 🟢 Green | Success |
| FAILED | 🔴 Red | Error |
| CANCELLED | 🔴 Red | Cancelled |
| REFUNDED | ⚫ Gray | Refunded |

---

## 💡 Best Practices

### ✅ DO
- Record cash payments immediately after receiving money
- Wait for bank clearance before confirming cheques
- Reconcile payments daily/weekly
- Add transaction numbers for all non-cash payments
- Use notes field for additional context

### ❌ DON'T
- Confirm cheque payments before clearance
- Modify reconciled payments
- Delete completed payments
- Record payments without verifying receipt

---

## 🐛 Troubleshooting

### Payment Not Auto-Confirming

**Check:**
1. Payment method spelling (must be CASH, UPI, or CARD exactly)
2. Browser console for errors
3. Network tab for failed API calls

**Solution:**
Manually confirm from payment list using "✓ Confirm" button

### Customer Balance Not Updating

**Likely Cause:**
Payment is still PENDING

**Solution:**
1. Go to Customer Payments list
2. Filter by "Pending"
3. Confirm the payment

### Cannot Find Pending Payments

**Solution:**
1. Click filter dropdown
2. Select "Pending"
3. All pending payments will appear

---

## 📊 Workflow Visual Summary

```
┌─────────────────┐
│ Create Payment  │
└────────┬────────┘
         │
         ▼
  ┌─────────────┐
  │ Check Method│
  └──────┬──────┘
         │
    ┌────┴────┐
    │         │
┌───▼──┐  ┌──▼────┐
│ CASH │  │CHEQUE │
│ UPI  │  │BANK   │
│ CARD │  │TRANSFER│
└───┬──┘  └──┬────┘
    │        │
    │    ┌───▼────┐
    │    │PENDING │
    │    │ Status │
    │    └───┬────┘
    │        │
    │    ┌───▼────────┐
    │    │Manual      │
    │    │Confirmation│
    │    └───┬────────┘
    │        │
    └────┬───┘
         │
    ┌────▼─────┐
    │COMPLETED │
    │  Status  │
    └────┬─────┘
         │
    ┌────▼──────┐
    │ Balance   │
    │ Updated   │
    └───────────┘
```

---

## 📞 Support

For issues or questions:
1. Check this quick reference
2. Review full documentation: [ENTERPRISE_PAYMENT_WORKFLOW.md](./ENTERPRISE_PAYMENT_WORKFLOW.md)
3. Check browser console for errors
4. Review server logs

---

## 🎓 Training Checklist

New users should practice:
- [ ] Create cash payment (auto-confirm)
- [ ] Create cheque payment (manual confirm)
- [ ] Confirm pending payment
- [ ] Reconcile completed payment
- [ ] Filter payments by status
- [ ] Use context menu for actions
- [ ] Process a refund

---

**Last Updated:** 2025-12-23
**Version:** 1.0 - Enterprise Payment Workflow
