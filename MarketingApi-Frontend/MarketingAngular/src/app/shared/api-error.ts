import { HttpErrorResponse } from '@angular/common/http';

export interface ApiErrorInfo {
  message: string;
  field?: string;
  code?: string;
  status: number;
}

interface ProblemDetailsPayload {
  title?: string;
  detail?: string;
  message?: string;
  field?: string;
  code?: string;
  errors?: Record<string, string[] | string>;
}

/**
 * Extracts a structured message, optional field name, and error code from an HTTP error / RFC 7807 ProblemDetails payload.
 */
export function parseApiError(error: unknown, fallback = 'The request could not be completed.'): ApiErrorInfo {
  if (!(error instanceof HttpErrorResponse)) {
    if (error instanceof Error && error.message) {
      return { message: error.message, status: 0 };
    }
    return { message: fallback, status: 0 };
  }

  if (error.status === 0) {
    return { message: 'Unable to reach the server. Please check your connection.', status: 0 };
  }

  const body = error.error;
  if (typeof body === 'string' && body.trim()) {
    return { message: body.trim(), status: error.status };
  }

  if (body && typeof body === 'object') {
    const payload = body as ProblemDetailsPayload;

    if (payload.errors && typeof payload.errors === 'object') {
      const entries = Object.entries(payload.errors);
      if (entries.length > 0) {
        const [firstKey, firstVal] = entries[0];
        const firstMsg = Array.isArray(firstVal) ? firstVal[0] : firstVal;
        if (firstMsg) {
          return {
            message: firstMsg,
            field: payload.field ?? firstKey.toLowerCase(),
            code: payload.code,
            status: error.status,
          };
        }
      }
    }

    const message = payload.detail || payload.message || payload.title || fallback;
    return {
      message,
      field: payload.field,
      code: payload.code,
      status: error.status,
    };
  }

  return { message: fallback, status: error.status };
}

export function readApiError(error: unknown, fallback = 'The request could not be completed.'): string {
  return parseApiError(error, fallback).message;
}
