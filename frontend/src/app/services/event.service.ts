import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { CreateEventRequest, EventItem } from '../interfaces/event';

@Injectable({ providedIn: 'root' })
export class EventService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    getUpcoming() {
        return this.http.get<EventItem[]>(`${this.apiUrl}/events`);
    }

    create(request: CreateEventRequest) {
        return this.http.post<EventItem>(`${this.apiUrl}/events`, request);
    }

    update(id: number, request: CreateEventRequest) {
        return this.http.put<EventItem>(`${this.apiUrl}/events/${id}`, request);
    }

    delete(id: number) {
        return this.http.delete(`${this.apiUrl}/events/${id}`);
    }
}
