import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { Capsule, CreateCapsuleRequest } from '../interfaces/capsule';

@Injectable({ providedIn: 'root' })
export class CapsuleService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    getAll() {
        return this.http.get<Capsule[]>(`${this.apiUrl}/capsules`);
    }

    create(request: CreateCapsuleRequest) {
        return this.http.post<Capsule>(`${this.apiUrl}/capsules`, request);
    }

    open(id: number) {
        return this.http.post<Capsule>(`${this.apiUrl}/capsules/${id}/open`, {});
    }

    delete(id: number) {
        return this.http.delete(`${this.apiUrl}/capsules/${id}`);
    }
}
