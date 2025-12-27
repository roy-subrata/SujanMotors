import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService, ConfirmationService } from 'primeng/api';
import { BrandService, BrandResponse, CreateBrandRequest, UpdateBrandRequest } from '../services/brand.service';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';

@Component({
  selector: 'app-brands',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule,
    TooltipModule,
    InputNumberModule,
    CheckboxModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './brands.component.html',
  styleUrls: ['./brands.component.css']
})
export class BrandsComponent implements OnInit {
  private readonly brandService = inject(BrandService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly codeGenerationService = inject(CodeGenerationService);

  brands: BrandResponse[] = [];
  filteredBrands: BrandResponse[] = [];
  brand: any = {};
  brandDialog = false;
  editMode = false;
  loading = false;
  generatingCode = false;
  searchQuery = '';

  ngOnInit(): void {
    this.loadBrands();
  }

  loadBrands(): void {
    this.loading = true;
    this.brandService.getAllBrands().subscribe({
      next: (brands) => {
        this.brands = brands;
        this.filteredBrands = brands;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading brands:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load brands'
        });
        this.loading = false;
      }
    });
  }

  onSearchChange(query: string): void {
    this.searchQuery = query;
    if (!query || query.trim() === '') {
      this.filteredBrands = this.brands;
    } else {
      const lowerQuery = query.toLowerCase().trim();
      this.filteredBrands = this.brands.filter(brand =>
        brand.name?.toLowerCase().includes(lowerQuery) ||
        brand.code?.toLowerCase().includes(lowerQuery) ||
        brand.country?.toLowerCase().includes(lowerQuery)
      );
    }
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.filteredBrands = this.brands;
  }

  openNew(): void {
    this.brand = {
      name: '',
      code: '',
      description: '',
      country: ''
    };
    this.editMode = false;
    this.brandDialog = true;
    this.generateBrandCode();
  }

  /**
   * Generate brand code automatically
   */
  private generateBrandCode(): void {
    this.generatingCode = true;
    this.codeGenerationService.generateBrandCode().subscribe({
      next: (code) => {
        this.brand.code = code;
        this.generatingCode = false;
      },
      error: (error) => {
        console.error('Error generating brand code:', error);
        this.messageService.add({
          severity: 'warn',
          summary: 'Warning',
          detail: 'Failed to generate brand code. Please enter manually.'
        });
        this.generatingCode = false;
      }
    });
  }

  editBrand(brand: BrandResponse): void {
    this.brand = { ...brand };
    this.editMode = true;
    this.brandDialog = true;
  }

  hideDialog(): void {
    this.brandDialog = false;
  }

  saveBrand(): void {
    if (this.editMode) {
      const updateRequest: UpdateBrandRequest = {
        id: this.brand.id,
        name: this.brand.name,
        code: this.brand.code,
        description: this.brand.description || '',
        logoUrl: this.brand.logoUrl || '',
        website: this.brand.website || '',
        country: this.brand.country || '',
        contactEmail: this.brand.contactEmail || '',
        contactPhone: this.brand.contactPhone || '',
        displayOrder: this.brand.displayOrder || 0,
        isActive: this.brand.isActive ?? true
      };

      this.brandService.updateBrand(this.brand.id, updateRequest).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Brand updated successfully'
          });
          this.loadBrands();
          this.hideDialog();
        },
        error: (error) => {
          console.error('Error updating brand:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to update brand'
          });
        }
      });
    } else {
      const createRequest: CreateBrandRequest = {
        name: this.brand.name,
        code: this.brand.code.toUpperCase(),
        description: this.brand.description || '',
        country: this.brand.country || ''
      };

      this.brandService.createBrand(createRequest).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Brand created successfully'
          });
          this.loadBrands();
          this.hideDialog();
        },
        error: (error) => {
          console.error('Error creating brand:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to create brand'
          });
        }
      });
    }
  }

  deleteBrand(brand: BrandResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the brand "${brand.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-secondary',
      accept: () => {
        this.brandService.deleteBrand(brand.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Brand deleted successfully'
            });
            this.loadBrands();
          },
          error: (error) => {
            console.error('Error deleting brand:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error.error?.message || 'Failed to delete brand'
            });
          }
        });
      }
    });
  }
}

