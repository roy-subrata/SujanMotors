import { Directive, Input, TemplateRef, ViewContainerRef, inject, OnInit, OnDestroy } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { Subscription } from 'rxjs';

/**
 * Structural directive to show/hide elements based on user permissions
 *
 * Usage examples:
 *
 * // Show if user has 'users.create' permission
 * <button *appHasPermission="'users.create'">Create User</button>
 *
 * // Show if user has any of the specified permissions
 * <div *appHasPermission="['users.create', 'users.update']">
 *   User Management Actions
 * </div>
 *
 * // Show if user has all specified permissions
 * <div *appHasPermission="['users.create', 'users.delete']; requireAll: true">
 *   Advanced User Management
 * </div>
 *
 * // Else template
 * <div *appHasPermission="'users.create'; else noAccess">
 *   Create User Button
 * </div>
 * <ng-template #noAccess>
 *   <p>You don't have permission to create users</p>
 * </ng-template>
 */
@Directive({
  selector: '[appHasPermission]',
  standalone: true
})
export class HasPermissionDirective implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private templateRef = inject(TemplateRef<any>);
  private viewContainer = inject(ViewContainerRef);

  private permissions: string | string[] = [];
  private requireAll = false;
  private elseTemplateRef: TemplateRef<any> | null = null;
  private subscription?: Subscription;

  @Input()
  set appHasPermission(permissions: string | string[]) {
    this.permissions = permissions;
    this.updateView();
  }

  @Input()
  set appHasPermissionRequireAll(value: boolean) {
    this.requireAll = value;
    this.updateView();
  }

  @Input()
  set appHasPermissionElse(templateRef: TemplateRef<any> | null) {
    this.elseTemplateRef = templateRef;
    this.updateView();
  }

  ngOnInit() {
    // Subscribe to authentication state changes
    this.subscription = this.authService.currentUser$.subscribe(() => {
      this.updateView();
    });
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  private updateView() {
    const hasAccess = this.checkAccess();

    this.viewContainer.clear();

    if (hasAccess) {
      this.viewContainer.createEmbeddedView(this.templateRef);
    } else if (this.elseTemplateRef) {
      this.viewContainer.createEmbeddedView(this.elseTemplateRef);
    }
  }

  private checkAccess(): boolean {
    if (!this.permissions) {
      return true; // No permissions specified, show by default
    }

    const permissionsArray = Array.isArray(this.permissions) ? this.permissions : [this.permissions];

    if (this.requireAll) {
      return this.authService.hasAllPermissions(permissionsArray);
    } else {
      return this.authService.hasAnyPermission(permissionsArray);
    }
  }
}
