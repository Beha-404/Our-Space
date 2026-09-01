import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../shared/toast/toast.service';

@Component({
  imports: [RouterLink, TranslatePipe],
  selector: 'app-login-page',
  styleUrl: './login-page.css',
  templateUrl: './login-page.html',
})
export class LoginPage {
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  loginData = signal({
    username: '',
    password: '',
  });

  errorKey = signal('');
  infoKey = signal('');
  showPassword = signal(false);
  step = signal<'credentials' | 'code'>('credentials');
  code = signal('');
  busy = signal(false);

  usernameTouched = signal(false);
  passwordTouched = signal(false);
  codeTouched = signal(false);

  usernameError = computed(() =>
    this.usernameTouched() && !this.loginData().username.trim() ? 'auth.errUsernameRequired' : '');

  passwordError = computed(() =>
    this.passwordTouched() && !this.loginData().password ? 'auth.errPasswordRequired' : '');

  codeError = computed(() =>
    this.codeTouched() && !this.code().trim() ? 'auth.errResetCodeEmpty' : '');

  constructor() {
    if (inject(ActivatedRoute).snapshot.queryParamMap.get('reset') === '1') {
      this.infoKey.set('auth.resetSuccess');
    }
  }

  updateField(field: 'username' | 'password', value: string): void {
    this.loginData.update(data => ({ ...data, [field]: value }));
  }

  login(): void {
    this.usernameTouched.set(true);
    this.passwordTouched.set(true);

    if (this.usernameError() || this.passwordError()) {
      this.errorKey.set('auth.errFillAll');
      return;
    }

    const data = this.loginData();

    this.errorKey.set('');
    this.busy.set(true);

    this.authService.login(data).subscribe({
      next: response => {
        this.busy.set(false);

        if (response.requiresTwoFactor) {
          this.step.set('code');
          this.infoKey.set('auth.twoFactorInfo');
          return;
        }

        this.toast.success('toast.loginSuccess');
        this.router.navigate(['/home']);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        const key = this.messageFor(err);
        this.errorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  private messageFor(err: HttpErrorResponse): string {
    if (err.status === 0) return 'auth.errNoConnection';
    return err.error?.title ?? 'auth.errLoginFailed';
  }

  verify(): void {
    this.codeTouched.set(true);

    if (this.codeError()) {
      return;
    }

    const code = this.code().trim();

    this.errorKey.set('');
    this.busy.set(true);

    this.authService.verifyLogin(this.loginData().username, code).subscribe({
      next: () => {
        this.toast.success('toast.loginSuccess');
        this.router.navigate(['/home']);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        const key = this.messageFor(err);
        this.errorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  backToCredentials(): void {
    this.step.set('credentials');
    this.code.set('');
    this.codeTouched.set(false);
    this.errorKey.set('');
    this.infoKey.set('');
  }
}
