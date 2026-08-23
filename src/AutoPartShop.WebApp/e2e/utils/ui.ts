import { Page, Locator, expect } from '@playwright/test';

/**
 * PrimeNG p-select / p-autocomplete dropdown panels render their options as
 * role="option" inside an overlay appended to <body>, not inside the input's own
 * DOM subtree. Both component types are driven the same way here: open the panel,
 * optionally filter by typing, then click the matching option by its visible text.
 */
export async function pickDropdownOption(
    page: Page,
    trigger: Locator,
    optionText: string,
    opts: { typeToFilter?: boolean } = {}
) {
    await trigger.click();
    if (opts.typeToFilter) {
        await trigger.pressSequentially(optionText, { delay: 20 });
    }
    const option = page.getByRole('option', { name: optionText, exact: false }).first();
    await expect(option).toBeVisible({ timeout: 10_000 });
    await option.click();
}

/** Same as pickDropdownOption but the input is a plain autocomplete text box that must be clicked+typed directly (not via a separate trigger). */
export async function pickAutocompleteOption(page: Page, input: Locator, query: string, optionText?: string) {
    await input.click();
    await input.fill('');
    await input.pressSequentially(query, { delay: 30 });
    const option = page.getByRole('option', { name: optionText ?? query, exact: false }).first();
    await expect(option).toBeVisible({ timeout: 10_000 });
    await option.click();
}

/** Waits for a PrimeNG toast/message with the given (substring) text to appear. */
export async function expectToast(page: Page, text: string) {
    await expect(page.getByText(text, { exact: false }).first()).toBeVisible({ timeout: 10_000 });
}
