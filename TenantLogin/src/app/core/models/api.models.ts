export interface LoginResponse {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  userType: number;
  tenantId?: string;
  tenantName?: string;
  subdomain?: string;
  token: string;
  tokenType: string;
  expiresIn: number;
  refreshToken: string;
  refreshTokenExpiry: string;
}

export interface ApiResult<T> {
  success: boolean;
  message: string;
  data: T;
  errorCode: number;
  errors: { code: number; error: string }[];
}
