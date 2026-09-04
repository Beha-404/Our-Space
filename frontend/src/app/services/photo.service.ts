import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { Photo } from '../interfaces/photo';
import { PagedResult } from '../interfaces/paged-result';

@Injectable({ providedIn: 'root' })
export class PhotoService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    getAll(page = 1, pageSize = 20) {
        return this.http.get<PagedResult<Photo>>(`${this.apiUrl}/photos?page=${page}&pageSize=${pageSize}`);
    }

    getCount() {
        return this.http.get<number>(`${this.apiUrl}/photos/count`);
    }

    upload(file: File, takenAt: string, caption: string | null) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('takenAt', takenAt);
        if (caption) formData.append('caption', caption);

        return this.http.post<Photo>(`${this.apiUrl}/photos`, formData, {
            reportProgress: true,
            observe: 'events',
        });
    }

    delete(id: number) {
        return this.http.delete(`${this.apiUrl}/photos/${id}`);
    }

    fullUrl(path: string): string {
        return `${config.mediaUrl}${path}`;
    }
}
