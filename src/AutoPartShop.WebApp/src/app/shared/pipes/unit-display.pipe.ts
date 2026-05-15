import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'unitDisplay',
  standalone: true
})
export class UnitDisplayPipe implements PipeTransform {
  /**
   * Formats quantity with unit symbol consistently across the app.
   * Format: "{qty} {symbol}" or "{qty} {symbol} ({baseQty} {baseSymbol})"
   * 
   * Usage:
   *   {{ quantity | unitDisplay:symbol:baseQty:baseSymbol }}
   * 
   * Examples:
   *   1 | unitDisplay:'Doz':12:'Pc' → "1 Doz (12 Pc)"
   *   5 | unitDisplay:'Pc' → "5 Pc"
   */
  transform(
    quantity: number,
    symbol: string | null,
    baseQuantity?: number,
    baseSymbol?: string | null
  ): string {
    const unitSymbol = symbol || '';
    const display = `${quantity} ${unitSymbol}`.trim();

    // Show base unit only if it's different from display quantity
    if (
      baseQuantity !== undefined &&
      baseQuantity !== null &&
      baseSymbol &&
      baseQuantity !== quantity
    ) {
      return `${display} (${baseQuantity} ${baseSymbol})`;
    }

    return display;
  }
}
