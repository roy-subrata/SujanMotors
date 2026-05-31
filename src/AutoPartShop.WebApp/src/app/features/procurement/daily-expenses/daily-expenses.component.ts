import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService, ConfirmationService } from 'primeng/api';
import {
    DailyExpenseService,
    DailyExpenseResponse,
    CreateDailyExpenseRequest,
    UpdateDailyExpenseRequest,
    ExpenseCategory
} from '../services/daily-expense.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { EXPENSE_PAYMENT_METHODS, PaymentMethodOption } from '../../../shared/constants/payment-methods.constants';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-daily-expenses',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        InputNumberModule,
        SelectModule,
        DatePickerModule,
        TextareaModule,
        ToastModule,
        ConfirmDialogModule,
        CheckboxModule,
        PageContainerComponent,
        PageHeaderComponent
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './daily-expenses.component.html',
    styleUrl: './daily-expenses.component.css'
})
export class DailyExpensesComponent implements OnInit {
    private readonly expenseService = inject(DailyExpenseService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    expenses = signal<DailyExpenseResponse[]>([]);
    categories = signal<ExpenseCategory[]>([]);
    loading = signal(false);

    displayDialog = false;
    isEditMode = false;

    expenseForm: CreateDailyExpenseRequest | UpdateDailyExpenseRequest = this.getEmptyForm();
    selectedExpenseId: string | null = null;

    // Use shared payment methods from centralized constants
    paymentMethods: PaymentMethodOption[] = EXPENSE_PAYMENT_METHODS;

    recurrencePatterns = [
        { label: 'Daily', value: 'DAILY' },
        { label: 'Weekly', value: 'WEEKLY' },
        { label: 'Monthly', value: 'MONTHLY' },
        { label: 'Yearly', value: 'YEARLY' }
    ];

    get currencyCode(): string {
        return this.currencyService.selectedCurrency();
    }

    get currencyLocale(): string {
        return this.currencyService.getSelectedCurrencyLocale();
    }

    ngOnInit(): void {
        this.loadExpenses();
        this.loadCategories();
    }

    loadExpenses(): void {
        this.loading.set(true);
        this.expenseService.getAll().subscribe({
            next: (expenses) => {
                this.expenses.set(expenses);
                this.loading.set(false);
            },
            error: (error) => {
                console.error('Error loading expenses:', error);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load expenses'
                });
                this.loading.set(false);
            }
        });
    }

    loadCategories(): void {
        this.expenseService.getCategories().subscribe({
            next: (categories) => {
                this.categories.set(categories);
            },
            error: (error) => {
                console.error('Error loading categories:', error);
            }
        });
    }

    openNewExpenseDialog(): void {
        this.isEditMode = false;
        this.expenseForm = this.getEmptyForm();
        this.selectedExpenseId = null;
        this.displayDialog = true;
    }

    editExpense(expense: DailyExpenseResponse): void {
        this.isEditMode = true;
        this.selectedExpenseId = expense.id;
        this.expenseForm = {
            expenseDate: expense.expenseDate,
            category: expense.category,
            amount: expense.amount,
            description: expense.description,
            paymentMethod: expense.paymentMethod,
            vendorName: expense.vendorName,
            referenceNumber: expense.referenceNumber,
            notes: expense.notes,
            isRecurring: expense.isRecurring,
            recurrencePattern: expense.recurrencePattern
        };
        this.displayDialog = true;
    }

    saveExpense(): void {
        if (!this.validateForm()) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please fill in all required fields'
            });
            return;
        }

        this.loading.set(true);

        if (this.isEditMode && this.selectedExpenseId) {
            this.expenseService.update(this.selectedExpenseId, this.expenseForm as UpdateDailyExpenseRequest)
                .subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Expense updated successfully'
                        });
                        this.displayDialog = false;
                        this.loadExpenses();
                    },
                    error: (error) => {
                        console.error('Error updating expense:', error);
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Failed to update expense'
                        });
                        this.loading.set(false);
                    }
                });
        } else {
            this.expenseService.create(this.expenseForm as CreateDailyExpenseRequest)
                .subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Expense created successfully'
                        });
                        this.displayDialog = false;
                        this.loadExpenses();
                    },
                    error: (error) => {
                        console.error('Error creating expense:', error);
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Failed to create expense'
                        });
                        this.loading.set(false);
                    }
                });
        }
    }

    deleteExpense(expense: DailyExpenseResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete this expense (${expense.description})?`,
            header: 'Confirm Delete',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.expenseService.delete(expense.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Expense deleted successfully'
                        });
                        this.loadExpenses();
                    },
                    error: (error) => {
                        console.error('Error deleting expense:', error);
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Failed to delete expense'
                        });
                    }
                });
            }
        });
    }

    validateForm(): boolean {
        return !!(
            this.expenseForm.expenseDate &&
            this.expenseForm.category &&
            this.expenseForm.amount > 0 &&
            this.expenseForm.description &&
            this.expenseForm.paymentMethod
        );
    }

    getEmptyForm(): CreateDailyExpenseRequest {
        return {
            expenseDate: new Date().toISOString().split('T')[0],
            category: '',
            amount: 0,
            description: '',
            paymentMethod: 'CASH',
            vendorName: '',
            referenceNumber: '',
            notes: '',
            isRecurring: false,
            recurrencePattern: ''
        };
    }

    getCategoryLabel(categoryValue: string): string {
        const category = this.categories().find(c => c.value === categoryValue);
        return category ? category.label : categoryValue;
    }

    getCategoryIcon(categoryValue: string): string {
        const category = this.categories().find(c => c.value === categoryValue);
        return category ? category.icon : 'pi-tag';
    }

    formatCurrency(value: number): string {
        return this.currencyService.formatCurrency(value, this.currencyService.selectedCurrency());
    }

    formatDate(dateString: string): string {
        return new Date(dateString).toLocaleDateString();
    }
}
