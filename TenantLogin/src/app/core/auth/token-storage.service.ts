import { Injectable } from '@angular/core';
import { LoginResponse } from '../models/api.models';

const ACCESS_TOKEN_KEY = 'tenant_access_token';
const REFRESH_TOKEN_KEY = 'tenant_refresh_token';
const USER_KEY = 'tenant_user';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly storage: Storage | null = typeof sessionStorage !== 'undefined' ? sessionStorage : null;

  saveSession(response: LoginResponse): void {
    this.set(ACCESS_TOKEN_KEY, response.token);
    this.set(REFRESH_TOKEN_KEY, response.refreshToken);
    this.set(USER_KEY, JSON.stringify(response));
  }

  updateTokens(response: LoginResponse): void {
    this.set(ACCESS_TOKEN_KEY, response.token);
    this.set(REFRESH_TOKEN_KEY, response.refreshToken);
    const user = this.getUser();
    if (user) {
      this.set(USER_KEY, JSON.stringify({ ...user, token: response.token, refreshToken: response.refreshToken, expiresIn: response.expiresIn }));
    }
  }

  getAccessToken(): string | null {
    return this.get(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return this.get(REFRESH_TOKEN_KEY);
  }

  getUser(): LoginResponse | null {
    const raw = this.get(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as LoginResponse;
    } catch {
      return null;
    }
  }

  clear(): void {
    this.remove(ACCESS_TOKEN_KEY);
    this.remove(REFRESH_TOKEN_KEY);
    this.remove(USER_KEY);
  }

  private get(key: string): string | null {
    return this.storage?.getItem(key) ?? null;
  }

  private set(key: string, value: string): void {
    this.storage?.setItem(key, value);
  }

  private remove(key: string): void {
    this.storage?.removeItem(key);
  }
}
