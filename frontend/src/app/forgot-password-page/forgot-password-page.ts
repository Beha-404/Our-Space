import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { LanguageSwitcher } from '../i18n/language-switcher/language-switcher';
import { AuthService } from '../services/auth.service';

const PASSWORD_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{9,}$/;

@Component({
  imports: [RouterLink, TranslatePipe, LanguageSwitcher],
  selector: 'app-forgot-password-page',
  styleUrl: './forgot-password-page.css',
  templateUrl: './forgot-password-page.html',
})
export class ForgotPasswordPage {
  private authService = inject(AuthService);
  private router = inject(Router);

  step = signal<'request' | 'confirm'>('request');

  email = signal('');
  code = signal('');
  newPassword = signal('');
  confirmPassword = signal('');

  busy = signal(false);
  errorKey = signal('');
  infoKey = signal('');

  requestCode(): void {
    const email = this.email().trim();
    if (!email) {
      this.errorKey.set('auth.errInvalidEmail');
      return;
    }

    this.busy.set(true);
    this.errorKey.set('');

    this.authService.forgotPassword(email).subscribe({
      next: () => {
        this.busy.set(false);
        this.infoKey.set('auth.codeSentInfo');
        this.step.set('confirm');
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.errorKey.set(err.error?.title ?? 'auth.errResetFailed');
      }
    });
  }

  resetPassword(): void {
    const code = this.code().trim();
    const password = this.newPassword();

    if (!code) {
      this.errorKey.set('auth.errResetCodeEmpty');
      return;
    }
    if (!PASSWORD_REGEX.test(password)) {
      this.errorKey.set('auth.errPasswordWeak');
      return;
    }
    if (password !== this.confirmPassword()) {
      this.errorKey.set('auth.errPasswordMismatch');
      return;
    }

    this.busy.set(true);
    this.errorKey.set('');

    this.authService.resetPassword(this.email().trim(), code, password).subscribe({
      next: () => {
        this.busy.set(false);
        this.router.navigate(['/login'], { queryParams: { reset: '1' } });
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.errorKey.set(err.error?.title ?? 'auth.errResetFailed');
      }
    });
  }

  backToRequest(): void {
    this.step.set('request');
    this.errorKey.set('');
    this.infoKey.set('');
    this.code.set('');
  }
}
