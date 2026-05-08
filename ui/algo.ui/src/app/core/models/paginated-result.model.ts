export interface PaginatedResult<TItem> {
  readonly items: TItem[];
  readonly pageNumber: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}
