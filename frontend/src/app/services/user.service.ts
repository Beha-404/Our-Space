import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { finalize, Observable, of, shareReplay, tap } from 'rxjs';
import { config } from '../config';
import { PairingCodeResponse, PairRequest } from '../interfaces/pairing';
import { UpdateUserRequest } from '../interfaces/updateUserRequest';
import { User } from '../interfaces/user';

@Injectable({ providedIn: 'root' })
export class UserService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    readonly currentUser = signal<User | null>(null);

    private inFlight: Observable<User> | null = null;

    getCurrentUser() {
        return this.http.get<User>(`${this.apiUrl}/user/current`);
    }

    ensureCurrentUser(): Observable<User> {
        const cached = this.currentUser();
        return cached ? of(cached) : this.refreshCurrentUser();
    }

    refreshCurrentUser(): Observable<User> {
        this.inFlight ??= this.getCurrentUser().pipe(
            tap(user => this.currentUser.set(user)),
            finalize(() => { this.inFlight = null; }),
            shareReplay({ bufferSize: 1, refCount: false }),
        );

        return this.inFlight;
    }

    clearCurrentUser(): void {
        this.currentUser.set(null);
        this.inFlight = null;
    }

    updateUser(userData: UpdateUserRequest) {
        return this.http.put<User>(`${this.apiUrl}/user`, userData).pipe(
            tap(user => this.currentUser.set(user))
        );
    }

    deleteUser(userId: number) {
        return this.http.delete(`${this.apiUrl}/user/${userId}`);
    }

    generatePairingCode() {
        return this.http.post<PairingCodeResponse>(`${this.apiUrl}/user/pairing-code`, {});
    }

    pair(request: PairRequest) {
        return this.http.post<User>(`${this.apiUrl}/user/pair`, request).pipe(
            tap(user => this.currentUser.set(user))
        );
    }

    unpair() {
        return this.http.post<User>(`${this.apiUrl}/user/unpair`, {}).pipe(
            tap(user => this.currentUser.set(user))
        );
    }

    setRelationshipDate(relationshipStartDate: string) {
        return this.http.put<User>(`${this.apiUrl}/user/relationship-date`, { relationshipStartDate }).pipe(
            tap(user => this.currentUser.set(user))
        );
    }

    updateLanguage(language: string) {
        return this.http.put(`${this.apiUrl}/user/language`, { language });
    }

    requestEmailChange(newEmail: string) {
        return this.http.post(`${this.apiUrl}/user/email-change`, { newEmail });
    }

    confirmEmailChange(code: string) {
        return this.http.post<User>(`${this.apiUrl}/user/email-change/confirm`, { code }).pipe(
            tap(user => this.currentUser.set(user))
        );
    }

    cancelEmailChange() {
        return this.http.delete<User>(`${this.apiUrl}/user/email-change`).pipe(
            tap(user => this.currentUser.set(user))
        );
    }

    uploadProfilePicture(file: File): Observable<HttpEvent<User>> {
        const formData = new FormData();
        formData.append('file', file);

        return this.http.post<User>(`${this.apiUrl}/user/profile-picture`, formData, {
            reportProgress: true,
            observe: 'events',
        }).pipe(
            tap(event => {
                if (event.type === HttpEventType.Response && event.body) {
                    this.currentUser.set(event.body);
                }
            })
        );
    }
}
