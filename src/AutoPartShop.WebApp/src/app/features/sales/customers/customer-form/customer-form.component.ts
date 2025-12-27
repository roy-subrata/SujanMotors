import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CustomerService, CreateCustomerRequest } from '../../services/customer.service';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';
@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    DatePickerModule,
    CardModule,
    ToastModule,
    TextareaModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './customer-form.component.html',
  styleUrls: ['./customer-form.component.css']
})
export class CustomerFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly customerService = inject(CustomerService);
  private readonly messageService = inject(MessageService);
  private readonly codeGenerationService = inject(CodeGenerationService);

  customerForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'edit' | 'view'>('create');
  customerId = signal<string | null>(null);
  generatingCode = signal(false);

  customerTypes = [
    { label: 'Retail', value: 'RETAIL' },
    { label: 'Wholesale', value: 'WHOLESALE' },
    { label: 'Corporate', value: 'CORPORATE' },
    { label: 'Distributor', value: 'DISTRIBUTOR' }
  ];

  countries = [
    { label: 'India', value: 'India' },
    { label: 'USA', value: 'USA' },
    { label: 'Canada', value: 'Canada' },
    { label: 'UK', value: 'UK' },
    { label: 'Germany', value: 'Germany' },
    { label: 'France', value: 'France' },
    { label: 'China', value: 'China' },
    { label: 'Japan', value: 'Japan' },
    { label: 'Australia', value: 'Australia' },
    { label: 'Mexico', value: 'Mexico' }
  ];

  ngOnInit(): void {
    this.initializeForm();

    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      const mode = params['mode'];

      if (id) {
        this.customerId.set(id);
        this.mode.set(mode === 'view' ? 'view' : 'edit');
        this.loadCustomer(id);
      } else {
        // Create mode - generate customer code automatically
        this.generateCustomerCode();
      }
    });

    if (this.mode() === 'view') {
      this.customerForm.disable();
    }
  }

  generateCustomerCode(): void {
    this.generatingCode.set(true);
    this.codeGenerationService.generateCustomerCode().subscribe({
      next: (code) => {
        this.customerForm.patchValue({ customerCode: code });
        this.generatingCode.set(false);
      },
      error: (err) => {
        console.error('Failed to generate customer code:', err);
        this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Could not auto-generate code. Please enter manually.' });
        this.generatingCode.set(false);
      }
    });
  }

  initializeForm(): void {
    this.customerForm = this.fb.group({
      customerCode: ['', [Validators.required, Validators.minLength(3)]],
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      companyName: [''],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required]],
      alternatePhone: [''],
      billingAddress: ['', [Validators.required]],
      shippingAddress: [''],
      city: ['', [Validators.required]],
      state: ['', [Validators.required]],
      postalCode: ['', [Validators.required]],
      country: ['India', [Validators.required]],
      dateOfBirth: [null, [Validators.required]],
      customerType: ['RETAIL', [Validators.required]],
      creditLimit: [0, [Validators.min(0)]],
      taxId: [''],
      primaryContactPerson: [''],
      notes: ['']
    });
  }

  loadCustomer(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.customerService.getCustomerById(id).subscribe({
      next: (customer: any) => {
        this.customerForm.patchValue({
          customerCode: customer.customerCode,
          firstName: customer.firstName,
          lastName: customer.lastName,
          companyName: customer.companyName,
          email: customer.email,
          phone: customer.phone,
          alternatePhone: customer.alternatePhone,
          billingAddress: customer.billingAddress,
          shippingAddress: customer.shippingAddress,
          city: customer.city,
          state: customer.state,
          postalCode: customer.postalCode,
          country: customer.country,
          dateOfBirth: customer.dateOfBirth ? new Date(customer.dateOfBirth) : null,
          customerType: customer.customerType,
          creditLimit: customer.creditLimit,
          taxId: customer.taxId,
          primaryContactPerson: customer.primaryContactPerson,
          notes: customer.notes
        });
        this.loading.set(false);

        if (this.mode() === 'view') {
          this.customerForm.disable();
        }
      },
      error: (err: any) => {
        this.error.set('Failed to load customer');
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load customer' });
        console.error('Error loading customer:', err);
      }
    });
  }

  onSubmit(): void {
    if (this.customerForm.invalid) {
      Object.keys(this.customerForm.controls).forEach(key => {
        const control = this.customerForm.get(key);
        if (control?.invalid) {
          control.markAsTouched();
        }
      });
      this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please fill all required fields correctly' });
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const formValue = this.customerForm.value;
    const dateOfBirth = formValue.dateOfBirth instanceof Date 
      ? formValue.dateOfBirth.toISOString().split('T')[0]
      : formValue.dateOfBirth;

    const request: CreateCustomerRequest = {
      customerCode: formValue.customerCode,
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      companyName: formValue.companyName,
      email: formValue.email,
      phone: formValue.phone,
      alternatePhone: formValue.alternatePhone,
      billingAddress: formValue.billingAddress,
      shippingAddress: formValue.shippingAddress || formValue.billingAddress,
      city: formValue.city,
      state: formValue.state,
      postalCode: formValue.postalCode,
      country: formValue.country,
      dateOfBirth: dateOfBirth,
      customerType: formValue.customerType,
      creditLimit: formValue.creditLimit,
      taxId: formValue.taxId,
      primaryContactPerson: formValue.primaryContactPerson,
      notes: formValue.notes
    };

    const operation = this.mode() === 'edit' && this.customerId()
      ? this.customerService.updateCustomer(this.customerId()!, request)
      : this.customerService.createCustomer(request);

    operation.subscribe({
      next: () => {
        this.messageService.add({ 
          severity: 'success', 
          summary: 'Success', 
          detail: `Customer ${this.mode() === 'edit' ? 'updated' : 'created'} successfully!` 
        });
        setTimeout(() => {
          this.router.navigate(['/sales/customers']);
        }, 1000);
      },
      error: (err: any) => {
        this.error.set(`Failed to ${this.mode() === 'edit' ? 'update' : 'create'} customer`);
        this.saving.set(false);
        this.messageService.add({ 
          severity: 'error', 
          summary: 'Error', 
          detail: `Failed to ${this.mode() === 'edit' ? 'update' : 'create'} customer` 
        });
        console.error(`Error ${this.mode() === 'edit' ? 'updating' : 'creating'} customer:`, err);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/sales/customers']);
  }

  getPageTitle(): string {
    switch (this.mode()) {
      case 'create': return 'Add New Customer';
      case 'edit': return 'Edit Customer';
      case 'view': return 'Customer Details';
      default: return 'Customer';
    }
  }

  getPageSubtitle(): string {
    switch (this.mode()) {
      case 'create': return 'Fill in the details to register a new customer';
      case 'edit': return 'Update customer information';
      case 'view': return 'View customer details and account information';
      default: return '';
    }
  }
}
