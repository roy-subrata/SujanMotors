import { Component, OnInit, inject, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService, ConfirmationService } from 'primeng/api';
import { SalesOrderService, CreateSalesOrderRequest, SalesOrderResponse } from '../../services/sales-order.service';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { PartService, PartResponse } from '../../../inventory/services/part.service';
import { TechnicianService, TechnicianResponse } from '../../services/technician.service';
import { UnitService, UnitResponse } from '../../../inventory/services/unit.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { CurrencySelectorComponent } from '../../../../shared/components/currency-selector/currency-selector.component';

@Component({
  selector: 'app-sales-order-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AutoCompleteModule,
    SelectModule,
    CurrencySelectorComponent,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    CardModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    TooltipModule,
    DatePickerModule
  ],
  providers: [MessageService, ConfirmationService],
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
  private readonly unitService = inject(UnitService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  salesOrderForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'edit' | 'view'>('create');
  salesOrderId = signal<string | null>(null);
  currentSO: SalesOrderResponse | null = null;

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

  // Units
  units = signal<UnitResponse[]>([]);
  loadingUnits = signal(false);
  // Map to store compatible units for each part (keyed by part ID)
  compatibleUnitsMap = new Map<string, UnitResponse[]>();

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

  grandTotal = computed(() => this.subTotal());

  ngOnInit(): void {
    this.initializeForm();
    this.loadCustomers();
    this.loadTechnicians();
    this.loadParts();
    this.loadUnits();

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
    this.customerService.getCustomers({
                search: '',
                pageNumber: 1,
                pageSize: 100
            }).subscribe({
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

  loadUnits(): void {
    this.loadingUnits.set(true);
    this.unitService.getActiveUnits().subscribe({
      next: (units) => {
        this.units.set(units);
        this.loadingUnits.set(false);
      },
      error: (err: any) => {
        console.error('Error loading units:', err);
        this.loadingUnits.set(false);
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

    // Load compatible units for the selected part
    if (part.unitId) {
      this.unitService.getCompatibleUnits(part.unitId).subscribe({
        next: (compatibleUnits) => {
          this.compatibleUnitsMap.set(part.id, compatibleUnits);

          // Automatically set the unit to the part's base unit if not already set
          if (!line.get('unitId')?.value) {
            line.patchValue({ unitId: part.unitId });
          }
        },
        error: (err) => {
          console.error('Error loading compatible units:', err);
          // Fallback to all units if error occurs
          this.compatibleUnitsMap.set(part.id, this.units());
        }
      });
    } else {
      // Part has no base unit, allow all units
      this.compatibleUnitsMap.set(part.id, this.units());
    }
  }

  /**
   * Get compatible units for a specific part
   */
  getCompatibleUnitsForPart(partId: string | null): UnitResponse[] {
    if (!partId) return this.units();
    return this.compatibleUnitsMap.get(partId) || this.units();
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
    // Get default currency from service
    const defaultCurrency = this.currencyService.selectedCurrency() || 'BDT';

    this.salesOrderForm = this.fb.group({
      customerName: ['', [Validators.required, Validators.minLength(2)]],
      customerEmail: ['', [Validators.required, Validators.email]],
      customerPhone: ['', [Validators.required]],
      customerCity: ['', [Validators.required]],
      technicianId: [null],
      technicianName: [null],
      deliveryDate: [null, [Validators.required]],
      currency: [defaultCurrency, [Validators.required]],
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
      unitId: [data?.unitId || null],  // Optional unit selection
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
        // Store the current SO for later use
        this.currentSO = order;

        // Set customer ID for autocomplete
        this.selectedCustomerId = order.customerId;

        // Set technician ID if available
        if (order.technicianId) {
          this.selectedTechnicianId = order.technicianId;
        }

        this.salesOrderForm.patchValue({
          customerName: order.customerName,
          customerEmail: order.customerEmail,
          customerPhone: order.customerPhone,
          customerCity: order.customerCity,
          deliveryDate: order.deliveryDate ? new Date(order.deliveryDate) : null,
          notes: order.notes
        });

        // Clear and add lines
        this.lines.clear();
        order.lines.forEach((line, index) => {
          this.lines.push(this.createLine({
            partId: line.partId,
            unitId: line.unitId,
            quantity: line.quantity,
            unitPrice: line.unitPrice,
            discount: line.discount
          }));

          // Set selected part ID for autocomplete
          this.selectedPartIds[index] = line.partId;

          // Initialize autocomplete arrays for this line
          this.filteredParts[index] = this.parts();
          this.partSearchTerms[index] = '';
          this.showPartDropdown[index] = false;
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
    // Convert Date object to ISO string for API
    const deliveryDate = formValue.deliveryDate instanceof Date
      ? formValue.deliveryDate.toISOString().split('T')[0]
      : formValue.deliveryDate;

    const request: CreateSalesOrderRequest = {
      customerId: this.selectedCustomerId,
      customerName: formValue.customerName,
      customerEmail: formValue.customerEmail,
      customerPhone: formValue.customerPhone,
      customerCity: formValue.customerCity,
      technicianId: this.selectedTechnicianId || undefined,
      technicianName: formValue.technicianName || undefined,
      deliveryDate: deliveryDate,
      notes: formValue.notes,
      lines: formValue.lines.map((line: any) => ({
        partId: line.partId,
        unitId: line.unitId,
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

  printProformaInvoice(): void {
    // Validate form first
    if (this.salesOrderForm.invalid || !this.selectedCustomerId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Incomplete Form',
        detail: 'Please complete all required fields before printing'
      });
      return;
    }

    // Get form data
    const formValue = this.salesOrderForm.value;
    const customer = this.customers().find(c => c.id === this.selectedCustomerId);
    const technician = this.technicians().find(t => t.id === this.selectedTechnicianId);

    // Build proforma invoice HTML
    const printWindow = window.open('', '_blank', 'width=800,height=600');
    if (!printWindow) {
      this.messageService.add({
        severity: 'error',
        summary: 'Print Failed',
        detail: 'Please allow pop-ups to print the proforma invoice'
      });
      return;
    }

    const currencyCode = formValue.currency || 'BDT';
    const today = new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });

    let lineItemsHTML = '';
    this.lines.controls.forEach((line, index) => {
      const partId = this.selectedPartIds[index];
      const part = this.parts().find(p => p.id === partId);
      const quantity = line.get('quantity')?.value || 0;
      const unitPrice = line.get('unitPrice')?.value || 0;
      const discount = line.get('discount')?.value || 0;
      const lineTotal = quantity * unitPrice * (1 - discount / 100);

      lineItemsHTML += `
        <tr>
          <td style="padding: 8px; border: 1px solid #ddd;">${index + 1}</td>
          <td style="padding: 8px; border: 1px solid #ddd;">${part?.name || 'N/A'}</td>
          <td style="padding: 8px; border: 1px solid #ddd;">${part?.partNumber || '-'}</td>
          <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">${quantity}</td>
          <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">${this.formatCurrency(unitPrice)}</td>
          <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">${discount}%</td>
          <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">${this.formatCurrency(lineTotal)}</td>
        </tr>
      `;
    });

    const htmlContent = `
      <!DOCTYPE html>
      <html>
      <head>
        <title>Proforma Invoice</title>
        <style>
          body { font-family: Arial, sans-serif; margin: 20px; }
          .header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }
          .header h1 { margin: 0; color: #333; }
          .header h2 { margin: 5px 0; color: #666; font-size: 24px; }
          .info-section { margin-bottom: 20px; }
          .info-row { display: flex; justify-content: space-between; margin-bottom: 15px; }
          .info-block { flex: 1; }
          .info-block h3 { margin: 0 0 10px 0; color: #333; font-size: 14px; border-bottom: 1px solid #ddd; padding-bottom: 5px; }
          table { width: 100%; border-collapse: collapse; margin-top: 20px; }
          th { background-color: #f0f0f0; padding: 10px; border: 1px solid #ddd; text-align: left; }
          td { padding: 8px; border: 1px solid #ddd; }
          .totals { margin-top: 20px; text-align: right; }
          .totals table { width: 300px; margin-left: auto; }
          .totals td { padding: 8px; }
          .totals .grand-total { font-weight: bold; font-size: 18px; background-color: #f0f0f0; }
          .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; text-align: center; color: #666; font-size: 12px; }
          .watermark { text-align: center; margin-top: 20px; color: #999; font-style: italic; }
          @media print {
            body { margin: 0; }
            .no-print { display: none; }
          }
        </style>
      </head>
      <body>
        <div class="header">
          <h1>Sujan Motors</h1>
          <h2>PROFORMA INVOICE</h2>
          <p style="margin: 5px 0; color: #666;">Date: ${today}</p>
        </div>

        <div class="info-section">
          <div class="info-row">
            <div class="info-block">
              <h3>Customer Information</h3>
              <p><strong>Name:</strong> ${customer?.fullName || 'N/A'}</p>
              <p><strong>Phone:</strong> ${customer?.phone || 'N/A'}</p>
              <p><strong>Email:</strong> ${customer?.email || 'N/A'}</p>
            </div>
            ${technician ? `
            <div class="info-block">
              <h3>Technician</h3>
              <p><strong>Name:</strong> ${technician.name}</p>
              <p><strong>Phone:</strong> ${technician.phone || 'N/A'}</p>
            </div>
            ` : ''}
          </div>
        </div>

        <table>
          <thead>
            <tr>
              <th style="width: 5%;">#</th>
              <th style="width: 35%;">Item Description</th>
              <th style="width: 15%;">Part Number</th>
              <th style="width: 10%; text-align: right;">Qty</th>
              <th style="width: 15%; text-align: right;">Unit Price</th>
              <th style="width: 10%; text-align: right;">Discount</th>
              <th style="width: 15%; text-align: right;">Total</th>
            </tr>
          </thead>
          <tbody>
            ${lineItemsHTML}
          </tbody>
        </table>

        <div class="totals">
          <table>
            <tr>
              <td>Subtotal:</td>
              <td style="text-align: right;"><strong>${this.formatCurrency(this.subTotal())}</strong></td>
            </tr>
            <tr class="grand-total">
              <td>Grand Total:</td>
              <td style="text-align: right;"><strong>${this.formatCurrency(this.grandTotal())}</strong></td>
            </tr>
          </table>
        </div>

        ${formValue.notes ? `
        <div style="margin-top: 30px;">
          <h3 style="margin: 0 0 10px 0; color: #333; font-size: 14px;">Notes:</h3>
          <p style="color: #666;">${formValue.notes}</p>
        </div>
        ` : ''}

        <div class="watermark">
          <p><strong>THIS IS NOT AN OFFICIAL INVOICE</strong></p>
          <p>This proforma invoice is for estimation purposes only.</p>
          <p>An official invoice will be issued upon order confirmation.</p>
        </div>

        <div class="footer">
          <p>Thank you for choosing Sujan Motors</p>
          <p>For any queries, please contact us</p>
        </div>

        <div class="no-print" style="text-align: center; margin-top: 20px;">
          <button onclick="window.print()" style="padding: 10px 30px; background-color: #4CAF50; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px;">
            Print
          </button>
          <button onclick="window.close()" style="padding: 10px 30px; background-color: #666; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; margin-left: 10px;">
            Close
          </button>
        </div>
      </body>
      </html>
    `;

    printWindow.document.write(htmlContent);
    printWindow.document.close();

    this.messageService.add({
      severity: 'success',
      summary: 'Print Ready',
      detail: 'Proforma invoice opened in new window'
    });
  }

  formatCurrency(amount: number): string {
    const currencyCode = this.salesOrderForm?.get('currency')?.value || 'BDT';
    return this.currencyService.formatCurrency(amount, currencyCode);
  }

  /**
   * Confirm sales order
   */
  confirmSalesOrder(): void {
    if (!this.salesOrderId() || !this.currentSO) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to confirm Sales Order ${this.currentSO.soNumber}? This action will make the order official and binding.`,
      header: 'Confirm Sales Order',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-success',
      accept: () => {
        this.salesOrderService.confirmSalesOrder(this.salesOrderId()!).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Sales Order ${this.currentSO!.soNumber} confirmed successfully`
            });
            // Reload the sales order to get updated status
            this.loadSalesOrder(this.salesOrderId()!);
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to confirm sales order'
            });
            console.error('Error confirming sales order:', error);
          }
        });
      }
    });
  }

  /**
   * Format date
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }

  /**
   * Get status badge severity
   */
  getStatusSeverity(status: string): string {
    const severityMap: Record<string, string> = {
      DRAFT: 'secondary',
      CONFIRMED: 'info',
      PARTIALLY_SHIPPED: 'warning',
      SHIPPED: 'primary',
      DELIVERED: 'success',
      CANCELLED: 'danger'
    };
    return severityMap[status] || 'secondary';
  }
}
