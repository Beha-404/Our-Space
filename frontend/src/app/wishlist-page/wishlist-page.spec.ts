import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { config } from '../config';
import { WishlistPage } from './wishlist-page';

describe('WishlistPage add-wish form', () => {
  let component: WishlistPage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WishlistPage],
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

describe('WishlistPage sky and list', () => {
  let component: WishlistPage;
  let httpMock: HttpTestingController;

  const wish = (id: number, isFulfilled = false, day = id) => ({
    id,
    title: `Wish ${id}`,
    isFulfilled,
    fulfilledAt: isFulfilled ? `2026-09-${String(day).padStart(2, '0')}T10:00:00Z` : null,
    createdByUsername: 'test1',
    createdAt: `2026-08-${String(day).padStart(2, '0')}T10:00:00Z`,
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WishlistPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ highlight: '3' }) } },
        },
      ],
    }).compileComponents();

    component = TestBed.createComponent(WishlistPage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: { id: 2 } });
  });

  afterEach(() => httpMock.verify());

  const load = (wishes: ReturnType<typeof wish>[]) =>
    httpMock.expectOne(`${config.apiUrl}/wishlist`).flush(wishes);

  it('counts fulfilled and waiting wishes and the progress', () => {
    load([wish(1, true), wish(2), wish(3), wish(4)]);

    expect(component.fulfilledCount()).toBe(1);
    expect(component.waitingCount()).toBe(3);
    expect(component.progressPercent()).toBe(25);
  });

  it('opens the wish named in the notification link', () => {
    load([wish(1), wish(2), wish(3)]);

    expect(component.selected()?.id).toBe(3);
  });

  it('falls back to the newest wish when nothing is selected', () => {
    component.selectedId.set(null);
    load([wish(1), wish(5)]);
    component.selectedId.set(null);

    expect(component.selected()?.id).toBe(5);
  });

  it('shows ten wishes per page and moves between pages', () => {
    load(Array.from({ length: 25 }, (_, index) => wish(index + 1)));

    expect(component.visibleWishes()).toHaveLength(10);
    expect(component.totalListPages()).toBe(3);

    component.goToListPage(3);

    expect(component.visibleWishes()).toHaveLength(5);

    component.goToListPage(99);

    expect(component.currentListPage()).toBe(3);
  });

  it('goes back to the first page when the tab changes', () => {
    load([...Array.from({ length: 25 }, (_, index) => wish(index + 1)), wish(100, true)]);
    component.goToListPage(3);

    component.setTab('fulfilled');

    expect(component.currentListPage()).toBe(1);
  });

  it('lists only fulfilled wishes on the fulfilled tab, most recently fulfilled first', () => {
    load([wish(1, true, 5), wish(2), wish(3, true, 9)]);

    component.setTab('fulfilled');

    expect(component.tabWishes().map(w => w.id)).toEqual([3, 1]);
  });

  it('caps the sky at forty stars', () => {
    load(Array.from({ length: 60 }, (_, index) => wish(index + 1, false, (index % 28) + 1)));

    expect(component.skyWishes()).toHaveLength(40);
  });

  it('selects the wish it just created after reloading', () => {
    load([wish(1)]);
    component.updateNewWish('Nova');
    component.addWish();

    httpMock.expectOne(r => r.method === 'POST').flush(wish(9));
    httpMock.expectOne(`${config.apiUrl}/wishlist`).flush([wish(1), wish(9)]);

    expect(component.selected()?.id).toBe(9);
  });
});

describe('WishlistPage sky', () => {
  let component: WishlistPage;
  let httpMock: HttpTestingController;

  const wish = (id: number, isFulfilled: boolean) => ({
    id,
    title: `Wish ${id}`,
    isFulfilled,
    fulfilledAt: isFulfilled ? '2026-09-01T10:00:00Z' : null,
    createdByUsername: 'test1',
    createdAt: new Date(Date.UTC(2026, 0, 1, 0, id)).toISOString(),
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WishlistPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();

    component = TestBed.createComponent(WishlistPage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: { id: 2 } });
  });

  afterEach(() => httpMock.verify());

  const load = (wishes: ReturnType<typeof wish>[]) =>
    httpMock.expectOne(`${config.apiUrl}/wishlist`).flush(wishes);

  it('shows the forty newest wishes whether or not they are fulfilled', () => {
    const older = Array.from({ length: 30 }, (_, index) => wish(index + 1, false));
    const newest = Array.from({ length: 50 }, (_, index) => wish(100 + index, index % 2 === 0));
    load([...older, ...newest]);

    const sky = component.skyWishes();

    expect(sky).toHaveLength(40);
    expect(sky.every(w => w.id >= 110)).toBe(true);
  });

  it('shows every wish when there are fewer than forty', () => {
    load([wish(1, false), wish(2, true), wish(3, false)]);

    expect(component.skyWishes()).toHaveLength(3);
  });
});

describe('WishlistPage opened from the recap', () => {
  it('starts on the fulfilled tab when the link asks for it', async () => {
    await TestBed.configureTestingModule({
      imports: [WishlistPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({ tab: 'fulfilled' }) } } },
      ],
    }).compileComponents();

    const component = TestBed.createComponent(WishlistPage).componentInstance;
    const httpMock = TestBed.inject(HttpTestingController);
    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: null });

    expect(component.tab()).toBe('fulfilled');
    httpMock.verify();
  });
});
