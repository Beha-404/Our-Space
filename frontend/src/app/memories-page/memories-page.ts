import { DatePipe } from '@angular/common';
import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { forkJoin } from 'rxjs';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Navbar } from '../navbar/navbar';
import { AudioMessage } from '../interfaces/audio';
import { Photo } from '../interfaces/photo';
import { AudioService } from '../services/audio.service';
import { PhotoService } from '../services/photo.service';
import { UserService } from '../services/user.service';
import { buildTimelineItems } from '../shared/build-timeline-items';
import { TimelineGraph } from './timeline-graph/timeline-graph';

type UploadType = 'photo' | 'audio';
type SortOrder = 'newest' | 'oldest';

interface FeedPost {
  id: number;
  type: UploadType;
  date: string;
  caption: string | null;
  imageUrl?: string;
  audioUrl?: string;
  uploadedByUsername: string;
}

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

  photos = signal<Photo[]>([]);
  audioItems = signal<AudioMessage[]>([]);
  loading = signal(true);

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

  feed = computed<FeedPost[]>(() => {
    const photoPosts: FeedPost[] = this.photos().map(p => ({
      id: p.id,
      type: 'photo',
      date: p.takenAt,
      caption: p.caption,
      imageUrl: this.photoService.fullUrl(p.thumbnailUrl),
      uploadedByUsername: p.uploadedByUsername,
    }));
    const audioPosts: FeedPost[] = this.audioItems().map(a => ({
      id: a.id,
      type: 'audio',
      date: a.recordedAt,
      caption: a.caption,
      audioUrl: this.audioService.fullUrl(a.url),
      uploadedByUsername: a.uploadedByUsername,
    }));

    const all = [...photoPosts, ...audioPosts].sort((a, b) => a.date.localeCompare(b.date));
    return this.sortOrder() === 'newest' ? all.reverse() : all;
  });

  constructor() {
    this.userService.refreshCurrentUser().subscribe(() => this.loadAll());
  }

  loadAll(): void {
    this.loading.set(true);
    forkJoin({
      photos: this.photoService.getAll(),
      audio: this.audioService.getAll(),
    }).subscribe({
      next: ({ photos, audio }) => {
        this.photos.set(photos);
        this.audioItems.set(audio);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
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
    const onError = () => {
      this.uploading.set(false);
      this.uploadErrorKey.set('memories.uploadError');
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

  deletePost(post: FeedPost): void {
    const request$ = post.type === 'photo' ? this.photoService.delete(post.id) : this.audioService.delete(post.id);
    request$.subscribe(() => this.loadAll());
  }
}
