import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, ElementRef, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { EventItem } from '../interfaces/event';
import { EventService } from '../services/event.service';
import { UserService } from '../services/user.service';
import { DatePicker } from '../shared/date-picker/date-picker';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';

@Component({
  imports: [DatePipe, TranslatePipe, Skeleton, DatePicker, RouterLink],
  selector: 'app-events-page',
  styleUrl: './events-page.css',
  templateUrl: './events-page.html',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'yearMenuOpen.set(false)',
  },
})
export class EventsPage {
  private eventService = inject(EventService);
  private userService = inject(UserService);
  private host = inject(ElementRef<HTMLElement>);
  private toast = inject(ToastService);

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  events = signal<EventItem[]>([]);
  loading = signal(true);

  private static readonly EVENTS_PER_PAGE = 5;

  sortOrder = signal<'newest' | 'oldest'>('newest');
  yearFilter = signal<number | 'all'>('all');
  eventsPage = signal(1);

  availableYears = computed(() => {
    const years = new Set(this.events().map(e => new Date(e.eventDate).getFullYear()));
    return [...years].sort((a, b) => b - a);
  });

  filteredSortedEvents = computed(() => {
    const yearFilter = this.yearFilter();
    const filtered = yearFilter === 'all'
      ? this.events()
      : this.events().filter(e => new Date(e.eventDate).getFullYear() === yearFilter);

    const sorted = [...filtered].sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime());
    return this.sortOrder() === 'newest' ? sorted.reverse() : sorted;
  });

  totalEventsPages = computed(() => Math.max(1, Math.ceil(this.filteredSortedEvents().length / EventsPage.EVENTS_PER_PAGE)));

  currentEventsPage = computed(() => Math.min(this.eventsPage(), this.totalEventsPages()));

  pagedEvents = computed(() => {
    const start = (this.currentEventsPage() - 1) * EventsPage.EVENTS_PER_PAGE;
    return this.filteredSortedEvents().slice(start, start + EventsPage.EVENTS_PER_PAGE);
  });

  eventPageNumbers = computed<(number | '…')[]>(() => {
    const total = this.totalEventsPages();
    const current = this.currentEventsPage();

    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    const keep = new Set<number>([1, total, current - 1, current, current + 1]);
    const sorted = [...keep].filter(p => p >= 1 && p <= total).sort((a, b) => a - b);

    const result: (number | '…')[] = [];
    let previous = 0;
    for (const page of sorted) {
      if (previous && page - previous > 1) result.push('…');
      result.push(page);
      previous = page;
    }
    return result;
  });

  goToEventsPage(page: number): void {
    this.eventsPage.set(Math.min(Math.max(page, 1), this.totalEventsPages()));
  }

  setSortOrder(order: 'newest' | 'oldest'): void {
    this.sortOrder.set(order);
    this.eventsPage.set(1);
  }

  setYearFilter(year: number | 'all'): void {
    this.yearFilter.set(year);
    this.eventsPage.set(1);
  }

  yearMenuOpen = signal(false);

  toggleYearMenu(): void {
    this.yearMenuOpen.update(value => !value);
  }

  onDocumentClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.yearMenuOpen.set(false);
    }
  }

  selectYear(year: number | 'all'): void {
    this.yearMenuOpen.set(false);
    this.setYearFilter(year);
  }

  showAddForm = signal(false);
  formData = signal({ title: '', description: '', eventDate: '' });
  adding = signal(false);
  addErrorKey = signal('');
  titleTouched = signal(false);
  addSubmitted = signal(false);

  titleError = computed(() =>
    this.titleTouched() && !this.formData().title.trim() ? 'events.errTitleRequired' : '');

  titleValid = computed(() => this.titleTouched() && !this.titleError() && !!this.formData().title.trim());
  eventDateValid = computed(() => !!this.formData().eventDate);

  showAddFillAllError = computed(() =>
    this.addSubmitted() && !!(this.titleError() || !this.formData().eventDate));

  openAddForm(): void {
    this.showAddForm.set(true);
  }

  closeAddForm(): void {
    this.showAddForm.set(false);
    this.formData.set({ title: '', description: '', eventDate: '' });
    this.titleTouched.set(false);
    this.addSubmitted.set(false);
    this.addErrorKey.set('');
  }

  editingEventId = signal<number | null>(null);
  editFormData = signal({ title: '', description: '', eventDate: '' });
  saving = signal(false);
  editErrorKey = signal('');
  editTitleTouched = signal(false);
  editSubmitted = signal(false);

  editTitleError = computed(() =>
    this.editTitleTouched() && !this.editFormData().title.trim() ? 'events.errTitleRequired' : '');

  editTitleValid = computed(() => this.editTitleTouched() && !this.editTitleError() && !!this.editFormData().title.trim());
  editEventDateValid = computed(() => !!this.editFormData().eventDate);

  showEditFillAllError = computed(() =>
    this.editSubmitted() && !!(this.editTitleError() || !this.editFormData().eventDate));

  constructor() {
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) this.loadEvents();
      else this.loading.set(false);
    });
  }

  updateField(field: 'title' | 'description' | 'eventDate', value: string): void {
    this.formData.update(data => ({ ...data, [field]: value }));
    this.addErrorKey.set('');
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
    this.titleTouched.set(true);
    this.addSubmitted.set(true);
    const data = this.formData();
    if (this.titleError() || !data.eventDate) {
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
        this.closeAddForm();
        this.loadEvents();
        this.toast.success('toast.eventAdded');
      },
      error: (err: HttpErrorResponse) => {
        this.adding.set(false);
        const key = err.error?.title ?? 'events.addError';
        this.addErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  eventPendingDelete = signal<EventItem | null>(null);

  confirmDelete(event: EventItem): void {
    this.eventPendingDelete.set(event);
  }

  cancelDelete(): void {
    this.eventPendingDelete.set(null);
  }

  deleteEvent(): void {
    const event = this.eventPendingDelete();
    if (!event) return;

    this.eventService.delete(event.id).subscribe({
      next: () => {
        this.eventPendingDelete.set(null);
        this.loadEvents();
        this.toast.success('toast.eventDeleted');
      },
      error: () => this.toast.error('toast.actionFailed')
    });
  }

  startEdit(event: EventItem): void {
    this.editErrorKey.set('');
    this.editTitleTouched.set(false);
    this.editSubmitted.set(false);
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
    this.editErrorKey.set('');
  }

  saveEdit(id: number): void {
    this.editTitleTouched.set(true);
    this.editSubmitted.set(true);
    const data = this.editFormData();
    if (this.editTitleError() || !data.eventDate) {
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
        this.toast.success('toast.eventUpdated');
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const key = err.error?.title ?? 'events.addError';
        this.editErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }
}
