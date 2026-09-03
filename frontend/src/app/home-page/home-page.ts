import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { TranslatePipe } from '../i18n/translate.pipe';
import { EventItem } from '../interfaces/event';
import { EventService } from '../services/event.service';
import { PhotoService } from '../services/photo.service';
import { AudioService } from '../services/audio.service';
import { UserService } from '../services/user.service';
import { WishlistService } from '../services/wishlist.service';
import { Wish } from '../interfaces/wish';
import { buildFeedPosts, FeedPost } from '../shared/build-feed-posts';
import { Photo } from '../interfaces/photo';
import { AudioMessage } from '../interfaces/audio';
import { Avatar } from '../shared/avatar/avatar';
import { AudioPlayer } from '../shared/audio-player/audio-player';
import { Lightbox } from '../shared/lightbox/lightbox';
import { Skeleton } from '../shared/skeleton/skeleton';

@Component({
  imports: [RouterLink, TranslatePipe, DatePipe, Avatar, AudioPlayer, Lightbox, Skeleton],
  selector: 'app-home-page',
  styleUrl: './home-page.css',
  templateUrl: './home-page.html',
})
export class HomePage {
  userService = inject(UserService);
  private eventService = inject(EventService);
  private photoService = inject(PhotoService);
  private audioService = inject(AudioService);
  private wishlistService = inject(WishlistService);

  upcomingEvents = signal<EventItem[]>([]);
  photos = signal<Photo[]>([]);
  audioItems = signal<AudioMessage[]>([]);
  wishes = signal<Wish[]>([]);
  totalMemories = signal<number | null>(null);

  recentWishes = computed(() =>
    [...this.wishes()].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 3)
  );

  private static readonly FEED_PREVIEW_SIZE = 6;
  private static readonly PREVIEW_FETCH_SIZE = 10;

  feedTypeFilter = signal<'all' | 'photo' | 'audio'>('all');

  allFeedPosts = computed<FeedPost[]>(() =>
    buildFeedPosts(
      this.photos(),
      this.audioItems(),
      path => this.photoService.fullUrl(path),
      path => this.audioService.fullUrl(path),
    )
  );

  feed = computed<FeedPost[]>(() => {
    const typeFilter = this.feedTypeFilter();

    return this.allFeedPosts()
      .filter(p => typeFilter === 'all' || p.type === typeFilter)
      .slice(0, HomePage.FEED_PREVIEW_SIZE);
  });

  setFeedTypeFilter(type: 'all' | 'photo' | 'audio'): void {
    this.feedTypeFilter.set(type);
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
    request$.subscribe(() => {
      this.postPendingDelete.set(null);
      this.loadMemories();
      this.loadMemoryCount();
    });
  }

  daysUntil(eventDate: string): number {
    const diffMs = new Date(eventDate).setHours(0, 0, 0, 0) - new Date().setHours(0, 0, 0, 0);
    return Math.round(diffMs / (1000 * 60 * 60 * 24));
  }

  daysTogether = computed(() => {
    const partner = this.userService.currentUser()?.partner;
    if (!partner?.relationshipStartDate) return null;

    const start = new Date(partner.relationshipStartDate).getTime();
    const diffMs = Date.now() - start;
    return Math.max(0, Math.floor(diffMs / (1000 * 60 * 60 * 24)));
  });

  constructor() {
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) {
        this.eventService.getUpcoming().subscribe(events => this.upcomingEvents.set(events.slice(0, 3)));
        this.wishlistService.getAll().subscribe(wishes => this.wishes.set(wishes));
        this.loadMemories();
        this.loadMemoryCount();
      }
    });
  }

  private loadMemories(): void {
    forkJoin({
      photos: this.photoService.getAll(1, HomePage.PREVIEW_FETCH_SIZE),
      audio: this.audioService.getAll(1, HomePage.PREVIEW_FETCH_SIZE),
    }).subscribe(({ photos, audio }) => {
      this.photos.set(photos.items);
      this.audioItems.set(audio.items);
    });
  }

  private loadMemoryCount(): void {
    forkJoin({
      photoCount: this.photoService.getCount(),
      audioCount: this.audioService.getCount(),
    }).subscribe(({ photoCount, audioCount }) => this.totalMemories.set(photoCount + audioCount));
  }
}
