/**
 * Request payload for recording a stock entry via POST /api/inventory/entries.
 */
export interface RecordStockEntryRequest {
  productIdentifier: string;
  quantity: number;
  reference: string;
  occurredAtUtc?: string | null;
  supplierId?: string | null;
}

/**
 * Request payload for recording a stock exit via POST /api/inventory/exits.
 */
export interface RecordStockExitRequest {
  productIdentifier: string;
  quantity: number;
  reference: string;
  occurredAtUtc?: string | null;
}

/**
 * Request payload for recording a stock adjustment via POST /api/inventory/adjustments.
 * quantityDelta must be a signed non-zero integer.
 */
export interface AdjustStockRequest {
  productIdentifier: string;
  quantityDelta: number;
  reason: string;
  occurredAtUtc?: string | null;
}

/**
 * Single product current inventory representation returned by GET /api/inventory/{productIdentifier}
 * or mutation endpoints.
 */
export interface InventoryResponse {
  productIdentifier: string;
  quantity: number;
}

/**
 * Chronological movement record for a single product from GET /api/inventory/{productIdentifier}/movements.
 */
export interface InventoryMovementResponse {
  id: string;
  productIdentifier: string;
  movementType: 'Entry' | 'Exit' | 'Adjustment' | string;
  quantity: number;
  reference: string;
  occurredAtUtc: string;
  supplierId?: string | null;
  supplierName?: string | null;
}

/**
 * Query filter for inventory stock status report GET /api/reports/inventory-status.
 */
export interface InventoryStatusQuery {
  categoryId?: string | null;
  inStockOnly?: boolean | null;
}

/**
 * Stock status item for a product in the catalog.
 */
export interface InventoryStatusItemResponse {
  productIdentifier: string;
  productName: string;
  categoryId: string;
  categoryName: string;
  quantity: number;
  hasInventoryRecord: boolean;
  isActive: boolean;
  stockStatus: 'InStock' | 'OutOfStock' | string;
}

/**
 * Full catalog stock status report from GET /api/reports/inventory-status.
 */
export interface InventoryStatusReportResponse {
  totalProducts: number;
  totalUnitsOnHand: number;
  inStockProductsCount: number;
  outOfStockProductsCount: number;
  items: InventoryStatusItemResponse[];
}

/**
 * Query filter for inventory movement traceability report GET /api/reports/inventory-movements.
 * fromUtc and toUtc are required by the backend.
 */
export interface InventoryMovementReportQuery {
  fromUtc: string;
  toUtc: string;
  productIdentifier?: string | null;
  movementType?: 'Entry' | 'Exit' | 'Adjustment' | string | null;
  supplierId?: string | null;
}

/**
 * Single movement record in the movement traceability report.
 */
export interface InventoryMovementReportItemResponse {
  id: string;
  occurredAtUtc: string;
  productIdentifier: string;
  movementType: 'Entry' | 'Exit' | 'Adjustment' | string;
  quantity: number;
  reference: string;
  supplierId?: string | null;
  supplierName?: string | null;
  userId?: string | null;
}

/**
 * Full movement traceability report from GET /api/reports/inventory-movements.
 */
export interface InventoryMovementReportResponse {
  fromUtc: string;
  toUtc: string;
  totalMovements: number;
  entryCount: number;
  exitCount: number;
  adjustmentCount: number;
  totalUnitsIn: number;
  totalUnitsOut: number;
  netQuantityDelta: number;
  movements: InventoryMovementReportItemResponse[];
}

/**
 * Operation type enumeration for the inventory mutation modal dialog.
 */
export type InventoryOperationType = 'entry' | 'exit' | 'adjustment';
