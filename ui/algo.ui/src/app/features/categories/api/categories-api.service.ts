import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  CategoryDetailsDto,
  CategoryDto,
  CreateCategoryCommand,
  UpdateCategoryRequest
} from '../models/categories.models';

@Injectable({ providedIn: 'root' })
export class CategoriesApiService {
  private readonly api = inject(ApiService);

  getCategories(query?: { includeTrashed?: boolean; onlyTrashed?: boolean }): Observable<CategoryDto[]> {
    return this.api.get<CategoryDto[]>('/Categories', query);
  }

  getCategory(id: number): Observable<CategoryDetailsDto> {
    return this.api.get<CategoryDetailsDto>(`/Categories/${id}`);
  }

  createCategory(command: CreateCategoryCommand): Observable<CategoryDetailsDto> {
    return this.api.post<CategoryDetailsDto, CreateCategoryCommand>('/Categories', command);
  }

  updateCategory(id: number, command: UpdateCategoryRequest): Observable<CategoryDetailsDto> {
    return this.api.put<CategoryDetailsDto, UpdateCategoryRequest>(`/Categories/${id}`, command);
  }

  deleteCategory(id: number): Observable<void> {
    return this.api.delete<void>(`/Categories/${id}`);
  }

  restoreCategory(id: number): Observable<void> {
    return this.api.patch<void>(`/Categories/${id}/restore`);
  }
}
