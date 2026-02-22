import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { HttpClientModule, HttpClient } from '@angular/common/http';

interface User {
  id: number;
  name: string;
  email: string;
  role: string;
  firstName?: string;
}

@Component({
  standalone: true,
  imports: [CommonModule, TableModule, HttpClientModule],
  selector: 'app-test',
  templateUrl: './test.component.html',
  styleUrls: ['./test.component.scss']
})
export class TestComponent implements OnInit {
  users: User[] = [];
  totalRecords = 1000;
  rowsPerPage = 20;
  loading = false;
  scrollHeight = '600px';

  constructor(private http: HttpClient) {
    this.calculateScrollHeight();
  }

  ngOnInit() {
    // Initial load
    this.loadUsers({ first: 0, rows: this.rowsPerPage });
  }

  @HostListener('window:resize')
  onResize() {
    this.calculateScrollHeight();
  }

  calculateScrollHeight() {
    // Calculate dynamic height: viewport height - header - padding - footer
    // For a 27" screen or large monitors, ensure we have a reasonable max height
    const isMobile = window.innerWidth <= 768;
    const isTablet = window.innerWidth > 768 && window.innerWidth <= 1024;

    let offset = 220; // Default offset for desktop
    if (isMobile) {
      offset = 180; // Less offset on mobile
    } else if (isTablet) {
      offset = 200; // Medium offset on tablet
    }

    const calculatedHeight = window.innerHeight - offset;
    // Ensure minimum height for scrolling to be visible
    const height = Math.max(400, calculatedHeight);

    this.scrollHeight = `${height}px`;
    console.log('Scroll height calculated:', this.scrollHeight, 'Window height:', window.innerHeight);
  }

  loadUsers(event: any) {
    this.loading = true;
    const start = event.first;
    const end = start + event.rows;

    console.log('Loading users:', { start, rows: event.rows, currentCount: this.users.length });

    // Replace with your API URL and pagination params
    // Example: GET /api/users?start=0&limit=20
    this.http
      .get<User[]>(`https://dummyjson.com/users?limit=${event.rows}&skip=${event.first}`)
      .subscribe({
        next: (data: any) => {
          // API may return array in `data.users` or directly
          const loadedUsers = data.users || data;

          console.log('Loaded users:', loadedUsers.length, 'Total in table:', this.users.length);

          if (start === 0) {
            this.users = loadedUsers;
          } else {
            this.users = [...this.users, ...loadedUsers];
          }

          // Update total records from API response if available
          if (data.total) {
            this.totalRecords = data.total;
          }

          this.loading = false;
          console.log('Total users now:', this.users.length, 'Total records:', this.totalRecords);
        },
        error: (err) => {
          console.error('Error loading users:', err);
          this.loading = false;
        }
      });
  }
}
