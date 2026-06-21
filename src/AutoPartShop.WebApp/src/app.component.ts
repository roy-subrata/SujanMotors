import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RouterModule, ConfirmDialogModule],
    // The root confirm dialog is driven by the root ConfirmationService — used by the auth
    // interceptor to offer a reload when the API returns a 409 concurrency conflict. The custom
    // key keeps it from colliding with page-level <p-confirmDialog> instances.
    template: `
        <router-outlet></router-outlet>
        <p-confirmdialog key="global-concurrency"></p-confirmdialog>
    `
})
export class AppComponent {}
