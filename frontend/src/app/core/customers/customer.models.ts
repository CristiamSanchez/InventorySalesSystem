export interface CustomerResponse {
  id: string;
  name: string;
  identificationNumber: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateCustomerRequest {
  name: string;
  identificationNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
}

export interface UpdateCustomerRequest {
  name: string;
  identificationNumber?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
}

export interface CustomerQueryFilter {
  isActive?: boolean | null;
  searchTerm?: string | null;
}
