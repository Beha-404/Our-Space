import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { config } from '../config';
import { MemoryMap } from '../interfaces/memory-map';

@Injectable({ providedIn: 'root' })
export class MemoryMapService {
    private http = inject(HttpClient);

    get() {
        return this.http.get<MemoryMap>(`${config.apiUrl}/memories/map`);
    }
}
