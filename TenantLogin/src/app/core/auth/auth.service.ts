import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResult, LoginResponse } from '../models/api.models';
import { getApiErrorMessage, unwrapApiResult } from '../api/api.util';
import { isJwtExpired } from './jwt.util';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokens = inject(TokenStorageService);

  private refreshInFlight: Observable<LoginResponse> | null = null;

  readonly currentUser = signal<LoginResponse | null>(this.tokens.getUser());

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<ApiResult<LoginResponse>>(`${environment.apiUrl}/auth/tenant-login`, { email, password }).pipe(
      map(unwrapApiResult),
      tap(res => this.persistSession(res))
    );
  }

  refreshAccessToken(): Observable<LoginResponse> {
    const token = this.getToken();
    if (!token) {
      return throwError(() => new Error('No access token'));
    }

    if (!this.refreshInFlight) {
      this.refreshInFlight = this.http.post<ApiResult<LoginResponse>>(
        `${environment.apiUrl}/auth/refresh-token`,
        {},
        { headers: { Authorization: `Bearer ${token}` } }
      ).pipe(
        map(unwrapApiResult),
        tap(res => {
          this.tokens.updateTokens(res);
          this.currentUser.set(this.tokens.getUser());
        }),
        catchError(err => {
          this.clearSession();
          return throwError(() => err);
        }),
        finalize(() => {
          this.refreshInFlight = null;
        }),
        shareReplay(1)
      );
    }

    return this.refreshInFlight;
  }

  logout(): void {
    const token = this.getToken();
    if (token) {
      this.http.post(`${environment.apiUrl}/auth/logout`, {}, {
        headers: { Authorization: `Bearer ${token}` }
      }).pipe(catchError(() => of(null))).subscribe();
    }
    this.clearSession();
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.tokens.getAccessToken();
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    return !!token && !isJwtExpired(token);
  }

  isTokenExpiringSoon(): boolean {
    const token = this.getToken();
    return !!token && isJwtExpired(token, 300);
  }

  ensureValidToken(): Observable<boolean> {
    const token = this.getToken();
    if (!token) return of(false);
    if (!isJwtExpired(token)) return of(true);
    return this.refreshAccessToken().pipe(
      map(() => true),
      catchError(() => of(false))
    );
  }

  private persistSession(res: LoginResponse): void {
    this.tokens.saveSession(res);
    this.currentUser.set(res);
  }

  private clearSession(): void {
    this.tokens.clear();
    this.currentUser.set(null);
  }
}
