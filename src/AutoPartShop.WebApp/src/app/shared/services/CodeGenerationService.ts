import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

export type CodeType = 'unit' | 'category' | 'brand' | 'part' | 'warehouse' | 'invoice' | 'sales-order' | 'customer' | 'supplier' | 'purchase-order';

@Injectable({
    providedIn: 'root'
})
export class CodeGenerationService {
    private readonly httpClient = inject(HttpClient);
    private readonly apiUrl = 'http://localhost:5292/api/code-generate';

    /**
     * Generate code for a specific entity type
     * @param type The type of entity to generate code for
     * @returns Observable<string> The generated code
     */
    getCode(type: CodeType | string): Observable<string> {
        // Map old type names to new endpoint names for backward compatibility
        const typeMap: Record<string, string> = {
            'Part': 'part',
            'Unit': 'unit',
            'Category': 'category',
            'Brand': 'brand',
            'Warehouse': 'warehouse',
            'Invoice': 'invoice',
            'SalesOrder': 'sales-order',
            'Customer': 'customer',
            'Supplier': 'supplier',
            'PurchaseOrder': 'purchase-order'
        };

        const endpoint = typeMap[type] || type.toLowerCase();

        return this.httpClient.get<any>(`${this.apiUrl}/${endpoint}`).pipe(
            map(response => {
                // Handle both plain text string and JSON object responses
                if (typeof response === 'string') {
                    // Plain string response - strip quotes if present
                    return response.replace(/^"|"$/g, '');
                } else if (typeof response === 'object' && response !== null) {
                    return Object.values(response)[0] as string;
                }
                return String(response);
            })
        );
    }

    /**
     * Generate unit code
     */
    generateUnitCode(): Observable<string> {
        return this.getCode('unit');
    }

    /**
     * Generate category code
     */
    generateCategoryCode(): Observable<string> {
        return this.getCode('category');
    }

    /**
     * Generate brand code
     */
    generateBrandCode(): Observable<string> {
        return this.getCode('brand');
    }

    /**
     * Generate part/SKU code
     */
    generatePartCode(): Observable<string> {
        return this.getCode('part');
    }

    /**
     * Generate warehouse code
     */
    generateWarehouseCode(): Observable<string> {
        return this.getCode('warehouse');
    }

    /**
     * Generate invoice number
     */
    generateInvoiceNumber(): Observable<string> {
        return this.getCode('invoice');
    }

    /**
     * Generate sales order number
     */
    generateSalesOrderNumber(): Observable<string> {
        return this.getCode('sales-order');
    }

    /**
     * Generate customer code
     */
    generateCustomerCode(): Observable<string> {
        return this.getCode('customer');
    }

    /**
     * Generate supplier code
     */
    generateSupplierCode(): Observable<string> {
        return this.getCode('supplier');
    }

    /**
     * Generate purchase order number
     */
    generatePurchaseOrderNumber(): Observable<string> {
        return this.getCode('purchase-order');
    }
}
