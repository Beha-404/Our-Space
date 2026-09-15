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
