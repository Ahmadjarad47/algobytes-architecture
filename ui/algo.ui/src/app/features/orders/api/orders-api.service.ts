import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import { CreateOrderCommand, OrderDto } from '../models/orders.models';

@Injectable({ providedIn: 'root' })
export class OrdersApiService {
  private readonly api = inject(ApiService);

  getOrders(): Observable<OrderDto[]> {
    return this.api.get<OrderDto[]>('/Orders');
  }

  getOrder(id: number): Observable<OrderDto> {
    return this.api.get<OrderDto>(`/Orders/${id}`);
  }

  createOrder(command: CreateOrderCommand): Observable<OrderDto> {
    return this.api.post<OrderDto, CreateOrderCommand>('/Orders', command);
  }
}
