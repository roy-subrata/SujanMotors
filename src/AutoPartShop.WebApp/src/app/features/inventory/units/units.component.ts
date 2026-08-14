import { Component, inject, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { UnitService, UnitResponse } from '../services/unit.service';
import { UnitConversionService, UnitConversionResponse } from '../services/unit-conversion.service';
import { UnitsListComponent } from './units-list/units-list.component';
import { UnitsFormDialogComponent } from './units-form-dialog/units-form-dialog.component';
import { ConversionsListComponent } from './conversions-list/conversions-list.component';
import { ConversionsFormDialogComponent } from './conversions-form-dialog/conversions-form-dialog.component';
import { TabsModule } from 'primeng/tabs';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-units',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ToastModule,
    ConfirmDialogModule,
    TabsModule,
    UnitsListComponent,
    UnitsFormDialogComponent,
    ConversionsListComponent,
    ConversionsFormDialogComponent,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent,
    TranslatePipe
  ],
  providers: [UnitService, UnitConversionService, MessageService, ConfirmationService],
  templateUrl: './units.component.html',
  styleUrls: ['./units.component.css']
})
export class UnitsComponent implements OnInit {
  @ViewChild(UnitsListComponent) listComponent!: UnitsListComponent;
  @ViewChild(UnitsFormDialogComponent) formDialogComponent!: UnitsFormDialogComponent;
  @ViewChild(ConversionsListComponent) conversionsListComponent!: ConversionsListComponent;
  @ViewChild(ConversionsFormDialogComponent) conversionsFormDialogComponent!: ConversionsFormDialogComponent;

  private readonly unitService = inject(UnitService);
  private readonly conversionService = inject(UnitConversionService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  private readonly i18n = inject(I18nService);

  // Units tab state
  selectedUnit: UnitResponse | null = null;
  displayCreateDialog = false;
  displayUpdateDialog = false;
  searchTerm = '';

  // Conversions tab state
  selectedConversion: UnitConversionResponse | null = null;
  displayConversionCreateDialog = false;
  displayConversionUpdateDialog = false;
  units: UnitResponse[] = [];
  conversionSearchTerm = '';

  ngOnInit(): void {
    // Initialize component if needed
  }

  /**
   * Handle create button click
   */
  onNewUnitClick(): void {
    this.selectedUnit = null;
    this.displayCreateDialog = true;
    this.displayUpdateDialog = false;
  }

  /**
   * Handle search input
   */
  onSearch(query: string): void {
    this.listComponent.search(query);
  }

  clearUnitsSearch(): void {
    this.searchTerm = '';
    this.listComponent.clearSearch();
  }

  hasUnitsFilters(): boolean {
    return !!this.searchTerm;
  }

  /**
   * Handle edit unit
   */
  selectAndOpenUpdate(unit: UnitResponse): void {
    this.selectedUnit = unit;
    this.displayUpdateDialog = true;
    this.displayCreateDialog = false;
  }

  /**
   * Handle delete unit with confirmation
   */
  selectAndDelete(unit: UnitResponse): void {
    this.confirmationService.confirm({
      message: this.i18n.t('units.messages.deleteConfirm', { name: unit.name }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-secondary',
      accept: () => {
        this.performDelete(unit);
      },
      reject: () => {
        this.messageService.add({
          severity: 'info',
          summary: this.i18n.t('common.messages.info'),
          detail: this.i18n.t('common.messages.actionCancelled')
        });
      }
    });
  }

  /**
   * Perform the actual delete operation
   */
  private performDelete(unit: UnitResponse): void {
    this.unitService.deleteUnit(unit.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('units.messages.deleteSuccess')
        });
        this.listComponent.reload();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('units.messages.deleteFailed')
        });
      }
    });
  }

  /**
   * Handle toggle unit status
   */
  selectAndToggleStatus(unit: UnitResponse): void {
    const isDeactivating = unit.isActive;
    const confirmKey = isDeactivating ? 'units.messages.deactivateConfirm' : 'units.messages.activateConfirm';
    const header = isDeactivating ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate');

    this.confirmationService.confirm({
      message: this.i18n.t(confirmKey, { name: unit.name }),
      header,
      icon: 'pi pi-info-circle',
      acceptButtonStyleClass: unit.isActive ? 'p-button-warning' : 'p-button-success',
      rejectButtonStyleClass: 'p-button-secondary',
      accept: () => {
        this.performToggleStatus(unit);
      }
    });
  }

  /**
   * Perform the actual status toggle
   */
  private performToggleStatus(unit: UnitResponse): void {
    const request = unit.isActive ? this.unitService.deactivateUnit(unit.id) : this.unitService.activateUnit(unit.id);
    const successKey = unit.isActive ? 'units.messages.deactivateSuccess' : 'units.messages.activateSuccess';

    request.subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t(successKey)
        });
        this.listComponent.reload();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('common.messages.updateFailed')
        });
      }
    });
  }

  /**
   * Handle create dialog visibility change
   */
  onDisplayCreateDialogChange(display: boolean): void {
    this.displayCreateDialog = display;
  }

  /**
   * Handle update dialog visibility change
   */
  onDisplayUpdateDialogChange(display: boolean): void {
    this.displayUpdateDialog = display;
  }

  /**
   * Handle create success
   */
  onCreateSuccess(): void {
    this.listComponent.reload();
  }

  /**
   * Handle update success
   */
  onUpdateSuccess(): void {
    this.listComponent.reload();
  }

  // ============== CONVERSIONS METHODS ==============

  /**
   * Load all units for conversion dropdown
   */
  private loadUnitsForConversion(): void {
    this.unitService.getAllUnits().subscribe({
      next: (response) => {
        this.units = response;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('units.messages.loadFailed')
        });
      }
    });
  }

  /**
   * Handle create conversion click
   */
  onNewConversionClick(): void {
    this.loadUnitsForConversion();
    this.selectedConversion = null;
    this.displayConversionCreateDialog = true;
    this.displayConversionUpdateDialog = false;
  }

  /**
   * Handle conversion search
   */
  onConversionSearch(query: string): void {
    this.conversionsListComponent.search(query);
  }

  clearConversionSearch(): void {
    this.conversionSearchTerm = '';
    this.conversionsListComponent.clearSearch();
  }

  hasConversionFilters(): boolean {
    return !!this.conversionSearchTerm;
  }

  /**
   * Handle edit conversion
   */
  selectAndOpenConversionUpdate(conversion: UnitConversionResponse): void {
    this.selectedConversion = conversion;
    this.displayConversionUpdateDialog = true;
    this.displayConversionCreateDialog = false;
  }

  /**
   * Handle delete conversion with confirmation
   */
  selectAndDeleteConversion(conversion: UnitConversionResponse): void {
    const msg = `${conversion.fromUnitCode} → ${conversion.toUnitCode}`;
    this.confirmationService.confirm({
      message: this.i18n.t('conversions.messages.deleteConfirmDetailed', { units: msg }),
      header: this.i18n.t('common.messages.confirmDeletion'),
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-secondary',
      accept: () => {
        this.performConversionDelete(conversion);
      },
      reject: () => {
        this.messageService.add({
          severity: 'info',
          summary: this.i18n.t('common.messages.info'),
          detail: this.i18n.t('common.messages.actionCancelled')
        });
      }
    });
  }

  /**
   * Perform the actual conversion delete operation
   */
  private performConversionDelete(conversion: UnitConversionResponse): void {
    this.conversionService.deleteConversion(conversion.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('conversions.messages.deleteSuccess')
        });
        this.conversionsListComponent.reload();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('conversions.messages.deleteFailed')
        });
      }
    });
  }

  /**
   * Handle toggle conversion status
   */
  selectAndToggleConversionStatus(conversion: UnitConversionResponse): void {
    const isDeactivating = conversion.isActive;
    const action = isDeactivating ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate');
    const msg = `${conversion.fromUnitCode} → ${conversion.toUnitCode}`;

    this.confirmationService.confirm({
      message: this.i18n.t('conversions.messages.toggleConfirmDetailed', { action: action.toLowerCase(), units: msg }),
      header: action,
      icon: 'pi pi-info-circle',
      acceptButtonStyleClass: conversion.isActive ? 'p-button-warning' : 'p-button-success',
      rejectButtonStyleClass: 'p-button-secondary',
      accept: () => {
        this.performToggleConversionStatus(conversion);
      }
    });
  }

  /**
   * Perform the actual conversion status toggle
   */
  private performToggleConversionStatus(conversion: UnitConversionResponse): void {
    const updateRequest = {
      conversionFactor: conversion.conversionFactor,
      description: conversion.description,
      isActive: !conversion.isActive
    };
    const successKey = conversion.isActive ? 'conversions.messages.deactivateSuccess' : 'conversions.messages.activateSuccess';

    this.conversionService.updateConversion(conversion.id, updateRequest).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t(successKey)
        });
        this.conversionsListComponent.reload();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('common.messages.updateFailed')
        });
      }
    });
  }

  /**
   * Handle conversion create dialog visibility change
   */
  onDisplayConversionCreateDialogChange(display: boolean): void {
    this.displayConversionCreateDialog = display;
  }

  /**
   * Handle conversion update dialog visibility change
   */
  onDisplayConversionUpdateDialogChange(display: boolean): void {
    this.displayConversionUpdateDialog = display;
  }

  /**
   * Handle conversion create success
   */
  onConversionCreateSuccess(): void {
    this.conversionsListComponent.reload();
  }

  /**
   * Handle conversion update success
   */
  onConversionUpdateSuccess(): void {
    this.conversionsListComponent.reload();
  }

  get unitsTotalRecords(): number {
    return this.listComponent?.totalRecords || 0;
  }
}
