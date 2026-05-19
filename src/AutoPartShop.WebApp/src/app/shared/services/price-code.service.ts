import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, of, tap } from 'rxjs';
import { environment } from 'src/environments/environment';

const SETTING_KEY = 'PRICE_CODE_WORD';
const PREFIX_KEY = 'PRICE_CODE_PREFIX';
const SUFFIX_KEY = 'PRICE_CODE_SUFFIX';

@Injectable({
  providedIn: 'root'
})
export class PriceCodeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/applicationsettings`;

  /** The magic word (10 unique letters mapping to digits 1-9, 0) */
  magicWord = signal<string>('');

  /** Optional prefix prepended to encoded price */
  prefix = signal<string>('');

  /** Optional suffix appended to encoded price */
  suffix = signal<string>('');

  /** Whether to show actual numeric price (false = show coded letters) */
  showActualPrice = signal<boolean>(false);

  /** Whether the magic word has been loaded from the server */
  loaded = signal<boolean>(false);

  /** Whether a valid magic word is configured */
  isConfigured = computed(() => this.validateMagicWord(this.magicWord()).valid);

  constructor() {
    this.loadMagicWord();
  }

  /** Load magic word and prefix/suffix from application settings */
  loadMagicWord(): void {
    this.http.get<{ key: string; value: string }>(`${this.apiUrl}/${SETTING_KEY}`).pipe(
      tap(response => {
        this.magicWord.set(response.value);
        this.loaded.set(true);
      }),
      catchError(() => {
        this.loaded.set(true);
        return of(null);
      })
    ).subscribe();

    // Load prefix (optional)
    this.http.get<{ key: string; value: string }>(`${this.apiUrl}/${PREFIX_KEY}`).pipe(
      tap(response => this.prefix.set(response.value || '')),
      catchError(() => of(null))
    ).subscribe();

    // Load suffix (optional)
    this.http.get<{ key: string; value: string }>(`${this.apiUrl}/${SUFFIX_KEY}`).pipe(
      tap(response => this.suffix.set(response.value || '')),
      catchError(() => of(null))
    ).subscribe();
  }

  /** Save magic word to application settings */
  saveMagicWord(word: string, prefix?: string, suffix?: string) {
    const upperWord = word.toUpperCase();
    const saveWord$ = this.http.put(`${this.apiUrl}/${SETTING_KEY}`, {
      value: upperWord,
      dataType: 'STRING',
      category: 'PRICING',
      description: 'Magic word for price code encoding (10 unique letters, each maps to a digit 1-9,0)'
    }).pipe(
      tap(() => this.magicWord.set(upperWord))
    );

    // Save prefix if provided (even if empty to clear it)
    if (prefix !== undefined) {
      this.http.put(`${this.apiUrl}/${PREFIX_KEY}`, {
        value: prefix,
        dataType: 'STRING',
        category: 'PRICING',
        description: 'Optional prefix prepended to encoded price code'
      }).pipe(
        tap(() => this.prefix.set(prefix)),
        catchError(() => of(null))
      ).subscribe();
    }

    // Save suffix if provided (even if empty to clear it)
    if (suffix !== undefined) {
      this.http.put(`${this.apiUrl}/${SUFFIX_KEY}`, {
        value: suffix,
        dataType: 'STRING',
        category: 'PRICING',
        description: 'Optional suffix appended to encoded price code'
      }).pipe(
        tap(() => this.suffix.set(suffix)),
        catchError(() => of(null))
      ).subscribe();
    }

    return saveWord$;
  }

  /** Clear the magic word (disable price encoding) */
  clearMagicWord() {
    return this.http.delete(`${this.apiUrl}/${SETTING_KEY}`).pipe(
      tap(() => {
        this.magicWord.set('');
        this.prefix.set('');
        this.suffix.set('');
      }),
      catchError(() => {
        // If delete fails (key not found), just clear locally
        this.magicWord.set('');
        this.prefix.set('');
        this.suffix.set('');
        return of(null);
      })
    );
  }

  /** Toggle between coded and actual price display */
  toggleVisibility(): void {
    this.showActualPrice.update(v => !v);
  }

  /**
   * Encode a numeric price into magic-code letters.
   * Mapping: 1st letter → 1, 2nd → 2, ... 9th → 9, 10th → 0
   * Decimal points are preserved.
   */
  encode(price: number): string {
    const word = this.magicWord();
    if (!word || word.length !== 10) return '***';

    const priceStr = price.toString();
    let encoded = '';

    for (const ch of priceStr) {
      if (ch === '.') {
        encoded += '.';
      } else {
        const digit = parseInt(ch, 10);
        if (isNaN(digit)) {
          encoded += ch;
        } else {
          // digit 1→index 0, digit 2→index 1, ... digit 9→index 8, digit 0→index 9
          const index = digit === 0 ? 9 : digit - 1;
          encoded += word[index];
        }
      }
    }

    // Apply prefix and suffix
    return this.prefix() + encoded + this.suffix();
  }

  /**
   * Decode magic-code letters back into a numeric price.
   * Automatically strips prefix/suffix if present.
   */
  decode(code: string): number {
    const word = this.magicWord().toUpperCase();
    if (!word || word.length !== 10) return 0;

    // Strip prefix and suffix
    let stripped = code;
    const pre = this.prefix();
    const suf = this.suffix();
    if (pre && stripped.startsWith(pre)) stripped = stripped.slice(pre.length);
    if (suf && stripped.endsWith(suf)) stripped = stripped.slice(0, -suf.length);

    let decoded = '';
    for (const ch of stripped) {
      if (ch === '.') {
        decoded += '.';
      } else {
        const index = word.indexOf(ch.toUpperCase());
        if (index === -1) {
          decoded += ch;
        } else {
          // index 0→1, index 1→2, ... index 8→9, index 9→0
          decoded += index === 9 ? '0' : (index + 1).toString();
        }
      }
    }

    return parseFloat(decoded) || 0;
  }

  /**
   * Validate a magic word: must be exactly 10 unique A-Z letters.
   */
  validateMagicWord(word: string): { valid: boolean; error?: string } {
    if (!word) return { valid: false, error: 'Magic word is required' };

    const upper = word.toUpperCase();
    if (upper.length !== 10) return { valid: false, error: 'Must be exactly 10 letters' };
    if (!/^[A-Z]+$/.test(upper)) return { valid: false, error: 'Only letters A-Z allowed' };

    const unique = new Set(upper);
    if (unique.size !== 10) return { valid: false, error: 'All 10 letters must be unique' };

    return { valid: true };
  }

  /**
   * Get display value for a cost price.
   * Returns encoded string when hidden, or null when should show actual price.
   */
  getDisplayPrice(price: number): string | null {
    if (this.showActualPrice() || !this.isConfigured()) {
      return null; // Caller should use normal formatting
    }
    return this.encode(price);
  }
}
