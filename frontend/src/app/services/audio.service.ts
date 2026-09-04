import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { AudioMessage } from '../interfaces/audio';
import { PagedResult } from '../interfaces/paged-result';

@Injectable({ providedIn: 'root' })
export class AudioService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    getAll(page = 1, pageSize = 20) {
        return this.http.get<PagedResult<AudioMessage>>(`${this.apiUrl}/audio?page=${page}&pageSize=${pageSize}`);
    }

    getCount() {
        return this.http.get<number>(`${this.apiUrl}/audio/count`);
    }

    upload(file: File, recordedAt: string, caption: string | null) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('recordedAt', recordedAt);
        if (caption) formData.append('caption', caption);

        return this.http.post<AudioMessage>(`${this.apiUrl}/audio`, formData, {
            reportProgress: true,
            observe: 'events',
        });
    }

    delete(id: number) {
        return this.http.delete(`${this.apiUrl}/audio/${id}`);
    }

    fullUrl(path: string): string {
        return `${config.mediaUrl}${path}`;
    }
}
