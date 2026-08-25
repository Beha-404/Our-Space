import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, of, tap } from 'rxjs';
import { config } from '../config';
import { AuthResponse } from '../interfaces/authResponse';
import { LoginRequest } from '../interfaces/loginRequest';
import { RegisterRequest } from '../interfaces/registerRequest';
import { UserService } from './user.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private http = inject(HttpClient);
    private userService = inject(UserService);
    private apiUrl = config.apiUrl;

    login(loginData: LoginRequest) {
        return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, loginData).pipe(
            tap(response => this.storeTokens(response))
        );
    }

    register(registerData: RegisterRequest) {
        return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, registerData);
    }

    logout() {
        const refreshToken = localStorage.getItem('refreshToken');
        this.clearTokens();

        if (refreshToken) {
            this.http.post(`${this.apiUrl}/auth/logout`, { refreshToken }).subscribe();
        }
    }

    /**
     * True only if a NON-EXPIRED access token is present. A read-only check — does NOT
     * clear an expired access token, since a still-valid refresh token might exist
     * alongside it (the normal case every ~15 minutes for an active session) and callers
     * need that refresh token intact to silently re-authenticate via refreshSession().
     */
    isLoggedIn(): boolean {
        return this.isTokenValid(localStorage.getItem('accessToken'));
    }

    hasRefreshToken(): boolean {
        return !!localStorage.getItem('refreshToken');
    }

    /** Attempts to exchange the stored refresh token for a fresh session. */
    refreshSession() {
        const refreshToken = localStorage.getItem('refreshToken');
        if (!refreshToken) {
            return of(false);
        }

        return this.http.post<AuthResponse>(`${this.apiUrl}/auth/refresh`, { refreshToken }).pipe(
            tap(response => this.storeTokens(response)),
            map(() => true),
            catchError(() => {
                this.clearTokens();
                return of(false);
            })
        );
    }

    getToken() {
        return localStorage.getItem('accessToken');
    }

    getRefreshToken() {
        return localStorage.getItem('refreshToken');
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

    private clearTokens(): void {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        this.userService.clearCurrentUser();
    }

    private storeTokens(response: AuthResponse): void {
        localStorage.setItem('accessToken', response.token);
        localStorage.setItem('refreshToken', response.refreshToken);
    }
}
