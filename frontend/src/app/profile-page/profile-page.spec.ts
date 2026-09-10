import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { config } from '../config';
import { ProfilePage } from './profile-page';

describe('ProfilePage forms', () => {
  let component: ProfilePage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    component = TestBed.createComponent(ProfilePage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: null });
  });

  afterEach(() => httpMock.verify());

  describe('username field', () => {
    it('requires a non-empty username', () => {
      component.updateField('username', '');
      component.usernameTouched.set(true);

      expect(component.usernameError()).toBe('profile.errUsernameRequired');
      expect(component.usernameValid()).toBe(false);
    });

    it('clears a stale save error and success message reactively as soon as the username is edited', () => {
      component.saveErrorKey.set('profile.saveError');
      component.saveMessageKey.set('profile.saved');

      component.updateField('username', 'newname');

      expect(component.saveErrorKey()).toBe('');
      expect(component.saveMessageKey()).toBe('');
    });
  });

  describe('pairing code field', () => {
    it('requires a non-empty code', () => {
      component.submitPair();
      expect(component.pairInputError()).toBe('profile.pairErrorEmpty');
    });

    it('clears a stale pairing error reactively as soon as the code is edited', () => {
      component.pairErrorKey.set('profile.pairError');

      component.updatePairInput('ABC123');

      expect(component.pairErrorKey()).toBe('');
    });
  });

  describe('email change field', () => {
    it('requires a valid email', () => {
      component.requestEmailChange();
      expect(component.newEmailError()).toBe('auth.errEmailRequired');

      component.updateNewEmail('not-an-email');
      expect(component.newEmailError()).toBe('auth.errInvalidEmail');
    });

    it('clears a stale email-change error reactively as soon as the email is edited', () => {
      component.emailErrorKey.set('profile.emailChangeError');

      component.updateNewEmail('new@example.com');

      expect(component.emailErrorKey()).toBe('');
    });
  });

  describe('email confirmation code field', () => {
    it('requires a non-empty code', () => {
      component.confirmEmailChange();
      expect(component.confirmCodeError()).toBe('profile.emailConfirmError');
    });

    it('clears a stale confirmation error reactively as soon as the code is edited', () => {
      component.confirmErrorKey.set('profile.emailConfirmError');

      component.updateConfirmCode('123456');

      expect(component.confirmErrorKey()).toBe('');
    });
  });

  describe('relationship date field', () => {
    it('clears a stale date error reactively as soon as the date is edited', () => {
      component.relationshipDateErrorKey.set('profile.dateEmptyError');

      component.updateRelationshipDateEdit('2026-01-01');

      expect(component.relationshipDateErrorKey()).toBe('');
    });
  });
});
