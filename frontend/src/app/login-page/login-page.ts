import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
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

  updateField(field: 'username' | 'password', value: string): void {
    this.loginData.update(data => ({ ...data, [field]: value }));
  }

  login(): void {
    const data = this.loginData();

    if (!data.username || !data.password) {
      this.errorKey.set('auth.errFillAll');
      return;
    }

    this.authService.login(data).subscribe({
      next: () => this.router.navigate(['/home']),
      error: () => this.errorKey.set('auth.errLoginFailed')
    });
  }
}
