/**
 * Frontend status union types mirroring the backend's status enums under
 * `src/AutoPartShop.Domain/Enums/` (and `Enums/HR/`).
 *
 * The wire format is unchanged by the backend's `refactor/status-enums` work: every enum
 * member serializes to exactly the historical string literal (global `JsonStringEnumConverter`,
 * no naming policy). These unions exist purely so the Angular app can stop treating status
 * fields as untyped `string` — the *values* below must stay byte-for-byte identical to the
 * corresponding C# enum member names. Do not paraphrase, re-case, or reorder them; when the
 * backend enum changes, update the matching union here.
 *
 * One union per backend enum, named identically to the C# enum type.
 */

/** `AutoPartShop.Domain.Enums.SalesOrderStatus` */
export type SalesOrderStatus =
  | 'PENDING'
  | 'DRAFT'
  | 'CONFIRMED'
  | 'READY_FOR_DELIVERY'
  | 'PAID'
  | 'PACKED'
  | 'PARTIALLY_SHIPPED'
  | 'SHIPPED'
  | 'DELIVERED'
  | 'COMPLETED'
  | 'CANCELLED'
  | 'RETURNED';

/** `AutoPartShop.Domain.Enums.SalesOrderPaymentStatus` */
export type SalesOrderPaymentStatus = 'PENDING' | 'PARTIAL' | 'PAID';

/** `AutoPartShop.Domain.Enums.QuotationStatus` */
export type QuotationStatus = 'DRAFT' | 'SENT' | 'ACCEPTED' | 'REJECTED' | 'EXPIRED' | 'CONVERTED';

/** `AutoPartShop.Domain.Enums.InvoiceStatus` */
export type InvoiceStatus =
  | 'DRAFT'
  | 'ISSUED'
  | 'DUE'
  | 'PAID'
  | 'PARTIALLY_PAID'
  | 'OVERDUE'
  | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.ProformaInvoiceStatus` */
export type ProformaInvoiceStatus = 'ISSUED' | 'EXPIRED' | 'SUPERSEDED';

/** `AutoPartShop.Domain.Enums.SalesReturnStatus` */
export type SalesReturnStatus = 'PENDING' | 'APPROVED' | 'RECEIVED' | 'REJECTED' | 'PROCESSED';

/** `AutoPartShop.Domain.Enums.CustomerPaymentStatus` */
export type CustomerPaymentStatus =
  | 'PENDING'
  | 'PROCESSING'
  | 'COMPLETED'
  | 'FAILED'
  | 'REFUNDED'
  | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.CreditNoteStatus` (supplier credit note) */
export type CreditNoteStatus = 'AVAILABLE' | 'PARTIALLY_USED' | 'FULLY_USED' | 'EXPIRED' | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.CustomerCreditNoteStatus` */
export type CustomerCreditNoteStatus = 'AVAILABLE' | 'PARTIALLY_USED' | 'FULLY_USED' | 'EXPIRED' | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.CustomerDebitNoteStatus` */
export type CustomerDebitNoteStatus = 'ISSUED' | 'SETTLED' | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.CustomerStatus` */
export type CustomerStatus = 'ACTIVE' | 'INACTIVE' | 'SUSPENDED' | 'BLACKLISTED';

/** `AutoPartShop.Domain.Enums.ChallanStatus` */
export type ChallanStatus = 'DRAFT' | 'ISSUED' | 'DELIVERED';

/** `AutoPartShop.Domain.Enums.TillSessionStatus` */
export type TillSessionStatus = 'OPEN' | 'CLOSED';

/** `AutoPartShop.Domain.Enums.TechnicianStatus` */
export type TechnicianStatus = 'ACTIVE' | 'INACTIVE';

/** `AutoPartShop.Domain.Enums.PurchaseOrderStatus` */
export type PurchaseOrderStatus =
  | 'DRAFT'
  | 'SUBMITTED'
  | 'CONFIRMED'
  | 'PARTIAL'
  | 'DELIVERED'
  | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.PurchaseOrderPaymentStatus` */
export type PurchaseOrderPaymentStatus = 'PENDING' | 'PARTIAL' | 'PAID';

/** `AutoPartShop.Domain.Enums.GoodsReceiptStatus` */
export type GoodsReceiptStatus = 'PENDING' | 'VERIFIED' | 'ACCEPTED' | 'REJECTED';

/** `AutoPartShop.Domain.Enums.PurchaseReturnStatus` */
export type PurchaseReturnStatus =
  | 'PENDING'
  | 'APPROVED'
  | 'RETURNED'
  | 'RECEIVED'
  | 'REJECTED'
  | 'CREDITED';

/** `AutoPartShop.Domain.Enums.PurchaseReturnSettlementStatus` */
export type PurchaseReturnSettlementStatus = 'PENDING' | 'SETTLED';

/** `AutoPartShop.Domain.Enums.SupplierPaymentStatus` */
export type SupplierPaymentStatus =
  | 'PENDING'
  | 'PROCESSING'
  | 'COMPLETED'
  | 'FAILED'
  | 'CANCELLED'
  | 'RETURNED';

/** `AutoPartShop.Domain.Enums.PaymentProviderStatus` */
export type PaymentProviderStatus = 'ACTIVE' | 'INACTIVE' | 'SUSPENDED';

/** `AutoPartShop.Domain.Enums.StockTakeStatus` */
export type StockTakeStatus = 'COUNTING' | 'REVIEW' | 'COMPLETED' | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.StockLotStatus` */
export type StockLotStatus = 'AVAILABLE' | 'DAMAGED' | 'QUARANTINE';

/** `AutoPartShop.Domain.Enums.WarrantyClaimStatus` */
export type WarrantyClaimStatus =
  | 'PENDING'
  | 'UNDER_REVIEW'
  | 'APPROVED'
  | 'REJECTED'
  | 'IN_PROGRESS'
  | 'COMPLETED'
  | 'CLOSED';

/** `AutoPartShop.Domain.Enums.WarrantyRegistrationStatus` */
export type WarrantyRegistrationStatus = 'ACTIVE' | 'EXPIRED' | 'CLAIMED' | 'VOID';

/** `AutoPartShop.Domain.Enums.NotificationLogStatus` */
export type NotificationLogStatus = 'PENDING' | 'SENT' | 'FAILED';

/** `AutoPartShop.Domain.Enums.NotificationChannel` (not a lifecycle status, but wire-identical union) */
export type NotificationChannel = 'SMS' | 'WHATSAPP' | 'EMAIL';

/**
 * `AutoPartShop.Domain.Enums.BackupRecordStatus`
 * NOTE: unlike the other enums, backend members here are PascalCase (not SCREAMING_SNAKE_CASE) —
 * that is intentional and matches the C# source exactly.
 */
export type BackupRecordStatus = 'Pending' | 'Running' | 'Succeeded' | 'UploadFailed' | 'Failed';

/** `AutoPartShop.Domain.Enums.HR.EmployeeStatus` */
export type EmployeeStatus = 'ACTIVE' | 'INACTIVE';

/** `AutoPartShop.Domain.Enums.HR.LeaveRequestStatus` */
export type LeaveRequestStatus = 'PENDING' | 'APPROVED' | 'REJECTED' | 'CANCELLED';

/** `AutoPartShop.Domain.Enums.HR.AttendanceStatus` */
export type AttendanceStatus = 'PRESENT' | 'LATE' | 'HALF_DAY' | 'ABSENT' | 'LEAVE' | 'HOLIDAY';

/** `AutoPartShop.Domain.Enums.HR.PayrollRunStatus` */
export type PayrollRunStatus = 'DRAFT' | 'APPROVED' | 'PAID';

/** `AutoPartShop.Domain.Enums.HR.SalaryAdvanceStatus` */
export type SalaryAdvanceStatus = 'REQUESTED' | 'OUTSTANDING' | 'SETTLED' | 'REJECTED';
