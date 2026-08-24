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

/**
 * Same as pickDropdownOption but the input is a plain autocomplete text box that must be
 * clicked+typed directly (not via a separate trigger).
 *
 * app-lazy-autocomplete's fetchData() cancels the in-flight request as soon as the query
 * changes again (see cancelPendingRequest() in lazy-autocomplete.component.ts), and its
 * backing list endpoints (e.g. POST /api/v1/customers/list) have been observed taking up
 * to ~11s under sustained dev load (traced to OperationCanceledException from a *retyped*
 * search cancelling the still-in-flight first request — not a data or query-correctness
 * bug; the equivalent raw SQL runs in ~1ms). Retyping to "retry" only compounds this by
 * cancelling a request that was about to succeed, so type once and wait out a single
 * generous timeout instead.
 */
export async function pickAutocompleteOption(page: Page, input: Locator, query: string, optionText?: string) {
    const option = page.getByRole('option', { name: optionText ?? query, exact: false }).first();

    await input.click();
    await input.fill('');
    await input.pressSequentially(query, { delay: 30 });
    await expect(option).toBeVisible({ timeout: 25_000 });
    await option.click();
}

/** Waits for a PrimeNG toast/message with the given (substring) text to appear. */
export async function expectToast(page: Page, text: string) {
    await expect(page.getByText(text, { exact: false }).first()).toBeVisible({ timeout: 10_000 });
}
