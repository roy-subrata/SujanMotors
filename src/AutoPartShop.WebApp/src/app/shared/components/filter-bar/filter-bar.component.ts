import { Component } from '@angular/core';

/**
 * Standard filter bar for list pages. Projects three slots into a
 * consistent, responsive row, plus an optional active-filters row.
 *
 * Usage:
 *   <app-filter-bar>
 *     <div search class="filter-group search-group">
 *       <div class="search-input-wrapper"> ... </div>
 *     </div>
 *     <div filters class="filter-group"> ... </div>
 *     <div filterActions class="filter-group filter-actions"> ... </div>
 *     <div activeFilters class="active-filters"> ... </div>
 *   </app-filter-bar>
 */
@Component({
  selector: 'app-filter-bar',
  standalone: true,
  templateUrl: './filter-bar.component.html',
})
export class FilterBarComponent {}
