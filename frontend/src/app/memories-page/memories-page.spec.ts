import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { config } from '../config';
import { MemoriesPage } from './memories-page';

describe('MemoriesPage upload form', () => {
  let component: MemoriesPage;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MemoriesPage],
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

    component = TestBed.createComponent(MemoriesPage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: null });
  });

  afterEach(() => httpMock.verify());

  it('flags an empty selection, date and caption after clicking upload', () => {
    component.upload();

    expect(component.showUploadFillAllError()).toBe(true);
    expect(component.uploadCaptionError()).toBe('memories.errTitleRequired');
  });

  it('does not submit while no file has been selected, even with a date and caption filled in', () => {
    component.updateUploadDate('2026-09-10');
    component.updateUploadCaption('Beach day');

    component.upload();

    expect(component.uploading()).toBe(false);
  });

  it('clears the fill-all error reactively once a file, date and caption are all provided', () => {
    component.upload();
    expect(component.showUploadFillAllError()).toBe(true);

    component.uploadQueue.set([{ file: new File(['x'], 'a.jpg'), progress: 0, status: 'pending' }]);
    component.updateUploadDate('2026-09-10');
    component.updateUploadCaption('Beach day');

    expect(component.showUploadFillAllError()).toBe(false);
  });

  it('clears a stale server error from a failed upload as soon as the caption is edited', () => {
    component.uploadErrorKey.set('memories.uploadError');

    component.updateUploadCaption('Beach day');

    expect(component.uploadErrorKey()).toBe('');
  });

  it('clears a stale server error from a failed upload as soon as the date is edited', () => {
    component.uploadErrorKey.set('memories.uploadError');

    component.updateUploadDate('2026-09-10');

    expect(component.uploadErrorKey()).toBe('');
  });
});

describe('MemoriesPage feed toolbar', () => {
  let component: MemoriesPage;
  let httpMock: HttpTestingController;

  const emptyFeed = { items: [], hasMore: false, totalCount: 30, years: [] };

  function expectFeedRequest(params: Record<string, string>) {
    const req = httpMock.expectOne(r => r.url === `${config.apiUrl}/memories`);
    for (const [key, value] of Object.entries(params)) {
      expect(req.request.params.get(key)).toBe(value);
    }
    req.flush(emptyFeed);
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MemoriesPage],
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

    component = TestBed.createComponent(MemoriesPage).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: { id: 2, username: 'test2' } });
    expectFeedRequest({ page: '1', sort: 'newest' });
  });

  afterEach(() => httpMock.verify());

  it('asks the server only for voice letters when that type is chosen', () => {
    component.setFeedTypeFilter('audio');

    expectFeedRequest({ page: '1', type: 'audio' });
    expect(component.isFiltered()).toBe(true);
  });

  it('does not reload when the already selected type is chosen again', () => {
    component.setFeedTypeFilter('all');

    httpMock.expectNone(r => r.url === `${config.apiUrl}/memories`);
  });

  it('flips between newest and oldest with one button', () => {
    component.toggleSortOrder();
    expectFeedRequest({ sort: 'oldest' });

    component.toggleSortOrder();
    expectFeedRequest({ sort: 'newest' });
  });

  it('closes the more menu when clicking anywhere outside it', () => {
    component.toggleMoreMenu();
    expect(component.moreMenuOpen()).toBe(true);

    component.onDocumentClick({ target: document.body } as unknown as MouseEvent);

    expect(component.moreMenuOpen()).toBe(false);
  });

  it('works out the number of pages from the total the server reports', () => {
    expect(component.totalPages()).toBe(2);
  });
});

describe('MemoriesPage filter from a link', () => {
  it('opens with only photos when the link asks for them', async () => {
    await TestBed.configureTestingModule({
      imports: [MemoriesPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ filter: 'photo' }) } },
        },
      ],
    }).compileComponents();

    const component = TestBed.createComponent(MemoriesPage).componentInstance;
    const httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne(`${config.apiUrl}/user/current`).flush({ id: 1, username: 'test1', partner: { id: 2, username: 'test2' } });
    const req = httpMock.expectOne(r => r.url === `${config.apiUrl}/memories`);
    expect(req.request.params.get('type')).toBe('photo');
    req.flush({ items: [], hasMore: false, totalCount: 0, years: [] });

    expect(component.feedTypeFilter()).toBe('photo');
    httpMock.verify();
  });
});
