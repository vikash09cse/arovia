import { ApiResult } from '../models/api.models';
import { environment } from '../../../environments/environment';

export function unwrapApiResult<T>(result: ApiResult<T>): T {
  if (!result.success || result.data == null) {
    throw { error: result };
  }
  return result.data;
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  const body = (error as { error?: ApiResult<unknown> })?.error;
  if (!body) return fallback;
  return body.message || body.errors?.[0]?.error || fallback;
}

/** Resolve API upload paths (e.g. /uploads/...) against filesBaseUrl for img src. */
export function resolveFileUrl(url: string | null | undefined): string | null {
  if (!url) return null;
  const value = url.trim();
  if (!value) return null;
  if (value.startsWith('data:') || value.startsWith('blob:')) return value;
  if (value.startsWith('http://') || value.startsWith('https://')) {
    const uploadsIdx = value.toLowerCase().indexOf('/uploads/');
    if (uploadsIdx >= 0) {
      const relative = value.slice(uploadsIdx);
      return `${environment.filesBaseUrl.replace(/\/$/, '')}${relative}`;
    }
    return value;
  }
  const path = value.startsWith('/') ? value : `/${value}`;
  return `${environment.filesBaseUrl.replace(/\/$/, '')}${path}`;
}
