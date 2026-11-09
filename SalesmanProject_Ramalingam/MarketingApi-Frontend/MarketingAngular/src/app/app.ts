import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';
import { ToastService } from './services/toast.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly auth = inject(AuthService);
  readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }

  logout(): void {
    const userName = this.auth.currentUser()?.name || 'User';
    this.auth.logout();
    this.toast.info(`Signed out of session for ${userName}.`, 'Session Ended');
    this.router.navigateByUrl('/login');
  }

  dismissToast(id: string): void {
    this.toast.dismiss(id);
  }
}
