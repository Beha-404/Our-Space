import { HttpErrorResponse, HttpEvent, HttpEventType } from '@angular/common/http';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, concatMap, from, Observable, of, tap } from 'rxjs';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { TranslationService } from '../i18n/translation.service';
import { AudioService } from '../services/audio.service';
import { ExportService } from '../services/export.service';
import { MemoryFeedService, MemoryItem } from '../services/memory-feed.service';
import { PhotoService } from '../services/photo.service';
import { UserService } from '../services/user.service';
import { toFeedPost, FeedPost } from '../shared/build-feed-posts';
import { AudioPlayer } from '../shared/audio-player/audio-player';
import { DatePicker } from '../shared/date-picker/date-picker';
import { LocationPicker } from '../shared/location-picker/location-picker';
import { pageNumbers } from '../shared/page-numbers';
import { PickedLocation } from '../shared/location-picker/geocoding';
import { Lightbox } from '../shared/lightbox/lightbox';
import { scrollAndHighlight } from '../shared/scroll-and-highlight';
import { SelectDropdown, SelectOption } from '../shared/select-dropdown/select-dropdown';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';

type UploadType = 'photo' | 'audio';
type SortOrder = 'newest' | 'oldest';
type FeedType = 'all' | 'photo' | 'audio';

interface UploadItem {
  file: File;
  progress: number;
  status: 'pending' | 'uploading' | 'done' | 'error';
}

interface MemoryMonth {
  key: string;
  label: string;
  posts: FeedPost[];
}

@Component({
  imports: [LocalDatePipe, TranslatePipe, SelectDropdown, Lightbox, Skeleton, AudioPlayer, DatePicker, LocationPicker, RouterLink],
  selector: 'app-memories-page',
  styleUrl: './memories-page.css',
  templateUrl: './memories-page.html',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'moreMenuOpen.set(false)',
  },
})
export class MemoriesPage {
  private photoService = inject(PhotoService);
  private audioService = inject(AudioService);
  private exportService = inject(ExportService);
  private memoryFeed = inject(MemoryFeedService);
  private userService = inject(UserService);
  private i18n = inject(TranslationService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  private static readonly MAX_HIGHLIGHT_SEARCH_PAGES = 25;

  fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  private static readonly PAGE_SIZE = 24;

  feedItems = signal<MemoryItem[]>([]);
  availableYears = signal<number[]>([]);
  hasMore = signal(false);
  loading = signal(true);

  showUploadForm = signal(false);

  openUploadForm(): void {
    this.showUploadForm.set(true);
  }

  closeUploadForm(): void {
    this.showUploadForm.set(false);
    this.clearSelectedFile();
    this.uploadDate.set('');
    this.uploadCaption.set('');
    this.uploadLocation.set(null);
    this.uploadCaptionTouched.set(false);
    this.uploadSubmitted.set(false);
    this.uploadErrorKey.set('');
  }

  uploadType = signal<UploadType>('photo');
  selectedFile = signal<File | null>(null);
  uploadQueue = signal<UploadItem[]>([]);
  uploadDate = signal('');
  uploadCaption = signal('');
  uploadLocation = signal<PickedLocation | null>(null);
  uploadCaptionTouched = signal(false);
  uploading = signal(false);
  uploadErrorKey = signal('');

  uploadCaptionError = computed(() =>
    this.uploadCaptionTouched() && !this.uploadCaption().trim() ? 'memories.errTitleRequired' : '');

  uploadCaptionValid = computed(() =>
    this.uploadCaptionTouched() && !this.uploadCaptionError() && !!this.uploadCaption().trim());

  uploadDateValid = computed(() => !!this.uploadDate());
  uploadFileValid = computed(() => this.uploadQueue().length > 0);

  uploadSubmitted = signal(false);

  showUploadFillAllError = computed(() => this.uploadSubmitted() &&
    (this.uploadQueue().length === 0 || !this.uploadDate() || !!this.uploadCaptionError()));

  sortOrder = signal<SortOrder>('newest');

  private static readonly MONTH_KEYS = [
    'memories.month1', 'memories.month2', 'memories.month3', 'memories.month4',
    'memories.month5', 'memories.month6', 'memories.month7', 'memories.month8',
    'memories.month9', 'memories.month10', 'memories.month11', 'memories.month12',
  ];

  feedYearFilter = signal<number | 'all'>('all');
  feedMonthFilter = signal<number | 'all'>('all');
  feedTypeFilter = signal<FeedType>('all');
  feedPage = signal(1);

  readonly typeOptions: { value: FeedType; labelKey: string }[] = [
    { value: 'all', labelKey: 'home.feedFilterAll' },
    { value: 'photo', labelKey: 'home.feedFilterPhotos' },
    { value: 'audio', labelKey: 'home.feedFilterAudio' },
  ];

  isFiltered = computed(() =>
    this.feedYearFilter() !== 'all' || this.feedMonthFilter() !== 'all' || this.feedTypeFilter() !== 'all');

  emptyFeedKey = computed(() => {
    const periodFiltered = this.feedYearFilter() !== 'all' || this.feedMonthFilter() !== 'all';
    if (periodFiltered) return 'memories.noMemoriesFiltered';
    if (this.feedTypeFilter() !== 'all') return 'home.feedFilterEmpty';
    return 'memories.noMemories';
  });

  pagedFeed = computed<FeedPost[]>(() =>
    this.feedItems().map(item => toFeedPost(item, path => this.photoService.fullUrl(path))));

  groupedFeed = computed<MemoryMonth[]>(() => {
    const months: MemoryMonth[] = [];

    for (const post of this.pagedFeed()) {
      const [year, month] = post.date.split('-');
      const key = `${year}-${month}`;
      const current = months[months.length - 1];

      if (current?.key === key) {
        current.posts.push(post);
        continue;
      }

      months.push({
        key,
        label: `${this.i18n.t(MemoriesPage.MONTH_KEYS[Number(month) - 1])} ${year}`,
        posts: [post],
      });
    }

    return months;
  });

  yearOptions = computed<SelectOption[]>(() => [
    { value: 'all', label: this.i18n.t('events.allYears') },
    ...this.availableYears().map(year => ({ value: year, label: String(year) })),
  ]);

  monthOptions = computed<SelectOption[]>(() => [
    { value: 'all', label: this.i18n.t('memories.allMonths') },
    ...MemoriesPage.MONTH_KEYS.map((key, i) => ({ value: i + 1, label: this.i18n.t(key) })),
  ]);

  setFeedYearFilter(year: number | 'all'): void {
    this.feedYearFilter.set(year);
    this.loadPage(1);
  }

  setFeedMonthFilter(month: number | 'all'): void {
    this.feedMonthFilter.set(month);
    this.loadPage(1);
  }

  setFeedTypeFilter(type: FeedType): void {
    if (type === this.feedTypeFilter()) return;
    this.feedTypeFilter.set(type);
    this.loadPage(1);
  }

  toggleSortOrder(): void {
    this.sortOrder.update(order => order === 'newest' ? 'oldest' : 'newest');
    this.loadPage(1);
  }

  moreMenuOpen = signal(false);

  toggleMoreMenu(): void {
    this.moreMenuOpen.update(open => !open);
  }

  exportFromMenu(): void {
    this.moreMenuOpen.set(false);
    this.exportAll();
  }

  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target?.closest('.more-menu')) this.moreMenuOpen.set(false);
  }

  totalCount = signal(0);

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / MemoriesPage.PAGE_SIZE)));

  feedPageNumbers = computed(() => pageNumbers(this.totalPages(), this.feedPage()));

  goToFeedPage(page: number): void {
    const target = Math.min(Math.max(page, 1), this.totalPages());
    if (target === this.feedPage()) return;

    this.loadPage(target, () =>
      document.getElementById('memories-feed')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
  }

  constructor() {
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) {
        this.loadPage(1);
        this.highlightFromQueryParams();
      } else {
        this.loading.set(false);
      }
    });
  }

  loadAll(): void {
    this.loadPage(1);
  }

  private highlightFromQueryParams(): void {
    const idParam = this.route.snapshot.queryParamMap.get('highlight');
    const type = this.route.snapshot.queryParamMap.get('type') as 'photo' | 'audio' | null;
    if (!idParam || !type) return;

    this.findHighlightPage(Number(idParam), type, 1);
  }

  private findHighlightPage(id: number, type: 'photo' | 'audio', page: number): void {
    if (page > MemoriesPage.MAX_HIGHLIGHT_SEARCH_PAGES) return;

    this.memoryFeed.getPage({
      page,
      pageSize: MemoriesPage.PAGE_SIZE,
      sort: 'newest',
      year: 'all',
      month: 'all',
      type: 'all',
    }).subscribe({
      next: feed => {
        if (feed.items.some(item => item.id === id && item.type === type)) {
          this.sortOrder.set('newest');
          this.feedYearFilter.set('all');
          this.feedMonthFilter.set('all');
          this.feedTypeFilter.set('all');
          this.loadPage(page);
          scrollAndHighlight(`post-${type}-${id}`);
        } else if (feed.hasMore) {
          this.findHighlightPage(id, type, page + 1);
        }
      },
      error: () => {}
    });
  }

  private loadPage(page: number, onLoaded?: () => void): void {
    this.loading.set(true);

    this.memoryFeed.getPage({
      page,
      pageSize: MemoriesPage.PAGE_SIZE,
      sort: this.sortOrder(),
      year: this.feedYearFilter(),
      month: this.feedMonthFilter(),
      type: this.feedTypeFilter(),
    }).subscribe({
      next: feed => {
        this.feedItems.set(feed.items);
        this.hasMore.set(feed.hasMore);
        this.totalCount.set(feed.totalCount ?? 0);
        this.feedPage.set(page);
        if (feed.years) this.availableYears.set(feed.years);
        this.loading.set(false);
        onLoaded?.();
      },
      error: () => this.loading.set(false)
    });
  }

  updateUploadCaption(value: string): void {
    this.uploadCaption.set(value);
    this.uploadErrorKey.set('');
  }

  updateUploadDate(value: string): void {
    this.uploadDate.set(value);
    this.uploadErrorKey.set('');
  }

  setUploadType(type: UploadType): void {
    this.uploadType.set(type);
    this.clearSelectedFile();
    this.uploadErrorKey.set('');
  }

  previewUrl = signal<string | null>(null);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);

    this.uploadErrorKey.set('');
    this.selectedFile.set(files[0] ?? null);
    this.uploadQueue.set(files.map(file => ({ file, progress: 0, status: 'pending' as const })));

    this.revokePreviewUrl();
    this.previewUrl.set(
      files.length === 1 && this.uploadType() === 'photo' ? URL.createObjectURL(files[0]) : null);
  }

  private revokePreviewUrl(): void {
    const url = this.previewUrl();
    if (url) URL.revokeObjectURL(url);
  }

  uploadedCount = computed(() => this.uploadQueue().filter(i => i.status === 'done').length);

  upload(): void {
    this.uploadCaptionTouched.set(true);
    this.uploadSubmitted.set(true);

    const queue = this.uploadQueue();
    const date = this.uploadDate();
    const caption = this.uploadCaption().trim();

    if (queue.length === 0 || !date || this.uploadCaptionError()) {
      return;
    }

    this.uploading.set(true);
    this.uploadErrorKey.set('');

    from(queue.map((_, index) => index))
      .pipe(concatMap(index => this.uploadOne(index, date, caption)))
      .subscribe({
        complete: () => {
          this.uploading.set(false);

          const failed = this.uploadQueue().filter(i => i.status === 'error').length;
          if (failed === 0) {
            this.closeUploadForm();
            this.toast.success('toast.memoryAdded');
          } else {
            this.toast.error(this.uploadErrorKey() || 'memories.uploadError');
          }

          this.loadAll();
        }
      });
  }

  private uploadOne(index: number, date: string, caption: string): Observable<unknown> {
    const item = this.uploadQueue()[index];
    this.patchQueueItem(index, { status: 'uploading' });

    const request$: Observable<HttpEvent<unknown>> = this.uploadType() === 'photo'
      ? this.photoService.upload(item.file, date, caption, this.uploadLocation())
      : this.audioService.upload(item.file, date, caption);

    return request$.pipe(
      tap(event => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          this.patchQueueItem(index, { progress: Math.round(100 * event.loaded / event.total) });
        } else if (event.type === HttpEventType.Response) {
          this.patchQueueItem(index, { progress: 100, status: 'done' });
        }
      }),
      catchError((err: HttpErrorResponse) => {
        this.patchQueueItem(index, { status: 'error' });
        this.uploadErrorKey.set(err.error?.title ?? 'memories.uploadError');
        return of(null);
      })
    );
  }

  private patchQueueItem(index: number, changes: Partial<UploadItem>): void {
    this.uploadQueue.update(queue =>
      queue.map((item, i) => i === index ? { ...item, ...changes } : item));
  }

  private clearSelectedFile(): void {
    this.selectedFile.set(null);
    this.uploadQueue.set([]);
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
      next: response => {
        this.exporting.set(false);

        if (response.status === 202) {
          this.toast.success('toast.exportQueued');
          return;
        }

        if (response.body) {
          this.triggerDownload(response.body, `ourspace-uspomene-${new Date().toISOString().slice(0, 10)}.zip`);
        }
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
