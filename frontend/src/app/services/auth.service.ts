import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { catchError, map, Observable, of, shareReplay, tap } from 'rxjs';
import { config } from '../config';
import { AuthResponse, LoginResponse } from '../interfaces/authResponse';
import { LoginRequest } from '../interfaces/loginRequest';
import { RegisterRequest } from '../interfaces/registerRequest';
import { UserService } from './user.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private http = inject(HttpClient);
    private userService = inject(UserService);
    private apiUrl = config.apiUrl;

    private accessToken: string | null = null;
    private refreshInFlight: Observable<boolean> | null = null;

    readonly sessionRestored = signal(false);

    login(loginData: LoginRequest) {
        return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, loginData, { withCredentials: true }).pipe(
            tap(response => {
                if (response.auth) this.accessToken = response.auth.token;
            })
        );
    }

    verifyLogin(username: string, code: string) {
        return this.http.post<AuthResponse>(`${this.apiUrl}/auth/verify-login`, { username, code }, { withCredentials: true }).pipe(
            tap(response => this.accessToken = response.token)
        );
    }

    register(registerData: RegisterRequest) {
        return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, registerData, { withCredentials: true });
    }

    clearSession(): boolean {
        const hadSession = this.accessToken !== null || this.userService.currentUser() !== null;

        this.accessToken = null;
        this.refreshInFlight = null;
        this.sessionRestored.set(true);
        this.userService.clearCurrentUser();

        return hadSession;
    }

    logout(): Observable<void> {
        this.clearSession();

        return this.http.post<void>(`${this.apiUrl}/auth/logout`, {}, { withCredentials: true }).pipe(
            catchError(() => of(void 0)),
            shareReplay({ bufferSize: 1, refCount: false }),
        );
    }

    isLoggedIn(): boolean {
        return this.isTokenValid(this.accessToken);
    }

    restoreSession(): Observable<boolean> {
        return this.refreshSession().pipe(tap(() => this.sessionRestored.set(true)));
    }

    ensureSessionRestored(): Observable<boolean> {
        return this.sessionRestored() ? of(this.isLoggedIn()) : this.restoreSession();
    }

    refreshSession(): Observable<boolean> {
        this.refreshInFlight ??= this.http
            .post<AuthResponse>(`${this.apiUrl}/auth/refresh`, null, { withCredentials: true })
            .pipe(
                tap(response => this.accessToken = response.token),
                map(() => true),
                catchError(() => {
                    this.accessToken = null;
                    return of(false);
                }),
                tap(() => queueMicrotask(() => this.refreshInFlight = null)),
                shareReplay({ bufferSize: 1, refCount: false }),
            );

        return this.refreshInFlight;
    }

    forgotPassword(email: string) {
        return this.http.post(`${this.apiUrl}/auth/forgot-password`, { email });
    }

    resetPassword(email: string, code: string, newPassword: string) {
        return this.http.post(`${this.apiUrl}/auth/reset-password`, { email, code, newPassword });
    }

    getToken(): string | null {
        return this.accessToken;
    }

    private isTokenValid(token: string | null): boolean {
        if (!token) return false;

        try {
            const payload = JSON.parse(this.base64UrlDecode(token.split('.')[1]));
            return typeof payload.exp === 'number' && payload.exp * 1000 > Date.now();
        } catch {
            return false;
        }
    }

    private base64UrlDecode(input: string): string {
        const base64 = input.replace(/-/g, '+').replace(/_/g, '/');
        const padded = base64.padEnd(base64.length + (4 - (base64.length % 4)) % 4, '=');
        return atob(padded);
    }
}
