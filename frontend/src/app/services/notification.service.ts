import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { config } from '../config';
import { AppNotification } from '../interfaces/notification';

const POLL_INTERVAL_MS = 25000;
const PAGE_SIZE = 20;

@Injectable({ providedIn: 'root' })
export class NotificationService {
    private http = inject(HttpClient);
    private apiUrl = config.apiUrl;

    readonly notifications = signal<AppNotification[]>([]);
    readonly unreadCount = signal(0);
    readonly hasMore = signal(false);
    readonly loadingMore = signal(false);

    private pollHandle: ReturnType<typeof setInterval> | null = null;

    startPolling(): void {
        if (this.pollHandle) return;

        this.refreshUnreadCount();
        this.pollHandle = setInterval(() => this.refreshUnreadCount(), POLL_INTERVAL_MS);
    }

    stopPolling(): void {
        if (this.pollHandle) {
            clearInterval(this.pollHandle);
            this.pollHandle = null;
        }
    }

    refreshUnreadCount(): void {
        this.http.get<number>(`${this.apiUrl}/notifications/unread-count`).subscribe({
            next: count => this.unreadCount.set(count),
            error: () => {},
        });
    }

    loadRecent(): void {
        this.http.get<AppNotification[]>(`${this.apiUrl}/notifications`, { params: { skip: 0, take: PAGE_SIZE + 1 } }).subscribe({
            next: list => {
                this.notifications.set(list.slice(0, PAGE_SIZE));
                this.hasMore.set(list.length > PAGE_SIZE);
            },
            error: () => {},
        });
    }

    loadMore(): void {
        if (!this.hasMore() || this.loadingMore()) return;

        this.loadingMore.set(true);
        const skip = this.notifications().length;

        this.http.get<AppNotification[]>(`${this.apiUrl}/notifications`, { params: { skip, take: PAGE_SIZE + 1 } }).subscribe({
            next: list => {
                const known = new Set(this.notifications().map(n => n.id));
                const fresh = list.slice(0, PAGE_SIZE).filter(n => !known.has(n.id));
                this.notifications.update(current => [...current, ...fresh]);
                this.hasMore.set(list.length > PAGE_SIZE);
                this.loadingMore.set(false);
            },
            error: () => this.loadingMore.set(false),
        });
    }

    markRead(id: number): void {
        this.notifications.update(list => list.map(n => n.id === id ? { ...n, isRead: true } : n));
        this.unreadCount.update(count => Math.max(0, count - 1));

        this.http.post(`${this.apiUrl}/notifications/${id}/read`, {}).subscribe({ error: () => {} });
    }

    markAllRead(): void {
        this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        this.unreadCount.set(0);

        this.http.post(`${this.apiUrl}/notifications/read-all`, {}).subscribe({ error: () => {} });
    }
}
