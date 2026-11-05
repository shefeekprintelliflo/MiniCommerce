// Mirrors Microservice.Orders/Models/OrderDtos.cs. ASP.NET serializes these as camelCase.

export interface OrderLine {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

// Must match the names registered in Microservice.Orders/Payments.
export type PaymentMethod = 'card' | 'paypal' | 'cashOnDelivery';

export const paymentMethodLabels: Record<PaymentMethod, string> = {
  card: 'Credit or debit card',
  paypal: 'PayPal',
  cashOnDelivery: 'Cash on delivery',
};

export interface OrderResponse {
  id: string;
  status: string;
  total: number;
  paymentMethod: PaymentMethod | null;
  failureReason: string | null;
  createdAtUtc: string;
  lines: OrderLine[];
}
