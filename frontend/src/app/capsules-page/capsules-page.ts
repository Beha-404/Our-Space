import { HttpErrorResponse } from '@angular/common/http';
import { afterNextRender, Component, computed, inject, Injector, signal } from '@angular/core';
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
import { OpenPreset, capsuleOpenedOn, daysUntil, presetDate } from './capsule-dates';
import { CapsuleScroll } from './capsule-scroll/capsule-scroll';
import { Pager } from '../shared/pager/pager';

type OpenMode = 'date' | 'anytime';

const DEFAULT_PRESET: OpenPreset = 'year';
const SEALED_PAGE_SIZE = 4;
const OPENED_PAGE_SIZE = 5;

@Component({
  imports: [LocalDatePipe, TranslatePipe, Skeleton, DatePicker, RouterLink, Parchment, CapsuleBottle, CapsuleScroll, Pager],
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

  sealed = computed(() =>
    this.capsules().filter(c => !c.isUnlocked).sort((a, b) => Number(b.canOpenNow) - Number(a.canOpenNow)));
  opened = computed(() =>
    this.capsules().filter(c => c.isUnlocked).sort((a, b) => capsuleOpenedOn(b).localeCompare(capsuleOpenedOn(a))));

  private sealedPageRequested = signal(1);
  private openedPageRequested = signal(1);

  sealedPages = computed(() => Math.max(1, Math.ceil(this.sealed().length / SEALED_PAGE_SIZE)));
  openedPages = computed(() => Math.max(1, Math.ceil(this.opened().length / OPENED_PAGE_SIZE)));
  sealedPage = computed(() => Math.min(this.sealedPageRequested(), this.sealedPages()));
  openedPage = computed(() => Math.min(this.openedPageRequested(), this.openedPages()));

  visibleSealed = computed(() => this.sealed().slice((this.sealedPage() - 1) * SEALED_PAGE_SIZE, this.sealedPage() * SEALED_PAGE_SIZE));
  visibleOpened = computed(() => this.opened().slice((this.openedPage() - 1) * OPENED_PAGE_SIZE, this.openedPage() * OPENED_PAGE_SIZE));

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

  private highlightHandled = false;

  private highlightFromQueryParams(): void {
    const idParam = this.route.snapshot.queryParamMap.get('highlight');
    if (!idParam || this.highlightHandled) return;

    this.highlightHandled = true;
    this.reveal(Number(idParam));
    scrollAndHighlight(`capsule-${Number(idParam)}`);
  }

  goToSealedPage(page: number): void {
    this.sealedPageRequested.set(page);
    this.scrollToSection('capsule-sea');
  }

  goToOpenedPage(page: number): void {
    this.openedPageRequested.set(page);
    this.scrollToSection('capsule-letters');
  }

  private scrollToSection(id: string): void {
    setTimeout(() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 0);
  }

  private scrollToCapsule(id: number): void {
    document.getElementById(`capsule-${id}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  private reveal(id: number): void {
    const sealedIndex = this.sealed().findIndex(c => c.id === id);
    if (sealedIndex >= 0) this.sealedPageRequested.set(Math.floor(sealedIndex / SEALED_PAGE_SIZE) + 1);

    const openedIndex = this.opened().findIndex(c => c.id === id);
    if (openedIndex >= 0) this.openedPageRequested.set(Math.floor(openedIndex / OPENED_PAGE_SIZE) + 1);
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
        this.load(() => {
          this.reveal(created.id);
          setTimeout(() => document.getElementById('capsule-sea')?.scrollIntoView({ behavior: 'smooth', block: 'center' }), 0);
        });
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

  private injector = inject(Injector);

  openingId = signal<number | null>(null);

  openCapsule(capsule: Capsule): void {
    this.openingId.set(capsule.id);

    this.capsuleService.open(capsule.id).subscribe({
      next: opened => {
        this.openingId.set(null);
        this.capsules.update(list => list.map(c => c.id === opened.id ? opened : c));
        this.reveal(opened.id);
        afterNextRender(
          () => this.scrollToCapsule(opened.id),
          { injector: this.injector },
        );
        this.toast.success('toast.capsuleOpened');
      },
      error: (err: HttpErrorResponse) => {
        this.openingId.set(null);
        this.toast.error(err.error?.title ?? 'toast.actionFailed');
      }
    });
  }

  canDelete(capsule: Capsule): boolean {
    return capsule.createdByUsername === this.userService.currentUser()?.username;
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
