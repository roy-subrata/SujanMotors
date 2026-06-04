import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { DatePickerModule } from 'primeng/datepicker';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PurchaseReturnService, PurchaseReturnResponse, AvailableLotForReturn } from '../../services/purchase-return.service';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { PartService, PartResponse } from '../../../inventory/services/part.service';
import { CurrencyService } from '../../../../shared/services/currency.service';

@Component({
  selector: 'app-purchase-returns-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TextareaModule,
    Select,
    AutoCompleteModule,
    DatePickerModule,
    TableModule,
    CardModule,
    ToastModule,
    TooltipModule,
    DialogModule,
    CheckboxModule,
    TagModule,
    ConfirmDialogModule
  ],
  templateUrl: './purchase-returns-form.component.html',
  styleUrls: ['./purchase-returns-form.component.css'],
  providers: [MessageService, ConfirmationService]
})
export class PurchaseReturnsFormComponent implements OnInit {
  form: FormGroup;
  isEditing = false;
  isViewMode = false;
  isSubmitting = false;
  returnId: string | null = null;
  filteredPurchaseOrders: PurchaseOrderResponse[] = [];
  purchaseOrders: PurchaseOrderResponse[] = [];
  parts: PartResponse[] = [];
  filteredParts: PartResponse[] = [];
  availablePOItems: any[] = [];
  showItemSelectionDialog = false;
  selectedPOItems: any[] = [];
  selectedPurchaseOrder: PurchaseOrderResponse | null = null;

  // View mode properties
  currentReturn: PurchaseReturnResponse | null = null;

  // Lot selection - map of partId to available lots
  availableLotsMap: Map<string, AvailableLotForReturn[]> = new Map();
  loadingLotsMap: Map<string, boolean> = new Map();

  conditionOptions = [
    { label: 'Unopened', value: 'UNOPENED' },
    { label: 'Opened', value: 'OPENED' },
    { label: 'Damaged', value: 'DAMAGED' },
    { label: 'Defective', value: 'DEFECTIVE' }
  ];

  reasonOptions = [
    { label: 'Defective', value: 'Defective' },
    { label: 'Damaged', value: 'Damaged' },
    { label: 'Wrong Item', value: 'Wrong Item' },
    { label: 'Quality Issue', value: 'Quality Issue' },
    { label: 'Not Needed', value: 'Not Needed' },
    { label: 'Overstock', value: 'Overstock' },
    { label: 'Other', value: 'Other' }
  ];

  private readonly prService = inject(PurchaseReturnService);
  private readonly poService = inject(PurchaseOrderService);
  private readonly partService = inject(PartService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    this.form = this.createForm();
  }

  /**
   * Get current currency code from settings
   */
  get currencyCode(): string {
    return this.currencyService.selectedCurrency();
  }

  ngOnInit(): void {
    this.loadPurchaseOrders();
    this.loadParts();
    this.checkEditMode();
  }

  /**
   * Create form group
   */
  private createForm(): FormGroup {
    return this.fb.group({
      purchaseOrderId: ['', Validators.required],
      returnDate: [new Date(), Validators.required],
      reason: ['', Validators.required],
      notes: [''],
      lineItems: this.fb.array([])
    });
  }

  /**
   * Get line items form array
   */
  get lineItemsArray(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  /**
   * Load purchase orders from API
   */
  private loadPurchaseOrders(): void {
    this.poService.getAllPurchaseOrders().subscribe({
      next: (pos) => {
        // Only show CONFIRMED, PARTIAL, or DELIVERED orders (orders eligible for returns)
        const validOrders = (Array.isArray(pos) ? pos : []).filter(po =>
          po.status === 'CONFIRMED' || po.status === 'PARTIAL' || po.status === 'DELIVERED'
        );
        this.purchaseOrders = validOrders;
        this.filteredPurchaseOrders = validOrders;
      },
      error: (error) => {
        console.error('Error loading purchase orders:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load purchase orders'
        });
      }
    });
  }

  /**
   * Load parts for ID→name lookup (used by getPartName and addPOLineItem helpers)
   */
  private loadParts(): void {
    this.partService.getParts({ search: '', pageNumber: 1, pageSize: 500, isActive: true, flattenVariants: true }).subscribe({
      next: (res) => {
        this.parts = Array.isArray(res.data) ? res.data : [];
      },
      error: (error) => {
        console.error('Error loading parts:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load parts'
        });
      }
    });
  }

  /**
   * Check if editing or viewing existing return
   */
  private checkEditMode(): void {
    this.returnId = this.route.snapshot.queryParamMap.get('id');
    const url = this.router.url;

    if (this.returnId) {
      // Check if it's view mode
      if (url.includes('/view')) {
        this.isViewMode = true;
        this.isEditing = false;
      } else {
        this.isEditing = true;
        this.isViewMode = false;
      }
      this.loadPurchaseReturn(this.returnId);
    }
  }

  /**
   * Load existing purchase return
   */
  private loadPurchaseReturn(id: string): void {
    this.prService.getPurchaseReturnById(id).subscribe({
      next: (pr) => {
        // Store current return for view mode
        this.currentReturn = pr;

        const matchingPO = this.purchaseOrders.find(po => po.id === pr.purchaseOrderId);

        this.form.patchValue({
          purchaseOrderId: matchingPO || pr.purchaseOrderId,
          returnDate: new Date(pr.returnDate),
          reason: pr.reason,
          notes: pr.notes
        });

        // Load line items
        this.lineItemsArray.clear();
        if (pr.lines && pr.lines.length > 0) {
          pr.lines.forEach(line => {
            this.lineItemsArray.push(this.createLineItem(line));
          });
        }

        // Disable form in view mode
        if (this.isViewMode) {
          this.form.disable();
        }
      },
      error: (error) => {
        console.error('Error loading purchase return:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load purchase return'
        });
      }
    });
  }

  /**
   * Create line item form group
   */
  private createLineItem(lineData?: any): FormGroup {
    // Find the part object if we have a partId
    let partObj = null;
    if (lineData?.partId) {
      partObj = this.parts.find(p => p.id === lineData.partId);
      // Load available lots for this part
      this.loadAvailableLotsForPart(lineData.partId);
    }

    return this.fb.group({
      id: [lineData?.id || ''],
      purchaseOrderLineId: [lineData?.purchaseOrderLineId || ''],
      part: [partObj, Validators.required],
      partId: [lineData?.partId || '', Validators.required],
      displayName: [lineData?.displayName || partObj?.name || ''],
      stockLotId: [lineData?.stockLotId || null],  // Optional: specific lot to return from
      quantity: [lineData?.quantity || 1, [Validators.required, Validators.min(1)]],
      rejectedQuantity: [lineData?.rejectedQuantity || 0, Validators.min(0)],
      unitPrice: [lineData?.unitPrice || 0, [Validators.required, Validators.min(0)]],
      condition: [lineData?.condition || 'UNOPENED', Validators.required],
      notes: [lineData?.notes || '']
    });
  }

  /**
   * Load available lots for a part
   */
  loadAvailableLotsForPart(partId: string, forceRefresh = false): void {
    if (!partId) return;
    if (!forceRefresh && this.availableLotsMap.has(partId)) return;

    const supplierId = this.selectedPurchaseOrder?.supplierId;

    this.loadingLotsMap.set(partId, true);
    this.prService.getAvailableLotsForReturn(partId, supplierId).subscribe({
      next: (lots) => {
        this.availableLotsMap.set(partId, lots);
        this.loadingLotsMap.set(partId, false);
      },
      error: (error) => {
        console.error('Error loading available lots for part:', partId, error);
        this.availableLotsMap.set(partId, []);
        this.loadingLotsMap.set(partId, false);
      }
    });
  }

  /**
   * Get available lots for a part (for dropdown)
   */
  getAvailableLotsForPart(partId: string): AvailableLotForReturn[] {
    return this.availableLotsMap.get(partId) || [];
  }

  /**
   * Check if lots are loading for a part
   */
  isLoadingLots(partId: string): boolean {
    return this.loadingLotsMap.get(partId) || false;
  }

  /**
   * Add new line item
   */
  addLineItem(): void {
    this.lineItemsArray.push(this.createLineItem());
  }

  /**
   * Remove line item
   */
  removeLineItem(index: number): void {
    if (this.lineItemsArray.length > 1) {
      this.lineItemsArray.removeAt(index);
    } else {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'Purchase return must have at least one line item'
      });
    }
  }

  /**
   * Filter purchase orders
   */
  filterPurchaseOrders(event: any): void {
    const filtered: PurchaseOrderResponse[] = [];
    const query = event.query;

    if (query && query.trim() !== '') {
      const queryLower = query.toLowerCase();
      for (const po of this.purchaseOrders) {
        if (
          po.poNumber.toLowerCase().includes(queryLower) ||
          po.supplierName?.toLowerCase().includes(queryLower)
        ) {
          filtered.push(po);
        }
      }
    } else {
      for (const po of this.purchaseOrders) {
        filtered.push(po);
      }
    }

    this.filteredPurchaseOrders = filtered.slice(0, 10);
  }

  /**
   * Handle PO selection change
   */
  onPurchaseOrderSelected(event: any): void {
    if (event && event.id) {
      this.poService.getPurchaseOrderById(event.id).subscribe({
        next: (po) => {
          this.selectedPurchaseOrder = po;
          this.availablePOItems = po.lines || [];

          // Clear lot cache when PO (and therefore supplier) changes
          this.availableLotsMap.clear();
          this.loadingLotsMap.clear();

          // Clear existing line items
          this.lineItemsArray.clear();

          this.messageService.add({
            severity: 'info',
            summary: 'Purchase Order Loaded',
            detail: `${this.availablePOItems.length} items available from ${po.supplierName || 'supplier'}. Use "Add Items" or "Add All Items" to continue.`,
            life: 5000
          });
        },
        error: (_error) => {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load PO details' });
        }
      });
    }
  }

  /**
   * Add all items from selected PO
   */
  addAllItemsFromPO(): void {
    const poId = this.form.get('purchaseOrderId')?.value;
    if (!poId || !this.availablePOItems.length) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Purchase Order',
        detail: 'Please select a purchase order first'
      });
      return;
    }

    this.lineItemsArray.clear();
    this.availablePOItems.forEach(poLine => {
      this.addPOLineItem(poLine);
    });

    this.messageService.add({
      severity: 'success',
      summary: 'Items Added',
      detail: `${this.availablePOItems.length} items added to return`
    });
  }

  /**
   * Show dialog to select specific items
   */
  showSelectItemsDialog(): void {
    const poId = this.form.get('purchaseOrderId')?.value;
    if (!poId || !this.availablePOItems.length) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Purchase Order',
        detail: 'Please select a purchase order first'
      });
      return;
    }

    this.selectedPOItems = [];
    this.showItemSelectionDialog = true;
  }

  /**
   * Add selected items from dialog
   */
  confirmItemSelection(): void {
    if (this.selectedPOItems.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Items Selected',
        detail: 'Please select at least one item'
      });
      return;
    }

    this.lineItemsArray.clear();
    this.selectedPOItems.forEach(item => {
      this.addPOLineItem(item);
    });

    this.showItemSelectionDialog = false;
    this.messageService.add({
      severity: 'success',
      summary: 'Items Added',
      detail: `${this.selectedPOItems.length} items added to return`
    });
  }

  /**
   * Cancel item selection
   */
  cancelItemSelection(): void {
    this.showItemSelectionDialog = false;
    this.selectedPOItems = [];
  }

  /**
   * Helper method to add a PO line item to the form
   */
  private addPOLineItem(poLine: any): void {
    const partObj = this.parts.find(p => p.id === poLine.partId);

    // Load available lots for this part
    if (poLine.partId) {
      this.loadAvailableLotsForPart(poLine.partId);
    }

    const lineItem = this.fb.group({
      id: [''],
      purchaseOrderLineId: [poLine.id, Validators.required],
      part: [partObj, Validators.required],
      partId: [poLine.partId, Validators.required],
      displayName: [poLine.displayName || partObj?.name || ''],
      stockLotId: [null],  // Optional: specific lot to return from
      quantity: [poLine.quantity, [Validators.required, Validators.min(1)]],
      rejectedQuantity: [0, Validators.min(0)],
      unitPrice: [poLine.unitPrice || 0, [Validators.required, Validators.min(0)]],
      condition: ['UNOPENED', Validators.required],
      notes: ['']
    });
    this.lineItemsArray.push(lineItem);
  }

  /**
   * Filter parts for autocomplete — DB-backed search per query
   */
  filterParts(event: any, _rowIndex: number): void {
    const query: string = event.query ?? '';
    this.partService.getParts({ search: query, pageNumber: 1, pageSize: 20, isActive: true, flattenVariants: true })
      .subscribe({ next: (res) => { this.filteredParts = res.data ?? []; } });
  }

  /**
   * Handle part selection
   */
  onPartSelected(event: any, rowIndex: number): void {
    if (event && event.id) {
      const lineItemControl = this.lineItemsArray.at(rowIndex);
      if (lineItemControl) {
        lineItemControl.patchValue({
          partId: event.id,
          unitPrice: event.costPrice || 0,
          stockLotId: null  // Reset lot selection when part changes
        });

        // Load available lots for the selected part
        this.loadAvailableLotsForPart(event.id);
      }
    }
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please fill all required fields'
      });
      return;
    }

    this.isSubmitting = true;
    const formValue = this.form.value;
    const poId = typeof formValue.purchaseOrderId === 'string'
      ? formValue.purchaseOrderId
      : formValue.purchaseOrderId?.id;
    const supplierId = this.purchaseOrders.find(po => po.id === poId)?.supplierId;

    if (!poId || !supplierId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Invalid purchase order selected'
      });
      this.isSubmitting = false;
      return;
    }

    const request = {
      purchaseOrderId: poId,
      supplierId,
      returnDate: new Date(formValue.returnDate).toISOString(),
      reason: formValue.reason,
      notes: formValue.notes,
      lines: formValue.lineItems.map((line: any) => ({
        purchaseOrderLineId: line.purchaseOrderLineId || '',
        partId: line.partId,
        stockLotId: line.stockLotId || null,  // Optional: specific lot to return from
        quantity: line.quantity,
        rejectedQuantity: line.rejectedQuantity || 0,
        unitPrice: line.unitPrice || 0,
        condition: line.condition,
        notes: line.notes
      }))
    };

    if (this.isEditing && this.returnId) {
      this.prService.updatePurchaseReturn(this.returnId, { ...request, id: this.returnId }).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Purchase return updated successfully'
          });
          this.router.navigate(['/procurement/purchase-returns']);
          this.isSubmitting = false;
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update purchase return'
          });
          console.error('Error updating purchase return:', error);
          this.isSubmitting = false;
        }
      });
    } else {
      this.prService.createPurchaseReturn(request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Purchase return created successfully'
          });
          this.router.navigate(['/procurement/purchase-returns']);
          this.isSubmitting = false;
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create purchase return'
          });
          console.error('Error creating purchase return:', error);
          this.isSubmitting = false;
        }
      });
    }
  }

  /**
   * Cancel form
   */
  onCancel(): void {
    this.router.navigate(['/procurement/purchase-returns']);
  }

  /**
   * Get part name by part ID
   */
  getPartName(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    return part ? `${part.partNumber} - ${part.name}` : 'Unknown Part';
  }

  /**
   * Format date for display
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }

  /**
   * Get status severity for display
   */
  getStatusSeverity(status: string): 'warn' | 'info' | 'success' | 'danger' | 'secondary' {
    switch (status?.toUpperCase()) {
      case 'PENDING': return 'warn';
      case 'APPROVED': return 'info';
      case 'RETURNED': return 'info';
      case 'RECEIVED': return 'success';
      case 'CREDITED': return 'success';
      case 'REJECTED': return 'danger';
      default: return 'secondary';
    }
  }

  /**
   * Get total quantity of all line items
   */
  getTotalQuantity(): number {
    return this.lineItemsArray.controls.reduce((sum, item) => {
      return sum + (item.get('quantity')?.value || 0);
    }, 0);
  }

  /**
   * Get total refund amount
   */
  getTotalRefundAmount(): number {
    return this.lineItemsArray.controls.reduce((sum, item) => {
      const qty = item.get('quantity')?.value || 0;
      const rejected = item.get('rejectedQuantity')?.value || 0;
      const price = item.get('unitPrice')?.value || 0;
      return sum + ((qty - rejected) * price);
    }, 0);
  }

  /**
   * Approve purchase return
   */
  approveReturn(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to approve return #${this.currentReturn.returnNumber}?`,
      header: 'Confirm Approval',
      icon: 'pi pi-check',
      accept: () => {
        this.prService.approvePurchaseReturn(this.currentReturn!.id).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Purchase Return #${updated.returnNumber} approved successfully`
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to approve purchase return'
            });
          }
        });
      }
    });
  }

  /**
   * Mark purchase return as returned
   */
  markAsReturned(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: `Mark return #${this.currentReturn.returnNumber} as returned to supplier?`,
      header: 'Confirm',
      icon: 'pi pi-send',
      accept: () => {
        this.prService.markAsReturned(this.currentReturn!.id).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Purchase Return #${updated.returnNumber} marked as returned`
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to mark as returned'
            });
          }
        });
      }
    });
  }

  /**
   * Mark purchase return as received by supplier
   */
  markAsReceived(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: `Mark return #${this.currentReturn.returnNumber} as received by supplier?`,
      header: 'Confirm',
      icon: 'pi pi-inbox',
      accept: () => {
        this.prService.markAsReceived(this.currentReturn!.id).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Purchase Return #${updated.returnNumber} marked as received`
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to mark as received'
            });
          }
        });
      }
    });
  }

  /**
   * Issue credit note for purchase return
   */
  issueCreditNote(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: `Issue credit note of ${this.currencyService.formatCurrency(this.currentReturn.refundAmount, this.currencyCode)} for return #${this.currentReturn.returnNumber}?`,
      header: 'Issue Credit Note',
      icon: 'pi pi-file',
      accept: () => {
        this.prService.issueCreditNote(this.currentReturn!.id, this.currentReturn!.refundAmount).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Credit note issued for return #${updated.returnNumber}`
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to issue credit note'
            });
          }
        });
      }
    });
  }

  /**
   * Reject purchase return
   */
  rejectReturn(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to reject return #${this.currentReturn.returnNumber}?`,
      header: 'Confirm Rejection',
      icon: 'pi pi-times',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.prService.rejectPurchaseReturn(this.currentReturn!.id, 'Rejected by user').subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Purchase Return #${updated.returnNumber} rejected`
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to reject purchase return'
            });
          }
        });
      }
    });
  }

  /**
   * Print the purchase return
   */
  printReturn(): void {
    if (!this.currentReturn) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'No purchase return data available to print'
      });
      return;
    }

    const printContent = this.generatePrintContent();
    const printWindow = window.open('', '_blank', 'width=800,height=600');
    if (printWindow) {
      printWindow.document.write(printContent);
      printWindow.document.close();
      printWindow.focus();
      setTimeout(() => {
        printWindow.print();
        printWindow.close();
      }, 250);
    }
  }

  /**
   * Generate print content HTML
   */
  private generatePrintContent(): string {
    const pr = this.currentReturn!;
    const returnDate = pr.returnDate ? new Date(pr.returnDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }) : '-';
    const lineItemsHtml = (pr.lines || []).map(line => `
      <tr>
        <td class="desc-cell">
          <div class="item-name">${line.displayName || line.partName || line.partSku || 'N/A'}</div>
          <div class="item-desc">${line.condition || '-'}</div>
        </td>
        <td class="num-cell">${line.quantity}</td>
        <td class="num-cell">${line.rejectedQuantity}</td>
        <td class="num-cell">${this.currencyService.formatCurrency(line.unitPrice, this.currencyCode)}</td>
        <td class="num-cell">${this.currencyService.formatCurrency(line.refundAmount, this.currencyCode)}</td>
      </tr>
    `).join('');

    return `
      <!DOCTYPE html>
      <html>
      <head>
        <title>Purchase Return - ${pr.returnNumber}</title>
        <style>
          * { margin: 0; padding: 0; box-sizing: border-box; }
          body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #333; padding: 20px; max-width: 800px; margin: 0 auto; }

          .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
          .logo-section { display: flex; align-items: center; gap: 10px; }
          .logo { width: 60px; height: 60px; background: linear-gradient(135deg, #1976d2 0%, #1565c0 100%); border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 24px; font-weight: bold; }
          .company-name { font-size: 22px; font-weight: 700; color: #1976d2; }
          .title-section { text-align: right; }
          .title-section h1 { font-size: 28px; color: #1976d2; font-weight: 300; margin-bottom: 8px; }
          .invoice-meta { font-size: 11px; color: #666; }
          .invoice-meta span { display: inline-block; min-width: 90px; }
          .invoice-meta .value { color: #333; font-weight: 500; }
          .status-pill { display: inline-block; padding: 4px 10px; border-radius: 999px; font-size: 10px; background: #e0e7ff; color: #1e40af; margin-top: 6px; }

          .address-section { display: flex; justify-content: space-between; margin-bottom: 20px; padding-bottom: 15px; border-bottom: 1px solid #e0e0e0; }
          .address-block { flex: 1; }
          .address-block.right { text-align: right; }
          .address-label { font-size: 10px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px; }
          .address-name { font-size: 14px; font-weight: 600; color: #333; margin-bottom: 4px; }
          .address-detail { font-size: 11px; color: #666; line-height: 1.5; }

          .items-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
          .items-table th { background: #1976d2; color: white; padding: 10px 8px; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 500; }
          .items-table th:first-child { text-align: left; border-radius: 4px 0 0 0; }
          .items-table th:last-child { border-radius: 0 4px 0 0; }
          .items-table th.num-col { text-align: right; }
          .items-table td { padding: 10px 8px; border-bottom: 1px solid #eee; vertical-align: top; }
          .desc-cell { width: 40%; }
          .num-cell { text-align: right; width: 15%; }
          .item-name { font-weight: 500; color: #333; }
          .item-desc { font-size: 10px; color: #999; margin-top: 2px; }

          .summary-section { display: flex; justify-content: space-between; margin-bottom: 20px; }
          .payment-info { flex: 1; padding-right: 40px; }
          .payment-info h4 { font-size: 11px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
          .payment-info p { font-size: 11px; color: #666; line-height: 1.6; }
          .totals-box { width: 250px; }
          .totals-row { display: flex; justify-content: space-between; padding: 6px 0; font-size: 11px; }
          .totals-row.total { border-top: 2px solid #1976d2; margin-top: 8px; padding-top: 10px; font-size: 14px; font-weight: 600; color: #1976d2; }
          .totals-label { color: #666; }
          .totals-value { font-weight: 500; }

          .footer { text-align: center; color: #999; font-size: 10px; padding-top: 10px; border-top: 1px solid #eee; }
          .signature-section { display: flex; justify-content: space-between; margin-top: 50px; }
          .signature-box { width: 200px; text-align: center; }
          .signature-line { border-top: 1px solid #333; margin-top: 40px; padding-top: 5px; font-size: 11px; color: #666; }
          @media print { body { padding: 10px; } }
        </style>
      </head>
      <body>
        <div class="header">
          <div class="logo-section">
            <div class="logo">SM</div>
            <div>
              <div class="company-name">Sujan Motors</div>
              <div class="address-detail">Auto Parts & Accessories</div>
              <div class="address-detail">Dhaka, Bangladesh</div>
            </div>
          </div>
          <div class="title-section">
            <h1>Purchase Return</h1>
            <div class="invoice-meta">
              <div><span>Return no.:</span> <span class="value">${pr.returnNumber}</span></div>
              <div><span>Return date:</span> <span class="value">${returnDate}</span></div>
              <div><span>PO no.:</span> <span class="value">${pr.purchaseOrderNumber || '-'}</span></div>
              <div><span>Reason:</span> <span class="value">${pr.reason || '-'}</span></div>
            </div>
            <div class="status-pill">${pr.status}</div>
          </div>
        </div>

        <div class="address-section">
          <div class="address-block">
            <div class="address-label">From</div>
            <div class="address-name">Sujan Motors</div>
            <div class="address-detail">
              Auto Parts & Accessories<br>
              Dhaka, Bangladesh
            </div>
          </div>
          <div class="address-block right">
            <div class="address-label">Supplier</div>
            <div class="address-name">${pr.supplierName || 'N/A'}</div>
            <div class="address-detail">
              ${pr.supplierCode ? `Supplier Code: ${pr.supplierCode}<br>` : ''}
              PO Ref: ${pr.purchaseOrderNumber || '-'}
            </div>
          </div>
        </div>

        <table class="items-table">
          <thead>
            <tr>
              <th>Description</th>
              <th class="num-col">Qty</th>
              <th class="num-col">Rejected</th>
              <th class="num-col">Unit Price</th>
              <th class="num-col">Refund</th>
            </tr>
          </thead>
          <tbody>
            ${lineItemsHtml || `<tr><td colspan="5" style="padding: 10px; text-align: center;">No items</td></tr>`}
          </tbody>
        </table>

        <div class="summary-section">
          <div class="payment-info">
            ${pr.notes ? `<h4>Notes</h4><p>${pr.notes}</p>` : ''}
          </div>
          <div class="totals-box">
            <div class="totals-row total">
              <span class="totals-label">Total Refund:</span>
              <span class="totals-value">${this.currencyService.formatCurrency(pr.refundAmount, this.currencyCode)}</span>
            </div>
            ${pr.creditNoteAmount > 0 ? `
            <div class="totals-row">
              <span class="totals-label">Credit Note:</span>
              <span class="totals-value">${this.currencyService.formatCurrency(pr.creditNoteAmount, this.currencyCode)}</span>
            </div>
            ` : ''}
          </div>
        </div>

        <div class="signature-section">
          <div class="signature-box">
            <div class="signature-line">Prepared By</div>
          </div>
          <div class="signature-box">
            <div class="signature-line">Approved By</div>
          </div>
          <div class="signature-box">
            <div class="signature-line">Received By Supplier</div>
          </div>
        </div>

        <div class="footer">
          <p>Thank you for choosing Sujan Motors | Printed on ${new Date().toLocaleString()}</p>
        </div>
      </body>
      </html>
    `;
  }
}
