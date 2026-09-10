import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { LoginPage } from './login-page';

describe('LoginPage validation', () => {
  let component: LoginPage;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({}) } },
        },
      ],
    }).compileComponents();

    component = TestBed.createComponent(LoginPage).componentInstance;
  });

  it('shows no error before the user submits anything', () => {
    expect(component.showFillAllError()).toBe(false);
  });

  it('shows the fill-all error after submitting with empty fields', () => {
    component.login();

    expect(component.showFillAllError()).toBe(true);
    expect(component.usernameError()).toBe('auth.errUsernameRequired');
    expect(component.passwordError()).toBe('auth.errPasswordRequired');
  });

  it('clears the fill-all error reactively as soon as the missing field is filled in, without resubmitting', () => {
    component.login();
    expect(component.showFillAllError()).toBe(true);

    component.updateField('username', 'test1');
    component.updateField('password', 'Password123');

    expect(component.showFillAllError()).toBe(false);
  });

  it('does not show the fill-all error while the user is still typing and has not submitted', () => {
    component.updateField('username', '');
    component.passwordTouched.set(true);

    expect(component.showFillAllError()).toBe(false);
  });
});
