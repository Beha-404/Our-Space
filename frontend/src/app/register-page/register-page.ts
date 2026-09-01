import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../shared/toast/toast.service';
import { isStrongPassword, isValidEmail } from '../shared/validators';

type Field = 'username' | 'email' | 'password' | 'confirmPassword';

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

  touched = signal<Record<Field, boolean>>({
    username: false,
    email: false,
    password: false,
    confirmPassword: false,
  });

  errorKey = signal('');

  usernameError = computed(() => {
    if (!this.touched().username) return '';
    return this.registerData().username.trim() ? '' : 'auth.errUsernameRequired';
  });

  emailError = computed(() => {
    if (!this.touched().email) return '';
    const email = this.registerData().email.trim();
    if (!email) return 'auth.errEmailRequired';
    return isValidEmail(email) ? '' : 'auth.errInvalidEmail';
  });

  passwordError = computed(() => {
    if (!this.touched().password) return '';
    const password = this.registerData().password;
    if (!password) return 'auth.errPasswordRequired';
    return isStrongPassword(password) ? '' : 'auth.errPasswordWeak';
  });

  confirmPasswordError = computed(() => {
    if (!this.touched().confirmPassword) return '';
    const { password, confirmPassword } = this.registerData();
    if (!confirmPassword) return 'auth.errConfirmPasswordRequired';
    return password === confirmPassword ? '' : 'auth.errPasswordMismatch';
  });

  updateField(field: Field, value: string): void {
    this.registerData.update(data => ({ ...data, [field]: value }));
  }

  markTouched(field: Field): void {
    this.touched.update(t => ({ ...t, [field]: true }));
  }

  register(): void {
    this.errorKey.set('');
    this.touched.set({ username: true, email: true, password: true, confirmPassword: true });

    if (this.usernameError() || this.emailError() || this.passwordError() || this.confirmPasswordError()) {
      this.errorKey.set('auth.errFillAll');
      return;
    }

    const data = this.registerData();

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
