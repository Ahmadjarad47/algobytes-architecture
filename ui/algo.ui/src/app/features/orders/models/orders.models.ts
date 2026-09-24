export interface CustomFieldsPayload {
  readonly [key: string]: unknown;
}

export interface OrderItemDto {
  readonly id: number;
  readonly orderId: number;
  readonly productId: number;
  readonly quantity: number;
  readonly unitPrice: number;
}

export interface PaymentDto {
  readonly id: number;
  readonly orderId: number;
  readonly currencyCode: string;
  readonly gatewayName: string;
  readonly gatewayTransactionId: string;
  readonly amount: number;
  readonly paymentStatus: string;
}

export interface OrderDto {
  readonly id: number;
  readonly userId: string;
  readonly orderNumber: string;
  readonly currencyCode: string;
  readonly totalAmount: number;
  readonly exchangeRateUsedToBase: number | null;
  readonly paymentMethod: string | null;
  readonly orderStatus: string;
  readonly createdAt: string;
  readonly customFields: CustomFieldsPayload | null;
  readonly items: readonly OrderItemDto[];
  readonly payments: readonly PaymentDto[];
}

export interface CreateOrderItemModel {
  readonly productId: number;
  readonly quantity: number;
}

export interface CreateOrderCommand {
  readonly orderNumber: string;
  readonly paymentMethod?: string | null;
  readonly exchangeRateUsedToBase?: number | null;
  readonly items: readonly CreateOrderItemModel[];
  readonly customFields?: CustomFieldsPayload | null;
}
