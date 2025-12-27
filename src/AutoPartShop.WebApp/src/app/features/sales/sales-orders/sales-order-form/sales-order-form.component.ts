import { Component, OnInit, inject, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SalesOrderService, CreateSalesOrderRequest } from '../../services/sales-order.service';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { PartService, PartResponse } from '../../../inventory/services/part.service';
import { TechnicianService, TechnicianResponse } from '../../services/technician.service';

@Component({
  selector: 'app-sales-order-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, AutoCompleteModule],
  templateUrl: './sales-order-form.component.html',
  styleUrls: ['./sales-order-form.component.css']
})
export class SalesOrderFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly salesOrderService = inject(SalesOrderService);
  private readonly customerService = inject(CustomerService);
  private readonly partService = inject(PartService);
  private readonly technicianService = inject(TechnicianService);

  salesOrderForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'edit' | 'view'>('create');
  salesOrderId = signal<string | null>(null);

  // Customer autocomplete
  customers = signal<CustomerResponse[]>([]);
  filteredCustomers = signal<CustomerResponse[]>([]);
  loadingCustomers = signal(false);
  customerSearchTerm = '';
  showCustomerDropdown = false;
  selectedCustomerId = '';

  // Technician autocomplete
  technicians = signal<TechnicianResponse[]>([]);
  filteredTechnicians = signal<TechnicianResponse[]>([]);
  loadingTechnicians = signal(false);
  selectedTechnicianId = '';

  // Parts autocomplete
  parts = signal<PartResponse[]>([]);
  filteredParts: { [lineIndex: number]: PartResponse[] } = {};
  loadingParts = signal(false);
  partSearchTerms: { [lineIndex: number]: string } = {};
  showPartDropdown: { [lineIndex: number]: boolean } = {};
  selectedPartIds: { [lineIndex: number]: string } = {};

  // Computed values
  subTotal = computed(() => {
    if (!this.salesOrderForm) return 0;
    const lines = this.lines.controls;
    return lines.reduce((sum, line) => {
      const qty = line.get('quantity')?.value || 0;
      const price = line.get('unitPrice')?.value || 0;
      const discount = line.get('discount')?.value || 0;
      return sum + (qty * price * (1 - discount / 100));
    }, 0);
  });

  taxAmount = computed(() => this.subTotal() * 0.1); // 10% tax
  grandTotal = computed(() => this.subTotal() + this.taxAmount());

  ngOnInit(): void {
    this.initializeForm();
    this.loadCustomers();
    this.loadTechnicians();
    this.loadParts();

    // Initialize first line's autocomplete
    if (this.mode() === 'create' && this.lines.length > 0) {
      this.filteredParts[0] = [];
      this.partSearchTerms[0] = '';
      this.showPartDropdown[0] = false;
      this.selectedPartIds[0] = '';
    }

    // Check route params
    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      const mode = params['mode'];

      if (id) {
        this.salesOrderId.set(id);
        this.mode.set(mode === 'view' ? 'view' : 'edit');
        this.loadSalesOrder(id);
      }
    });

    if (this.mode() === 'view') {
      this.salesOrderForm.disable();
    }
  }

  loadCustomers(): void {
    this.loadingCustomers.set(true);
    this.customerService.getCustomers(1, 1000).subscribe({
      next: (response) => {
        this.customers.set(response.data);
        this.filteredCustomers.set(response.data);
        this.loadingCustomers.set(false);
      },
      error: (err: any) => {
        console.error('Error loading customers:', err);
        this.loadingCustomers.set(false);
      }
    });
  }

  loadTechnicians(): void {
    this.loadingTechnicians.set(true);
    this.technicianService.getAllTechnicians().subscribe({
      next: (technicians) => {
        // Filter only active technicians
        const activeTechnicians = technicians.filter(t => t.status === 'ACTIVE');
        this.technicians.set(activeTechnicians);
        this.filteredTechnicians.set(activeTechnicians);
        this.loadingTechnicians.set(false);
      },
      error: (err: any) => {
        console.error('Error loading technicians:', err);
        this.loadingTechnicians.set(false);
      }
    });
  }

  loadParts(): void {
    this.loadingParts.set(true);
    this.partService.getActiveParts().subscribe({
      next: (parts) => {
        this.parts.set(parts);
        this.loadingParts.set(false);
      },
      error: (err: any) => {
        console.error('Error loading parts:', err);
        this.loadingParts.set(false);
      }
    });
  }

  onCustomerSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.customerSearchTerm = input.value.toLowerCase();
    this.showCustomerDropdown = true;

    if (!this.customerSearchTerm) {
      this.filteredCustomers.set(this.customers());
      this.clearCustomerSelection();
      return;
    }

    const filtered = this.customers().filter(c =>
      c.firstName.toLowerCase().includes(this.customerSearchTerm) ||
      c.lastName.toLowerCase().includes(this.customerSearchTerm) ||
      c.email.toLowerCase().includes(this.customerSearchTerm) ||
      c.customerCode.toLowerCase().includes(this.customerSearchTerm)
    );
    this.filteredCustomers.set(filtered);
  }

  selectCustomer(customer: CustomerResponse): void {
    this.selectedCustomerId = customer.id;
    this.customerSearchTerm = `${customer.firstName} ${customer.lastName} (${customer.customerCode})`;
    this.showCustomerDropdown = false;

    // Auto-fill customer details
    this.salesOrderForm.patchValue({
      customerName: `${customer.firstName} ${customer.lastName}`,
      customerEmail: customer.email,
      customerPhone: customer.phone,
      customerCity: customer.city
    });
  }

  clearCustomerSelection(): void {
    this.selectedCustomerId = '';
    this.salesOrderForm.patchValue({
      customerName: '',
      customerEmail: '',
      customerPhone: '',
      customerCity: ''
    });
  }

  onTechnicianFilter(event: any): void {
    const query = event.query.toLowerCase();

    if (!query) {
      this.filteredTechnicians.set(this.technicians());
      return;
    }

    const filtered = this.technicians().filter(t =>
      t.name.toLowerCase().includes(query) ||
      t.technicianCode.toLowerCase().includes(query) ||
      t.phone.toLowerCase().includes(query) ||
      t.shopName.toLowerCase().includes(query)
    );
    this.filteredTechnicians.set(filtered);
  }

  selectTechnician(event: any): void {
    const technician = event as TechnicianResponse;
    this.selectedTechnicianId = technician.id;

    // Store technician info in form
    this.salesOrderForm.patchValue({
      technicianId: technician.id,
      technicianName: technician.name
    });
  }

  clearTechnicianSelection(): void {
    this.selectedTechnicianId = '';
    this.salesOrderForm.patchValue({
      technicianId: null,
      technicianName: null
    });
  }

  onPartSearchInput(event: Event, lineIndex: number): void {
    const input = event.target as HTMLInputElement;
    this.partSearchTerms[lineIndex] = input.value.toLowerCase();
    this.showPartDropdown[lineIndex] = true;

    if (!this.partSearchTerms[lineIndex]) {
      this.filteredParts[lineIndex] = this.parts();
      return;
    }

    const filtered = this.parts().filter(p =>
      p.name.toLowerCase().includes(this.partSearchTerms[lineIndex]) ||
      p.partNumber.toLowerCase().includes(this.partSearchTerms[lineIndex]) ||
      p.sku.toLowerCase().includes(this.partSearchTerms[lineIndex])
    );
    this.filteredParts[lineIndex] = filtered;
  }

  selectPart(part: PartResponse, lineIndex: number): void {
    this.selectedPartIds[lineIndex] = part.id;
    this.partSearchTerms[lineIndex] = `${part.name} (${part.partNumber})`;
    this.showPartDropdown[lineIndex] = false;

    // Auto-fill part details
    const line = this.lines.at(lineIndex);
    line.patchValue({
      partId: part.id,
      partName: part.name,
      unitPrice: part.sellingPrice
    });
  }

  clearPartSelection(lineIndex: number): void {
    this.selectedPartIds[lineIndex] = '';
    this.partSearchTerms[lineIndex] = '';
    const line = this.lines.at(lineIndex);
    line.patchValue({
      partId: '',
      partName: '',
      unitPrice: 0
    });
  }

  getFilteredParts(lineIndex: number): PartResponse[] {
    return this.filteredParts[lineIndex] || this.parts();
  }

  getPartSearchTerm(lineIndex: number): string {
    return this.partSearchTerms[lineIndex] || '';
  }

  isPartDropdownVisible(lineIndex: number): boolean {
    return this.showPartDropdown[lineIndex] || false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.customer-autocomplete')) {
      this.showCustomerDropdown = false;
    }
    // Close all part dropdowns
    Object.keys(this.showPartDropdown).forEach(key => {
      if (!target.closest(`.part-autocomplete-${key}`)) {
        this.showPartDropdown[+key] = false;
      }
    });
  }

  initializeForm(): void {
    this.salesOrderForm = this.fb.group({
      customerName: ['', [Validators.required, Validators.minLength(2)]],
      customerEmail: ['', [Validators.required, Validators.email]],
      customerPhone: ['', [Validators.required]],
      customerCity: ['', [Validators.required]],
      technicianId: [null],
      technicianName: [null],
      deliveryDate: ['', [Validators.required]],
      notes: [''],
      lines: this.fb.array([])
    });

    // Add one empty line by default
    if (this.mode() === 'create') {
      this.addLine();
    }
  }

  get lines(): FormArray {
    return this.salesOrderForm.get('lines') as FormArray;
  }

  createLine(data?: any): FormGroup {
    return this.fb.group({
      partId: [data?.partId || '', [Validators.required]],
      partName: [data?.partName || ''],
      quantity: [data?.quantity || 1, [Validators.required, Validators.min(1)]],
      unitPrice: [data?.unitPrice || 0, [Validators.required, Validators.min(0)]],
      discount: [data?.discount || 0, [Validators.min(0), Validators.max(100)]]
    });
  }

  addLine(): void {
    const newIndex = this.lines.length;
    this.lines.push(this.createLine());
    // Initialize filtered parts for the new line
    this.filteredParts[newIndex] = this.parts();
    this.partSearchTerms[newIndex] = '';
    this.showPartDropdown[newIndex] = false;
    this.selectedPartIds[newIndex] = '';
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.lines.removeAt(index);
    }
  }

  getLineTotal(index: number): number {
    const line = this.lines.at(index);
    const qty = line.get('quantity')?.value || 0;
    const price = line.get('unitPrice')?.value || 0;
    const discount = line.get('discount')?.value || 0;
    return qty * price * (1 - discount / 100);
  }

  loadSalesOrder(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.salesOrderService.getSalesOrderById(id).subscribe({
      next: (order) => {
        this.salesOrderForm.patchValue({
          customerName: order.customerName,
          customerEmail: order.customerEmail,
          customerPhone: order.customerPhone,
          customerCity: order.customerCity,
          deliveryDate: order.deliveryDate.split('T')[0],
          notes: order.notes
        });

        // Clear and add lines
        this.lines.clear();
        order.lines.forEach(line => {
          this.lines.push(this.createLine({
            partId: line.partId,
            quantity: line.quantity,
            unitPrice: line.unitPrice,
            discount: line.discount
          }));
        });

        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to load sales order');
        this.loading.set(false);
        console.error('Error loading sales order:', err);
      }
    });
  }

  onSubmit(): void {
    // Validate customer selection
    if (!this.selectedCustomerId) {
      this.error.set('Please select a customer from the dropdown');
      return;
    }

    // Validate form
    if (this.salesOrderForm.invalid) {
      Object.keys(this.salesOrderForm.controls).forEach(key => {
        const control = this.salesOrderForm.get(key);
        if (control?.invalid) {
          control.markAsTouched();
        }
      });
      this.lines.controls.forEach(line => {
        Object.keys(line.value).forEach(key => {
          const control = line.get(key);
          if (control?.invalid) {
            control.markAsTouched();
          }
        });
      });
      this.error.set('Please fill in all required fields');
      return;
    }

    // Validate all parts are selected
    const invalidLines: number[] = [];
    this.lines.controls.forEach((line, index) => {
      const partId = line.get('partId')?.value;
      if (!partId || !this.selectedPartIds[index]) {
        invalidLines.push(index + 1);
      }
    });

    if (invalidLines.length > 0) {
      this.error.set(`Please select parts for line item(s): ${invalidLines.join(', ')}`);
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const formValue = this.salesOrderForm.value;
    const request: CreateSalesOrderRequest = {
      customerId: this.selectedCustomerId,
      customerName: formValue.customerName,
      customerEmail: formValue.customerEmail,
      customerPhone: formValue.customerPhone,
      customerCity: formValue.customerCity,
      technicianId: this.selectedTechnicianId || undefined,
      technicianName: formValue.technicianName || undefined,
      deliveryDate: formValue.deliveryDate,
      notes: formValue.notes,
      lines: formValue.lines.map((line: any) => ({
        partId: line.partId,
        quantity: line.quantity,
        unitPrice: line.unitPrice,
        discount: line.discount || 0
      }))
    };

    const operation = this.mode() === 'edit' && this.salesOrderId()
      ? this.salesOrderService.updateSalesOrder(this.salesOrderId()!, request)
      : this.salesOrderService.createSalesOrder(request);

    operation.subscribe({
      next: () => {
        alert(`Sales order ${this.mode() === 'edit' ? 'updated' : 'created'} successfully!`);
        this.router.navigate(['/sales/sales-orders']);
      },
      error: (err) => {
        let errorMessage = `Failed to ${this.mode() === 'edit' ? 'update' : 'create'} sales order`;

        // Try to extract more detailed error message
        if (err.error?.message) {
          errorMessage = err.error.message;
        } else if (err.error?.errors) {
          const errors = Object.values(err.error.errors).flat();
          errorMessage = errors.join(', ');
        } else if (err.message) {
          errorMessage = err.message;
        }

        this.error.set(errorMessage);
        this.saving.set(false);
        console.error(`Error ${this.mode() === 'edit' ? 'updating' : 'creating'} sales order:`, err);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/sales/sales-orders']);
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
