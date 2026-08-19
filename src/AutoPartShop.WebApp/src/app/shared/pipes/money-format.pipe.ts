import { Pipe, PipeTransform, inject } from '@angular/core';
import { CurrencyService } from '../services/currency.service';

/**
 * Accounting-style money format pipe.
 *
 * • Shows currency symbol (from CurrencyService).
 * • Negative values render in parentheses with a leading minus: -(5,000.00)
 * • Positive values render normally: 5,000.00
 *
 * Usage:
 *   {{ amount | moneyFormat }}            — default selected currency
 *   {{ amount | moneyFormat:'BDT' }}      — explicit currency code
 *   {{ amount | moneyFormat:'BDT':false }}— hide currency symbol
 */
@Pipe({
    name: 'moneyFormat',
    standalone: true
})
export class MoneyFormatPipe implements PipeTransform {
    private readonly currencyService = inject(CurrencyService);

    transform(value: number | null | undefined, currencyCode?: string, showSymbol = true): string {
        if (value === null || value === undefined) {
            return '—';
        }

        const code = currencyCode || this.currencyService.getSelectedCurrency();
        const symbol = showSymbol ? this.currencyService.getCurrencySymbol(code) : '';
        const decimals = this.currencyService.getCurrencyDecimalPlaces(code);
        const abs = Math.abs(value);
        const formatted = abs.toLocaleString(undefined, {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals
        });

        if (value < 0) {
            const sym = symbol ? `${symbol} ` : '';
            return `-${sym}(${formatted})`;
        }

        const sym = symbol ? `${symbol} ` : '';
        return `${sym}${formatted}`;
    }
}
