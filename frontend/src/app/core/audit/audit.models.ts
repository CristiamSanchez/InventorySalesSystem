/**
 * An audit event representation returned by the API (GET /api/audit and GET /api/audit/{id}).
 */
export interface AuditEventResponse {
  id: string;
  eventType: string;
  entityName: string;
  entityId: string | null;
  userId: string | null;
  occurredAtUtc: string;
  metadataJson: string | null;
  hasUser: boolean;
}

/**
 * Query filter parameters for the audit log endpoint GET /api/audit.
 * All fields are optional. Requires Admin role.
 */
export interface AuditQuery {
  eventType?: string | null;
  entityName?: string | null;
  entityId?: string | null;
  userId?: string | null;
  fromUtc?: string | null;
  toUtc?: string | null;
}

/**
 * Domain-defined event types for filtering and display categorization.
 */
export const AUDIT_EVENT_TYPES = [
  'SaleCreated',
  'SaleConfirmed',
  'SaleCancelled',
  'InventoryMovementRecorded',
  'ProductActivated',
  'ProductDeactivated',
  'SupplierActivated',
  'SupplierDeactivated',
  'CustomerActivated',
  'CustomerDeactivated',
  'UserLoginSucceeded',
  'UserLoginFailed',
  'UserActivated',
  'UserDeactivated'
] as const;

export type AuditEventType = (typeof AUDIT_EVENT_TYPES)[number];

/**
 * Domain-defined entity names for filtering and categorization.
 */
export const AUDIT_ENTITY_NAMES = [
  'Sale',
  'InventoryMovement',
  'Product',
  'Supplier',
  'Customer',
  'User'
] as const;

export type AuditEntityName = (typeof AUDIT_ENTITY_NAMES)[number];
