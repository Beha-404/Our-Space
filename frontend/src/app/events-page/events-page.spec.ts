import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { config } from '../config';
import { EventsPage } from './events-page';

describe('EventsPage add/edit forms', () => {
  let component: EventsPage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventsPage],
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

    component = TestBed.createComponent(EventsPage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: null });
  });

  afterEach(() => httpMock.verify());

  it('flags a missing title and date when adding an event', () => {
    component.addEvent();

    expect(component.showAddFillAllError()).toBe(true);
    expect(component.titleError()).toBe('events.errTitleRequired');
  });

  it('clears the add fill-all error reactively once title and date are both filled in', () => {
    component.addEvent();
    expect(component.showAddFillAllError()).toBe(true);

    component.updateField('title', 'Anniversary');
    component.updateField('eventDate', '2026-10-10');

    expect(component.showAddFillAllError()).toBe(false);
  });

  it('clears a stale server error on the add form as soon as a field is edited', () => {
    component.addErrorKey.set('events.addError');

    component.updateField('title', 'Anniversary');

    expect(component.addErrorKey()).toBe('');
  });

  it('flags a missing title and date when editing an event', () => {
    component.editFormData.set({ title: '', description: '', eventDate: '' });
    component.saveEdit(1);

    expect(component.showEditFillAllError()).toBe(true);
    expect(component.editTitleError()).toBe('events.errTitleRequired');
  });

  it('clears the edit fill-all error reactively once title and date are both filled in', () => {
    component.saveEdit(1);
    expect(component.showEditFillAllError()).toBe(true);

    component.updateEditField('title', 'Anniversary');
    component.updateEditField('eventDate', '2026-10-10');

    expect(component.showEditFillAllError()).toBe(false);
  });

  it('clears a stale server error on the edit form as soon as a field is edited', () => {
    component.editErrorKey.set('events.addError');

    component.updateEditField('title', 'Anniversary');

    expect(component.editErrorKey()).toBe('');
  });
});

describe('EventsPage cancelling', () => {
  let component: EventsPage;
  let httpMock: HttpTestingController;

  const event = (id: number, isCancelled = false) => ({
    id,
    title: `Event ${id}`,
    description: null,
    eventDate: '2027-01-10T00:00:00Z',
    createdByUsername: 'test1',
    createdAt: '2026-09-01T00:00:00Z',
    isCancelled,
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventsPage],
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

    component = TestBed.createComponent(EventsPage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: { id: 2 } });
    httpMock.expectOne(r => r.url.startsWith(`${config.apiUrl}/events?`)).flush([event(1), event(2)]);
  });

  afterEach(() => httpMock.verify());

  it('marks the event as cancelled in place without reloading the list', () => {
    component.cancelEvent(component.events()[0]);

    httpMock.expectOne(`${config.apiUrl}/events/1/cancel`).flush(event(1, true));

    expect(component.events().find(e => e.id === 1)?.isCancelled).toBe(true);
    expect(component.events().find(e => e.id === 2)?.isCancelled).toBe(false);
  });

  it('brings a cancelled event back', () => {
    component.events.set([event(1, true)]);

    component.restoreEvent(component.events()[0]);

    httpMock.expectOne(`${config.apiUrl}/events/1/restore`).flush(event(1, false));

    expect(component.events()[0].isCancelled).toBe(false);
  });

  it('leaves the event untouched when cancelling fails', () => {
    component.cancelEvent(component.events()[0]);

    httpMock.expectOne(`${config.apiUrl}/events/1/cancel`).flush('no', { status: 500, statusText: 'Server Error' });

    expect(component.events()[0].isCancelled).toBe(false);
  });
});
