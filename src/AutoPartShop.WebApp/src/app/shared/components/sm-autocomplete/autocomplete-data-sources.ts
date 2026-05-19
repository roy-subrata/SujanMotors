import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { SupplierService, SupplierResponse } from '@/features/inventory/services/supplier.service';
import { CustomerService, CustomerResponse } from '@/features/sales/services/customer.service';
import { PartService, PartResponse } from '@/features/inventory/services/part.service';
import { AutocompleteDataSource, PaginatedData } from './sm-autocomplete.component';

/**
 * Factory service for creating autocomplete data sources
 * This service provides pre-configured data sources for common entities
 */
@Injectable({
    providedIn: 'root'
})
export class AutocompleteDataSourceFactory {
    private readonly supplierService = inject(SupplierService);
    private readonly customerService = inject(CustomerService);
    private readonly partService = inject(PartService);

    /**
     * Create a data source for Supplier autocomplete
     */
 

    /**
     * Create a data source for Customer autocomplete
     */
    createCustomerDataSource(): AutocompleteDataSource<CustomerResponse> {
        return {
            fetchData: (search: string, pageNumber: number, pageSize: number): Observable<PaginatedData<CustomerResponse>> => {
                return this.customerService.getCustomers({
                    search,
                    pageNumber,
                    pageSize
                }).pipe(
                    map(response => ({
                        data: response.data as CustomerResponse[],
                        pagination: response.pagination
                    }))
                );
            },
            displayField: (customer: CustomerResponse) => customer.fullName || `${customer.firstName} ${customer.lastName}`,
            subtitleField: (customer: CustomerResponse) => `${customer.customerCode} - ${customer.phone || customer.email || 'No contact'}`,
            valueField: (customer: CustomerResponse) => customer.id
        };
    }

    /**
     * Create a data source for Part/Product autocomplete
     */
    // createPartDataSource(): AutocompleteDataSource<PartResponse> {
    //     return {
    //         fetchData: (search: string, pageNumber: number, pageSize: number): Observable<PaginatedData<PartResponse>> => {
    //             return this.partService.getParts({
    //                 search,
    //                 pageNumber,
    //                 pageSize
    //             }).pipe(
    //                 map(response => ({
    //                     data: response.data as PartResponse[],
    //                     pagination: response.pagination
    //                 }))
    //             );
    //         },
    //         displayField: (part: PartResponse) => part.name,
    //         subtitleField: (part: PartResponse) => `${part.partNumber} - Stock: ${part.stockQuantity}`,
    //         valueField: (part: PartResponse) => part.id
    //     };
    // }
}

/**
 * Helper function to create a custom data source
 * Use this when you need a data source for an entity not covered by the factory
 */
export function createDataSource<T>(config: {
    fetchFn: (search: string, pageNumber: number, pageSize: number) => Observable<PaginatedData<T>>;
    displayField: (item: T) => string;
    subtitleField?: (item: T) => string;
    valueField?: (item: T) => any;
}): AutocompleteDataSource<T> {
    return {
        fetchData: config.fetchFn,
        displayField: config.displayField,
        subtitleField: config.subtitleField,
        valueField: config.valueField
    };
}
