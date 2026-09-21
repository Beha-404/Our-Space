import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { config } from '../config';
import { Capsule } from '../interfaces/capsule';
import { CapsulesPage } from './capsules-page';
import { presetDate } from './capsule-dates';

function capsule(overrides: Partial<Capsule> = {}): Capsule {
  return {
    id: 1,
    title: 'Za našu petu godišnjicu',
    message: null,
    openAt: '2099-09-17',
    openedAt: null,
    isUnlocked: false,
    canOpenNow: false,
    createdByUsername: 'test1',
    createdAt: '2026-09-17T10:00:00Z',
    ...overrides,
  };
}

describe('CapsulesPage', () => {
  let component: CapsulesPage;
  let httpMock: HttpTestingController;

  const capsulesUrl = `${config.apiUrl}/capsules`;

  function flushLoad(list: Capsule[] = []) {
    httpMock.expectOne(capsulesUrl).flush(list);
  }

  beforeEach(async () => {
    localStorage.setItem('lang', 'bs');
    await TestBed.configureTestingModule({
      imports: [CapsulesPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: convertToParamMap({}) } } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(CapsulesPage);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`)
      .flush({ id: 1, username: 'test1', partner: { id: 2, username: 'test2' } });
    flushLoad();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.removeItem('lang');
  });

  it('opens the form with a date one year ahead already chosen', () => {
    component.openForm();

    expect(component.showForm()).toBe(true);
    expect(component.preset()).toBe('year');
    expect(component.openMode()).toBe('date');
    expect(component.formData().openAt).toBe(presetDate('year'));
  });

  it('fills the date from each quick choice', () => {
    component.openForm();

    component.setPreset('month');
    expect(component.formData().openAt).toBe(presetDate('month'));

    component.setPreset('fiveYears');
    expect(component.formData().openAt).toBe(presetDate('fiveYears'));
  });

  it('keeps the chosen date when switching to another date and clears it for anytime', () => {
    component.openForm();
    const before = component.formData().openAt;

    component.setPreset('custom');
    expect(component.openMode()).toBe('date');
    expect(component.formData().openAt).toBe(before);

    component.setPreset('anytime');
    expect(component.openMode()).toBe('anytime');
    expect(component.formData().openAt).toBe('');
    expect(component.dateMissing()).toBe(false);
  });

  it('does not send anything until a title and a message are written', () => {
    component.openForm();

    component.seal();

    httpMock.expectNone(capsulesUrl);
    expect(component.showFillAllError()).toBe(true);
  });

  it('creates the capsule with the chosen date and lets the new bottle splash in', () => {
    component.openForm();
    component.updateField('title', '  Otvori kad ti fali ');
    component.updateField('message', 'Volim te');

    component.seal();

    const post = httpMock.expectOne(r => r.method === 'POST' && r.url === capsulesUrl);
    expect(post.request.body).toEqual({
      title: 'Otvori kad ti fali',
      message: 'Volim te',
      openAt: presetDate('year'),
    });
    post.flush(capsule({ id: 9 }));

    expect(component.arrivedId()).toBe(9);
    expect(component.showForm()).toBe(false);
    flushLoad([capsule({ id: 9 })]);
    expect(component.sealed().length).toBe(1);
  });

  it('sends no date for a capsule that opens whenever', () => {
    component.openForm();
    component.setPreset('anytime');
    component.updateField('title', 'Test123');
    component.updateField('message', 'Poruka');

    component.seal();

    const post = httpMock.expectOne(r => r.method === 'POST' && r.url === capsulesUrl);
    expect(post.request.body.openAt).toBeNull();
    post.flush(capsule({ id: 3, openAt: null, canOpenNow: true }));
    flushLoad();
  });

  it('moves an opened capsule from the sea to the letters', () => {
    const anytime = capsule({ id: 5, openAt: null, canOpenNow: true });
    component.capsules.set([anytime]);
    expect(component.sealed().length).toBe(1);

    component.openCapsule(anytime);
    httpMock.expectOne(`${capsulesUrl}/5/open`).flush({ ...anytime, isUnlocked: true, canOpenNow: false, message: 'Pismo' });

    expect(component.sealed().length).toBe(0);
    expect(component.opened().map(c => c.id)).toEqual([5]);
    expect(component.openingId()).toBeNull();
  });

  it('lets the author delete a sealed capsule but not the partner', () => {
    expect(component.canDelete(capsule({ createdByUsername: 'test1' }))).toBe(true);
    expect(component.canDelete(capsule({ createdByUsername: 'test2' }))).toBe(false);
    expect(component.canDelete(capsule({ createdByUsername: 'test2', isUnlocked: true }))).toBe(true);
  });
});
