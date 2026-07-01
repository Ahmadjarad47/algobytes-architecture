import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { AdminConfirmDialog } from '../../../../shared/components/admin-confirm-dialog/admin-confirm-dialog';
import { AdminDataTable } from '../../../../shared/components/admin-data-table/admin-data-table';
import { AdminDetailsDrawer } from '../../../../shared/components/admin-details-drawer/admin-details-drawer';
import { AdminFormDialog } from '../../../../shared/components/admin-form-dialog/admin-form-dialog';
import {
  AdminDetailItem,
  AdminFormField,
  AdminRowAction,
  AdminTableColumn
} from '../../../../shared/models/admin-table.model';
import { AppToastService } from '../../../../core/services/app-toast.service';
import { AdminActionBusService } from '../../../../core/services/admin-action-bus.service';
import { Permissions } from '../../../../core/permissions/permission.catalog';
import { PermissionService } from '../../../../core/permissions/permission.service';
import { exportCsv, exportJson, ExportRow } from '../../../../shared/utils/export.utils';
import { CategoriesApiService } from '../../api/categories-api.service';
import {
  CategoryDetailsDto,
  CategoryDto,
  CreateCategoryCommand,
  UpdateCategoryRequest
} from '../../models/categories.models';

@Component({
  selector: 'app-categories-list',
  imports: [
    ReactiveFormsModule,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer,
    AdminConfirmDialog
  ],
  template: `
    <app-admin-data-table
      title="Categories"
      subtitle="Organize shop products into reusable catalog categories."
      [columns]="columns"
      [value]="categories()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="categories().length"
      [globalFilterFields]="globalFilterFields"
      [showCreate]="canCreate()"
      [showExport]="canExport()"
      searchPlaceholder="Search categories"
      emptyTitle="No categories yet"
      emptyMessage="Create a category before adding products."
      [actions]="actions()"
      (refresh)="loadCategories()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
      (exportCsv)="exportRows('categories', $event)"
      (exportJson)="exportRowsJson('categories', $event)"
    />

    <app-admin-form-dialog
      [visible]="formVisible()"
      [title]="editingCategoryId() ? 'Edit category' : 'Create category'"
      [form]="form"
      [fields]="fields"
      [submitLabel]="editingCategoryId() ? 'Save changes' : 'Create category'"
      [loading]="saving()"
      (visibleChange)="closeForm($event)"
      (submit)="save()"
    />

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedCategory()?.name ?? 'Category details'"
      [items]="detailItems()"
      (visibleChange)="detailsVisible.set($event)"
    />

    <app-admin-confirm-dialog
      [visible]="deleteDialogVisible()"
      title="Delete category"
      [message]="'Delete ' + (pendingDeleteCategory()?.name ?? 'this category') + '?'"
      description="Categories with assigned products cannot be deleted."
      confirmLabel="Delete category"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CategoriesList {
  private readonly api = inject(CategoriesApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly permissionService = inject(PermissionService);

  protected readonly categories = signal<CategoryDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly editingCategoryId = signal<number | null>(null);
  protected readonly selectedCategory = signal<CategoryDetailsDto | null>(null);
  protected readonly deleteDialogVisible = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pendingDeleteCategory = signal<CategoryDto | null>(null);

  protected readonly globalFilterFields = ['name', 'description'];

  protected readonly columns: AdminTableColumn[] = [
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'description', header: 'Description', filter: true },
    { field: 'productCount', header: 'Products', sortable: true }
  ];

  protected readonly fields: AdminFormField[] = [
    { key: 'name', label: 'Category name', type: 'text', required: true },
    { key: 'description', label: 'Description', type: 'textarea' }
  ];

  protected readonly canCreate = computed(() =>
    this.permissionService.can({ any: [Permissions.categories.create] })
  );
  protected readonly canUpdate = computed(() =>
    this.permissionService.can({ any: [Permissions.categories.update] })
  );
  protected readonly canDelete = computed(() =>
    this.permissionService.can({ any: [Permissions.categories.delete] })
  );
  protected readonly canExport = computed(() =>
    this.permissionService.can({ any: [Permissions.categories.read] })
  );

  protected readonly actions = computed<AdminRowAction<CategoryDto>[]>(() => [
    { id: 'view', label: 'View category', icon: 'pi pi-eye' },
    ...(this.canUpdate()
      ? [{ id: 'edit', label: 'Edit category', icon: 'pi pi-pencil' } as AdminRowAction<CategoryDto>]
      : []),
    ...(this.canDelete()
      ? [{ id: 'delete', label: 'Delete category', icon: 'pi pi-trash', severity: 'danger' as const }]
      : [])
  ]);

  protected readonly form = this.formBuilder.group({
    name: ['', Validators.required],
    description: ['']
  });

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const category = this.selectedCategory();
    if (!category) {
      return [];
    }

    return [
      { label: 'Category ID', value: category.id },
      { label: 'Name', value: category.name },
      { label: 'Description', value: category.description },
      { label: 'Products', value: category.productCount }
    ];
  });

  constructor() {
    this.loadCategories();
    this.actionBus.actions$.subscribe((action) => {
      if (action === 'create-category' && this.canCreate()) {
        this.openCreate();
      }
    });
  }

  protected loadCategories(): void {
    this.loading.set(true);
    this.api
      .getCategories()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((categories) => this.categories.set(categories));
  }

  protected openCreate(): void {
    this.editingCategoryId.set(null);
    this.form.reset({ name: '', description: '' });
    this.formVisible.set(true);
  }

  protected closeForm(visible: boolean): void {
    this.formVisible.set(visible);
    if (!visible) {
      this.editingCategoryId.set(null);
    }
  }

  protected handleAction(actionId: string, row: CategoryDto): void {
    switch (actionId) {
      case 'view':
        this.api.getCategory(row.id).subscribe((category) => {
          this.selectedCategory.set(category);
          this.detailsVisible.set(true);
        });
        break;
      case 'edit':
        this.editingCategoryId.set(row.id);
        this.form.reset({
          name: row.name,
          description: row.description ?? ''
        });
        this.formVisible.set(true);
        break;
      case 'delete':
        this.pendingDeleteCategory.set(row);
        this.deleteDialogVisible.set(true);
        break;
    }
  }

  protected closeDeleteDialog(visible: boolean): void {
    this.deleteDialogVisible.set(visible);
    if (!visible && !this.deleting()) {
      this.pendingDeleteCategory.set(null);
    }
  }

  protected confirmDelete(): void {
    const category = this.pendingDeleteCategory();
    if (!category || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.api
      .deleteCategory(category.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe(() => {
        this.toast.warn('Category deleted', category.name);
        this.deleteDialogVisible.set(false);
        this.pendingDeleteCategory.set(null);
        this.loadCategories();
      });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    this.saving.set(true);
    const value = this.form.getRawValue();
    const payload = {
      name: value.name,
      description: value.description || null
    };

    const saveRequest = this.editingCategoryId()
      ? this.api.updateCategory(this.editingCategoryId()!, payload as UpdateCategoryRequest)
      : this.api.createCategory(payload as CreateCategoryCommand);

    saveRequest
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => {
        this.toast.success(
          this.editingCategoryId() ? 'Category updated' : 'Category created',
          payload.name
        );
        this.formVisible.set(false);
        this.loadCategories();
      });
  }

  protected exportRows(fileName: string, rows: CategoryDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: CategoryDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }
}
