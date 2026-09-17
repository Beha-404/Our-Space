import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { config } from '../config';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { getToken: () => 'secret-token' } },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sends the token to our own API', () => {
    http.get(`${config.apiUrl}/home`).subscribe();

    const req = httpMock.expectOne(`${config.apiUrl}/home`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer secret-token');
    req.flush({});
  });

  it('never sends the token to another server', () => {
    http.get('https://nominatim.openstreetmap.org/search?q=Mostar').subscribe();

    const req = httpMock.expectOne('https://nominatim.openstreetmap.org/search?q=Mostar');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });
});
