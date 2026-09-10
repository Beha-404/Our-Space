import { HttpEventType } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { config } from '../config';
import { User } from '../interfaces/user';
import { UserService } from './user.service';

describe('UserService.uploadProfilePicture', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('posts the blob as multipart form data under the "file" field', () => {
    const blob = new Blob(['fake-image-bytes'], { type: 'image/jpeg' });

    service.uploadProfilePicture(blob).subscribe();

    const req = httpMock.expectOne(`${config.apiUrl}/user/profile-picture`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);
    expect((req.request.body as FormData).get('file')).toBeTruthy();

    req.flush({} as User);
  });

  it('reports upload progress events without touching currentUser', () => {
    const blob = new Blob(['fake-image-bytes'], { type: 'image/jpeg' });
    const events: number[] = [];

    service.uploadProfilePicture(blob).subscribe(event => {
      if (event.type === HttpEventType.UploadProgress && event.total) {
        events.push(Math.round(100 * event.loaded / event.total));
      }
    });

    const req = httpMock.expectOne(`${config.apiUrl}/user/profile-picture`);
    req.event({ type: HttpEventType.UploadProgress, loaded: 50, total: 100 });
    req.event({ type: HttpEventType.UploadProgress, loaded: 100, total: 100 });

    expect(events).toEqual([50, 100]);
    expect(service.currentUser()).toBeNull();

    req.flush({ id: 1, username: 'test1' } as unknown as User);
  });

  it('updates currentUser once the final response arrives', () => {
    const blob = new Blob(['fake-image-bytes'], { type: 'image/jpeg' });
    const updatedUser = { id: 1, username: 'test1', profilePictureUrl: '/uploads/avatars/x.webp' } as unknown as User;

    service.uploadProfilePicture(blob).subscribe();

    const req = httpMock.expectOne(`${config.apiUrl}/user/profile-picture`);
    req.flush(updatedUser);

    expect(service.currentUser()).toEqual(updatedUser);
  });
});
