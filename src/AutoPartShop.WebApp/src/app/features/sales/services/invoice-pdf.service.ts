import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { shareReplay } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { CurrencyService } from '../../../shared/services/currency.service';
import { AppSettingsService, ShopProfile } from '../../../shared/services/app-settings.service';
import { environment } from '../../../../environments/environment';

const DEFAULT_PROFILE: ShopProfile = {
  appName: 'Auto Part Shop', appLogoUrl: 'assets/logo.png',
  name: '', address: '', phone: '', email: '', taxNo: '',
  logoUrl: 'assets/logo.png', tagline: '',
  invoiceFooterText: 'Thank you for your business!',
  challanFooterText: 'Goods once dispatched will not be accepted back without prior notice.'
};

export interface InvoicePdfData {
  // Company Info
  companyName: string;
  companyAddress: string;
  companyPhone: string;
  companyEmail: string;
  companyLogo?: string;
  companyTaxId?: string;

  // Invoice Details
  invoiceNumber: string;
  invoiceDate: Date;
  dueDate?: Date;
  salesOrderNumber?: string;

  // Customer Info
  customerName: string;
  customerAddress?: string;
  customerPhone?: string;
  customerEmail?: string;

  // Technician Info (if applicable)
  technicianName?: string;
  technicianPhone?: string;

  // Line Items
  items: InvoicePdfItem[];

  // Totals
  subtotal: number;
  discountAmount: number;
  discountPercentage?: number;
  vatPercentage: number;
  vatAmount: number;
  grandTotal: number;

  // Payment Info
  payments: InvoicePdfPayment[];
  paidAmount: number;
  dueAmount: number;

  // Additional
  notes?: string;
  paymentTerms?: string;
  createdBy?: string;
}

export interface InvoicePdfItem {
  slNo: number;
  partNumber: string;
  description: string;
  quantity: number;
  unitPrice: number;
  discount: number;
  total: number;
}

export interface InvoicePdfPayment {
  method: string;
  amount: number;
  reference?: string;
  date?: Date;
}

@Injectable({ providedIn: 'root' })
export class InvoicePdfService {
  private readonly currencyService = inject(CurrencyService);
  private readonly appSettings = inject(AppSettingsService);
  private readonly http = inject(HttpClient);

  /** Loaded once from DB; all print components read this signal. */
  readonly shopProfile = toSignal(
    this.appSettings.getShopProfile().pipe(shareReplay(1)),
    { initialValue: DEFAULT_PROFILE }
  );

  /** Backward-compatible accessor — returns current profile values. */
  getCompanyConfig() {
    const p = this.shopProfile();
    return {
      companyName:    p.name,
      companyAddress: p.address,
      companyPhone:   p.phone,
      companyEmail:   p.email,
      companyTaxId:   p.taxNo,
      companyLogo:    p.logoUrl
    };
  }

  /**
   * Format currency for display
   */
  formatCurrency(amount: number): string {
    const currencyCode = this.currencyService.selectedCurrency();
    const locale = this.currencyService.getCurrencyLocale(currencyCode);
    const fractionDigits = this.currencyService.getCurrencyDecimalPlaces(currencyCode);
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: currencyCode,
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits
    }).format(amount);
  }

  /**
   * Format date for display
   */
  formatDate(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  /**
   * Format date with time
   */
  formatDateTime(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  /**
   * Get payment method display name
   */
  getPaymentMethodLabel(method: string): string {
    const labels: Record<string, string> = {
      'CASH': 'Cash',
      'MOBILE_BANKING': 'Mobile Banking',
      'CARD': 'Card Payment',
      'DUE': 'Credit/Due',
      'PART_PAY': 'Partial Payment'
    };
    return labels[method] || method;
  }

  /**
   * Generate invoice number with suffix for thermal print
   */
  generateThermalInvoiceNumber(invoiceNumber: string): string {
    return `${invoiceNumber}-T`;
  }

  /**
   * Convert number to words (for invoice total)
   */
  numberToWords(num: number): string {
    const ones = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten',
      'Eleven', 'Twelve', 'Thirteen', 'Fourteen', 'Fifteen', 'Sixteen', 'Seventeen', 'Eighteen', 'Nineteen'];
    const tens = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety'];

    const convertLessThanThousand = (n: number): string => {
      if (n === 0) return '';
      if (n < 20) return ones[n];
      if (n < 100) return tens[Math.floor(n / 10)] + (n % 10 ? ' ' + ones[n % 10] : '');
      return ones[Math.floor(n / 100)] + ' Hundred' + (n % 100 ? ' ' + convertLessThanThousand(n % 100) : '');
    };

    if (num === 0) return 'Zero';

    const intPart = Math.floor(num);
    const decPart = Math.round((num - intPart) * 100);

    let words = '';

    if (intPart >= 10000000) {
      words += convertLessThanThousand(Math.floor(intPart / 10000000)) + ' Crore ';
      num = intPart % 10000000;
    }
    if (intPart >= 100000) {
      words += convertLessThanThousand(Math.floor((intPart % 10000000) / 100000)) + ' Lakh ';
      num = intPart % 100000;
    }
    if (intPart >= 1000) {
      words += convertLessThanThousand(Math.floor((intPart % 100000) / 1000)) + ' Thousand ';
      num = intPart % 1000;
    }
    words += convertLessThanThousand(intPart % 1000);

    const currencyCode = this.currencyService.selectedCurrency();
    const currencyWords = this.getCurrencyWords(currencyCode);
    words = words.trim() + ` ${currencyWords.major}`;

    if (decPart > 0) {
      words += ' and ' + convertLessThanThousand(decPart) + ` ${currencyWords.minor}`;
    }

    return words + ' Only';
  }

  private getCurrencyWords(currencyCode: string): { major: string; minor: string } {
    switch ((currencyCode || '').toUpperCase()) {
      case 'USD':
        return { major: 'Dollars', minor: 'Cents' };
      case 'BDT':
        return { major: 'Taka', minor: 'Paisa' };
      case 'NPR':
      case 'INR':
        return { major: 'Rupees', minor: 'Paisa' };
      default:
        return { major: 'Units', minor: 'Subunits' };
    }
  }

  /**
   * Print invoice using browser print
   */
  printInvoice(printContainerId: string): void {
    const printContent = document.getElementById(printContainerId);
    if (!printContent) return;

    const printWindow = window.open('', '_blank', 'width=800,height=600');
    if (!printWindow) return;

    // Carry the app's stylesheets across so Angular's emulated-scoped styles still apply in the
    // print window (innerHTML keeps the _ngcontent-* attributes the scoped rules match on).
    const headStyles = Array.from(
      document.querySelectorAll('style, link[rel="stylesheet"]')
    ).map(el => el.outerHTML).join('\n');

    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>Invoice Print</title>
        ${headStyles}
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #333; background: #fff; }
          @page { size: A4; margin: 10mm; }
          @media print {
            body { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
          }
        </style>
      </head>
      <body>
        ${printContent.innerHTML}
        <script>
          // Wait for stylesheets to load before printing so the layout isn't captured bare.
          window.onload = function() { setTimeout(function() { window.print(); window.close(); }, 300); }
        </script>
      </body>
      </html>
    `);
    printWindow.document.close();
  }

  getInvoiceByNumber(invoiceNumber: string): Observable<{ id: string; invoiceNumber: string }> {
    return this.http.get<{ id: string; invoiceNumber: string }>(
      `${environment.apiUrl}/v1/salesorders/invoices/number/${encodeURIComponent(invoiceNumber)}`
    );
  }

  /**
   * Download the server-rendered QuestPDF invoice for the given invoice ID.
   * Returns an Observable that completes once the browser download is triggered.
   */
  downloadServerPdf(invoiceId: string, invoiceNumber: string): Observable<void> {
    const url = `${environment.apiUrl}/salesorders/invoices/${invoiceId}/pdf`;
    return this.http.get(url, { responseType: 'blob' }).pipe(
      map(blob => {
        const objectUrl = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = objectUrl;
        a.download = `invoice-${invoiceNumber}.pdf`;
        a.click();
        URL.revokeObjectURL(objectUrl);
      })
    );
  }
}
