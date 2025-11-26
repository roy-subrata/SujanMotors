import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { AvatarModule } from 'primeng/avatar';
import { MenuItem } from 'primeng/api';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule, ButtonModule, MenuModule, AvatarModule, RippleModule, TooltipModule],
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.css']
})
export class TopbarComponent {
  userMenuItems: MenuItem[] = [
    {
      label: 'Profile',
      icon: 'pi pi-user',
      command: () => console.log('Profile clicked')
    },
    {
      label: 'Settings',
      icon: 'pi pi-cog',
      command: () => console.log('Settings clicked')
    },
    {
      label: 'Preferences',
      icon: 'pi pi-sliders-h',
      command: () => console.log('Preferences clicked')
    },
    { separator: true },
    {
      label: 'Help & Support',
      icon: 'pi pi-question-circle',
      command: () => console.log('Help clicked')
    },
    { separator: true },
    {
      label: 'Logout',
      icon: 'pi pi-sign-out',
      styleClass: 'text-red-600 dark:text-red-400',
      command: () => console.log('Logout clicked')
    }
  ];
}
