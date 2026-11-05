// Mirrors Microservice.Orders/Models/OrderDtos.cs. ASP.NET serializes these as camelCase.

export interface OrderLine {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface OrderResponse {
  id: string;
  status: string;
  total: number;
  failureReason: string | null;
  createdAtUtc: string;
  lines: OrderLine[];
}
