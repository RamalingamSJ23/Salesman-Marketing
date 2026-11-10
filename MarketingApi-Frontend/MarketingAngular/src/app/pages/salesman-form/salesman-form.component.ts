import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, timeout } from 'rxjs';
import { DatePickerModule } from '@syncfusion/ej2-angular-calendars';
import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { SalesmanApiService } from '../../services/salesman-api.service';
import { ToastService } from '../../services/toast.service';
import { RecordStatus, Salesman } from '../../models';
import { parseApiError } from '../../shared/api-error';
import {
  VALIDATION_PATTERNS,
  getControlErrorMessage,
  noFutureDateValidator,
  normalizeEmail,
} from '../../shared/validation';

const REQUIRED_LABELS: Record<string, string> = {
  salesmancode: 'Salesman code is required.',
  salesmanname: 'Salesman name is required.',
  mobileno: 'Mobile number is required.',
  email: 'Email is required.',
  joiningdate: 'Joining date is required.',
  status: 'Status is required.',
};

const PATTERN_MESSAGES: Record<string, string> = {
  salesmancode: 'Only alphanumeric characters (A-Z, 0-9) are allowed.',
  salesmanname: 'Salesman name contains unsupported characters.',
  mobileno: 'Mobile number must be 7 to 15 digits.',
  email: 'Enter a valid email address (e.g. name@company.com).',
};

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
  private readonly destroyRef = inject(DestroyRef);

  readonly modalMode = input(false);
  readonly salesmanId = input<number | null>(null);
  readonly initialSalesman = input<Salesman | null>(null);
  readonly saved = output<Salesman>();
  readonly savedAndContinue = output<Salesman>();
  readonly cancelled = output<void>();

  readonly form = this.fb.nonNullable.group({
    salesmancode: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(20),
        Validators.pattern(VALIDATION_PATTERNS.code),
      ],
    ],
    salesmanname: [
      '',
      [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(100),
        Validators.pattern(VALIDATION_PATTERNS.personName),
      ],
    ],
    mobileno: ['', [Validators.required, Validators.pattern(VALIDATION_PATTERNS.mobile)]],
    email: ['', [Validators.required, Validators.maxLength(100), Validators.pattern(VALIDATION_PATTERNS.email)]],
    joiningdate: ['', [Validators.required, noFutureDateValidator]],
    status: [RecordStatus.Active as number, Validators.required],
  });

  readonly statusOptions = [
    { text: 'Active', value: RecordStatus.Active },
    { text: 'Inactive', value: RecordStatus.Inactive },
  ];
  readonly statusFields = { text: 'text', value: 'value' };
  readonly maxJoiningDate = new Date();

  readonly resolvedId = signal<number | null>(null);
  readonly editMode = computed(() => this.resolvedId() !== null);
  readonly codeConflictError = signal('');
  readonly mobileConflictError = signal('');
  readonly emailConflictError = signal('');
  readonly generalError = signal('');
  readonly saving = signal(false);
  readonly loading = signal(false);

  constructor() {
    effect(() => {
      if (!this.modalMode()) return;
      const incoming = this.initialSalesman();
      const id = this.salesmanId();
      this.resetErrors();

      if (incoming) {
        this.resolvedId.set(incoming.id);
        this.patchForm(incoming);
      } else if (id) {
        this.resolvedId.set(id);
        this.fetchById(id);
      } else {
        this.resolvedId.set(null);
        this.resetForm();
      }
    });
  }

  ngOnInit(): void {
    if (this.modalMode()) {
      return;
    }

    const paramId = this.route.snapshot.paramMap.get('id');
    if (!paramId) {
      this.loading.set(false);
      return;
    }

    const id = Number(paramId);
    this.resolvedId.set(id);
    this.fetchById(id);
  }

  onCodeInput(): void {
    const current = this.form.controls.salesmancode.value;
    if (current) {
      this.form.controls.salesmancode.setValue(current.toUpperCase(), { emitEvent: false });
    }
    this.codeConflictError.set('');
  }

  onMobileInput(): void {
    this.mobileConflictError.set('');
  }

  onEmailInput(): void {
    this.emailConflictError.set('');
  }

  getErrorMessage(field: string): string {
    return getControlErrorMessage(this.form, field, REQUIRED_LABELS, PATTERN_MESSAGES);
  }

  isInvalid(name: string): boolean {
    const control = this.form.get(name);
    return !!control && control.invalid && control.touched;
  }

  save(saveAndNew = false): void {
    this.resetErrors();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.toast.warning('Please fix validation errors before saving.');
      return;
    }

    const value = this.form.getRawValue();
    const cleanEmail = normalizeEmail(value.email);
    this.saving.set(true);

    const request = this.editMode()
      ? this.api.update(this.resolvedId()!, {
          salesmanname: value.salesmanname.trim(),
          mobileno: value.mobileno?.trim() || undefined,
          email: cleanEmail || undefined,
          joiningdate: value.joiningdate || undefined,
        })
      : this.api.create({
          salesmancode: value.salesmancode.trim().toUpperCase(),
          salesmanname: value.salesmanname.trim(),
          mobileno: value.mobileno?.trim() || undefined,
          email: cleanEmail || undefined,
          joiningdate: value.joiningdate || undefined,
          status: Number(value.status),
        });

    request
      .pipe(
        timeout(10000),
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (savedSalesman) => {
          this.toast.success(
            this.editMode()
              ? `Salesman ${savedSalesman.salesmancode} updated successfully.`
              : `Salesman ${savedSalesman.salesmancode} created successfully.`
          );

          if (saveAndNew && !this.editMode()) {
            this.resetForm();
            this.savedAndContinue.emit(savedSalesman);
            return;
          }

          if (this.modalMode()) {
            this.saved.emit(savedSalesman);
          } else {
            this.router.navigate(['/salesman']);
          }
        },
        error: (error) => {
          const parsed = parseApiError(error);
          if (parsed.field === 'mobileno') {
            this.mobileConflictError.set(parsed.message);
            this.toast.error(parsed.message, 'Mobile Conflict');
          } else if (parsed.field === 'email') {
            this.emailConflictError.set(parsed.message);
            this.toast.error(parsed.message, 'Email Conflict');
          } else if (parsed.field === 'salesmancode') {
            this.codeConflictError.set(parsed.message);
            this.toast.error(parsed.message, 'Code Conflict');
          } else {
            this.generalError.set(parsed.message);
            this.toast.error(parsed.message, 'Save Failed');
          }
        },
      });
  }

  cancel(): void {
    if (this.modalMode()) {
      this.cancelled.emit();
    } else {
      this.router.navigate(['/salesman']);
    }
  }

  private fetchById(id: number): void {
    this.loading.set(true);
    this.api
      .getById(id)
      .pipe(
        timeout(8000),
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (salesman) => this.patchForm(salesman),
        error: (error) => {
          const parsed = parseApiError(error, 'Failed to load salesman details.');
          this.generalError.set(parsed.message);
          this.toast.error(parsed.message, 'Not Found');
        },
      });
  }

  private patchForm(salesman: Salesman): void {
    this.form.patchValue({
      salesmancode: salesman.salesmancode,
      salesmanname: salesman.salesmanname,
      mobileno: salesman.mobileno || '',
      email: salesman.email || '',
      joiningdate: salesman.joiningdate ? salesman.joiningdate.slice(0, 10) : '',
      status: salesman.status,
    });
  }

  private resetForm(): void {
    this.form.reset({
      salesmancode: '',
      salesmanname: '',
      mobileno: '',
      email: '',
      joiningdate: '',
      status: RecordStatus.Active,
    });
    this.resetErrors();
  }

  private resetErrors(): void {
    this.codeConflictError.set('');
    this.mobileConflictError.set('');
    this.emailConflictError.set('');
    this.generalError.set('');
  }
}
