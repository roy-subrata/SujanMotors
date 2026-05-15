import { Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TabsModule } from 'primeng/tabs';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import { PasswordModule } from 'primeng/password';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';
import { AdminService, UserResponse, RoleResponse, PermissionResponse } from '../../shared/services/admin.service';
import { PriceCodeService } from '../../shared/services/price-code.service';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TabsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    DialogModule,
    ToastModule,
    TagModule,
    ConfirmDialogModule,
    MultiSelectModule,
    CheckboxModule,
    PasswordModule,
    TooltipModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './admin-settings.component.html',
  styleUrls: ['./admin-settings.component.css']
})
export class AdminSettingsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  readonly priceCodeService = inject(PriceCodeService);

  // Active tab (regular variable for two-way binding with p-tabs)
  activeTabIndex: number = 0;

  // Data signals
  users = signal<UserResponse[]>([]);
  roles = signal<RoleResponse[]>([]);
  permissions = signal<PermissionResponse[]>([]);

  // Loading states
  usersLoading = signal(false);
  rolesLoading = signal(false);
  permissionsLoading = signal(false);

  // Dialog states
  userDialogVisible = signal(false);
  roleDialogVisible = signal(false);
  permissionDialogVisible = signal(false);
  assignRolesDialogVisible = signal(false);
  assignPermissionsDialogVisible = signal(false);

  // Forms
  userForm!: FormGroup;
  roleForm!: FormGroup;
  permissionForm!: FormGroup;

  // Edit mode
  editingUser = signal<UserResponse | null>(null);
  editingRole = signal<RoleResponse | null>(null);

  // Role assignment
  selectedUserForRoles = signal<UserResponse | null>(null);
  selectedUserRoles = signal<string[]>([]);

  // Permission assignment
  selectedRoleForPermissions = signal<RoleResponse | null>(null);
  selectedPermissionIds = signal<string[]>([]);

  ngOnInit(): void {
    this.initializeForms();
    this.loadAllData();
  }

  private initializeForms(): void {
    this.userForm = this.fb.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(8)]],
      isActive: [true]
    });

    this.roleForm = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      isActive: [true]
    });

    this.permissionForm = this.fb.group({
      name: ['', Validators.required],
      displayName: ['', Validators.required],
      category: ['', Validators.required],
      description: ['']
    });
  }

  private loadAllData(): void {
    this.loadUsers();
    this.loadRoles();
    this.loadPermissions();
  }

  // User Management
  loadUsers(): void {
    this.usersLoading.set(true);
    this.adminService.getAllUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.usersLoading.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load users'
        });
        this.usersLoading.set(false);
      }
    });
  }

  openUserDialog(user?: UserResponse): void {
    if (user) {
      this.editingUser.set(user);
      this.userForm.patchValue({
        username: user.username,
        email: user.email,
        firstName: user.firstName,
        lastName: user.lastName,
        isActive: user.isActive
      });
      this.userForm.get('username')?.disable();
      this.userForm.get('password')?.clearValidators();
      this.userForm.get('password')?.updateValueAndValidity();
    } else {
      this.editingUser.set(null);
      this.userForm.reset({ isActive: true });
      this.userForm.get('username')?.enable();
      this.userForm.get('password')?.setValidators([Validators.required, Validators.minLength(8)]);
      this.userForm.get('password')?.updateValueAndValidity();
    }
    this.userDialogVisible.set(true);
  }

  saveUser(): void {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    const formValue = this.userForm.getRawValue();
    const editingUser = this.editingUser();

    if (editingUser) {
      // Update existing user
      const updateRequest = {
        firstName: formValue.firstName,
        lastName: formValue.lastName,
        email: formValue.email,
        isActive: formValue.isActive
      };

      this.adminService.updateUser(editingUser.id, updateRequest).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'User updated successfully'
          });
          this.userDialogVisible.set(false);
          this.loadUsers();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to update user'
          });
        }
      });
    } else {
      // Create new user
      this.adminService.createUser(formValue).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'User created successfully'
          });
          this.userDialogVisible.set(false);
          this.loadUsers();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to create user'
          });
        }
      });
    }
  }

  toggleUserStatus(user: UserResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to ${user.isActive ? 'deactivate' : 'activate'} this user?`,
      header: 'Confirm Action',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.adminService.toggleUserStatus(user.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `User ${user.isActive ? 'deactivated' : 'activated'} successfully`
            });
            this.loadUsers();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to update user status'
            });
          }
        });
      }
    });
  }

  openAssignRolesDialog(user: UserResponse): void {
    this.selectedUserForRoles.set(user);
    this.selectedUserRoles.set([...user.roles]);
    this.assignRolesDialogVisible.set(true);
  }

  saveUserRoles(): void {
    const user = this.selectedUserForRoles();
    if (!user) return;

    this.adminService.assignRolesToUser(user.id, { roles: this.selectedUserRoles() }).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Roles assigned successfully'
        });
        this.assignRolesDialogVisible.set(false);
        this.loadUsers();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to assign roles'
        });
      }
    });
  }

  // Role Management
  loadRoles(): void {
    this.rolesLoading.set(true);
    this.adminService.getAllRoles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.rolesLoading.set(false);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load roles'
        });
        this.rolesLoading.set(false);
      }
    });
  }

  openRoleDialog(role?: RoleResponse): void {
    if (role) {
      this.editingRole.set(role);
      this.roleForm.patchValue(role);
    } else {
      this.editingRole.set(null);
      this.roleForm.reset({ isActive: true });
    }
    this.roleDialogVisible.set(true);
  }

  saveRole(): void {
    if (this.roleForm.invalid) {
      this.roleForm.markAllAsTouched();
      return;
    }

    const formValue = this.roleForm.value;
    const editingRole = this.editingRole();

    if (editingRole) {
      this.adminService.updateRole(editingRole.id, formValue).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Role updated successfully'
          });
          this.roleDialogVisible.set(false);
          this.loadRoles();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to update role'
          });
        }
      });
    } else {
      this.adminService.createRole(formValue).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Role created successfully'
          });
          this.roleDialogVisible.set(false);
          this.loadRoles();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to create role'
          });
        }
      });
    }
  }

  deleteRole(role: RoleResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the role "${role.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.adminService.deleteRole(role.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Role deleted successfully'
            });
            this.loadRoles();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error.error?.message || 'Failed to delete role'
            });
          }
        });
      }
    });
  }

  openAssignPermissionsDialog(role: RoleResponse): void {
    this.selectedRoleForPermissions.set(role);

    // Load current permissions for this role
    this.adminService.getRolePermissions(role.id).subscribe({
      next: (permissions) => {
        this.selectedPermissionIds.set(permissions.map(p => p.id));
        this.assignPermissionsDialogVisible.set(true);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load role permissions'
        });
      }
    });
  }

  saveRolePermissions(): void {
    const role = this.selectedRoleForPermissions();
    if (!role) return;

    this.adminService.assignPermissionsToRole(role.id, {
      permissionIds: this.selectedPermissionIds()
    }).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Permissions assigned successfully'
        });
        this.assignPermissionsDialogVisible.set(false);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to assign permissions'
        });
      }
    });
  }

  // Permission Management
  loadPermissions(): void {
    this.permissionsLoading.set(true);
    this.adminService.getAllPermissions().subscribe({
      next: (permissions) => {
        this.permissions.set(permissions);
        this.permissionsLoading.set(false);
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load permissions'
        });
        this.permissionsLoading.set(false);
      }
    });
  }

  openPermissionDialog(): void {
    this.permissionForm.reset();
    this.permissionDialogVisible.set(true);
  }

  savePermission(): void {
    if (this.permissionForm.invalid) {
      this.permissionForm.markAllAsTouched();
      return;
    }

    this.adminService.createPermission(this.permissionForm.value).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Permission created successfully'
        });
        this.permissionDialogVisible.set(false);
        this.loadPermissions();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.message || 'Failed to create permission'
        });
      }
    });
  }

  getRoleOptions() {
    return this.roles().map(role => ({
      label: role.name,
      value: role.name
    }));
  }

  getPermissionOptions() {
    return this.permissions().map(permission => ({
      label: `${permission.displayName} (${permission.category})`,
      value: permission.id
    }));
  }

  getGroupedPermissions(): { category: string; permissions: PermissionResponse[] }[] {
    const groups = new Map<string, PermissionResponse[]>();
    for (const p of this.permissions()) {
      if (!groups.has(p.category)) groups.set(p.category, []);
      groups.get(p.category)!.push(p);
    }
    return Array.from(groups.entries())
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([category, permissions]) => ({ category, permissions }));
  }

  isPermissionSelected(id: string): boolean {
    return this.selectedPermissionIds().includes(id);
  }

  togglePermission(id: string): void {
    const current = this.selectedPermissionIds();
    if (current.includes(id)) {
      this.selectedPermissionIds.set(current.filter(p => p !== id));
    } else {
      this.selectedPermissionIds.set([...current, id]);
    }
  }

  toggleCategoryPermissions(category: string): void {
    const group = this.getGroupedPermissions().find(g => g.category === category);
    if (!group) return;
    const ids = group.permissions.map(p => p.id);
    const allSelected = ids.every(id => this.selectedPermissionIds().includes(id));
    if (allSelected) {
      this.selectedPermissionIds.set(this.selectedPermissionIds().filter(id => !ids.includes(id)));
    } else {
      const merged = new Set([...this.selectedPermissionIds(), ...ids]);
      this.selectedPermissionIds.set(Array.from(merged));
    }
  }

  isCategoryAllSelected(category: string): boolean {
    const group = this.getGroupedPermissions().find(g => g.category === category);
    if (!group || group.permissions.length === 0) return false;
    return group.permissions.every(p => this.selectedPermissionIds().includes(p.id));
  }

  isCategoryPartial(category: string): boolean {
    const group = this.getGroupedPermissions().find(g => g.category === category);
    if (!group) return false;
    const selected = group.permissions.filter(p => this.selectedPermissionIds().includes(p.id));
    return selected.length > 0 && selected.length < group.permissions.length;
  }

  selectAllPermissions(): void {
    this.selectedPermissionIds.set(this.permissions().map(p => p.id));
  }

  clearAllPermissions(): void {
    this.selectedPermissionIds.set([]);
  }

  getStatusSeverity(isActive: boolean): 'success' | 'danger' {
    return isActive ? 'success' : 'danger';
  }

  // ============= Price Code Management =============

  priceCodeWord = '';
  priceCodePrefix = '';
  priceCodeSuffix = '';
  priceCodeSaving = signal(false);
  priceCodePreviewPrice = 1234567890;

  private priceCodeEffect = effect(() => {
    // Auto-populate inputs once the service loads from server
    if (this.priceCodeService.loaded()) {
      if (!this.priceCodeWord) this.priceCodeWord = this.priceCodeService.magicWord();
      if (!this.priceCodePrefix) this.priceCodePrefix = this.priceCodeService.prefix();
      if (!this.priceCodeSuffix) this.priceCodeSuffix = this.priceCodeService.suffix();
    }
  });

  initPriceCode(): void {
    this.priceCodeWord = this.priceCodeService.magicWord();
    this.priceCodePrefix = this.priceCodeService.prefix();
    this.priceCodeSuffix = this.priceCodeService.suffix();
  }

  getPriceCodeValidation(): { valid: boolean; error?: string } {
    return this.priceCodeService.validateMagicWord(this.priceCodeWord);
  }

  getPriceCodePreview(): string {
    const validation = this.priceCodeService.validateMagicWord(this.priceCodeWord);
    if (!validation.valid) return '—';

    const word = this.priceCodeWord.toUpperCase();
    // Preview: map digits 1234567890 to letters
    let result = '';
    const digits = '1234567890';
    for (const d of digits) {
      const digit = parseInt(d, 10);
      const index = digit === 0 ? 9 : digit - 1;
      result += word[index];
    }
    return result;
  }

  savePriceCode(): void {
    const validation = this.getPriceCodeValidation();
    if (!validation.valid) {
      this.messageService.add({ severity: 'error', summary: 'Error', detail: validation.error || 'Invalid magic word' });
      return;
    }

    this.priceCodeSaving.set(true);
    this.priceCodeService.saveMagicWord(this.priceCodeWord, this.priceCodePrefix, this.priceCodeSuffix).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Price code word saved successfully' });
        this.priceCodeSaving.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to save price code word')
        });
        this.priceCodeSaving.set(false);
      }
    });
  }

  clearPriceCode(): void {
    this.confirmationService.confirm({
      message: 'Are you sure you want to clear the price code? Cost prices will be shown as plain numbers.',
      header: 'Clear Price Code',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.priceCodeSaving.set(true);
        this.priceCodeService.clearMagicWord().subscribe({
          next: () => {
            this.priceCodeWord = '';
            this.priceCodePrefix = '';
            this.priceCodeSuffix = '';
            this.messageService.add({ severity: 'success', summary: 'Cleared', detail: 'Price code removed. Cost prices will display as numbers.' });
            this.priceCodeSaving.set(false);
          },
          error: () => {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to clear price code' });
            this.priceCodeSaving.set(false);
          }
        });
      }
    });
  }
}
