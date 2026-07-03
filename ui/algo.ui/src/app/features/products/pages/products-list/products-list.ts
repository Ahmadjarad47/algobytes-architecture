import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, Observable, of, switchMap } from 'rxjs';

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
import { CategoriesApiService } from '../../../categories/api/categories-api.service';
import { CategoryDto } from '../../../categories/models/categories.models';
import { StorageSettingsApiService } from '../../../storage/api/storage-settings-api.service';
import { ProductsApiService } from '../../api/products-api.service';
import { CreateProductCommand, ProductDto, UpdateProductRequest } from '../../models/products.models';

@Component({
  selector: 'app-products-list',
  imports: [
    ReactiveFormsModule,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer,
    AdminConfirmDialog
  ],
  template: `
    <app-admin-data-table
      title="Products"
      subtitle="Catalog products with base pricing, optional discounts, and dynamic custom fields."
      [columns]="columns"
      [value]="products()"
      [loading]="loading()"
      [lazy]="false"
      [rows]="25"
      [totalRecords]="products().length"
      [globalFilterFields]="globalFilterFields"
      [showCreate]="canCreate()"
      [showExport]="canExport()"
      searchPlaceholder="Search products"
      emptyTitle="No products yet"
      emptyMessage="Create products to start selling in the shop."
      [actions]="actions()"
      (refresh)="loadProducts()"
      (create)="openCreate()"
      (rowAction)="handleAction($event.actionId, $event.row)"
      (exportCsv)="exportRows('products', $event)"
      (exportJson)="exportRowsJson('products', $event)"
    />

    <app-admin-form-dialog
      [visible]="formVisible()"
      [title]="editingProductId() ? 'Edit product' : 'Create product'"
      [form]="form"
      [fields]="fields()"
      [submitLabel]="editingProductId() ? 'Save changes' : 'Create product'"
      [loading]="saving()"
      (visibleChange)="closeForm($event)"
      (submit)="save()"
    />

    <app-admin-details-drawer
      [visible]="detailsVisible()"
      [title]="selectedProduct()?.name ?? 'Product details'"
      [items]="detailItems()"
      (visibleChange)="detailsVisible.set($event)"
    />

    <app-admin-confirm-dialog
      [visible]="deleteDialogVisible()"
      title="Delete product"
      [message]="'Delete ' + (pendingDeleteProduct()?.name ?? 'this product') + '?'"
      description="This permanently removes the product record."
      confirmLabel="Delete"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductsList {
  private readonly api = inject(ProductsApiService);
  private readonly storageApi = inject(StorageSettingsApiService);
  private readonly categoriesApi = inject(CategoriesApiService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly toast = inject(AppToastService);
  private readonly actionBus = inject(AdminActionBusService);
  private readonly permissionService = inject(PermissionService);

  protected readonly products = signal<ProductDto[]>([]);
  protected readonly categories = signal<CategoryDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly formVisible = signal(false);
  protected readonly detailsVisible = signal(false);
  protected readonly editingProductId = signal<number | null>(null);
  protected readonly selectedProduct = signal<ProductDto | null>(null);
  protected readonly deleteDialogVisible = signal(false);
  protected readonly deleting = signal(false);
  protected readonly pendingDeleteProduct = signal<ProductDto | null>(null);

  protected readonly globalFilterFields = ['name', 'categoryName', 'currencyCode'];
  protected readonly columns: AdminTableColumn[] = [
    { field: 'imageUrl', header: 'Image', cellType: 'image', widthClass: 'w-20' },
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'categoryName', header: 'Category', sortable: true, filter: true },
    { field: 'currencyCode', header: 'Currency', sortable: true, filter: true },
    { field: 'price', header: 'Price', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'discountedPrice', header: 'Discounted price', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'createdAt', header: 'Created', cellType: 'date', sortable: true }
  ];

  protected readonly fields = computed<AdminFormField[]>(() => [
    { key: 'name', label: 'Product name', type: 'text', required: true },
    {
      key: 'categoryId',
      label: 'Category',
      type: 'select',
      required: true,
      options: this.categories().map((category) => ({
        label: category.name,
        value: category.id
      }))
    },
    { key: 'currencyCode', label: 'Currency code', type: 'text', required: true },
    { key: 'price', label: 'Price', type: 'number', required: true },
    { key: 'discountedPrice', label: 'Discounted price', type: 'number' },
    {
      key: 'imageFile',
      label: 'Product image',
      type: 'file',
      accept: 'image/jpeg,image/png,image/webp,image/gif'
    },
    {
      key: 'customFieldsJson',
      label: 'Custom fields JSON',
      type: 'json',
      placeholder: '{"tier":"standard"}'
    }
  ]);

  protected readonly canCreate = computed(() =>
    this.permissionService.can({ any: [Permissions.products.create] })
  );
  protected readonly canUpdate = computed(() =>
    this.permissionService.can({ any: [Permissions.products.update] })
  );
  protected readonly canDelete = computed(() =>
    this.permissionService.can({ any: [Permissions.products.delete] })
  );
  protected readonly canExport = computed(() =>
    this.permissionService.can({ any: [Permissions.products.read] })
  );

  protected readonly actions = computed<AdminRowAction<ProductDto>[]>(() => [
    { id: 'view', label: 'View product', icon: 'pi pi-eye' },
    ...(this.canUpdate()
      ? [{ id: 'edit', label: 'Edit product', icon: 'pi pi-pencil' } as AdminRowAction<ProductDto>]
      : []),
    ...(this.canDelete()
      ? [{ id: 'delete', label: 'Delete product', icon: 'pi pi-trash', severity: 'danger' as const }]
      : [])
  ]);

  protected readonly form = this.formBuilder.group({
    name: ['', Validators.required],
    categoryId: [0, [Validators.required, Validators.min(1)]],
    currencyCode: ['USD', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    discountedPrice: [null as number | null, Validators.min(0)],
    imageFile: [null as File | null],
    imageUrl: [null as string | null],
    customFieldsJson: ['']
  });

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const product = this.selectedProduct();
    if (!product) {
      return [];
    }

    return [
      { label: 'Product ID', value: product.id },
      { label: 'Name', value: product.name },
      { label: 'Category', value: product.categoryName },
      { label: 'Currency', value: product.currencyCode },
      { label: 'Price', value: product.price },
      { label: 'Discounted price', value: product.discountedPrice },
      { label: 'Image URL', value: product.imageUrl },
      { label: 'Created at', value: product.createdAt, type: 'date' },
      { label: 'Custom fields', value: product.customFields, type: 'json' }
    ];
  });

  constructor() {
    this.loadCategories();
    this.loadProducts();
    this.actionBus.actions$.subscribe((action) => {
      if (action === 'create-product' && this.canCreate()) {
        this.openCreate();
      }
    });
  }

  protected loadProducts(): void {
    this.loading.set(true);
    this.api
      .getProducts()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((products) => this.products.set(products));
  }

  protected openCreate(): void {
    this.editingProductId.set(null);
    this.form.reset({
      name: '',
      categoryId: this.categories()[0]?.id ?? 0,
      currencyCode: 'USD',
      price: 0,
      discountedPrice: null,
      imageFile: null,
      imageUrl: null,
      customFieldsJson: ''
    });
    this.formVisible.set(true);
  }

  protected closeForm(visible: boolean): void {
    this.formVisible.set(visible);
    if (!visible) {
      this.editingProductId.set(null);
    }
  }

  protected handleAction(actionId: string, row: ProductDto): void {
    switch (actionId) {
      case 'view':
        this.api.getProduct(row.id).subscribe((product) => {
          this.selectedProduct.set(product);
          this.detailsVisible.set(true);
        });
        break;
      case 'edit':
        this.editingProductId.set(row.id);
        this.form.reset({
          name: row.name,
          categoryId: row.categoryId,
          currencyCode: row.currencyCode,
          price: row.price,
          discountedPrice: row.discountedPrice,
          imageFile: null,
          imageUrl: row.imageUrl,
          customFieldsJson: row.customFields ? JSON.stringify(row.customFields, null, 2) : ''
        });
        this.formVisible.set(true);
        break;
      case 'delete':
        this.pendingDeleteProduct.set(row);
        this.deleteDialogVisible.set(true);
        break;
    }
  }

  protected closeDeleteDialog(visible: boolean): void {
    this.deleteDialogVisible.set(visible);
    if (!visible && !this.deleting()) {
      this.pendingDeleteProduct.set(null);
    }
  }

  protected confirmDelete(): void {
    const product = this.pendingDeleteProduct();
    if (!product || this.deleting()) {
      return;
    }

    this.deleting.set(true);
    this.api
      .deleteProduct(product.id)
      .pipe(finalize(() => this.deleting.set(false)))
      .subscribe(() => {
        this.toast.warn('Product deleted', product.name);
        this.deleteDialogVisible.set(false);
        this.pendingDeleteProduct.set(null);
        this.loadProducts();
      });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    const value = this.form.getRawValue();
    const customFields = parseJsonObject(value.customFieldsJson);
    if (value.customFieldsJson.trim() && customFields === null) {
      this.toast.error('Invalid custom fields', 'Custom fields must be a valid JSON object.');
      return;
    }

    this.saving.set(true);
    const imageUrlRequest: Observable<{ readonly url: string | null }> = value.imageFile
      ? this.storageApi.uploadProductImage(value.imageFile)
      : of({ url: value.imageUrl });

    imageUrlRequest
      .pipe(
        switchMap(({ url }) => {
          const payload = {
            name: value.name,
            categoryId: value.categoryId,
            currencyCode: value.currencyCode.trim().toUpperCase(),
            price: value.price,
            discountedPrice: value.discountedPrice,
            customFields,
            imageUrl: url
          };

          return this.editingProductId()
            ? this.api.updateProduct(this.editingProductId()!, payload as UpdateProductRequest)
            : this.api.createProduct(payload as CreateProductCommand);
        }),
        finalize(() => this.saving.set(false))
      )
      .subscribe(() => {
        this.toast.success(
          this.editingProductId() ? 'Product updated' : 'Product created',
          value.name
        );
        this.formVisible.set(false);
        this.loadProducts();
      });
  }

  protected exportRows(fileName: string, rows: ProductDto[]): void {
    exportCsv(fileName, rows as unknown as ExportRow[]);
  }

  protected exportRowsJson(fileName: string, rows: ProductDto[]): void {
    exportJson(fileName, rows as unknown as ExportRow[]);
  }

  private loadCategories(): void {
    this.categoriesApi.getCategories().subscribe((categories) => this.categories.set(categories));
  }
}

function parseJsonObject(value: string): Record<string, unknown> | null {
  if (!value.trim()) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as unknown;
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return null;
    }

    return parsed as Record<string, unknown>;
  } catch {
    return null;
  }
}
