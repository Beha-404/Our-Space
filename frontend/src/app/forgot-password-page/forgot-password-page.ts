import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { LanguageSwitcher } from '../i18n/language-switcher/language-switcher';
import { AuthService } from '../services/auth.service';
import { OtpInput } from '../shared/otp-input/otp-input';
import { ToastService } from '../shared/toast/toast.service';
import { isStrongPassword, isValidEmail } from '../shared/validators';

@Component({
  imports: [RouterLink, TranslatePipe, LanguageSwitcher, OtpInput],
  selector: 'app-forgot-password-page',
  styleUrl: './forgot-password-page.css',
  templateUrl: './forgot-password-page.html',
})
export class ForgotPasswordPage {
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  step = signal<'request' | 'confirm'>('request');

  email = signal('');
  code = signal('');
  newPassword = signal('');
  confirmPassword = signal('');

  busy = signal(false);
  errorKey = signal('');
  infoKey = signal('');

  emailTouched = signal(false);
  codeTouched = signal(false);
  newPasswordTouched = signal(false);
  confirmPasswordTouched = signal(false);

  emailError = computed(() => {
    if (!this.emailTouched()) return '';
    const value = this.email().trim();
    if (!value) return 'auth.errEmailRequired';
    return isValidEmail(value) ? '' : 'auth.errInvalidEmail';
  });

  codeError = computed(() =>
    this.codeTouched() && !this.code().trim() ? 'auth.errResetCodeEmpty' : '');

  newPasswordError = computed(() => {
    if (!this.newPasswordTouched()) return '';
    const value = this.newPassword();
    if (!value) return 'auth.errPasswordRequired';
    return isStrongPassword(value) ? '' : 'auth.errPasswordWeak';
  });

  confirmPasswordError = computed(() => {
    if (!this.confirmPasswordTouched()) return '';
    if (!this.confirmPassword()) return 'auth.errConfirmPasswordRequired';
    return this.newPassword() === this.confirmPassword() ? '' : 'auth.errPasswordMismatch';
  });

  emailValid = computed(() => this.emailTouched() && !this.emailError() && !!this.email().trim());
  newPasswordValid = computed(() => this.newPasswordTouched() && !this.newPasswordError() && !!this.newPassword());
  confirmPasswordValid = computed(() =>
    this.confirmPasswordTouched() && !this.confirmPasswordError() && !!this.confirmPassword());

  updateEmail(value: string): void {
    this.email.set(value);
    this.errorKey.set('');
  }

  updateCode(value: string): void {
    this.code.set(value);
    this.errorKey.set('');
  }

  updateNewPassword(value: string): void {
    this.newPassword.set(value);
    this.errorKey.set('');
  }

  updateConfirmPassword(value: string): void {
    this.confirmPassword.set(value);
    this.errorKey.set('');
  }

  requestCode(): void {
    this.emailTouched.set(true);

    if (this.emailError()) {
      return;
    }

    this.busy.set(true);
    this.errorKey.set('');

    this.authService.forgotPassword(this.email().trim()).subscribe({
      next: () => {
        this.busy.set(false);
        this.infoKey.set('auth.codeSentInfo');
        this.step.set('confirm');
        this.toast.success('auth.codeSentInfo');
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        const key = err.error?.title ?? 'auth.errResetFailed';
        this.errorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  resetPassword(): void {
    this.codeTouched.set(true);
    this.newPasswordTouched.set(true);
    this.confirmPasswordTouched.set(true);

    if (this.codeError() || this.newPasswordError() || this.confirmPasswordError()) {
      return;
    }

    const code = this.code().trim();
    const password = this.newPassword();

    this.busy.set(true);
    this.errorKey.set('');

    this.authService.resetPassword(this.email().trim(), code, password).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success('auth.resetSuccess');
        this.router.navigate(['/login'], { queryParams: { reset: '1' } });
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        const key = err.error?.title ?? 'auth.errResetFailed';
        this.errorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  backToRequest(): void {
    this.step.set('request');
    this.errorKey.set('');
    this.infoKey.set('');
    this.code.set('');
    this.codeTouched.set(false);
    this.newPasswordTouched.set(false);
    this.confirmPasswordTouched.set(false);
  }
}
