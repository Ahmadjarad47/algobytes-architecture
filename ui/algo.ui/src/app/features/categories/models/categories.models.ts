export interface CategoryDto {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly imageUrl: string | null;
  readonly productCount: number;
  readonly trashedAt: string | null;
  readonly trashExpiresAt: string | null;
  readonly deletedAt: string | null;
}

export interface CategoryDetailsDto extends CategoryDto {}

export interface CreateCategoryCommand {
  readonly name: string;
  readonly description?: string | null;
  readonly imageUrl?: string | null;
}

export interface UpdateCategoryRequest {
  readonly name: string;
  readonly description?: string | null;
  readonly imageUrl?: string | null;
}
