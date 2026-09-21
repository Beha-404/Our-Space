import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { TranslationService } from '../i18n/translation.service';
import { EventItem } from '../interfaces/event';
import { EventService } from '../services/event.service';
import { HomeService, YearTeaser as YearTeaserData } from '../services/home.service';
import { PhotoService } from '../services/photo.service';
import { UserService } from '../services/user.service';
import { WishlistService } from '../services/wishlist.service';
import { Wish } from '../interfaces/wish';
import { toFeedPost, FeedPost } from '../shared/build-feed-posts';
import { MemoryItem } from '../services/memory-feed.service';
import { pluralKey } from '../shared/plural';
import { AudioPlayer } from '../shared/audio-player/audio-player';
import { Lightbox } from '../shared/lightbox/lightbox';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';
import { YearTeaser } from '../shared/year-teaser/year-teaser';
import { PartnerCard } from './partner-card/partner-card';

@Component({
  imports: [LocalDatePipe, RouterLink, TranslatePipe, PartnerCard, AudioPlayer, Lightbox, Skeleton, YearTeaser],
  selector: 'app-home-page',
  styleUrl: './home-page.css',
  templateUrl: './home-page.html',
})
export class HomePage {
  userService = inject(UserService);
  private homeService = inject(HomeService);
  private i18n = inject(TranslationService);
  private photoService = inject(PhotoService);
  private eventService = inject(EventService);
  private wishlistService = inject(WishlistService);
  private toast = inject(ToastService);

  upcomingEvents = signal<EventItem[]>([]);
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

  cancelEvent(event: EventItem): void {
    this.eventService.cancel(event.id).subscribe({
      next: () => {
        this.upcomingEvents.update(list => list.filter(e => e.id !== event.id));
        this.toast.success('toast.eventCancelled');
      },
      error: () => this.toast.error('toast.actionFailed'),
    });
  }

  toggleWish(wish: Wish): void {
    this.wishlistService.toggleFulfilled(wish.id).subscribe({
      next: updated => {
        this.wishes.update(list => list.map(w => w.id === updated.id ? updated : w));
        this.toast.success('toast.wishUpdated');
      },
      error: () => this.toast.error('toast.actionFailed'),
    });
  }

  lightboxPost = signal<FeedPost | null>(null);

  openLightbox(post: FeedPost): void {
    this.lightboxPost.set(post);
  }

  closeLightbox(): void {
    this.lightboxPost.set(null);
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
    this.load();
  }

  private load(): void {
    this.homeService.get().subscribe(summary => {
      this.userService.currentUser.set(summary.user);
      this.upcomingEvents.set(summary.upcomingEvents);
      this.wishes.set(summary.recentWishes);
      this.totalMemories.set(summary.user.partner ? summary.totalMemories : null);
      this.onThisDay.set(summary.onThisDay ?? []);
      this.yearTeaser.set(summary.yearTeaser ?? null);
    });
  }
}
