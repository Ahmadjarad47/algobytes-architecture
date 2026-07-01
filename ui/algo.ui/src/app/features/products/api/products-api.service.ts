import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  CreateProductCommand,
  ProductDto,
  UpdateProductRequest
} from '../models/products.models';

@Injectable({ providedIn: 'root' })
export class ProductsApiService {
  private readonly api = inject(ApiService);

  getProducts(): Observable<ProductDto[]> {
    return this.api.get<ProductDto[]>('/Products');
  }

  getProduct(id: number): Observable<ProductDto> {
    return this.api.get<ProductDto>(`/Products/${id}`);
  }

  createProduct(command: CreateProductCommand): Observable<ProductDto> {
    return this.api.post<ProductDto, CreateProductCommand>('/Products', command);
  }

  updateProduct(id: number, command: UpdateProductRequest): Observable<ProductDto> {
    return this.api.put<ProductDto, UpdateProductRequest>(`/Products/${id}`, command);
  }

  deleteProduct(id: number): Observable<void> {
    return this.api.delete<void>(`/Products/${id}`);
  }
}
