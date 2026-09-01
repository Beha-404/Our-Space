import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  type: 'success' | 'error';
  messageKey: string;
}

const DISMISS_AFTER_MS = 4000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;

  readonly toasts = signal<Toast[]>([]);

  success(messageKey: string): void {
    this.show('success', messageKey);
  }

  error(messageKey: string): void {
    this.show('error', messageKey);
  }

  dismiss(id: number): void {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  private show(type: Toast['type'], messageKey: string): void {
    const id = this.nextId++;
    this.toasts.update(list => [...list, { id, type, messageKey }]);
    setTimeout(() => this.dismiss(id), DISMISS_AFTER_MS);
  }
}
