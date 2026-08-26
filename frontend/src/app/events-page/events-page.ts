import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Navbar } from '../navbar/navbar';
import { EventItem } from '../interfaces/event';
import { EventService } from '../services/event.service';
import { UserService } from '../services/user.service';

@Component({
  imports: [Navbar, DatePipe, TranslatePipe],
  selector: 'app-events-page',
  styleUrl: './events-page.css',
  templateUrl: './events-page.html',
})
export class EventsPage {
  private eventService = inject(EventService);
  private userService = inject(UserService);

  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  events = signal<EventItem[]>([]);
  loading = signal(true);

  formData = signal({ title: '', description: '', eventDate: '' });
  adding = signal(false);
  addErrorKey = signal('');

  editingEventId = signal<number | null>(null);
  editFormData = signal({ title: '', description: '', eventDate: '' });
  saving = signal(false);
  editErrorKey = signal('');

  constructor() {
    this.userService.refreshCurrentUser().subscribe();
    this.loadEvents();
  }

  updateField(field: 'title' | 'description' | 'eventDate', value: string): void {
    this.formData.update(data => ({ ...data, [field]: value }));
  }

  loadEvents(): void {
    this.loading.set(true);
    this.eventService.getUpcoming(true).subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  daysUntil(eventDate: string): number {
    const diffMs = new Date(eventDate).setHours(0, 0, 0, 0) - new Date().setHours(0, 0, 0, 0);
    return Math.round(diffMs / (1000 * 60 * 60 * 24));
  }

  addEvent(): void {
    const data = this.formData();
    if (!data.title.trim() || !data.eventDate) {
      this.addErrorKey.set('auth.errFillAll');
      return;
    }

    this.adding.set(true);
    this.addErrorKey.set('');

    this.eventService.create({
      title: data.title.trim(),
      description: data.description.trim() || null,
      eventDate: data.eventDate,
    }).subscribe({
      next: () => {
        this.adding.set(false);
        this.formData.set({ title: '', description: '', eventDate: '' });
        this.loadEvents();
      },
      error: (err: HttpErrorResponse) => {
        this.adding.set(false);
        this.addErrorKey.set(err.error?.title ?? 'events.addError');
      }
    });
  }

  deleteEvent(id: number): void {
    this.eventService.delete(id).subscribe(() => this.loadEvents());
  }

  startEdit(event: EventItem): void {
    this.editErrorKey.set('');
    this.editFormData.set({
      title: event.title,
      description: event.description ?? '',
      eventDate: event.eventDate.slice(0, 10),
    });
    this.editingEventId.set(event.id);
  }

  cancelEdit(): void {
    this.editingEventId.set(null);
  }

  updateEditField(field: 'title' | 'description' | 'eventDate', value: string): void {
    this.editFormData.update(data => ({ ...data, [field]: value }));
  }

  saveEdit(id: number): void {
    const data = this.editFormData();
    if (!data.title.trim() || !data.eventDate) {
      this.editErrorKey.set('auth.errFillAll');
      return;
    }

    this.saving.set(true);
    this.editErrorKey.set('');

    this.eventService.update(id, {
      title: data.title.trim(),
      description: data.description.trim() || null,
      eventDate: data.eventDate,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editingEventId.set(null);
        this.loadEvents();
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        this.editErrorKey.set(err.error?.title ?? 'events.addError');
      }
    });
  }
}
