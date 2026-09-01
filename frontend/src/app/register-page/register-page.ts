import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { form, required } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../shared/toast/toast.service';

const EMAIL_REGEX = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const PASSWORD_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{9,}$/;

@Component({
  imports: [RouterLink, TranslatePipe],
  selector: 'app-register-page',
  styleUrl: './register-page.css',
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  registerData = signal({
    username: '',
    email: '',
    password: '',
    confirmPassword: ''
  });

  registerForm = form(this.registerData, schema => {
    required(schema.username);
    required(schema.email);
    required(schema.password);
    required(schema.confirmPassword);
  });

  errorKey = signal('');

  updateField(field: 'username' | 'email' | 'password' | 'confirmPassword', value: string): void {
    this.registerData.update(data => ({ ...data, [field]: value }));
  }

  register(): void {
    this.errorKey.set('');
    const data = this.registerData();

    if (!this.registerForm().valid()) {
      this.errorKey.set('auth.errFillAll');
      return;
    }

    if (!EMAIL_REGEX.test(data.email)) {
      this.errorKey.set('auth.errInvalidEmail');
      return;
    }

    if (!PASSWORD_REGEX.test(data.password)) {
      this.errorKey.set('auth.errPasswordWeak');
      return;
    }

    if (data.password !== data.confirmPassword) {
      this.errorKey.set('auth.errPasswordMismatch');
      return;
    }

    this.authService.register({
      username: data.username,
      email: data.email,
      password: data.password
    }).subscribe({
      next: () => {
        this.toast.success('toast.registerSuccess');
        this.router.navigate(['/login']);
      },
      error: (err: HttpErrorResponse) => {
        const key = err.error?.title ?? 'auth.errRegisterFailed';
        this.errorKey.set(key);
        this.toast.error(key);
      }
    });
  }
}
