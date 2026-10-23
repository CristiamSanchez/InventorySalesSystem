export interface SupplierResponse {
  id: string;
  name: string;
  contactName: string | null;
  identificationNumber: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateSupplierRequest {
  name: string;
  contactName?: string | null;
  identificationNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
}

export interface UpdateSupplierRequest {
  name: string;
  contactName?: string | null;
  identificationNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
}

export interface SupplierQueryFilter {
  isActive?: boolean | null;
  searchTerm?: string | null;
}
