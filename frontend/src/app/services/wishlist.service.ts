import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { Wish } from '../interfaces/wish';

@Injectable({ providedIn: 'root' })
export class WishlistService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    getAll() {
        return this.http.get<Wish[]>(`${this.apiUrl}/wishlist`);
    }

    create(title: string) {
        return this.http.post<Wish>(`${this.apiUrl}/wishlist`, { title });
    }

    toggleFulfilled(id: number) {
        return this.http.put<Wish>(`${this.apiUrl}/wishlist/${id}/toggle`, {});
    }

    delete(id: number) {
        return this.http.delete(`${this.apiUrl}/wishlist/${id}`);
    }
}
