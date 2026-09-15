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
