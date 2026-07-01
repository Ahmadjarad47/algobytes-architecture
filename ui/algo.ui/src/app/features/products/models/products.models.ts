export interface ProductDto {
  readonly id: number;
  readonly name: string;
  readonly categoryId: number;
  readonly categoryName: string;
  readonly priceUsd: number | null;
  readonly priceSyp: number | null;
  readonly discountedPriceUsd: number | null;
  readonly discountedPriceSyp: number | null;
  readonly externalGameId: string | null;
  readonly provider: string | null;
  readonly imageUrl: string | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
  readonly trashedAt: string | null;
  readonly trashExpiresAt: string | null;
  readonly deletedAt: string | null;
}

export interface CreateProductCommand {
  readonly name: string;
  readonly categoryId: number;
  readonly priceUsd?: number | null;
  readonly priceSyp?: number | null;
  readonly discountedPriceUsd?: number | null;
  readonly discountedPriceSyp?: number | null;
  readonly externalGameId?: string | null;
  readonly provider?: string | null;
  readonly imageUrl?: string | null;
}

export interface UpdateProductRequest {
  readonly name: string;
  readonly categoryId: number;
  readonly priceUsd?: number | null;
  readonly priceSyp?: number | null;
  readonly discountedPriceUsd?: number | null;
  readonly discountedPriceSyp?: number | null;
  readonly externalGameId?: string | null;
  readonly provider?: string | null;
  readonly imageUrl?: string | null;
}
