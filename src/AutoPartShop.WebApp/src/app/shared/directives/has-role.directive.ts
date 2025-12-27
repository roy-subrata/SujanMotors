import { Directive, Input, TemplateRef, ViewContainerRef, inject, OnInit, OnDestroy } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { Subscription } from 'rxjs';

/**
 * Structural directive to show/hide elements based on user roles
 *
 * Usage examples:
 *
 * // Show if user has 'Admin' role
 * <button *appHasRole="'Admin'">Admin Only Button</button>
 *
 * // Show if user has any of the specified roles
 * <div *appHasRole="['Admin', 'Manager']">
 *   Content for Admins or Managers
 * </div>
 *
 * // Show if user has all specified roles
 * <div *appHasRole="['Admin', 'Manager']; requireAll: true">
 *   Content for users who are both Admin AND Manager
 * </div>
 *
 * // Else template
 * <div *appHasRole="'Admin'; else noAccess">
 *   Admin content
 * </div>
 * <ng-template #noAccess>
 *   <p>You don't have access</p>
 * </ng-template>
 */
@Directive({
  selector: '[appHasRole]',
  standalone: true
})
export class HasRoleDirective implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private templateRef = inject(TemplateRef<any>);
  private viewContainer = inject(ViewContainerRef);

  private roles: string | string[] = [];
  private requireAll = false;
  private elseTemplateRef: TemplateRef<any> | null = null;
  private subscription?: Subscription;

  @Input()
  set appHasRole(roles: string | string[]) {
    this.roles = roles;
    this.updateView();
  }

  @Input()
  set appHasRoleRequireAll(value: boolean) {
    this.requireAll = value;
    this.updateView();
  }

  @Input()
  set appHasRoleElse(templateRef: TemplateRef<any> | null) {
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
    if (!this.roles) {
      return true; // No roles specified, show by default
    }

    const rolesArray = Array.isArray(this.roles) ? this.roles : [this.roles];

    if (this.requireAll) {
      return this.authService.hasAllRoles(rolesArray);
    } else {
      return this.authService.hasAnyRole(rolesArray);
    }
  }
}
