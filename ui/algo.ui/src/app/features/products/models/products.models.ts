export interface CustomFieldsPayload {
  readonly [key: string]: unknown;
}

export interface ProductDto {
  readonly id: number;
  readonly name: string;
  readonly categoryId: number;
  readonly categoryName: string;
  readonly currencyCode: string;
  readonly price: number;
  readonly discountedPrice: number | null;
  readonly customFields: CustomFieldsPayload | null;
  readonly imageUrl: string | null;
  readonly createdAt: string;
}

export interface CreateProductCommand {
  readonly name: string;
  readonly categoryId: number;
  readonly currencyCode: string;
  readonly price: number;
  readonly discountedPrice?: number | null;
  readonly customFields?: CustomFieldsPayload | null;
  readonly imageUrl?: string | null;
}

export interface UpdateProductRequest {
  readonly name: string;
  readonly categoryId: number;
  readonly currencyCode: string;
  readonly price: number;
  readonly discountedPrice?: number | null;
  readonly customFields?: CustomFieldsPayload | null;
  readonly imageUrl?: string | null;
}
