-- =====================================================
-- ADVANCE PAYMENT VERIFICATION QUERIES
-- Use these to verify correct accounting behavior
-- =====================================================

-- 1. CHECK CUSTOMER PAYMENT BREAKDOWN
-- Shows all payment types and their sources
SELECT
    c.FirstName + ' ' + c.LastName AS CustomerName,
    cp.TransactionNumber,
    cp.PaymentType,
    cp.Amount,
    cp.RemainingAmount,
    cp.Status,
    cp.SourceAdvancePaymentId,
    CASE
        WHEN cp.PaymentType = 1 THEN 'ADVANCE - Original'
        WHEN cp.SourceAdvancePaymentId IS NOT NULL THEN 'REGULAR - From Advance (DO NOT COUNT in TotalPaid)'
        ELSE 'REGULAR - New Payment (COUNT in TotalPaid)'
    END AS PaymentCategory,
    cp.PaymentDate,
    cp.InvoiceId
FROM CustomerPayments cp
JOIN Customers c ON c.Id = cp.CustomerId
WHERE c.FirstName + ' ' + c.LastName = 'Your Customer Name'  -- Replace with actual customer name
ORDER BY cp.PaymentDate DESC;

-- 2. VERIFY TOTAL PAID (Should NOT include payments from advance)
SELECT
    c.FirstName + ' ' + c.LastName AS CustomerName,

    -- WRONG CALCULATION (old way - includes double counting)
    SUM(CASE WHEN cp.Status = 'COMPLETED' THEN cp.Amount ELSE 0 END) AS TotalPaid_Wrong,

    -- CORRECT CALCULATION (new way - excludes advance-sourced payments)
    SUM(CASE
        WHEN cp.Status = 'COMPLETED' AND
             (cp.PaymentType = 1 OR cp.SourceAdvancePaymentId IS NULL)
        THEN cp.Amount
        ELSE 0
    END) AS TotalPaid_Correct,

    -- Difference (should be zero after fix)
    SUM(CASE WHEN cp.Status = 'COMPLETED' THEN cp.Amount ELSE 0 END) -
    SUM(CASE
        WHEN cp.Status = 'COMPLETED' AND
             (cp.PaymentType = 1 OR cp.SourceAdvancePaymentId IS NULL)
        THEN cp.Amount
        ELSE 0
    END) AS DoubleCountedAmount

FROM Customers c
LEFT JOIN CustomerPayments cp ON cp.CustomerId = c.Id
WHERE c.FirstName + ' ' + c.LastName = 'Your Customer Name'  -- Replace with actual customer name
GROUP BY c.Id, c.FirstName, c.LastName;

-- 3. VERIFY ADVANCE BALANCE (Should use RemainingAmount, not Amount)
SELECT
    c.FirstName + ' ' + c.LastName AS CustomerName,

    -- WRONG CALCULATION (old way - uses Amount)
    SUM(CASE
        WHEN cp.PaymentType = 1 AND cp.Status = 'COMPLETED'
        THEN cp.Amount
        ELSE 0
    END) AS AdvanceBalance_Wrong,

    -- CORRECT CALCULATION (new way - uses RemainingAmount)
    SUM(CASE
        WHEN cp.PaymentType = 1 AND cp.Status = 'COMPLETED' AND cp.RemainingAmount > 0
        THEN cp.RemainingAmount
        ELSE 0
    END) AS AdvanceBalance_Correct

FROM Customers c
LEFT JOIN CustomerPayments cp ON cp.CustomerId = c.Id
WHERE c.FirstName + ' ' + c.LastName = 'Your Customer Name'  -- Replace with actual customer name
GROUP BY c.Id, c.FirstName, c.LastName;

-- 4. INVOICE PAYMENT STATUS CHECK
-- Verify invoice status is correct after advance payment
SELECT
    i.InvoiceNumber,
    i.GrandTotal,
    i.Status AS CurrentStatus,

    -- Total amount paid to this invoice (includes advance-sourced payments)
    ISNULL(SUM(cp.Amount), 0) AS AmountPaid,

    i.GrandTotal - ISNULL(SUM(cp.Amount), 0) AS OutstandingAmount,

    -- What status SHOULD be
    CASE
        WHEN ISNULL(SUM(cp.Amount), 0) = 0 THEN 'ISSUED'
        WHEN ISNULL(SUM(cp.Amount), 0) >= i.GrandTotal THEN 'PAID'
        ELSE 'PARTIALLY_PAID'
    END AS ExpectedStatus,

    -- Count of payments from advance
    SUM(CASE WHEN cp.SourceAdvancePaymentId IS NOT NULL THEN 1 ELSE 0 END) AS PaymentsFromAdvance

FROM Invoices i
LEFT JOIN CustomerPayments cp ON cp.InvoiceId = i.Id AND cp.Status = 'COMPLETED'
WHERE i.InvoiceNumber = 'INV-XXXXX'  -- Replace with actual invoice number
GROUP BY i.Id, i.InvoiceNumber, i.GrandTotal, i.Status;

-- 5. FIND ALL CUSTOMERS WITH ADVANCE BALANCE
SELECT
    c.CustomerCode,
    c.FirstName + ' ' + c.LastName AS CustomerName,

    -- Available advance credit
    SUM(CASE
        WHEN cp.PaymentType = 1 AND cp.Status = 'COMPLETED' AND cp.RemainingAmount > 0
        THEN cp.RemainingAmount
        ELSE 0
    END) AS AvailableAdvanceCredit,

    -- Total new money received
    SUM(CASE
        WHEN cp.Status = 'COMPLETED' AND
             (cp.PaymentType = 1 OR cp.SourceAdvancePaymentId IS NULL)
        THEN cp.Amount
        ELSE 0
    END) AS TotalPaid,

    c.CurrentBalance AS OutstandingBalance

FROM Customers c
LEFT JOIN CustomerPayments cp ON cp.CustomerId = c.Id
GROUP BY c.Id, c.CustomerCode, c.FirstName, c.LastName, c.CurrentBalance
HAVING SUM(CASE
    WHEN cp.PaymentType = 1 AND cp.Status = 'COMPLETED' AND cp.RemainingAmount > 0
    THEN cp.RemainingAmount
    ELSE 0
END) > 0
ORDER BY AvailableAdvanceCredit DESC;

-- 6. SUPPLIER PAYMENT BREAKDOWN (Same logic for suppliers)
SELECT
    s.Name AS SupplierName,
    sp.TransactionNumber,
    sp.PaymentType,
    sp.Amount,
    sp.RemainingAmount,
    sp.Status,
    sp.SourceAdvancePaymentId,
    CASE
        WHEN sp.PaymentType = 1 THEN 'ADVANCE - Original'
        WHEN sp.SourceAdvancePaymentId IS NOT NULL THEN 'REGULAR - From Advance (DO NOT COUNT in TotalPaid)'
        ELSE 'REGULAR - New Payment (COUNT in TotalPaid)'
    END AS PaymentCategory,
    sp.PaymentDate
FROM SupplierPayments sp
JOIN Suppliers s ON s.Id = sp.SupplierId
WHERE s.Name = 'Your Supplier Name'  -- Replace with actual supplier name
ORDER BY sp.PaymentDate DESC;

-- 7. DETAILED TRANSACTION TRACE (For debugging specific sale)
-- Shows complete flow for a specific invoice
SELECT
    'Invoice Created' AS EventType,
    i.InvoiceNumber AS Reference,
    i.InvoiceDate AS EventDate,
    i.GrandTotal AS Amount,
    NULL AS PaymentType,
    NULL AS RemainingAmount
FROM Invoices i
WHERE i.InvoiceNumber = 'INV-XXXXX'  -- Replace with actual invoice number

UNION ALL

SELECT
    CASE
        WHEN cp.PaymentType = 1 THEN 'Advance Payment Received'
        WHEN cp.SourceAdvancePaymentId IS NOT NULL THEN 'Advance Applied to Invoice'
        ELSE 'Regular Payment Received'
    END AS EventType,
    cp.TransactionNumber AS Reference,
    cp.PaymentDate AS EventDate,
    cp.Amount AS Amount,
    CASE cp.PaymentType WHEN 1 THEN 'ADVANCE' ELSE 'REGULAR' END AS PaymentType,
    cp.RemainingAmount
FROM CustomerPayments cp
WHERE cp.InvoiceId = (SELECT Id FROM Invoices WHERE InvoiceNumber = 'INV-XXXXX')  -- Replace
   OR cp.Id IN (
       SELECT SourceAdvancePaymentId
       FROM CustomerPayments
       WHERE InvoiceId = (SELECT Id FROM Invoices WHERE InvoiceNumber = 'INV-XXXXX')
   )
ORDER BY EventDate;

-- 8. QUICK VALIDATION - Are there any double-counted payments?
-- This should return 0 rows after fix is applied
SELECT
    c.CustomerCode,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    COUNT(*) AS NumberOfAdvanceSourcedPayments,
    SUM(cp.Amount) AS TotalDoubleCountedAmount
FROM Customers c
JOIN CustomerPayments cp ON cp.CustomerId = c.Id
WHERE cp.Status = 'COMPLETED'
  AND cp.SourceAdvancePaymentId IS NOT NULL  -- These are payments from advance
GROUP BY c.Id, c.CustomerCode, c.FirstName, c.LastName
HAVING COUNT(*) > 0
ORDER BY TotalDoubleCountedAmount DESC;
