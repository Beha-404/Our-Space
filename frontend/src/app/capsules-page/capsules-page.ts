import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Capsule } from '../interfaces/capsule';
import { CapsuleService } from '../services/capsule.service';
import { UserService } from '../services/user.service';
import { DatePicker } from '../shared/date-picker/date-picker';
import { Parchment } from '../shared/parchment/parchment';
import { scrollAndHighlight } from '../shared/scroll-and-highlight';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';
import { CapsuleBottle } from './capsule-bottle/capsule-bottle';
import { OpenPreset, daysUntil, presetDate } from './capsule-dates';
import { CapsuleScroll } from './capsule-scroll/capsule-scroll';

type OpenMode = 'date' | 'anytime';

const DEFAULT_PRESET: OpenPreset = 'year';

@Component({
  imports: [LocalDatePipe, TranslatePipe, Skeleton, DatePicker, RouterLink, Parchment, CapsuleBottle, CapsuleScroll],
  selector: 'app-capsules-page',
  styleUrl: './capsules-page.css',
  templateUrl: './capsules-page.html',
})
export class CapsulesPage {
  private capsuleService = inject(CapsuleService);
  private userService = inject(UserService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  readonly daysUntil = daysUntil;

  readonly presets: { key: OpenPreset; labelKey: string }[] = [
    { key: 'month', labelKey: 'capsules.presetMonth' },
    { key: 'sixMonths', labelKey: 'capsules.presetSixMonths' },
    { key: 'year', labelKey: 'capsules.presetYear' },
    { key: 'fiveYears', labelKey: 'capsules.presetFiveYears' },
    { key: 'custom', labelKey: 'capsules.presetCustom' },
    { key: 'anytime', labelKey: 'capsules.modeAnytime' },
  ];

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  capsules = signal<Capsule[]>([]);
  loading = signal(true);
  arrivedId = signal<number | null>(null);

  sealed = computed(() => this.capsules().filter(c => !c.isUnlocked));
  opened = computed(() => this.capsules().filter(c => c.isUnlocked));

  showForm = signal(false);
  preset = signal<OpenPreset>(DEFAULT_PRESET);
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

  load(onLoaded?: () => void): void {
    this.loading.set(true);
    this.capsuleService.getAll().subscribe({
      next: capsules => {
        this.capsules.set(capsules);
        this.loading.set(false);
        this.highlightFromQueryParams();
        onLoaded?.();
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
    this.setPreset(DEFAULT_PRESET);
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.formData.set({ title: '', message: '', openAt: '' });
    this.preset.set(DEFAULT_PRESET);
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

  setPreset(preset: OpenPreset): void {
    this.preset.set(preset);

    if (preset === 'anytime') {
      this.openMode.set('anytime');
      this.updateField('openAt', '');
      return;
    }

    this.openMode.set('date');
    const date = presetDate(preset);
    if (date) this.updateField('openAt', date);
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
      next: created => {
        this.saving.set(false);
        this.closeForm();
        this.arrivedId.set(created.id);
        this.load(() => document.getElementById('capsule-sea')?.scrollIntoView({ behavior: 'smooth', block: 'center' }));
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
