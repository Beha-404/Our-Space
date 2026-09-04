import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { AudioMessage } from '../interfaces/audio';
import { EventItem } from '../interfaces/event';
import { Photo } from '../interfaces/photo';
import { User } from '../interfaces/user';
import { Wish } from '../interfaces/wish';

export interface HomeSummary {
    user: User;
    upcomingEvents: EventItem[];
    recentWishes: Wish[];
    photos: Photo[];
    audio: AudioMessage[];
    totalMemories: number;
}

@Injectable({ providedIn: 'root' })
export class HomeService {
    private http = inject(HttpClient);

    get() {
        return this.http.get<HomeSummary>(`${config.apiUrl}/home`);
    }
}
