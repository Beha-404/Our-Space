import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { MemoryItem } from './memory-feed.service';

export interface Recap {
    year: number;
    photos: number;
    audioMessages: number;
    events: number;
    wishesFulfilled: number;
    capsulesSealed: number;
    memoriesPerMonth: number[];
    highlights: MemoryItem[];
    availableYears: number[];
}

@Injectable({ providedIn: 'root' })
export class RecapService {
    private http = inject(HttpClient);

    get(year?: number) {
        const params = year ? new HttpParams().set('year', year) : undefined;
        return this.http.get<Recap>(`${config.apiUrl}/recap`, { params });
    }
}
