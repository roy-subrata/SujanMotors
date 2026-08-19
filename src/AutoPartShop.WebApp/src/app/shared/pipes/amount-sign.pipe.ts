import { Pipe, PipeTransform } from '@angular/core';

/**
 * Returns 'negative' when the value is < 0, empty string otherwise.
 * Used for CSS class bindings: [class]="amount | amountSign"
 */
@Pipe({
    name: 'amountSign',
    standalone: true
})
export class AmountSignPipe implements PipeTransform {
    transform(value: number | null | undefined): string {
        return value != null && value < 0 ? 'negative' : '';
    }
}
