import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CatalogService } from '../services/catalog.service';
import { CatalogLandingResponse, CatalogProductListItem } from '../models/catalog.model';

@Component({
  selector: 'app-ecommerce-landing',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ButtonModule, InputTextModule],
  templateUrl: './ecommerce-landing.component.html',
  styleUrls: ['./ecommerce-landing.component.css']
})
export class EcommerceLandingComponent implements OnInit {
  private readonly catalogService = inject(CatalogService);

  landing?: CatalogLandingResponse;
  loading = true;
  searchTerm = '';

  ngOnInit(): void {
    this.catalogService.getLanding().subscribe({
      next: (data) => {
        this.landing = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  trackById(_index: number, item: CatalogProductListItem) {
    return item.partId;
  }
}
