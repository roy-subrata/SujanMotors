import { Component } from '@angular/core';

/**
 * Standard page shell for list/data pages.
 * Provides the consistent background + min-height; child sections
 * (app-page-header, app-filter-bar, table, app-data-pagination)
 * manage their own gutters via the shared design tokens.
 *
 * Usage:
 *   <app-page-container>
 *     <app-page-header ...></app-page-header>
 *     <app-filter-bar ...></app-filter-bar>
 *     <section class="table-section desktop-only"> ... </section>
 *   </app-page-container>
 */
@Component({
  selector: 'app-page-container',
  standalone: true,
  template: `<div class="page-wrapper"><ng-content></ng-content></div>`
})
export class PageContainerComponent {}
