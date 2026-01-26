/**
 * Centralized Payment Methods Configuration
 * This is the single source of truth for all payment method types in the application.
 * Used for: Supplier Payments, Customer Payments, Payment Providers, Supplier Payment Accounts, Daily Expenses
 *
 * IMPORTANT: Keep this in sync with backend enum at:
 * - AutoPartShop.Domain/Enums/PaymentMethod.cs (if exists)
 * - Backend validation/accepted values
 */

export interface PaymentMethodOption {
    label: string;
    value: string;
    description?: string;
    icon?: string;
}

/**
 * All available payment methods in the system
 * Values should match backend enum/string values exactly
 */
export const PAYMENT_METHODS: PaymentMethodOption[] = [
    { label: 'Cash', value: 'CASH', description: 'Cash payment', icon: 'pi-money-bill' },
    { label: 'Check', value: 'CHECK', description: 'Payment by check', icon: 'pi-file-edit' },
    { label: 'Cheque', value: 'CHEQUE', description: 'Payment by cheque', icon: 'pi-file-edit' },
    { label: 'Bank Transfer', value: 'BANK_TRANSFER', description: 'Direct bank transfer', icon: 'pi-building' },
    { label: 'Mobile Banking', value: 'MOBILE_BANKING', description: 'Mobile wallet (bKash, Nagad, etc.)', icon: 'pi-mobile' },
    { label: 'Mobile Payment', value: 'MOBILE_PAYMENT', description: 'Mobile payment', icon: 'pi-mobile' },
    { label: 'Credit Card', value: 'CREDIT_CARD', description: 'Credit card payment', icon: 'pi-credit-card' },
    { label: 'Debit Card', value: 'DEBIT_CARD', description: 'Debit card payment', icon: 'pi-credit-card' },
    { label: 'Card', value: 'CARD', description: 'Card payment', icon: 'pi-credit-card' },
    { label: 'Online Payment', value: 'ONLINE_PAYMENT', description: 'Online payment gateway', icon: 'pi-globe' },
    { label: 'UPI', value: 'UPI', description: 'Unified Payments Interface', icon: 'pi-qrcode' },
    { label: 'NEFT', value: 'NEFT', description: 'National Electronic Funds Transfer', icon: 'pi-send' },
    { label: 'RTGS', value: 'RTGS', description: 'Real Time Gross Settlement', icon: 'pi-send' },
    { label: 'NEFT/RTGS', value: 'NEFT_RTGS', description: 'NEFT or RTGS transfer', icon: 'pi-send' },
    { label: 'IMPS', value: 'IMPS', description: 'Immediate Payment Service', icon: 'pi-bolt' },
    { label: 'Demand Draft', value: 'DEMAND_DRAFT', description: 'Demand draft payment', icon: 'pi-file' }
];

/**
 * Payment method values as a type for type safety
 */
export type PaymentMethodValue =
    | 'CASH'
    | 'CHECK'
    | 'CHEQUE'
    | 'BANK_TRANSFER'
    | 'MOBILE_BANKING'
    | 'MOBILE_PAYMENT'
    | 'CREDIT_CARD'
    | 'DEBIT_CARD'
    | 'CARD'
    | 'ONLINE_PAYMENT'
    | 'UPI'
    | 'NEFT'
    | 'RTGS'
    | 'NEFT_RTGS'
    | 'IMPS'
    | 'DEMAND_DRAFT';

/**
 * Helper function to get payment method label by value
 */
export function getPaymentMethodLabel(value: string): string {
    const method = PAYMENT_METHODS.find(m => m.value === value);
    return method?.label || value;
}

/**
 * Helper function to get payment method by value
 */
export function getPaymentMethod(value: string): PaymentMethodOption | undefined {
    return PAYMENT_METHODS.find(m => m.value === value);
}

/**
 * Helper function to get payment method icon by value
 */
export function getPaymentMethodIcon(value: string): string {
    const method = PAYMENT_METHODS.find(m => m.value === value);
    return method?.icon ? `pi ${method.icon}` : 'pi pi-money-bill';
}

/**
 * Payment types for categorizing payments (Regular vs Advance)
 */
export interface PaymentTypeOption {
    label: string;
    value: string;
    description: string;
}

export const PAYMENT_TYPES: PaymentTypeOption[] = [
    { label: 'Regular Payment', value: 'REGULAR', description: 'Payment for a specific order' },
    { label: 'Advance Payment', value: 'ADVANCE', description: 'Prepayment/deposit without a specific order' }
];

/**
 * Account types for Supplier Payment Accounts
 */
export const ACCOUNT_TYPES: PaymentMethodOption[] = [
    { label: 'Bank Transfer', value: 'BANK_TRANSFER', description: 'Bank account for transfers' },
    { label: 'Mobile Banking', value: 'MOBILE_BANKING', description: 'Mobile wallet account' },
    { label: 'Cash', value: 'CASH', description: 'Cash collection' },
    { label: 'Check', value: 'CHECK', description: 'Check payment' },
    { label: 'Other', value: 'OTHER', description: 'Other payment method' }
];

/**
 * Provider types for Payment Providers (your business accounts)
 */
export const PROVIDER_TYPES: PaymentMethodOption[] = [
    { label: 'Online Gateway', value: 'ONLINE_GATEWAY', description: 'Online payment gateway' },
    { label: 'Bank Transfer', value: 'BANK_TRANSFER', description: 'Business bank account' },
    { label: 'Mobile Banking', value: 'MOBILE_BANKING', description: 'Business mobile wallet' },
    { label: 'Cash', value: 'CASH', description: 'Cash drawer/register' },
    { label: 'Check', value: 'CHECK', description: 'Check payments' },
    { label: 'Crypto', value: 'CRYPTO', description: 'Cryptocurrency' },
    { label: 'Other', value: 'OTHER', description: 'Other payment method' }
];

/**
 * Mobile banking providers
 */
export const MOBILE_PROVIDERS: { label: string; value: string }[] = [
    { label: 'bKash', value: 'bKash' },
    { label: 'Nagad', value: 'Nagad' },
    { label: 'eZ Cash', value: 'eZ Cash' },
    { label: 'FriMi', value: 'FriMi' },
    { label: 'Upay', value: 'Upay' },
    { label: 'Rocket', value: 'Rocket' },
    { label: 'Other', value: 'Other' }
];

/**
 * Subset of payment methods for customer payments
 */
export const CUSTOMER_PAYMENT_METHODS: PaymentMethodOption[] = [
    { label: 'Cash', value: 'CASH', icon: 'pi-money-bill' },
    { label: 'UPI', value: 'UPI', icon: 'pi-mobile' },
    { label: 'Credit Card', value: 'CREDIT_CARD', icon: 'pi-credit-card' },
    { label: 'Debit Card', value: 'DEBIT_CARD', icon: 'pi-credit-card' },
    { label: 'Card', value: 'CARD', icon: 'pi-credit-card' },
    { label: 'Cheque', value: 'CHEQUE', icon: 'pi-file-edit' },
    { label: 'Bank Transfer', value: 'BANK_TRANSFER', icon: 'pi-building' },
    { label: 'NEFT', value: 'NEFT', icon: 'pi-building' },
    { label: 'RTGS', value: 'RTGS', icon: 'pi-building' },
    { label: 'Demand Draft', value: 'DEMAND_DRAFT', icon: 'pi-file' }
];

/**
 * Subset of payment methods for supplier payments
 */
export const SUPPLIER_PAYMENT_METHODS: PaymentMethodOption[] = [
    { label: 'Cash', value: 'CASH', description: 'Cash payment', icon: 'pi-money-bill' },
    { label: 'Check', value: 'CHECK', description: 'Payment by check', icon: 'pi-file-edit' },
    { label: 'Bank Transfer', value: 'BANK_TRANSFER', description: 'Direct bank transfer', icon: 'pi-building' },
    { label: 'Mobile Banking', value: 'MOBILE_BANKING', description: 'Mobile wallet (bKash, Nagad, etc.)', icon: 'pi-mobile' },
    { label: 'Credit Card', value: 'CREDIT_CARD', description: 'Credit card payment', icon: 'pi-credit-card' },
    { label: 'Debit Card', value: 'DEBIT_CARD', description: 'Debit card payment', icon: 'pi-credit-card' },
    { label: 'Online Payment', value: 'ONLINE_PAYMENT', description: 'Online payment gateway', icon: 'pi-globe' },
    { label: 'UPI', value: 'UPI', description: 'Unified Payments Interface', icon: 'pi-qrcode' },
    { label: 'NEFT/RTGS', value: 'NEFT_RTGS', description: 'NEFT or RTGS transfer', icon: 'pi-send' },
    { label: 'IMPS', value: 'IMPS', description: 'Immediate Payment Service', icon: 'pi-bolt' }
];

/**
 * Subset of payment methods for daily expenses
 */
export const EXPENSE_PAYMENT_METHODS: PaymentMethodOption[] = [
    { label: 'Cash', value: 'CASH' },
    { label: 'Bank Transfer', value: 'BANK_TRANSFER' },
    { label: 'Check', value: 'CHECK' },
    { label: 'Credit Card', value: 'CREDIT_CARD' },
    { label: 'Mobile Payment', value: 'MOBILE_PAYMENT' }
];
