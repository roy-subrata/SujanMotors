import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-units-header',
  standalone: true,
  imports: [CommonModule, FormsModule, InputTextModule, ButtonModule, TooltipModule],
  templateUrl: './units-header.component.html',
  styleUrls: ['./units-header.component.css']
})
export class UnitsHeaderComponent {
  @Output() createClick = new EventEmitter<void>();
  @Output() searchChange = new EventEmitter<string>();

  searchQuery = '';

  /**
   * Handle create button click
   */
  onCreateClick(): void {
    this.createClick.emit();
  }

  /**
   * Handle search input change
   */
  onSearch(value: string): void {
    this.searchQuery = value;
    this.searchChange.emit(value);
  }

  /**
   * Clear search
   */
  clearSearch(): void {
    this.searchQuery = '';
    this.searchChange.emit('');
  }
}
