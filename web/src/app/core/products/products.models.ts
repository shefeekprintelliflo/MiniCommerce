// Mirrors Microservice.Products/Models/ProductDtos.cs. ASP.NET serializes these as camelCase.

export interface ProductResponse {
  id: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  isActive: boolean;
  updatedAtUtc: string;
}
