import { Component, OnInit, inject, input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { CurrencyService, Currency } from '../../services/currency.service';
import { toSignal } from '@angular/core/rxjs-interop';

/**
 * Currency Selector Component
 *
 * Reusable component for selecting currencies in reactive forms
 *
 * Usage:
 * <app-currency-selector
 *   formControlName="currency"
 *   [disabled]="false"
 *   [showLabel]="true">
 * </app-currency-selector>
 */
@Component({
  selector: 'app-currency-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, SelectModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CurrencySelectorComponent),
      multi: true
    }
  ],
  templateUrl: './currency-selector.component.html',
  styleUrl: './currency-selector.component.scss',
})
export class CurrencySelectorComponent implements OnInit, ControlValueAccessor {
  private currencyService = inject(CurrencyService);

  // Inputs
  showLabel = input<boolean>(true);
  placeholder = input<string>('Select currency');
  disabled = input<boolean>(false);
  filter = input<boolean>(true);
  showClear = input<boolean>(false);
  syncWithGlobal = input<boolean>(true);

  // Internal value - will be synced with default currency from settings
  value: string = '';
  isDisabled: boolean = false;
  private valueSetByParent = false;

  // Currencies list as signal
  currencies = toSignal(this.currencyService.activeCurrencies$, { initialValue: [] });

  // ControlValueAccessor callbacks
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    // Load active currencies
    this.currencyService.loadActiveCurrencies();

    // Set initial currency to default currency from settings if not already set by parent
    this.currencyService.defaultCurrency$.subscribe(defaultCurrency => {
      if (defaultCurrency && defaultCurrency.code && !this.valueSetByParent && !this.value) {
        this.value = defaultCurrency.code;
        this.onChange(this.value);
      }
    });

    // Fallback to selected currency from service (which syncs with default currency)
    if (!this.value && !this.valueSetByParent) {
      const selectedCurrency = this.currencyService.selectedCurrency();
      if (selectedCurrency) {
        this.value = selectedCurrency;
      }
    }
  }

  // ControlValueAccessor implementation
  writeValue(value: string): void {
    if (value) {
      this.valueSetByParent = true;
      this.value = value;
    } else {
      // Use default currency from settings if no value provided
      this.value = this.currencyService.selectedCurrency();
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled = isDisabled;
  }

  // Handle value change
  onValueChange(value: string): void {
    this.value = value;
    this.onChange(value);
    this.onTouched();

    if (this.syncWithGlobal()) {
      this.currencyService.setSelectedCurrency(value);
    }
  }
}
