import { AbstractControl, FormGroup, ValidationErrors } from '@angular/forms';

export const VALIDATION_PATTERNS = {
  code: /^[a-zA-Z0-9]+$/,
  // Allows Unicode letters (accents, Tamil, etc.), spaces, and standard name punctuation (M13)
  personName: /^[\p{L}\p{M}\s.'-]+$/u,
  mobile: /^[0-9]{7,15}$/,
  email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  teamText: /^[\p{L}\p{M}0-9\s&.\-/'(),!?"]+$/u,
} as const;

export function noFutureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const selected = new Date(control.value);
  if (Number.isNaN(selected.getTime())) return null;
  const today = new Date();
  today.setHours(23, 59, 59, 999);
  return selected > today ? { futureDate: true } : null;
}

export function normalizeEmail(value: string | null | undefined): string {
  return (value ?? '').trim().toLowerCase();
}

export function getControlErrorMessage(
  form: FormGroup,
  field: string,
  requiredLabels: Record<string, string>,
  patternMessages: Record<string, string>
): string {
  const control = form.get(field);
  if (!control || !control.errors || !control.touched) return '';

  if (control.hasError('required')) {
    return requiredLabels[field] || 'This field is required.';
  }
  if (control.hasError('min') && requiredLabels[field]) {
    return requiredLabels[field];
  }
  if (control.hasError('minlength')) {
    return `Must be at least ${control.errors['minlength'].requiredLength} characters.`;
  }
  if (control.hasError('maxlength')) {
    return `Cannot exceed ${control.errors['maxlength'].requiredLength} characters.`;
  }
  if (control.hasError('pattern')) {
    return patternMessages[field] || 'Invalid format.';
  }
  if (control.hasError('futureDate')) {
    return 'Joining date cannot be in the future.';
  }

  return 'Invalid value.';
}
