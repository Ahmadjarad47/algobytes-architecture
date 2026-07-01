import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonModule } from 'primeng/button';

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
import {
  CreateProductCommand,
  ProductDto,
  UpdateProductRequest
} from '../../models/products.models';

@Component({
  selector: 'app-products-list',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    AdminDataTable,
    AdminFormDialog,
    AdminDetailsDrawer,
    AdminConfirmDialog
  ],
  template: `
    <app-admin-data-table
      title="Products"
      subtitle="Game shop catalog with pricing, provider metadata, and category assignment."
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
      emptyMessage="Create a product to populate the shop catalog."
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
    >
      <div adminFormExtras class="md:col-span-2 rounded-2xl border border-slate-200 bg-slate-50/80 p-4">
        <div class="text-sm font-semibold text-slate-950">Product image</div>
        <p class="mt-1 text-[12px] text-slate-500">
          Uploads to Amazon S3 using the storage settings saved in Settings.
        </p>

        @if (imagePreviewUrl()) {
          <div class="mt-3 overflow-hidden rounded-xl border border-slate-200 bg-white">
            <img [src]="imagePreviewUrl()!" [alt]="form.controls.name.value || 'Product image'" class="max-h-48 w-full object-contain" />
          </div>
        }

        <div class="mt-3 flex flex-wrap items-center gap-2">
          <input
            #imageInput
            type="file"
            accept="image/png,image/jpeg,image/webp,image/gif"
            class="hidden"
            (change)="onImageSelected($event)"
          />
          <p-button
            label="Choose image"
            icon="pi pi-upload"
            size="small"
            [outlined]="true"
            [loading]="imageUploading()"
            (onClick)="imageInput.click()"
          />
          @if (imagePreviewUrl()) {
            <p-button
              label="Remove image"
              icon="pi pi-times"
              size="small"
              severity="secondary"
              [outlined]="true"
              [disabled]="imageUploading()"
              (onClick)="clearImage()"
            />
          }
        </div>
      </div>
    </app-admin-form-dialog>

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
      description="This action permanently removes the product from the catalog."
      confirmLabel="Delete product"
      [loading]="deleting()"
      (visibleChange)="closeDeleteDialog($event)"
      (confirm)="confirmDelete()"
    />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductsList {
  private readonly api = inject(ProductsApiService);
  private readonly categoriesApi = inject(CategoriesApiService);
  private readonly storageApi = inject(StorageSettingsApiService);
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
  protected readonly imagePreviewUrl = signal<string | null>(null);
  protected readonly imageUploading = signal(false);

  protected readonly globalFilterFields = [
    'name',
    'categoryName',
    'provider',
    'externalGameId'
  ];

  protected readonly columns: AdminTableColumn[] = [
    { field: 'name', header: 'Name', sortable: true, filter: true },
    { field: 'categoryName', header: 'Category', sortable: true, filter: true },
    { field: 'imageUrl', header: 'Image', cellType: 'image' },
    { field: 'priceUsd', header: 'Price (USD)', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'priceSyp', header: 'Price (SYP)', sortable: true, cellType: 'currency', currencyCode: 'SYP' },
    { field: 'discountedPriceUsd', header: 'Discount (USD)', sortable: true, cellType: 'currency', currencyCode: 'USD' },
    { field: 'discountedPriceSyp', header: 'Discount (SYP)', sortable: true, cellType: 'currency', currencyCode: 'SYP' },
    { field: 'provider', header: 'Provider', sortable: true, filter: true },
    { field: 'externalGameId', header: 'External ID', filter: true },
    { field: 'createdAt', header: 'Created', cellType: 'date' }
  ];

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
    { key: 'priceUsd', label: 'Price (USD)', type: 'number' },
    { key: 'priceSyp', label: 'Price (SYP)', type: 'number' },
    { key: 'discountedPriceUsd', label: 'Discounted price (USD)', type: 'number' },
    { key: 'discountedPriceSyp', label: 'Discounted price (SYP)', type: 'number' },
    { key: 'provider', label: 'Provider', type: 'text' },
    { key: 'externalGameId', label: 'External game ID', type: 'text' }
  ]);

  protected readonly form = this.formBuilder.group(
    {
      name: ['', Validators.required],
      categoryId: [0, [Validators.required, Validators.min(1)]],
      priceUsd: [null as number | null, Validators.min(0)],
      priceSyp: [null as number | null, Validators.min(0)],
      discountedPriceUsd: [null as number | null, Validators.min(0)],
      discountedPriceSyp: [null as number | null, Validators.min(0)],
      provider: [''],
      externalGameId: [''],
      imageUrl: ['']
    },
    { validators: [requireAtLeastOnePrice] }
  );

  protected readonly detailItems = computed<AdminDetailItem[]>(() => {
    const product = this.selectedProduct();
    if (!product) {
      return [];
    }

    return [
      { label: 'Product ID', value: product.id },
      { label: 'Name', value: product.name },
      { label: 'Category', value: product.categoryName },
      { label: 'Price (USD)', value: product.priceUsd },
      { label: 'Price (SYP)', value: product.priceSyp },
      { label: 'Discounted price (USD)', value: product.discountedPriceUsd },
      { label: 'Discounted price (SYP)', value: product.discountedPriceSyp },
      { label: 'Provider', value: product.provider },
      { label: 'Image URL', value: product.imageUrl },
      { label: 'External game ID', value: product.externalGameId },
      { label: 'Created at', value: product.createdAt, type: 'date' },
      { label: 'Updated at', value: product.updatedAt, type: 'date' }
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
      priceUsd: null,
      priceSyp: null,
      discountedPriceUsd: null,
      discountedPriceSyp: null,
      provider: '',
      externalGameId: '',
      imageUrl: ''
    });
    this.imagePreviewUrl.set(null);
    this.formVisible.set(true);
  }

  protected closeForm(visible: boolean): void {
    this.formVisible.set(visible);
    if (!visible) {
      this.editingProductId.set(null);
      this.imagePreviewUrl.set(null);
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
          priceUsd: row.priceUsd,
          priceSyp: row.priceSyp,
          discountedPriceUsd: row.discountedPriceUsd,
          discountedPriceSyp: row.discountedPriceSyp,
          provider: row.provider ?? '',
          externalGameId: row.externalGameId ?? '',
          imageUrl: row.imageUrl ?? ''
        });
        this.imagePreviewUrl.set(row.imageUrl);
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

    this.saving.set(true);
    const value = this.form.getRawValue();
    const payload = {
      name: value.name,
      categoryId: value.categoryId,
      priceUsd: value.priceUsd,
      priceSyp: value.priceSyp,
      discountedPriceUsd: value.discountedPriceUsd,
      discountedPriceSyp: value.discountedPriceSyp,
      externalGameId: value.externalGameId || null,
      provider: value.provider || null,
      imageUrl: value.imageUrl || null
    };

    const saveRequest = this.editingProductId()
      ? this.api.updateProduct(this.editingProductId()!, payload as UpdateProductRequest)
      : this.api.createProduct(payload as CreateProductCommand);

    saveRequest
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe(() => {
        this.toast.success(
          this.editingProductId() ? 'Product updated' : 'Product created',
          payload.name
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

  protected onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.item(0) ?? null;
    if (!file || this.imageUploading()) {
      return;
    }

    this.imageUploading.set(true);
    this.storageApi
      .uploadProductImage(file)
      .pipe(finalize(() => this.imageUploading.set(false)))
      .subscribe({
        next: ({ url }) => {
          this.form.patchValue({ imageUrl: url });
          this.imagePreviewUrl.set(url);
          this.toast.success('Image uploaded', 'Product image stored in S3.');
        },
        error: () => {
          this.toast.error('Upload failed', 'Configure S3 in Settings or try another image.');
        }
      });

    if (input) {
      input.value = '';
    }
  }

  protected clearImage(): void {
    this.form.patchValue({ imageUrl: '' });
    this.imagePreviewUrl.set(null);
  }
}

function requireAtLeastOnePrice(control: AbstractControl): ValidationErrors | null {
  const priceUsd = control.get('priceUsd')?.value;
  const priceSyp = control.get('priceSyp')?.value;
  const hasUsd = priceUsd !== null && priceUsd !== undefined && priceUsd !== '';
  const hasSyp = priceSyp !== null && priceSyp !== undefined && priceSyp !== '';
  return hasUsd || hasSyp ? null : { requireAtLeastOnePrice: true };
}
