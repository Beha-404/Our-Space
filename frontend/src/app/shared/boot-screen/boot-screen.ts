import { Component, DestroyRef, inject, signal } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, Router } from '@angular/router';
import { filter, take } from 'rxjs';
import { TranslatePipe } from '../../i18n/translate.pipe';

export const BOOT_NOTICE_DELAY_MS = 1200;
export const BOOT_SLOW_DELAY_MS = 75000;

@Component({
  selector: 'app-boot-screen',
  imports: [TranslatePipe],
  templateUrl: './boot-screen.html',
  styleUrl: './boot-screen.css',
})
export class BootScreen {
  protected readonly booting = signal(true);
  protected readonly showNotice = signal(false);
  protected readonly slow = signal(false);

  constructor() {
    const router = inject(Router);
    const noticeTimer = setTimeout(() => this.showNotice.set(true), BOOT_NOTICE_DELAY_MS);
    const slowTimer = setTimeout(() => this.slow.set(true), BOOT_SLOW_DELAY_MS);
    const stopTimers = () => {
      clearTimeout(noticeTimer);
      clearTimeout(slowTimer);
    };

    const subscription = router.events.pipe(
      filter(e => e instanceof NavigationEnd || e instanceof NavigationError || e instanceof NavigationCancel),
      take(1),
    ).subscribe(() => {
      stopTimers();
      this.booting.set(false);
    });

    inject(DestroyRef).onDestroy(() => {
      stopTimers();
      subscription.unsubscribe();
    });
  }

  protected reload(): void {
    location.reload();
  }
}
