import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CatalogService } from '../services/catalog.service';
import { CatalogProductDetail, CatalogVariant } from '../models/catalog.model';

@Component({
  selector: 'app-ecommerce-product',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonModule],
  templateUrl: './ecommerce-product.component.html',
  styleUrls: ['./ecommerce-product.component.css']
})
export class EcommerceProductComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);

  product?: CatalogProductDetail;
  selectedVariant?: CatalogVariant;
  loading = true;

  ngOnInit(): void {
    const partId = this.route.snapshot.paramMap.get('partId');
    if (partId) {
      this.catalogService.getProductDetail(partId).subscribe({
        next: (data) => {
          this.product = data;
          this.selectedVariant = data.variants[0];
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
    }
  }

  selectVariant(variant: CatalogVariant): void {
    this.selectedVariant = variant;
  }
}
