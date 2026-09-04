import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';

@Injectable({ providedIn: 'root' })
export class ExportService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    exportMemories() {
        return this.http.get(`${this.apiUrl}/export/memories`, {
            responseType: 'blob',
            observe: 'response',
        });
    }
}
