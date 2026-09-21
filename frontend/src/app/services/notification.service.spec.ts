import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { config } from '../config';
import { AppNotification } from '../interfaces/notification';
import { NotificationService } from './notification.service';

function make(from: number, to: number): AppNotification[] {
  return Array.from({ length: to - from + 1 }, (_, i) => ({
    id: from + i,
    type: 'PhotoAdded',
    entityType: 'photo',
    entityId: from + i,
    entityTitle: `Stavka ${from + i}`,
    actorUsername: 'test2',
    createdAt: '2026-09-21T10:00:00Z',
    isRead: false,
  }));
}

describe('NotificationService paging', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  const url = `${config.apiUrl}/notifications`;

  const request = (skip: number) =>
    httpMock.expectOne(r => r.url === url && r.params.get('skip') === String(skip) && r.params.get('take') === '21');

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('shows the newest twenty and knows there is more when a twenty-first comes back', () => {
    service.loadRecent();
    request(0).flush(make(1, 21));

    expect(service.notifications().length).toBe(20);
    expect(service.hasMore()).toBe(true);
  });

  it('has nothing more when the server returns exactly twenty', () => {
    service.loadRecent();
    request(0).flush(make(1, 20));

    expect(service.notifications().length).toBe(20);
    expect(service.hasMore()).toBe(false);
  });

  it('adds the next twenty after the ones already shown', () => {
    service.loadRecent();
    request(0).flush(make(1, 21));

    service.loadMore();
    expect(service.loadingMore()).toBe(true);
    request(20).flush(make(21, 35));

    expect(service.notifications().length).toBe(35);
    expect(service.hasMore()).toBe(false);
    expect(service.loadingMore()).toBe(false);
  });

  it('does not show the same notification twice when new ones arrived in between', () => {
    service.loadRecent();
    request(0).flush(make(1, 21));

    service.loadMore();
    request(20).flush(make(20, 30));

    const ids = service.notifications().map(n => n.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('ignores a second load more while one is running and when there is nothing more', () => {
    service.loadRecent();
    request(0).flush(make(1, 21));

    service.loadMore();
    service.loadMore();
    request(20).flush(make(21, 25));

    service.loadMore();
    httpMock.expectNone(r => r.url === url);
  });

  it('lets the user try again after a failed load more', () => {
    service.loadRecent();
    request(0).flush(make(1, 21));

    service.loadMore();
    request(20).flush(null, { status: 500, statusText: 'Server Error' });

    expect(service.loadingMore()).toBe(false);
    expect(service.hasMore()).toBe(true);
    expect(service.notifications().length).toBe(20);
  });
});
