import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-ecommerce-shell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './ecommerce-shell.component.html',
  styleUrls: ['./ecommerce-shell.component.css']
})
export class EcommerceShellComponent implements OnInit, OnDestroy {
  private readonly document = inject(DOCUMENT);

  ngOnInit(): void {
    this.document.body.classList.add('storefront-scroll');
    this.document.documentElement.classList.add('storefront-scroll');
  }

  ngOnDestroy(): void {
    this.document.body.classList.remove('storefront-scroll');
    this.document.documentElement.classList.remove('storefront-scroll');
  }
}
