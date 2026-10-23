/**
 * Category response contract matching backend `CategoryResponse`.
 */
export interface CategoryResponse {
  id: string;
  name: string;
  isActive: boolean;
  products: ProductSummaryResponse[];
}

/**
 * Product summary nested in category response matching backend `ProductSummaryResponse`.
 */
export interface ProductSummaryResponse {
  identifier: string;
  name: string;
  isActive: boolean;
}

/**
 * Request to create a category matching backend `CreateCategoryRequest`.
 */
export interface CreateCategoryRequest {
  name: string;
}

/**
 * Request to update a category matching backend `UpdateCategoryRequest`.
 */
export interface UpdateCategoryRequest {
  name: string;
  isActive: boolean;
}

/**
 * Product response contract matching backend `ProductResponse`.
 */
export interface ProductResponse {
  identifier: string;
  name: string;
  categoryId: string;
  categoryName: string;
  isActive: boolean;
}

/**
 * Request to create a product matching backend `CreateProductRequest`.
 */
export interface CreateProductRequest {
  identifier: string;
  name: string;
  categoryId: string;
}

/**
 * Request to update a product matching backend `UpdateProductRequest`.
 */
export interface UpdateProductRequest {
  name: string;
  categoryId: string;
  isActive: boolean;
}

/**
 * Query filter parameters supported by backend `GET /api/products`.
 */
export interface ProductQueryFilter {
  categoryId?: string | null;
  isActive?: boolean | null;
  searchTerm?: string | null;
}
