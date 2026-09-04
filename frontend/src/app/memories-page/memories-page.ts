import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { TranslatePipe } from '../i18n/translate.pipe';
import { TranslationService } from '../i18n/translation.service';
import { AudioMessage } from '../interfaces/audio';
import { Photo } from '../interfaces/photo';
import { AudioService } from '../services/audio.service';
import { ExportService } from '../services/export.service';
import { PhotoService } from '../services/photo.service';
import { UserService } from '../services/user.service';
import { buildFeedPosts, FeedPost } from '../shared/build-feed-posts';
import { AudioPlayer } from '../shared/audio-player/audio-player';
import { DatePicker } from '../shared/date-picker/date-picker';
import { Lightbox } from '../shared/lightbox/lightbox';
import { SelectDropdown, SelectOption } from '../shared/select-dropdown/select-dropdown';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';

type UploadType = 'photo' | 'audio';
type SortOrder = 'newest' | 'oldest';

@Component({
  imports: [DatePipe, TranslatePipe, SelectDropdown, Lightbox, Skeleton, AudioPlayer, DatePicker],
  selector: 'app-memories-page',
  styleUrl: './memories-page.css',
  templateUrl: './memories-page.html',
})
export class MemoriesPage {
  private photoService = inject(PhotoService);
  private audioService = inject(AudioService);
  private exportService = inject(ExportService);
  private userService = inject(UserService);
  private i18n = inject(TranslationService);
  private toast = inject(ToastService);

  fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  private static readonly PAGE_SIZE = 500;

  photos = signal<Photo[]>([]);
  audioItems = signal<AudioMessage[]>([]);
  loading = signal(true);
  loadingMore = signal(false);

  private photoPage = signal(1);
  private audioPage = signal(1);
  private hasMorePhotos = signal(false);
  private hasMoreAudio = signal(false);
  hasMore = computed(() => this.hasMorePhotos() || this.hasMoreAudio());

  showUploadForm = signal(false);

  openUploadForm(): void {
    this.showUploadForm.set(true);
  }

  closeUploadForm(): void {
    this.showUploadForm.set(false);
    this.clearSelectedFile();
    this.uploadDate.set('');
    this.uploadCaption.set('');
    this.uploadCaptionTouched.set(false);
    this.uploadErrorKey.set('');
  }

  uploadType = signal<UploadType>('photo');
  selectedFile = signal<File | null>(null);
  uploadDate = signal('');
  uploadCaption = signal('');
  uploadCaptionTouched = signal(false);
  uploading = signal(false);
  uploadErrorKey = signal('');

  uploadCaptionError = computed(() =>
    this.uploadCaptionTouched() && !this.uploadCaption().trim() ? 'memories.errTitleRequired' : '');

  sortOrder = signal<SortOrder>('newest');

  private static readonly MONTH_KEYS = [
    'memories.month1', 'memories.month2', 'memories.month3', 'memories.month4',
    'memories.month5', 'memories.month6', 'memories.month7', 'memories.month8',
    'memories.month9', 'memories.month10', 'memories.month11', 'memories.month12',
  ];

  allFeedPosts = computed<FeedPost[]>(() =>
    buildFeedPosts(
      this.photos(),
      this.audioItems(),
      path => this.photoService.fullUrl(path),
      path => this.audioService.fullUrl(path),
      this.sortOrder(),
    )
  );

  feedYearFilter = signal<number | 'all'>('all');
  feedMonthFilter = signal<number | 'all'>('all');

  yearOptions = computed<SelectOption[]>(() => {
    const years = new Set(this.allFeedPosts().map(p => new Date(p.date).getFullYear()));
    const sorted = [...years].sort((a, b) => b - a);
    return [{ value: 'all', label: this.i18n.t('events.allYears') }, ...sorted.map(y => ({ value: y, label: String(y) }))];
  });

  monthOptions = computed<SelectOption[]>(() => [
    { value: 'all', label: this.i18n.t('memories.allMonths') },
    ...MemoriesPage.MONTH_KEYS.map((key, i) => ({ value: i + 1, label: this.i18n.t(key) })),
  ]);

  setFeedYearFilter(year: number | 'all'): void {
    this.feedYearFilter.set(year);
    this.feedPage.set(1);
  }

  setFeedMonthFilter(month: number | 'all'): void {
    this.feedMonthFilter.set(month);
    this.feedPage.set(1);
  }

  feed = computed<FeedPost[]>(() => {
    const year = this.feedYearFilter();
    const month = this.feedMonthFilter();

    return this.allFeedPosts().filter(p => {
      const d = new Date(p.date);
      if (year !== 'all' && d.getFullYear() !== year) return false;
      if (month !== 'all' && d.getMonth() + 1 !== month) return false;
      return true;
    });
  });

  private static readonly FEED_PER_PAGE = 5;

  feedPage = signal(1);

  totalFeedPages = computed(() => Math.max(1, Math.ceil(this.feed().length / MemoriesPage.FEED_PER_PAGE)));

  currentFeedPage = computed(() => Math.min(this.feedPage(), this.totalFeedPages()));

  pagedFeed = computed(() => {
    const start = (this.currentFeedPage() - 1) * MemoriesPage.FEED_PER_PAGE;
    return this.feed().slice(start, start + MemoriesPage.FEED_PER_PAGE);
  });

  goToFeedPage(page: number): void {
    this.feedPage.set(Math.min(Math.max(page, 1), this.totalFeedPages()));
  }

  pageNumbers = computed<(number | '…')[]>(() => {
    const total = this.totalFeedPages();
    const current = this.currentFeedPage();

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

  setSortOrder(order: SortOrder): void {
    this.sortOrder.set(order);
    this.feedPage.set(1);
  }

  constructor() {
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) this.loadAll();
      else this.loading.set(false);
    });
  }

  loadAll(): void {
    this.loading.set(true);
    this.photoPage.set(1);
    this.audioPage.set(1);

    forkJoin({
      photos: this.photoService.getAll(1, MemoriesPage.PAGE_SIZE),
      audio: this.audioService.getAll(1, MemoriesPage.PAGE_SIZE),
    }).subscribe({
      next: ({ photos, audio }) => {
        this.photos.set(photos.items);
        this.audioItems.set(audio.items);
        this.hasMorePhotos.set(photos.hasMore);
        this.hasMoreAudio.set(audio.hasMore);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  loadMore(): void {
    this.loadingMore.set(true);

    const nextPhotoPage = this.hasMorePhotos() ? this.photoPage() + 1 : null;
    const nextAudioPage = this.hasMoreAudio() ? this.audioPage() + 1 : null;

    forkJoin({
      photos: nextPhotoPage
        ? this.photoService.getAll(nextPhotoPage, MemoriesPage.PAGE_SIZE)
        : of({ items: [] as Photo[], hasMore: false }),
      audio: nextAudioPage
        ? this.audioService.getAll(nextAudioPage, MemoriesPage.PAGE_SIZE)
        : of({ items: [] as AudioMessage[], hasMore: false }),
    }).subscribe({
      next: ({ photos, audio }) => {
        if (nextPhotoPage) {
          this.photos.update(items => [...items, ...photos.items]);
          this.photoPage.set(nextPhotoPage);
          this.hasMorePhotos.set(photos.hasMore);
        }
        if (nextAudioPage) {
          this.audioItems.update(items => [...items, ...audio.items]);
          this.audioPage.set(nextAudioPage);
          this.hasMoreAudio.set(audio.hasMore);
        }
        this.loadingMore.set(false);
      },
      error: () => this.loadingMore.set(false)
    });
  }

  setUploadType(type: UploadType): void {
    this.uploadType.set(type);
    this.clearSelectedFile();
    this.uploadErrorKey.set('');
  }

  previewUrl = signal<string | null>(null);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.selectedFile.set(file);

    this.revokePreviewUrl();
    this.previewUrl.set(file && this.uploadType() === 'photo' ? URL.createObjectURL(file) : null);
  }

  private revokePreviewUrl(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
  }

  upload(): void {
    this.uploadCaptionTouched.set(true);

    const file = this.selectedFile();
    const date = this.uploadDate();
    const caption = this.uploadCaption().trim();

    if (!file || !date || this.uploadCaptionError()) {
      this.uploadErrorKey.set('memories.errFillAll');
      return;
    }

    this.uploading.set(true);
    this.uploadErrorKey.set('');

    const onSuccess = () => {
      this.uploading.set(false);
      this.closeUploadForm();
      this.loadAll();
      this.toast.success('toast.memoryAdded');
    };
    const onError = (err: HttpErrorResponse) => {
      this.uploading.set(false);
      const key = err.error?.title ?? 'memories.uploadError';
      this.uploadErrorKey.set(key);
      this.toast.error(key);
    };

    if (this.uploadType() === 'photo') {
      this.photoService.upload(file, date, caption).subscribe({ next: onSuccess, error: onError });
    } else {
      this.audioService.upload(file, date, caption).subscribe({ next: onSuccess, error: onError });
    }
  }

  private clearSelectedFile(): void {
    this.selectedFile.set(null);
    this.revokePreviewUrl();
    this.previewUrl.set(null);
    const input = this.fileInputRef()?.nativeElement;
    if (input) input.value = '';
  }

  lightboxPost = signal<FeedPost | null>(null);

  openLightbox(post: FeedPost): void {
    this.lightboxPost.set(post);
  }

  closeLightbox(): void {
    this.lightboxPost.set(null);
  }

  postPendingDelete = signal<FeedPost | null>(null);

  confirmDeletePost(post: FeedPost): void {
    this.postPendingDelete.set(post);
  }

  cancelDeletePost(): void {
    this.postPendingDelete.set(null);
  }

  deletePost(): void {
    const post = this.postPendingDelete();
    if (!post) return;

    const request$ = post.type === 'photo' ? this.photoService.delete(post.id) : this.audioService.delete(post.id);
    request$.subscribe({
      next: () => {
        this.postPendingDelete.set(null);
        this.loadAll();
        this.toast.success('toast.memoryDeleted');
      },
      error: () => this.toast.error('toast.actionFailed')
    });
  }

  exporting = signal(false);

  exportAll(): void {
    if (this.exporting()) return;

    this.exporting.set(true);

    this.exportService.exportMemories().subscribe({
      next: blob => {
        this.exporting.set(false);
        this.triggerDownload(blob, `ourspace-uspomene-${new Date().toISOString().slice(0, 10)}.zip`);
      },
      error: () => {
        this.exporting.set(false);
        this.toast.error('toast.actionFailed');
      }
    });
  }

  private triggerDownload(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}
