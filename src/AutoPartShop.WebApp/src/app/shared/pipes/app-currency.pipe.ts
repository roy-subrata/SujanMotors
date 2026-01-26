import { Pipe, PipeTransform, inject } from '@angular/core';
import { CurrencyService } from '../services/currency.service';

/**
 * Custom currency pipe for formatting amounts with proper currency symbols
 *
 * Usage:
 * {{ amount | appCurrency:'USD' }}
 * {{ 1000 | appCurrency:selectedCurrency }}
 */
@Pipe({
  name: 'appCurrency',
  standalone: true
})
export class AppCurrencyPipe implements PipeTransform {
  private currencyService = inject(CurrencyService);

  transform(value: number | null | undefined, currencyCode?: string): string {
    if (value === null || value === undefined) {
      return '';
    }

    // If no currency code provided, use base currency or default
    if (!currencyCode) {
      const baseCurrency = this.currencyService.baseCurrency$ | null;
      currencyCode = baseCurrency ? 'BDT' : 'BDT'; // Fallback to BDT
    }

    return this.currencyService.formatCurrency(value, currencyCode);
  }
}
