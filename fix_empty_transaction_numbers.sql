-- Fix existing SupplierPayments with empty TransactionNumber
-- This script updates all records with empty TransactionNumber to have unique values

UPDATE SupplierPayments
SET TransactionNumber = 'TXN-' + CONVERT(VARCHAR(20), GETDATE(), 112) + '-' +
                        CAST(NEWID() AS VARCHAR(36))
WHERE TransactionNumber = '' OR TransactionNumber IS NULL;

-- Verify the update
SELECT COUNT(*) AS EmptyTransactionCount
FROM SupplierPayments
WHERE TransactionNumber = '' OR TransactionNumber IS NULL;
