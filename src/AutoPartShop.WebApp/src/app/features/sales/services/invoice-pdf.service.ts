import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

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
  private readonly http = inject(HttpClient);

  // Company configuration - In real app, this would come from settings/API
  private readonly companyConfig = {
    companyName: 'Sujan Motors',
    companyAddress: 'Kathmandu, Nepal',
    companyPhone: '+977-1-4XXXXXX',
    companyEmail: 'info@sujanmotors.com',
    companyTaxId: 'PAN: XXXXXXXXX',
    companyLogo: 'assets/logo.png'
  };

  /**
   * Get company configuration
   */
  getCompanyConfig() {
    return this.companyConfig;
  }

  /**
   * Format currency for display
   */
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-NP', {
      style: 'currency',
      currency: 'NPR',
      minimumFractionDigits: 2
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

    words = words.trim() + ' Rupees';

    if (decPart > 0) {
      words += ' and ' + convertLessThanThousand(decPart) + ' Paisa';
    }

    return words + ' Only';
  }

  /**
   * Print invoice using browser print
   */
  printInvoice(printContainerId: string): void {
    const printContent = document.getElementById(printContainerId);
    if (!printContent) return;

    const printWindow = window.open('', '_blank', 'width=800,height=600');
    if (!printWindow) return;

    printWindow.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>Invoice Print</title>
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #333; }
          @page { size: A4; margin: 10mm; }
          @media print {
            body { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
          }
        </style>
      </head>
      <body>
        ${printContent.innerHTML}
        <script>
          window.onload = function() { window.print(); window.close(); }
        </script>
      </body>
      </html>
    `);
    printWindow.document.close();
  }

  /**
   * Download invoice as PDF using html2canvas and jspdf
   */
  async downloadAsPdf(elementId: string, filename: string): Promise<void> {
    const element = document.getElementById(elementId);
    if (!element) return;

    // Dynamic import for better bundle size
    const html2canvas = (await import('html2canvas')).default;
    const { jsPDF } = await import('jspdf');

    const canvas = await html2canvas(element, {
      scale: 2,
      useCORS: true,
      logging: false
    });

    const imgData = canvas.toDataURL('image/png');
    const pdf = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const pdfWidth = pdf.internal.pageSize.getWidth();
    const pdfHeight = (canvas.height * pdfWidth) / canvas.width;

    pdf.addImage(imgData, 'PNG', 0, 0, pdfWidth, pdfHeight);
    pdf.save(`${filename}.pdf`);
  }
}
