import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RegisterPage } from './register-page';

describe('RegisterPage validation', () => {
  let component: RegisterPage;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    component = TestBed.createComponent(RegisterPage).componentInstance;
  });

  it('shows no errors before submitting', () => {
    expect(component.showFillAllError()).toBe(false);
  });

  it('flags every empty field after submitting', () => {
    component.register();

    expect(component.showFillAllError()).toBe(true);
    expect(component.usernameError()).toBe('auth.errUsernameRequired');
    expect(component.emailError()).toBe('auth.errEmailRequired');
    expect(component.passwordError()).toBe('auth.errPasswordRequired');
    expect(component.confirmPasswordError()).toBe('auth.errConfirmPasswordRequired');
  });

  it('rejects an invalid email even when every field is filled in', () => {
    component.register();
    component.updateField('username', 'test1');
    component.updateField('email', 'not-an-email');
    component.updateField('password', 'Password123');
    component.updateField('confirmPassword', 'Password123');

    expect(component.emailError()).toBe('auth.errInvalidEmail');
    expect(component.showFillAllError()).toBe(true);
  });

  it('rejects a weak password', () => {
    component.register();
    component.updateField('password', 'weak');

    expect(component.passwordError()).toBe('auth.errPasswordWeak');
  });

  it('flags a confirm-password mismatch', () => {
    component.register();
    component.updateField('password', 'Password123');
    component.updateField('confirmPassword', 'Password124');

    expect(component.confirmPasswordError()).toBe('auth.errPasswordMismatch');
  });

  it('clears the fill-all error reactively once every field becomes valid, without resubmitting', () => {
    component.register();
    expect(component.showFillAllError()).toBe(true);

    component.updateField('username', 'test1');
    component.updateField('email', 'test1@example.com');
    component.updateField('password', 'Password123');
    component.updateField('confirmPassword', 'Password123');

    expect(component.showFillAllError()).toBe(false);
  });

  it('marks each field valid only once it is both touched and correct', () => {
    expect(component.usernameValid()).toBe(false);
    component.updateField('username', 'test1');
    component.markTouched('username');
    expect(component.usernameValid()).toBe(true);
  });
});
