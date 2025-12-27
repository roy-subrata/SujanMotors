import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-conversions-header',
  standalone: true,
  imports: [CommonModule, FormsModule, InputTextModule, ButtonModule, TooltipModule],
  templateUrl: './conversions-header.component.html',
  styleUrls: ['./conversions-header.component.css']
})
export class ConversionsHeaderComponent {
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
