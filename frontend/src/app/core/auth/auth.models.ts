/**
 * Credentials submitted to authenticate a user.
 * Matches backend `SistemaInventarioVentas.API.Contracts.LoginRequest`.
 */
export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * Bearer access token returned after successful authentication.
 * Matches backend `SistemaInventarioVentas.API.Contracts.LoginResponse`.
 */
export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
}

/**
 * Identity and roles associated with the authenticated caller.
 * Matches backend `SistemaInventarioVentas.API.Contracts.CurrentUserResponse`.
 */
export interface CurrentUserResponse {
  userId: string;
  roles: string[];
}

/**
 * Credentials and profile information required to register a user.
 * Matches backend `SistemaInventarioVentas.API.Contracts.RegisterRequest`.
 */
export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

/**
 * Result of registering a user account.
 * Matches backend `SistemaInventarioVentas.API.Contracts.RegisterResponse`.
 */
export interface RegisterResponse {
  userId: string;
  isActive: boolean;
}

/**
 * Active authenticated session model maintained in frontend state.
 */
export interface UserSession {
  userId: string;
  roles: string[];
  accessToken: string;
  expiresAtUtc: string;
}
