import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Capsule } from '../interfaces/capsule';
import { CapsuleService } from '../services/capsule.service';
import { UserService } from '../services/user.service';
import { DatePicker } from '../shared/date-picker/date-picker';
import { scrollAndHighlight } from '../shared/scroll-and-highlight';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';

type OpenMode = 'date' | 'anytime';

@Component({
  imports: [LocalDatePipe, TranslatePipe, Skeleton, DatePicker, RouterLink],
  selector: 'app-capsules-page',
  styleUrl: './capsules-page.css',
  templateUrl: './capsules-page.html',
})
export class CapsulesPage {
  private capsuleService = inject(CapsuleService);
  private userService = inject(UserService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  capsules = signal<Capsule[]>([]);
  loading = signal(true);

  sealed = computed(() => this.capsules().filter(c => !c.isUnlocked));
  opened = computed(() => this.capsules().filter(c => c.isUnlocked));

  showForm = signal(false);
  openMode = signal<OpenMode>('date');
  formData = signal({ title: '', message: '', openAt: '' });
  saving = signal(false);
  errorKey = signal('');
  titleTouched = signal(false);
  messageTouched = signal(false);
  submitted = signal(false);

  titleError = computed(() =>
    this.titleTouched() && !this.formData().title.trim() ? 'capsules.errTitleRequired' : '');

  messageError = computed(() =>
    this.messageTouched() && !this.formData().message.trim() ? 'capsules.errMessageRequired' : '');

  dateMissing = computed(() => this.openMode() === 'date' && !this.formData().openAt);

  showFillAllError = computed(() =>
    this.submitted() && !!(this.titleError() || this.messageError() || this.dateMissing()));

  constructor() {
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) this.load();
      else this.loading.set(false);
    });
  }

  load(): void {
    this.loading.set(true);
    this.capsuleService.getAll().subscribe({
      next: capsules => {
        this.capsules.set(capsules);
        this.loading.set(false);
        this.highlightFromQueryParams();
      },
      error: () => this.loading.set(false)
    });
  }

  private highlightFromQueryParams(): void {
    const idParam = this.route.snapshot.queryParamMap.get('highlight');
    if (!idParam) return;

    scrollAndHighlight(`capsule-${Number(idParam)}`);
  }

  openForm(): void {
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.formData.set({ title: '', message: '', openAt: '' });
    this.openMode.set('date');
    this.titleTouched.set(false);
    this.messageTouched.set(false);
    this.submitted.set(false);
    this.errorKey.set('');
  }

  updateField(field: 'title' | 'message' | 'openAt', value: string): void {
    this.formData.update(data => ({ ...data, [field]: value }));
    this.errorKey.set('');
  }

  setOpenMode(mode: OpenMode): void {
    this.openMode.set(mode);
    if (mode === 'anytime') this.updateField('openAt', '');
  }

  daysUntil(date: string): number {
    const diffMs = new Date(date).setHours(0, 0, 0, 0) - new Date().setHours(0, 0, 0, 0);
    return Math.round(diffMs / (1000 * 60 * 60 * 24));
  }

  seal(): void {
    this.titleTouched.set(true);
    this.messageTouched.set(true);
    this.submitted.set(true);

    const data = this.formData();
    if (this.titleError() || this.messageError() || this.dateMissing()) return;

    this.saving.set(true);
    this.errorKey.set('');

    this.capsuleService.create({
      title: data.title.trim(),
      message: data.message.trim(),
      openAt: this.openMode() === 'date' ? data.openAt : null,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeForm();
        this.load();
        this.toast.success('toast.capsuleSealed');
      },
      error: (err: HttpErrorResponse) => {
        this.saving.set(false);
        const key = err.error?.title ?? 'capsules.sealError';
        this.errorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  openingId = signal<number | null>(null);

  openCapsule(capsule: Capsule): void {
    this.openingId.set(capsule.id);

    this.capsuleService.open(capsule.id).subscribe({
      next: opened => {
        this.openingId.set(null);
        this.capsules.update(list => list.map(c => c.id === opened.id ? opened : c));
        this.toast.success('toast.capsuleOpened');
      },
      error: (err: HttpErrorResponse) => {
        this.openingId.set(null);
        this.toast.error(err.error?.title ?? 'toast.actionFailed');
      }
    });
  }

  canDelete(capsule: Capsule): boolean {
    return capsule.isUnlocked || capsule.createdByUsername === this.userService.currentUser()?.username;
  }

  capsulePendingDelete = signal<Capsule | null>(null);

  confirmDelete(capsule: Capsule): void {
    this.capsulePendingDelete.set(capsule);
  }

  cancelDelete(): void {
    this.capsulePendingDelete.set(null);
  }

  deleteCapsule(): void {
    const capsule = this.capsulePendingDelete();
    if (!capsule) return;

    this.capsuleService.delete(capsule.id).subscribe({
      next: () => {
        this.capsulePendingDelete.set(null);
        this.load();
        this.toast.success('toast.capsuleDeleted');
      },
      error: (err: HttpErrorResponse) => this.toast.error(err.error?.title ?? 'toast.actionFailed')
    });
  }
}
