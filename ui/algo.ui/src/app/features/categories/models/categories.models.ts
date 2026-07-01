export interface CategoryDto {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly productCount: number;
}

export interface CategoryDetailsDto extends CategoryDto {}

export interface CreateCategoryCommand {
  readonly name: string;
  readonly description?: string | null;
}

export interface UpdateCategoryRequest {
  readonly name: string;
  readonly description?: string | null;
}
