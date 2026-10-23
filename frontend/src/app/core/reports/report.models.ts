import type {
  SalesSummaryReportQuery,
  SalesSummaryReportResponse,
  SalesSummarySaleItemResponse
} from '../sales/sales.models';
import type {
  InventoryStatusQuery,
  InventoryStatusReportResponse,
  InventoryStatusItemResponse,
  InventoryMovementReportQuery,
  InventoryMovementReportResponse,
  InventoryMovementReportItemResponse
} from '../inventory/inventory.models';

/**
 * Query filter parameters for the product sales performance report GET /api/reports/product-sales.
 * fromUtc and toUtc are mandatory query parameters in the backend. Requires Admin role.
 */
export interface ProductSalesReportQuery {
  fromUtc: string;
  toUtc: string;
  productIdentifier?: string | null;
  categoryId?: string | null;
}

/**
 * Individual product sales contribution item in the product sales performance report.
 * Reflects immutable sale-detail snapshots from confirmed sales.
 */
export interface ProductSalesItemResponse {
  productIdentifier: string;
  productName: string;
  categoryId: string;
  categoryName: string;
  unitsSold: number;
  subtotal: number;
  taxAmount: number;
  total: number;
}

/**
 * Product sales performance report response from GET /api/reports/product-sales.
 */
export interface ProductSalesReportResponse {
  fromUtc: string;
  toUtc: string;
  totalProductsSold: number;
  totalUnitsSold: number;
  totalSubtotal: number;
  totalTax: number;
  totalRevenue: number;
  items: ProductSalesItemResponse[];
}

/**
 * Tab identifier for the Reports dashboard.
 */
export type ReportTab = 'sales-summary' | 'product-sales' | 'inventory-status' | 'inventory-movements';

// Re-export sales and inventory report models for consolidated access
export type {
  SalesSummaryReportQuery,
  SalesSummaryReportResponse,
  SalesSummarySaleItemResponse,
  InventoryStatusQuery,
  InventoryStatusReportResponse,
  InventoryStatusItemResponse,
  InventoryMovementReportQuery,
  InventoryMovementReportResponse,
  InventoryMovementReportItemResponse
};
