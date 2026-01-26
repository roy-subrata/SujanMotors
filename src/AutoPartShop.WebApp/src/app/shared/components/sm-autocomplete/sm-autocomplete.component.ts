import { Component, Input, Output, EventEmitter, forwardRef, OnInit, OnDestroy, ViewChild, TemplateRef, ContentChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { AutoCompleteModule, AutoComplete, AutoCompleteCompleteEvent, AutoCompleteSelectEvent } from 'primeng/autocomplete';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { Observable, Subject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil, finalize, catchError } from 'rxjs/operators';

/**
 * Generic paginated response interface
 */
export interface PaginatedData<T> {
    data: T[];
    pagination: {
        pageNumber: number;
        pageSize: number;
        totalCount: number;
        totalPages: number;
    };
}

/**
 * Data source configuration for the autocomplete
 */
export interface AutocompleteDataSource<T> {
    /**
     * Function to fetch paginated data
     * @param search Search term
     * @param pageNumber Current page number (1-based)
     * @param pageSize Number of items per page
     */
    fetchData: (search: string, pageNumber: number, pageSize: number) => Observable<PaginatedData<T>>;

    /**
     * Function to get display label from item
     */
    displayField: (item: T) => string;

    /**
     * Optional function to get unique identifier from item
     */
    valueField?: (item: T) => any;

    /**
     * Optional function to get secondary display text (subtitle)
     */
    subtitleField?: (item: T) => string;
    
}

@Component({
    selector: 'sm-autocomplete',
    standalone: true,
    imports: [CommonModule, FormsModule, AutoCompleteModule, ProgressSpinnerModule],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => SmAutocompleteComponent),
            multi: true
        }
    ],
    templateUrl:"./sm.autocomplete.component.html",
    styleUrls:['./sm.autocomplete.component.scss']
})
export class SmAutocompleteComponent<T = any> implements ControlValueAccessor, OnInit, OnDestroy {
    @ViewChild('autoComplete') autoComplete!: AutoComplete;

    /** Custom item template */
    @ContentChild('itemTemplate') itemTemplate?: TemplateRef<any>;

    /** Data source configuration */
    @Input() dataSource!: AutocompleteDataSource<T>;

    /** Placeholder text */
    @Input() placeholder = 'Search...';

    /** Show dropdown button */
    @Input() showDropdown = true;

    /** Show clear button */
    @Input() showClear = true;

    /** Minimum characters before search */
    @Input() minLength = 1;

    /** Search delay in milliseconds */
    @Input() searchDelay = 300;

    /** Force selection from suggestions */
    @Input() forceSelection = true;

    /** Scroll height for dropdown */
    @Input() scrollHeight = '300px';

    /** Virtual scroll item size */
    @Input() virtualScrollItemSize = 40;

    /** Page size for pagination */
    @Input() pageSize = 20;

    /** Empty message when no results */
    @Input() emptyMessage = 'No results found';

    /** Complete on focus (show suggestions when focused) */
    @Input() completeOnFocus = true;

    /** Field name for object display (used by p-autocomplete internally) */
    @Input() fieldName = 'label';

    /** Option label field - defaults to _displayLabel which uses dataSource.displayField */
    @Input() optionLabel = '_displayLabel';

    /** Input style class */
    @Input() inputStyleClass = '';

    /** Component style class */
    @Input() styleClass = '';

    /** Panel style class */
    @Input() panelStyleClass = '';

    /** Disabled state */
    @Input() disabled = false;

    /** Event emitted when item is selected */
    @Output() itemSelected = new EventEmitter<T>();

    /** Event emitted when selection is cleared */
    @Output() cleared = new EventEmitter<void>();

    suggestions: T[] = [];
    selectedItem: T | null = null;
    loading = false;

    private currentSearch = '';
    private currentPage = 1;
    private totalCount = 0;
    private hasMore = true;
    private destroy$ = new Subject<void>();
    private searchSubject$ = new Subject<string>();
    private cancelSearch$ = new Subject<void>();
    private isSearching = false;

    // ControlValueAccessor callbacks
    private onChange: (value: T | null) => void = () => {};
    onTouched: () => void = () => {};

    ngOnInit(): void {
        this.setupSearchDebounce();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    private setupSearchDebounce(): void {
        this.searchSubject$
            .pipe(
                debounceTime(this.searchDelay),
                distinctUntilChanged(),
                takeUntil(this.destroy$)
            )
            .subscribe(search => {
                this.performSearch(search);
            });
    }

    onSearch(event: AutoCompleteCompleteEvent): void {
        // Handle case where query might be an object (selected item) instead of string
        let query = '';
        if (typeof event.query === 'string') {
            query = event.query;
        } else if (event.query && this.dataSource) {
            // If query is an object, use empty string to show all results
            query = '';
        }

        // Always reset and search when query changes or suggestions are empty
        const shouldReset = query !== this.currentSearch || this.suggestions.length === 0;
        if (shouldReset) {
            this.currentSearch = query;
            this.currentPage = 1;
            this.suggestions = [];
            this.hasMore = true;
            // Directly perform search to bypass distinctUntilChanged
            this.performSearch(query);
        } else {
            this.searchSubject$.next(query);
        }
    }

    private performSearch(search: string): void {
        if (!this.dataSource) {
            console.warn('PaginatedAutocomplete: No data source configured');
            return;
        }

        // Cancel any in-progress search
        if (this.isSearching) {
            this.cancelSearch$.next();
        }

        this.loading = true;
        this.isSearching = true;

        this.dataSource.fetchData(search, this.currentPage, this.pageSize)
            .pipe(
                takeUntil(this.cancelSearch$),
                takeUntil(this.destroy$),
                catchError(error => {
                    console.error('PaginatedAutocomplete: Error fetching data', error);
                    return of({ data: [], pagination: { pageNumber: 1, pageSize: this.pageSize, totalCount: 0, totalPages: 0 } });
                }),
                finalize(() => {
                    this.loading = false;
                    this.isSearching = false;
                })
            )
            .subscribe(response => {
                // Add computed _displayLabel to each item for p-autocomplete
                const itemsWithLabel = response.data.map(item => ({
                    ...item,
                    _displayLabel: this.dataSource.displayField(item)
                }));

                if (this.currentPage === 1) {
                    this.suggestions = itemsWithLabel;
                } else {
                    this.suggestions = [...this.suggestions, ...itemsWithLabel];
                }

                this.totalCount = response.pagination.totalCount;
                this.hasMore = this.currentPage < response.pagination.totalPages;
            });
    }

    onLazyLoad(event: any): void {
        // Load more data when scrolling
        if (this.hasMore && !this.loading) {
            this.currentPage++;
            this.performSearch(this.currentSearch);
        }
    }

    onItemSelect(event: AutoCompleteSelectEvent): void {
        const item = event.value as T;
        this.selectedItem = item;
        this.onChange(item);
        this.itemSelected.emit(item);
    }

    onClear(): void {
        this.selectedItem = null;
        this.onChange(null);
        this.cleared.emit();
        this.resetSearch();
    }

    private resetSearch(): void {
        // Cancel any in-progress search
        if (this.isSearching) {
            this.cancelSearch$.next();
        }
        this.currentSearch = '';
        this.currentPage = 1;
        this.suggestions = [];
        this.hasMore = true;
        this.loading = false;
        this.isSearching = false;
    }

    getDisplayLabel(item: T): string {
        if (!item || !this.dataSource) return '';
        return this.dataSource.displayField(item);
    }

    getSubtitle(item: T): string {
        if (!item || !this.dataSource?.subtitleField) return '';
        return this.dataSource.subtitleField(item);
    }
     

    // ControlValueAccessor implementation
    writeValue(value: T | null): void {
        this.selectedItem = value;
    }

    registerOnChange(fn: (value: T | null) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    /**
     * Programmatically set a value and trigger change
     */
    setValue(value: T | null): void {
        this.selectedItem = value;
        this.onChange(value);
    }

    /**
     * Clear the selection programmatically
     */
    clear(): void {
        this.onClear();
    }

    /**
     * Focus the autocomplete input
     */
    focus(): void {
       // this.autoComplete?.focusInput();
    }
}
