import { HttpErrorResponse } from '@angular/common/http';
import { parseApiError, readApiError } from './api-error';

describe('api-error', () => {
  it('reads ProblemDetails detail, field, and code (H9, M11)', () => {
    const err = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Conflict',
        detail: 'Mobile number already exists.',
        field: 'mobileno',
        code: 'DUPLICATE_MOBILE',
      },
    });

    const parsed = parseApiError(err);
    expect(parsed.status).toBe(409);
    expect(parsed.message).toBe('Mobile number already exists.');
    expect(parsed.field).toBe('mobileno');
    expect(parsed.code).toBe('DUPLICATE_MOBILE');
  });

  it('extracts first validation error from ASP.NET errors map (H9)', () => {
    const err = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        errors: {
          Email: ['Invalid email format.'],
        },
      },
    });

    expect(readApiError(err)).toBe('Invalid email format.');
    expect(parseApiError(err).field).toBe('email');
  });
});
