import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { config } from '../config';
import { WishlistPage } from './wishlist-page';

describe('WishlistPage add-wish form', () => {
  let component: WishlistPage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WishlistPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    component = TestBed.createComponent(WishlistPage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: null });
  });

  afterEach(() => httpMock.verify());

  it('shows an error when adding an empty wish', () => {
    component.addWish();

    expect(component.newWishError()).toBe('wishlist.errEmpty');
    expect(component.addErrorKey()).toBe('wishlist.errEmpty');
  });

  it('clears a stale add-error reactively as soon as the user starts typing, without resubmitting', () => {
    component.addWish();
    expect(component.addErrorKey()).toBe('wishlist.errEmpty');

    component.updateNewWish('a puppy');

    expect(component.addErrorKey()).toBe('');
    expect(component.newWishError()).toBe('');
  });

  it('clears a stale server error from a failed add as soon as the field is edited', () => {
    component.addErrorKey.set('wishlist.addError');

    component.updateNewWish('a new idea');

    expect(component.addErrorKey()).toBe('');
  });

  it('marks the field valid only once touched, non-empty and not yet submitted with an error', () => {
    expect(component.newWishValid()).toBe(false);

    component.updateNewWish('a puppy');
    component.newWishTouched.set(true);

    expect(component.newWishValid()).toBe(true);
  });
});
