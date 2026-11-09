import { Signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface ToastMessage {
  id: string;
  type: ToastType;
  title?: string;
  message: string;
}

export interface IToastService {
  readonly toasts: Signal<ToastMessage[]>;
  show(type: ToastType, message: string, title?: string, duration?: number): void;
  success(message: string, title?: string): void;
  error(message: string, title?: string): void;
  warning(message: string, title?: string): void;
  info(message: string, title?: string): void;
  dismiss(id: string): void;
}
