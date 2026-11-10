import { CommonModule } from '@angular/common';
import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  showPassword = false;
  errorMessage = '';
  loading = false;

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.minLength(3), Validators.pattern(/^[a-zA-Z0-9._-]+$/)]],
    password: ['', [Validators.required, Validators.minLength(4)]],
  });

  submit(): void {
    this.errorMessage = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    this.loading = true;
    this.cdr.markForCheck();
    const value = this.form.getRawValue();

    this.auth.login({ username: value.username.trim(), password: value.password }).subscribe({
      next: (response) => {
        this.loading = false;
        this.toast.success(`Welcome back, ${response.name || response.username}!`, 'Signed In');
        this.router.navigateByUrl(this.auth.getDefaultRoute());
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error?.error?.message || 'Invalid username or password. Please check your credentials.';
        this.cdr.markForCheck();
      },
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
}
