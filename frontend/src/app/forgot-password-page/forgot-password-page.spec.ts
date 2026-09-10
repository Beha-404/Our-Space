import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ForgotPasswordPage } from './forgot-password-page';

describe('ForgotPasswordPage validation', () => {
  let component: ForgotPasswordPage;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ForgotPasswordPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    component = TestBed.createComponent(ForgotPasswordPage).componentInstance;
  });

  it('requires a valid email before requesting a code', () => {
    component.requestCode();
    expect(component.emailError()).toBe('auth.errEmailRequired');

    component.updateEmail('not-an-email');
    expect(component.emailError()).toBe('auth.errInvalidEmail');

    component.updateEmail('test1@example.com');
    expect(component.emailError()).toBe('');
  });

  it('requires the code and a strong new password before resetting', () => {
    component.resetPassword();

    expect(component.codeError()).toBe('auth.errResetCodeEmpty');
    expect(component.newPasswordError()).toBe('auth.errPasswordRequired');
    expect(component.confirmPasswordError()).toBe('auth.errConfirmPasswordRequired');
  });

  it('flags a confirm-password mismatch', () => {
    component.updateNewPassword('Password123');
    component.updateConfirmPassword('Password124');
    component.resetPassword();

    expect(component.confirmPasswordError()).toBe('auth.errPasswordMismatch');
  });

  it('clears a stale server error as soon as the email is edited, instead of leaving it stuck', () => {
    component.errorKey.set('auth.errResetFailed');

    component.updateEmail('test1@example.com');

    expect(component.errorKey()).toBe('');
  });

  it('clears a stale server error as soon as the code is edited', () => {
    component.errorKey.set('auth.errResetFailed');

    component.updateCode('123456');

    expect(component.errorKey()).toBe('');
  });

  it('clears a stale server error as soon as the new password is edited', () => {
    component.errorKey.set('auth.errResetFailed');

    component.updateNewPassword('Password123');

    expect(component.errorKey()).toBe('');
  });

  it('clears a stale server error as soon as the confirm password is edited', () => {
    component.errorKey.set('auth.errResetFailed');

    component.updateConfirmPassword('Password123');

    expect(component.errorKey()).toBe('');
  });
});
