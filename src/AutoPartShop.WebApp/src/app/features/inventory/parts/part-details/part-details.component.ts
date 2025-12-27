import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PartService, PartResponse, VehicleCompatibilityResponse } from '../../services/part.service';
import { ProductLocationManagerComponent } from '../product-location-manager.component';

@Component({
    selector: 'app-part-details',
    standalone: true,
    imports: [CommonModule, ButtonModule, CardModule, TableModule, TagModule, ToastModule, ProductLocationManagerComponent],
    providers: [MessageService],
    templateUrl: './part-details.component.html',
    styleUrls: ['./part-details.component.css']
})
export class PartDetailsComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);

    part: PartResponse | null = null;
    compatibleVehicles: VehicleCompatibilityResponse[] = [];
    loading = false;
    loadingVehicles = false;
    partId: string | null = null;

    ngOnInit(): void {
        this.route.params.subscribe((params) => {
            this.partId = params['id'];
            if (this.partId) {
                this.loadPartDetails();
                this.loadCompatibleVehicles();
            }
        });
    }

    /**
     * Load part details
     */
    private loadPartDetails(): void {
        if (!this.partId) return;

        this.loading = true;
        this.partService.getPartById(this.partId).subscribe({
            next: (part) => {
                this.part = part;
                this.loading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load part details'
                });
                console.error('Error loading part:', error);
                this.loading = false;
            }
        });
    }

    /**
     * Load compatible vehicles
     */
    private loadCompatibleVehicles(): void {
        if (!this.partId) return;

        this.loadingVehicles = true;
        this.partService.getPartCompatibleVehicles(this.partId).subscribe({
            next: (vehicles) => {
                this.compatibleVehicles = vehicles;
                this.loadingVehicles = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load compatible vehicles'
                });
                console.error('Error loading vehicles:', error);
                this.loadingVehicles = false;
            }
        });
    }

    /**
     * Navigate back to parts list
     */
    onBack(): void {
        this.router.navigate(['/inventory/parts']);
    }

    /**
     * Navigate to edit part
     */
    onEdit(): void {
        if (this.part) {
            this.router.navigate(['/inventory/parts'], {
                queryParams: { edit: this.part.id }
            });
        }
    }

    /**
     * Format price for display
     */
    formatPrice(price: number): string {
        return `₹${price.toFixed(2)}`;
    }

    /**
     * Calculate margin percentage
     */
    calculateMargin(costPrice: number, sellingPrice: number): string {
        if (costPrice === 0) return '0%';
        const margin = ((sellingPrice - costPrice) / costPrice) * 100;
        return `${margin.toFixed(2)}%`;
    }

    /**
     * Get margin color class
     */
    getMarginClass(costPrice: number, sellingPrice: number): string {
        return sellingPrice >= costPrice ? 'positive-margin' : 'negative-margin';
    }

    /**
     * Get status badge severity
     */
    getStatusSeverity(isActive: boolean): string {
        return isActive ? 'success' : 'danger';
    }

    /**
     * Get compatibility badge severity
     */
    getCompatibilitySeverity(isCompatible: boolean): string {
        return isCompatible ? 'success' : 'warning';
    }
}
