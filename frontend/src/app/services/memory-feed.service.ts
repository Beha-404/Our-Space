import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';

export interface MemoryItem {
    id: number;
    type: 'photo' | 'audio';
    url: string;
    thumbnailUrl: string | null;
    mediumUrl: string | null;
    caption: string | null;
    status: 'ready' | 'processing' | 'failed';
    date: string;
    uploadedByUsername: string;
    createdAt: string;
}

export interface MemoryFeed {
    items: MemoryItem[];
    hasMore: boolean;
    years: number[] | null;
}

export interface MemoryFeedQuery {
    page: number;
    pageSize: number;
    sort: 'newest' | 'oldest';
    year: number | 'all';
    month: number | 'all';
    type: 'all' | 'photo' | 'audio';
}

@Injectable({ providedIn: 'root' })
export class MemoryFeedService {
    private http = inject(HttpClient);

    getPage(query: MemoryFeedQuery) {
        let params = new HttpParams()
            .set('page', query.page)
            .set('pageSize', query.pageSize)
            .set('sort', query.sort);

        if (query.year !== 'all') params = params.set('year', query.year);
        if (query.month !== 'all') params = params.set('month', query.month);
        if (query.type !== 'all') params = params.set('type', query.type);

        return this.http.get<MemoryFeed>(`${config.apiUrl}/memories`, { params });
    }
}
