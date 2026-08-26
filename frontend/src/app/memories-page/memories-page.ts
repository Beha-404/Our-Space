import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Navbar } from '../navbar/navbar';
import { AudioMessage } from '../interfaces/audio';
import { Photo } from '../interfaces/photo';
import { AudioService } from '../services/audio.service';
import { PhotoService } from '../services/photo.service';
import { UserService } from '../services/user.service';
import { buildFeedPosts, FeedPost } from '../shared/build-feed-posts';
import { buildTimelineItems } from '../shared/build-timeline-items';
import { TimelineGraph } from './timeline-graph/timeline-graph';

type UploadType = 'photo' | 'audio';
type SortOrder = 'newest' | 'oldest';

@Component({
  imports: [Navbar, DatePipe, TranslatePipe, TimelineGraph],
  selector: 'app-memories-page',
  styleUrl: './memories-page.css',
  templateUrl: './memories-page.html',
})
export class MemoriesPage {
  private photoService = inject(PhotoService);
  private audioService = inject(AudioService);
  private userService = inject(UserService);

  fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  private static readonly PAGE_SIZE = 20;

  photos = signal<Photo[]>([]);
  audioItems = signal<AudioMessage[]>([]);
  loading = signal(true);
  loadingMore = signal(false);

  private photoPage = signal(1);
  private audioPage = signal(1);
  private hasMorePhotos = signal(false);
  private hasMoreAudio = signal(false);
  hasMore = computed(() => this.hasMorePhotos() || this.hasMoreAudio());

  uploadType = signal<UploadType>('photo');
  selectedFile = signal<File | null>(null);
  uploadDate = signal('');
  uploadCaption = signal('');
  uploading = signal(false);
  uploadErrorKey = signal('');

  sortOrder = signal<SortOrder>('newest');

  timelineItems = computed(() =>
    buildTimelineItems(this.photos(), this.audioItems(), path => this.photoService.fullUrl(path))
  );

  feed = computed<FeedPost[]>(() =>
    buildFeedPosts(
      this.photos(),
      this.audioItems(),
      path => this.photoService.fullUrl(path),
      path => this.audioService.fullUrl(path),
      this.sortOrder(),
    )
  );

  constructor() {
    this.userService.refreshCurrentUser().subscribe();
    this.loadAll();
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

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  upload(): void {
    const file = this.selectedFile();
    const date = this.uploadDate();

    if (!file || !date) {
      this.uploadErrorKey.set('memories.errFillAll');
      return;
    }

    this.uploading.set(true);
    this.uploadErrorKey.set('');

    const caption = this.uploadCaption().trim() || null;
    const onSuccess = () => {
      this.uploading.set(false);
      this.clearSelectedFile();
      this.uploadDate.set('');
      this.uploadCaption.set('');
      this.loadAll();
    };
    const onError = (err: HttpErrorResponse) => {
      this.uploading.set(false);
      this.uploadErrorKey.set(err.error?.title ?? 'memories.uploadError');
    };

    if (this.uploadType() === 'photo') {
      this.photoService.upload(file, date, caption).subscribe({ next: onSuccess, error: onError });
    } else {
      this.audioService.upload(file, date, caption).subscribe({ next: onSuccess, error: onError });
    }
  }

  private clearSelectedFile(): void {
    this.selectedFile.set(null);
    const input = this.fileInputRef()?.nativeElement;
    if (input) input.value = '';
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
    request$.subscribe(() => {
      this.postPendingDelete.set(null);
      this.loadAll();
    });
  }
}
