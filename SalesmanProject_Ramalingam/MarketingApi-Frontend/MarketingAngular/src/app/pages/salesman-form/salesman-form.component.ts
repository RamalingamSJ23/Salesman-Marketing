import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { DatePickerModule } from '@syncfusion/ej2-angular-calendars';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import { Salesman } from '../../shared/models';

function noFutureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const selected = new Date(control.value);
  if (isNaN(selected.getTime())) return null;
  const today = new Date();
  today.setHours(23, 59, 59, 999);
  if (selected > today) {
    return { futureDate: true };
  }
  return null;
}

@Component({
  selector: 'app-salesman-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, DatePickerModule, DropDownListModule],
  templateUrl: './salesman-form.component.html',
})
export class SalesmanFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(SalesmanApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly form = this.fb.nonNullable.group({
    salesmancode: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(20),
        Validators.pattern(/^[a-zA-Z0-9]+$/),
      ],
    ],
    salesmanname: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
        Validators.pattern(/^[a-zA-Z\s.'-]+$/),
      ],
    ],
    mobileno: [
      '',
      [
        Validators.required,
        Validators.pattern(/^[0-9]{7,15}$/),
      ],
    ],
    email: [
      '',
      [
        Validators.required,
        Validators.maxLength(100),
        Validators.pattern(/^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/),
      ],
    ],
    joiningdate: ['', [Validators.required, noFutureDateValidator]],
    status: [1, Validators.required],
  });

  statusOptions = [
    { text: 'Active', value: 1 },
    { text: 'Inactive', value: 2 },
  ];
  statusFields = { text: 'text', value: 'value' };

  id: number | null = null;
  salesman: Salesman | null = null;
  codeConflictError = '';
  mobileConflictError = '';
  emailConflictError = '';
  generalError = '';
  saving = false;
  loading = false;
  readonly maxJoiningDate = new Date();

  get editMode(): boolean {
    return this.id !== null;
  }

  ngOnInit(): void {
    const paramId = this.route.snapshot.paramMap.get('id');
    if (!paramId) {
      this.loading = false;
      this.cdr.markForCheck();
      return;
    }

    this.id = Number(paramId);
    this.loading = true;
    this.cdr.markForCheck();

    this.api
      .getById(this.id)
      .pipe(
        timeout(8000),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (salesman) => {
          this.salesman = salesman;
          this.form.patchValue({
            salesmancode: salesman.salesmancode,
            salesmanname: salesman.salesmanname,
            mobileno: salesman.mobileno || '',
            email: salesman.email || '',
            joiningdate: salesman.joiningdate ? salesman.joiningdate.slice(0, 10) : '',
            status: salesman.status,
          });
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.generalError = this.readError(error);
          this.toast.error(this.generalError, 'Not Found');
          this.cdr.markForCheck();
        },
      });
  }

  onCodeInput(): void {
    const current = this.form.controls.salesmancode.value;
    if (current) {
      this.form.controls.salesmancode.setValue(current.toUpperCase(), { emitEvent: false });
    }
    this.codeConflictError = '';
  }

  onMobileInput(): void {
    this.mobileConflictError = '';
  }

  onEmailInput(): void {
    this.emailConflictError = '';
  }

  getErrorMessage(field: string): string {
    const control = this.form.get(field);
    if (!control || !control.errors || !control.touched) return '';

    if (control.hasError('required')) {
      switch (field) {
        case 'salesmancode': return 'Salesman code is required.';
        case 'salesmanname': return 'Salesman name is required.';
        case 'mobileno': return 'Mobile number is required.';
        case 'email': return 'Email is required.';
        case 'joiningdate': return 'Joining date is required.';
        case 'status': return 'Status is required.';
        default: return 'This field is required.';
      }
    }
    if (control.hasError('minlength')) {
      return `Must be at least ${control.errors['minlength'].requiredLength} characters.`;
    }
    if (control.hasError('maxlength')) {
      return `Cannot exceed ${control.errors['maxlength'].requiredLength} characters.`;
    }
    if (control.hasError('pattern')) {
      switch (field) {
        case 'salesmancode': return 'Only letters and numbers (A-Z, 0-9) are allowed, with no spaces.';
        case 'salesmanname': return 'Only English letters (A-Z, a-z), spaces, and standard characters (., -, \') are allowed.';
        case 'mobileno': return 'Mobile number must be between 7 and 15 digits.';
        case 'email':
          if (control.value && /[A-Z]/.test(control.value)) {
            return 'Capital letters are not allowed in email. Use lowercase only.';
          }
          return 'Please enter a valid email address in lowercase (e.g. name@company.com).';
        default: return 'Invalid format.';
      }
    }
    if (control.hasError('futureDate')) {
      return 'Joining date cannot be in the future.';
    }
    return 'Invalid value.';
  }

  save(saveAndNew = false): void {
    this.codeConflictError = '';
    this.mobileConflictError = '';
    this.emailConflictError = '';
    this.generalError = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please fix validation errors before saving.');
      this.cdr.markForCheck();
      return;
    }

    const value = this.form.getRawValue();

    if (value.email && /[A-Z]/.test(value.email)) {
      this.emailConflictError = 'Capital letters are not allowed in email. Use lowercase only.';
      this.toast.error(this.emailConflictError, 'Validation Error');
      this.cdr.markForCheck();
      return;
    }

    this.saving = true;

    const request = this.editMode
      ? this.api.update(this.id!, {
          salesmanname: value.salesmanname.trim(),
          mobileno: value.mobileno?.trim() || undefined,
          email: value.email?.trim() || undefined,
          joiningdate: value.joiningdate || undefined,
        })
      : this.api.create({
          salesmancode: value.salesmancode.trim().toUpperCase(),
          salesmanname: value.salesmanname.trim(),
          mobileno: value.mobileno?.trim() || undefined,
          email: value.email?.trim() || undefined,
          joiningdate: value.joiningdate || undefined,
          status: Number(value.status),
        });

    request
      .pipe(
        timeout(10000),
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (saved) => {
          this.toast.success(
            this.editMode
              ? `Salesman ${saved.salesmancode} updated successfully.`
              : `Salesman ${saved.salesmancode} created successfully.`
          );

          if (saveAndNew) {
            this.form.reset({
              salesmancode: '',
              salesmanname: '',
              mobileno: '',
              email: '',
              joiningdate: '',
              status: 1,
            });
            this.codeConflictError = '';
            this.mobileConflictError = '';
            this.emailConflictError = '';
            this.cdr.markForCheck();
          } else {
            // Spec: Save on a form returns to its list
            this.router.navigate(['/salesman']);
          }
        },
        error: (error) => {
          const msg = this.readError(error);
          const lower = msg.toLowerCase();
          if (lower.includes('mobile')) {
            this.mobileConflictError = msg;
            this.toast.error(msg, 'Mobile Conflict');
          } else if (lower.includes('email')) {
            this.emailConflictError = msg;
            this.toast.error(msg, 'Email Conflict');
          } else if (error.status === 409 || lower.includes('code') || lower.includes('already exists')) {
            this.codeConflictError = msg || `Salesman code ${value.salesmancode} already exists.`;
            this.toast.error(this.codeConflictError, 'Code Conflict');
          } else {
            this.generalError = msg;
            this.toast.error(msg, 'Save Error');
          }
          this.cdr.markForCheck();
        },
      });
  }

  isInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }

  private readError(error: any): string {
    return error?.error?.message || error?.message || 'The request could not be completed.';
  }
}
