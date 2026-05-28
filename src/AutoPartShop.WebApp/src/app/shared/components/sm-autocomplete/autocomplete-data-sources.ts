import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { CustomerService, CustomerResponse } from '@/features/sales/services/customer.service';
import { AutocompleteDataSource, PaginatedData } from './sm-autocomplete.component';

@Injectable({
    providedIn: 'root'
})
export class AutocompleteDataSourceFactory {
    private readonly customerService = inject(CustomerService);

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
