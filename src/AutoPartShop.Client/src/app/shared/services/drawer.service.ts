import { Injectable } from '@angular/core';
import { signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DrawerService {
  isOpen = signal(false);
  isCollapsed = signal(false);

  toggleDrawer() {
    this.isOpen.update(value => !value);
  }

  toggleCollapse() {
    this.isCollapsed.update(value => !value);
  }

  openDrawer() {
    this.isOpen.set(true);
  }

  closeDrawer() {
    this.isOpen.set(false);
  }

  setCollapsed(collapsed: boolean) {
    this.isCollapsed.set(collapsed);
  }
}
