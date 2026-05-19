import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-parts-header',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, TooltipModule],
  templateUrl: './parts-header.component.html',
  styleUrls: ['./parts-header.component.css']
})
export class PartsHeaderComponent {
  @Output() createClick = new EventEmitter<void>();
  @Output() searchChange = new EventEmitter<string>();
  @Output() searchClear = new EventEmitter<void>();

  searchQuery: string = '';

  onCreateClick(): void {
    this.createClick.emit();
  }

  onSearch(query: string): void {
    this.searchChange.emit(query);
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchClear.emit();
  }
}
