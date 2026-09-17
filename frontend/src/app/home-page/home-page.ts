import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { TranslationService } from '../i18n/translation.service';
import { EventItem } from '../interfaces/event';
import { HomeService, YearTeaser as YearTeaserData } from '../services/home.service';
import { PhotoService } from '../services/photo.service';
import { AudioService } from '../services/audio.service';
import { UserService } from '../services/user.service';
import { Wish } from '../interfaces/wish';
import { buildFeedPosts, toFeedPost, FeedPost } from '../shared/build-feed-posts';
import { MemoryItem } from '../services/memory-feed.service';
import { pluralKey } from '../shared/plural';
import { Photo } from '../interfaces/photo';
import { AudioMessage } from '../interfaces/audio';
import { Avatar } from '../shared/avatar/avatar';
import { AudioPlayer } from '../shared/audio-player/audio-player';
import { Lightbox } from '../shared/lightbox/lightbox';
import { Skeleton } from '../shared/skeleton/skeleton';
import { YearTeaser } from '../shared/year-teaser/year-teaser';

@Component({
  imports: [LocalDatePipe, RouterLink, TranslatePipe, Avatar, AudioPlayer, Lightbox, Skeleton, YearTeaser],
  selector: 'app-home-page',
  styleUrl: './home-page.css',
  templateUrl: './home-page.html',
})
export class HomePage {
  userService = inject(UserService);
  private homeService = inject(HomeService);
  private i18n = inject(TranslationService);
  private photoService = inject(PhotoService);
  private audioService = inject(AudioService);

  upcomingEvents = signal<EventItem[]>([]);
  photos = signal<Photo[]>([]);
  audioItems = signal<AudioMessage[]>([]);
  wishes = signal<Wish[]>([]);
  totalMemories = signal<number | null>(null);
  onThisDay = signal<MemoryItem[]>([]);
  yearTeaser = signal<YearTeaserData | null>(null);

  onThisDayPosts = computed<FeedPost[]>(() =>
    this.onThisDay().map(item => toFeedPost(item, path => this.photoService.fullUrl(path)))
  );

  yearsAgo(date: string): number {
    return new Date().getFullYear() - new Date(date).getFullYear();
  }

  yearsAgoKey(date: string): string {
    return pluralKey('home.yearAgo', this.yearsAgo(date), this.i18n.lang());
  }

  recentWishes = computed(() =>
    [...this.wishes()].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 3)
  );

  private static readonly FEED_PREVIEW_SIZE = 6;

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
      this.load();
    });
  }

  daysUntil(eventDate: string): number {
    const diffMs = new Date(eventDate).setHours(0, 0, 0, 0) - new Date().setHours(0, 0, 0, 0);
    return Math.round(diffMs / (1000 * 60 * 60 * 24));
  }

  private static readonly RING_CIRCUMFERENCE = 163.36;
  private static readonly RING_HORIZON_DAYS = 30;

  ringOffset(eventDate: string): number {
    const days = Math.max(0, this.daysUntil(eventDate));
    const filled = Math.max(0, 1 - days / HomePage.RING_HORIZON_DAYS);
    return HomePage.RING_CIRCUMFERENCE * (1 - filled);
  }

  daysTogether = computed(() => {
    const partner = this.userService.currentUser()?.partner;
    if (!partner?.relationshipStartDate) return null;

    const start = new Date(partner.relationshipStartDate).getTime();
    const diffMs = Date.now() - start;
    return Math.max(0, Math.floor(diffMs / (1000 * 60 * 60 * 24)));
  });

  constructor() {
    this.load();
  }

  private load(): void {
    this.homeService.get().subscribe(summary => {
      this.userService.currentUser.set(summary.user);
      this.upcomingEvents.set(summary.upcomingEvents);
      this.wishes.set(summary.recentWishes);
      this.photos.set(summary.photos);
      this.audioItems.set(summary.audio);
      this.totalMemories.set(summary.user.partner ? summary.totalMemories : null);
      this.onThisDay.set(summary.onThisDay ?? []);
      this.yearTeaser.set(summary.yearTeaser ?? null);
    });
  }
}
