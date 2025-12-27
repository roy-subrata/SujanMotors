import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-categories-header',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule],
  templateUrl: './categories-header.component.html',
  styleUrls: ['./categories-header.component.css']
})
export class CategoriesHeaderComponent {
  @Input() loading = false;
  @Output() createClick = new EventEmitter<void>();
  @Output() searchChange = new EventEmitter<string>();

  searchQuery = '';

  onCreateClick() {
    this.createClick.emit();
  }

  onSearchChange(query: string) {
    this.searchQuery = query;
    this.searchChange.emit(query);
  }

  clearSearch() {
    this.searchQuery = '';
    this.searchChange.emit('');
  }
}
