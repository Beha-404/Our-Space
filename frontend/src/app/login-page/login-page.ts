import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { AuthService } from '../services/auth.service';

@Component({
  imports: [RouterLink, TranslatePipe],
  selector: 'app-login-page',
  styleUrl: './login-page.css',
  templateUrl: './login-page.html',
})
export class LoginPage {
  private authService = inject(AuthService);
  private router = inject(Router);

  loginData = signal({
    username: '',
    password: '',
  });

  errorKey = signal('');
  infoKey = signal('');
  step = signal<'credentials' | 'code'>('credentials');
  code = signal('');
  busy = signal(false);

  constructor() {
    if (inject(ActivatedRoute).snapshot.queryParamMap.get('reset') === '1') {
      this.infoKey.set('auth.resetSuccess');
    }
  }

  updateField(field: 'username' | 'password', value: string): void {
    this.loginData.update(data => ({ ...data, [field]: value }));
  }

  login(): void {
    const data = this.loginData();

    if (!data.username || !data.password) {
      this.errorKey.set('auth.errFillAll');
      return;
    }

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

        this.router.navigate(['/home']);
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.errorKey.set(err.error?.title ?? 'auth.errLoginFailed');
      }
    });
  }

  verify(): void {
    const code = this.code().trim();

    if (!code) {
      this.errorKey.set('auth.errResetCodeEmpty');
      return;
    }

    this.errorKey.set('');
    this.busy.set(true);

    this.authService.verifyLogin(this.loginData().username, code).subscribe({
      next: () => this.router.navigate(['/home']),
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.errorKey.set(err.error?.title ?? 'auth.errLoginFailed');
      }
    });
  }

  backToCredentials(): void {
    this.step.set('credentials');
    this.code.set('');
    this.errorKey.set('');
    this.infoKey.set('');
  }
}
