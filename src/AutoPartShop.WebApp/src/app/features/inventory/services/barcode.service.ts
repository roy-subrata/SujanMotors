import { Injectable } from '@angular/core';
import JsBarcode from 'jsbarcode';

export type BarcodeType = 'qrcode' | 'code128' | 'code39' | 'ean13';

export interface BarcodeConfig {
  type: BarcodeType;
  value: string;
  displayText?: string;
  width?: number;
  height?: number;
}

@Injectable({
  providedIn: 'root'
})
export class BarcodeService {
  /**
   * Generate a linear barcode (Code128, Code39, EAN13) as SVG
   */
  generateLinearBarcode(config: BarcodeConfig): string {
    try {
      const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
      JsBarcode(svg, config.value, {
        format: this.getJsBarcodeFormat(config.type),
        width: config.width || 2,
        height: config.height || 100,
        displayValue: config.displayText !== undefined ? config.displayText.length > 0 : true,
        text: config.displayText
      });
      return new XMLSerializer().serializeToString(svg);
    } catch (error) {
      console.error('Error generating barcode:', error);
      return '';
    }
  }

  /**
   * Generate a canvas-based barcode for images
   */
  generateBarcodeCanvas(config: BarcodeConfig): HTMLCanvasElement | null {
    try {
      const canvas = document.createElement('canvas');
      JsBarcode(canvas, config.value, {
        format: this.getJsBarcodeFormat(config.type),
        width: config.width || 2,
        height: config.height || 100,
        displayValue: config.displayText !== undefined ? config.displayText.length > 0 : true,
        text: config.displayText
      });
      return canvas;
    } catch (error) {
      console.error('Error generating barcode canvas:', error);
      return null;
    }
  }

  /**
   * Generate QR Code data URL
   */
  generateQRCodeDataUrl(value: string, size: number = 200): Promise<string> {
    return new Promise((resolve, reject) => {
      try {
        // Use canvas API for QR code generation
        // This is compatible with angularx-qrcode component
        const canvas = document.createElement('canvas');
        const ctx = canvas.getContext('2d');

        if (!ctx) {
          reject('Could not get canvas context');
          return;
        }

        canvas.width = size;
        canvas.height = size;

        // For now, we'll use a simple placeholder
        // In template, we'll use the ngx-qrcode component
        // This method is kept for potential API-based QR generation
        resolve(`data:image/svg+xml;base64,${btoa(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><rect width="${size}" height="${size}" fill="white"/></svg>`)}`);
      } catch (error) {
        reject(error);
      }
    });
  }

  /**
   * Download barcode as PNG image
   */
  async downloadBarcodeAsPNG(
    config: BarcodeConfig,
    filename: string,
    isQRCode: boolean = false
  ): Promise<void> {
    try {
      let canvas: HTMLCanvasElement | null = null;

      if (isQRCode) {
        // For QR codes, find the canvas in the DOM
        const qrcodeCanvas = document.querySelector('canvas[role="img"]') as HTMLCanvasElement;
        if (qrcodeCanvas) {
          canvas = qrcodeCanvas;
        }
      } else {
        canvas = this.generateBarcodeCanvas(config);
      }

      if (!canvas) {
        throw new Error('Failed to generate barcode canvas');
      }

      const link = document.createElement('a');
      link.href = canvas.toDataURL('image/png');
      link.download = `${filename}.png`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    } catch (error) {
      console.error('Error downloading barcode:', error);
      throw error;
    }
  }

  /**
   * Download barcode as SVG
   */
  downloadBarcodeAsSVG(config: BarcodeConfig, filename: string): void {
    try {
      const svgString = this.generateLinearBarcode(config);
      if (!svgString) {
        throw new Error('Failed to generate SVG barcode');
      }

      const blob = new Blob([svgString], { type: 'image/svg+xml' });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `${filename}.svg`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Error downloading SVG:', error);
      throw error;
    }
  }

  /**
   * Print barcode
   */
  printBarcode(element: HTMLElement, title: string = 'Barcode'): void {
    try {
      const printWindow = window.open('', '', 'width=800,height=600');
      if (!printWindow) {
        throw new Error('Could not open print window');
      }

      const htmlContent = `
        <html>
          <head>
            <title>${title}</title>
            <style>
              body {
                font-family: Arial, sans-serif;
                display: flex;
                justify-content: center;
                align-items: center;
                min-height: 100vh;
                margin: 0;
                padding: 20px;
                background-color: #f5f5f5;
              }
              .print-container {
                background-color: white;
                padding: 40px;
                border-radius: 8px;
                box-shadow: 0 2px 8px rgba(0,0,0,0.1);
                text-align: center;
              }
              .print-title {
                font-size: 24px;
                font-weight: bold;
                margin-bottom: 30px;
                color: #333;
              }
              .barcode-container {
                margin: 20px 0;
              }
              svg, canvas, img {
                max-width: 100%;
                height: auto;
              }
            </style>
          </head>
          <body>
            <div class="print-container">
              <div class="print-title">${title}</div>
              <div class="barcode-container">
                ${element.innerHTML}
              </div>
            </div>
          </body>
        </html>
      `;

      printWindow.document.write(htmlContent);
      printWindow.document.close();

      setTimeout(() => {
        printWindow.print();
      }, 250);
    } catch (error) {
      console.error('Error printing barcode:', error);
      throw error;
    }
  }

  /**
   * Validate barcode value for specific format
   */
  validateBarcodeValue(type: BarcodeType, value: string): boolean {
    switch (type) {
      case 'code128':
        return value.length > 0 && value.length <= 80;
      case 'code39':
        return /^[A-Z0-9\s\-\.$/+%]*$/.test(value) && value.length <= 80;
      case 'ean13':
        return /^\d{13}$/.test(value);
      case 'qrcode':
        return value.length > 0;
      default:
        return false;
    }
  }

  /**
   * Get format string for JSBarcode library
   */
  private getJsBarcodeFormat(type: BarcodeType): string {
    switch (type) {
      case 'code128':
        return 'CODE128';
      case 'code39':
        return 'CODE39';
      case 'ean13':
        return 'EAN13';
      default:
        return 'CODE128';
    }
  }

  /**
   * Generate EAN13 from SKU or part number
   */
  generateEAN13FromSKU(sku: string): string {
    // Simple algorithm: take first 12 digits from hash of SKU
    const hash = this.hashString(sku);
    const digits = hash.replace(/\D/g, '').substring(0, 12);
    const paddedDigits = digits.padEnd(12, '0').substring(0, 12);

    // Calculate check digit
    let sum = 0;
    for (let i = 0; i < 12; i++) {
      sum += parseInt(paddedDigits[i], 10) * (i % 2 === 0 ? 1 : 3);
    }
    const checkDigit = (10 - (sum % 10)) % 10;

    return paddedDigits + checkDigit;
  }

  /**
   * Simple hash function for string
   */
  private hashString(str: string): string {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = (hash << 5) - hash + char;
      hash = hash & hash; // Convert to 32bit integer
    }
    return Math.abs(hash).toString();
  }
}
