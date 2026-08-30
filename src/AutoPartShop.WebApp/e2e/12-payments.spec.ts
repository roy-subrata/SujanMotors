import { test, expect } from '@playwright/test';
import { uniqueSuffix } from './utils/api';
import { pickDropdownOption, pickAutocompleteOption } from './utils/ui';

/**
 * Both payment types are recorded as "advance" so the flow doesn't need a specific
 * invoice/PO to already exist with an outstanding balance — a Payment Provider ("Cash")
 * is the only prerequisite, seeded fresh here since none may exist on a clean DB.
 */
const providerName = `E2E Test Cash ${uniqueSuffix()}`;
const supplierName = `E2E Payment Supplier ${uniqueSuffix()}`;

test.describe.serial('Customer & Supplier Payments', () => {
    test('0. seed a Cash payment provider', async ({ page }) => {
        await page.goto('/procurement/payment-providers/new');
        await page.getByPlaceholder(/e\.g\., Stripe, PayPal, BOC/i).fill(providerName);
        await pickDropdownOption(page, page.getByPlaceholder('Select Type'), 'Cash', { typeToFilter: true });
        await page.getByRole('button', { name: 'Save Provider' }).click();
        await expect(page).toHaveURL(/\/procurement\/payment-providers$/);
        // The list isn't sorted newest-first and paginates at 10 — with many providers
        // accumulated from repeated runs, the new one can be off the default first page.
        const providerSearchBox = page.getByPlaceholder('Search by provider name...');
        await providerSearchBox.fill(providerName);
        await providerSearchBox.press('Enter');
        await expect(page.getByText(providerName).first()).toBeVisible({ timeout: 10_000 });
    });

    test('1. record a customer advance payment', async ({ page }) => {
        await page.goto('/sales/customer-payments/new');

        const customerSearch = page.getByPlaceholder('Search customer...').last();
        // The option's accessible name is just the customer's first name ("Walk-in") —
        // the component's optionLabel="firstName" — even though the rendered row shows
        // "Walk-in Customer" via a custom item template.
        await pickAutocompleteOption(page, customerSearch, 'Walk', 'Walk-in');

        const providerSearch = page.getByPlaceholder('Search provider...').last();
        await pickAutocompleteOption(page, providerSearch, providerName);

        // Both the Amount and Payment Fee p-inputNumber fields duplicate their placeholder
        // on wrapper + inner input, so ".last()" over placeholder alone lands on the wrong
        // (Fee) field — anchor off the Amount field's own label instead.
        const amountInput = page.getByText('Amount *', { exact: true }).locator('xpath=following::input[1]');
        await amountInput.click();
        await amountInput.fill('500');
        // Under heavy load a fill can land before Angular finishes settling the field from
        // the autocomplete picks above and silently get dropped — verify it stuck (same
        // reasoning as the payment-method retry below) before relying on it for submit.
        if (!(await amountInput.inputValue().catch(() => '')).includes('500')) {
            await amountInput.click();
            await amountInput.fill('500');
        }

        await pickDropdownOption(page, page.getByText('Select payment method', { exact: false }).first(), 'Cash on Delivery');
        // The dropdown selection occasionally didn't stick under heavier page load —
        // verify the placeholder text is gone (a method is actually selected) before
        // submitting, retrying the pick once if not.
        if (await page.getByText('Select payment method', { exact: false }).first().isVisible().catch(() => false)) {
            await pickDropdownOption(page, page.getByText('Select payment method', { exact: false }).first(), 'Cash on Delivery');
        }

        const advanceCreditCheckbox = page.getByLabel(/record as advance credit/i);
        await advanceCreditCheckbox.check();
        if (!(await advanceCreditCheckbox.isChecked().catch(() => false))) {
            await advanceCreditCheckbox.check();
        }

        // The endpoint has occasionally been slow under sustained dev load (same class of
        // issue documented for the customer/provider search endpoints in ui.ts) — give it
        // more margin than the default action timeout.
        const [response] = await Promise.all([
            page.waitForResponse((r) => r.url().endsWith('/api/v1/customer-payments') && r.request().method() === 'POST', { timeout: 25_000 }),
            page.getByRole('button', { name: 'Save Payment' }).click()
        ]);
        expect(response.ok()).toBe(true);
        await expect(page.getByText('Payment Recorded Successfully')).toBeVisible({ timeout: 10_000 });
    });

    test('2. seed a supplier for the supplier-payment flow', async ({ page }) => {
        await page.goto('/inventory/suppliers/create');
        await page.getByPlaceholder('e.g., ABC Auto Parts Ltd').fill(supplierName);
        await page.getByPlaceholder('e.g., John Smith').fill('Payment Contact');
        await page.getByPlaceholder('e.g., contact@supplier.com').fill(`payment-${uniqueSuffix()}@test.com`);
        await page.getByPlaceholder('e.g., +91 98765 43210').fill('+8801700000005');
        await page.getByPlaceholder('e.g., 123 Industrial Estate').fill('456 Payment Road');
        await page.getByPlaceholder('e.g., Mumbai').fill('Dhaka');
        await page.getByPlaceholder('e.g., Maharashtra').fill('Dhaka');
        await page.getByPlaceholder('e.g., 400001').fill('1207');
        await page.getByRole('button', { name: 'Create Supplier' }).click();
        await expect(page).toHaveURL(/\/inventory\/suppliers$/);
        await expect(page.getByText(supplierName).first()).toBeVisible();
    });

    test('3. record a supplier advance payment', async ({ page }) => {
        await page.goto('/procurement/supplier-payments/new');

        const supplierSearch = page.getByPlaceholder('Search supplier...');
        await pickDropdownOption(page, supplierSearch, supplierName, { typeToFilter: true });

        await page.getByText('Advance Payment', { exact: true }).click();

        const payFromSearch = page.getByPlaceholder('Select your payment account...');
        await pickDropdownOption(page, payFromSearch, providerName, { typeToFilter: true });

        const amountInput = page.getByPlaceholder('0.00').last();
        await amountInput.click();
        await amountInput.fill('300');

        await page.getByPlaceholder(/e\.g\., Advance for upcoming orders/i).fill('E2E advance payment');

        await page.getByRole('button', { name: 'Record Payment' }).click();
        await expect(page).toHaveURL(/\/procurement\/supplier-payments$/);
    });
});
