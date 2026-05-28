import { Component, Input, Output, EventEmitter, ContentChild, TemplateRef, forwardRef, ViewChild, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { AutoCompleteModule, AutoComplete } from 'primeng/autocomplete';
import { Observable, Subject, Subscription } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';

export interface LazyRequest {
    search: string;
    pageNumber: number;
    pageSize: number;
}

export interface LazyResponse<T> {
    items: T[];
    totalCount: number;
}

@Component({
    selector: 'app-lazy-autocomplete',
    standalone: true,
    imports: [CommonModule, FormsModule, AutoCompleteModule],
    templateUrl: './lazy-autocomplete.component.html',
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => LazyAutocompleteComponent),
            multi: true
        }
    ]
})
export class LazyAutocompleteComponent<T = any> implements ControlValueAccessor, OnInit, OnDestroy {
    @ViewChild('autoComplete') autoComplete!: AutoComplete;

    value: T | null = null;
    disabled = false;
    private onChange: (value: T | null) => void = () => {};
    private onTouched: () => void = () => {};

    // Subscription management
    private destroy$ = new Subject<void>();
    private currentRequest$: Subscription | null = null;

    /* ================= INPUTS ================= */
    @Input() fetchFn!: (req: LazyRequest) => Observable<LazyResponse<T>>;
    @Input() optionLabel = 'name';
    @Input() placeholder = 'Search';
    @Input() pageSize = 20;
    @Input() itemSize = 48;
    @Input() showClear = true;
    @Input() minLength = 0;
    @Input() scrollHeight = '320px';
    @Input() emptyMessage = 'No results found';
    @Input() loadingMessage = 'Loading...';


    /* ================= OUTPUTS ================= */
    @Output() onItemSelect = new EventEmitter<T>();
    @Output() onClearEvent = new EventEmitter<void>();
    @Output() onError = new EventEmitter<Error>();

    /* ================= TEMPLATE ================= */
    @ContentChild(TemplateRef) itemTemplate?: TemplateRef<any>;

    /* ================= STATE ================= */
    suggestions: T[] = [];
    totalCount = 0;
    loading = false;
    private cachedItems: T[] = [];
    private pageNumber = 1;
    private lastQuery = '';
    private dataLoaded = false;

    /* ================= LIFECYCLE ================= */
    ngOnInit(): void {
        // Validate required inputs
        if (!this.fetchFn) {
            console.warn('LazyAutocompleteComponent: fetchFn is required');
        }
    }

    ngOnDestroy(): void {
        // Cancel any pending request
        this.cancelPendingRequest();

        // Complete the destroy subject
        this.destroy$.next();
        this.destroy$.complete();

        // Clear references
        this.cachedItems = [];
        this.suggestions = [];
    }

    /* ================= ControlValueAccessor ================= */
    writeValue(value: T | null): void {
        this.value = value;
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

    /* ================= EVENTS ================= */
    onSearch(event: { query?: string } | null): void {
        const query = event?.query ?? '';

        // New query - cancel previous request and reset
        if (query !== this.lastQuery) {
            this.cancelPendingRequest();
            this.resetState(query);
            this.fetchData(query);
            return;
        }

        // Same query - use cached if available
        if (this.dataLoaded && this.cachedItems.length > 0) {
            this.suggestions = [...this.cachedItems];
            return;
        }

        // First load
        if (!this.loading) {
            this.fetchData(query);
        }
    }

    onLazyLoad(event: { first?: number; last?: number } | null): void {
        if (!event) return;
        if (this.loading) return;
        // Don't trigger scroll-fetch before the first search/dropdown open
        if (!this.dataLoaded) return;
        // All pages already loaded
        if (this.cachedItems.length >= this.totalCount) return;

        const last = event.last ?? 0;
        const loadThreshold = Math.max(0, this.cachedItems.length - 5);
        if (last >= loadThreshold) {
            this.fetchData(this.lastQuery);
        }
    }

    onDropdownClick(): void {
        if (this.dataLoaded && this.cachedItems.length > 0) {
            this.suggestions = [...this.cachedItems];
        } else if (!this.loading) {
            this.fetchData('');
        }
    }

    onSelect(): void {
        this.onChange(this.value);
        this.onTouched();
        if (this.value) {
            this.onItemSelect.emit(this.value);
        }
    }

    onClearValue(): void {
        this.value = null;
        this.onChange(this.value);
        this.onTouched();
        this.onClearEvent.emit();
        this.resetState('');
    }

    /* ================= PUBLIC METHODS ================= */

    /** Refresh data - clears cache and fetches fresh data */
    refresh(): void {
        this.cancelPendingRequest();
        this.resetState('');
        this.fetchData('');
    }

    /** Clear selection and reset state */
    clear(): void {
        this.onClearValue(); // resetState is called inside onClearValue
    }

    /** Get current loading state */
    isLoading(): boolean {
        return this.loading;
    }

    /** Get total count of items */
    getTotalCount(): number {
        return this.totalCount;
    }

    /** Get number of loaded items */
    getLoadedCount(): number {
        return this.cachedItems.length;
    }

    /* ================= PRIVATE METHODS ================= */

    private fetchData(query: string): void {
        if (this.loading || !this.fetchFn) return;

        this.loading = true;

        // Cancel any previous request
        this.cancelPendingRequest();

        this.currentRequest$ = this.fetchFn({
            search: query,
            pageNumber: this.pageNumber,
            pageSize: this.pageSize
        })
            .pipe(
                takeUntil(this.destroy$),
                finalize(() => {
                    this.loading = false;
                    this.currentRequest$ = null;
                })
            )
            .subscribe({
                next: (res) => {
                    // Validate response
                    const items = res?.items ?? [];
                    const totalCount = res?.totalCount ?? 0;

                    this.cachedItems = [...this.cachedItems, ...items];
                    this.totalCount = totalCount;
                    this.pageNumber++;
                    this.suggestions = [...this.cachedItems];
                    this.dataLoaded = true;
                },
                error: (error: Error) => {
                    console.error('LazyAutocompleteComponent: Error fetching data', error);
                    this.suggestions = [];
                    this.onError.emit(error);
                }
            });
    }

    private cancelPendingRequest(): void {
        if (this.currentRequest$) {
            this.currentRequest$.unsubscribe();
            this.currentRequest$ = null;
            this.loading = false;
        }
    }

    private resetState(query: string): void {
        this.cachedItems = [];
        this.pageNumber = 1;
        this.totalCount = 0;
        this.lastQuery = query;
        this.dataLoaded = false;
        this.suggestions = [];
    }
}
