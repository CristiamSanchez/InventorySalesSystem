/**
 * Request payload for creating a draft sale via POST /api/sales.
 */
export interface CreateSaleRequest {
  customerId?: string | null;
  customerReference?: string | null;
  userId?: string | null;
}

/**
 * Request payload for updating the customer association on a draft sale via PUT /api/sales/{id}/customer.
 */
export interface UpdateSaleCustomerRequest {
  customerId?: string | null;
  customerReference?: string | null;
}

/**
 * Request payload for adding a detail line to a draft sale via POST /api/sales/{id}/details.
 */
export interface AddSaleDetailRequest {
  productIdentifier: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
}

/**
 * Request payload for updating an existing detail line via PUT /api/sales/{id}/details/{detailIdOrProductIdentifier}.
 */
export interface UpdateSaleDetailRequest {
  quantity: number;
  unitPrice?: number | null;
  taxRate?: number | null;
}

/**
 * Line item detail in a sale representation.
 */
export interface SaleDetailResponse {
  id: string;
  saleId: string;
  productIdentifier: string;
  quantity: number;
  unitPrice: number;
  taxRate: number;
  subtotal: number;
  taxAmount: number;
  total: number;
}

/**
 * Complete sale representation returned by GET /api/sales/{id} and mutation endpoints.
 */
export interface SaleResponse {
  id: string;
  customerReference: string;
  userId: string;
  status: 'Draft' | 'Confirmed' | 'Cancelled' | string;
  subtotal: number;
  taxAmount: number;
  total: number;
  createdAtUtc: string;
  confirmedAtUtc?: string | null;
  details: SaleDetailResponse[];
  customerId?: string | null;
}

/**
 * Query filter parameters for the sales summary report GET /api/reports/sales-summary.
 * Requires Admin role.
 */
export interface SalesSummaryReportQuery {
  fromUtc: string;
  toUtc: string;
  customerId?: string | null;
  userId?: string | null;
  status?: 'Draft' | 'Confirmed' | 'Cancelled' | string | null;
}

/**
 * Individual sale item in the sales summary report response.
 */
export interface SalesSummarySaleItemResponse {
  id: string;
  createdAtUtc: string;
  confirmedAtUtc?: string | null;
  status: string;
  customerId?: string | null;
  customerReference: string;
  userId: string;
  totalUnits: number;
  lineCount: number;
  subtotal: number;
  taxAmount: number;
  total: number;
}

/**
 * Sales summary and transaction history report response.
 */
export interface SalesSummaryReportResponse {
  fromUtc: string;
  toUtc: string;
  totalTransactions: number;
  confirmedTransactions: number;
  draftTransactions: number;
  cancelledTransactions: number;
  totalSubtotal: number;
  totalTax: number;
  totalRevenue: number;
  sales: SalesSummarySaleItemResponse[];
}
