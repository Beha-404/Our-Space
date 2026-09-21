import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Wish } from '../interfaces/wish';
import { UserService } from '../services/user.service';
import { WishlistService } from '../services/wishlist.service';
import { Pager } from '../shared/pager/pager';
import { scrollAndHighlight } from '../shared/scroll-and-highlight';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';
import { SKY_CAPACITY } from './wish-sky/wish-sky-layout';
import { WishSky } from './wish-sky/wish-sky';

type WishTab = 'waiting' | 'fulfilled';

const LIST_PAGE_SIZE = 10;
const FLASH_MS = 900;

@Component({
  imports: [LocalDatePipe, TranslatePipe, Skeleton, RouterLink, WishSky, Pager],
  selector: 'app-wishlist-page',
  styleUrl: './wishlist-page.css',
  templateUrl: './wishlist-page.html',
})
export class WishlistPage {
  private wishlistService = inject(WishlistService);
  private userService = inject(UserService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  wishes = signal<Wish[]>([]);
  loading = signal(true);

  selectedId = signal<number | null>(null);
  flashId = signal<number | null>(null);
  tab = signal<WishTab>('waiting');
  listPage = signal(1);

  newestFirst = computed(() =>
    [...this.wishes()].sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt) || b.id - a.id));

  skyWishes = computed(() => this.newestFirst().slice(0, SKY_CAPACITY));

  fulfilledCount = computed(() => this.wishes().filter(w => w.isFulfilled).length);
  waitingCount = computed(() => this.wishes().length - this.fulfilledCount());
  progressPercent = computed(() =>
    this.wishes().length ? Math.round((this.fulfilledCount() / this.wishes().length) * 100) : 0);

  selected = computed(() => {
    const id = this.selectedId();
    return this.wishes().find(w => w.id === id) ?? this.newestFirst()[0] ?? null;
  });

  tabWishes = computed(() => {
    if (this.tab() === 'waiting') return this.newestFirst().filter(w => !w.isFulfilled);

    return this.wishes()
      .filter(w => w.isFulfilled)
      .sort((a, b) => Date.parse(b.fulfilledAt ?? b.createdAt) - Date.parse(a.fulfilledAt ?? a.createdAt));
  });

  totalListPages = computed(() => Math.max(1, Math.ceil(this.tabWishes().length / LIST_PAGE_SIZE)));
  currentListPage = computed(() => Math.min(this.listPage(), this.totalListPages()));

  visibleWishes = computed(() => {
    const start = (this.currentListPage() - 1) * LIST_PAGE_SIZE;
    return this.tabWishes().slice(start, start + LIST_PAGE_SIZE);
  });

  setTab(tab: WishTab): void {
    this.tab.set(tab);
    this.listPage.set(1);
  }

  goToListPage(page: number): void {
    this.listPage.set(Math.min(Math.max(page, 1), this.totalListPages()));
  }

  select(id: number): void {
    this.selectedId.set(id);
  }

  selectFromList(id: number): void {
    this.select(id);
    scrollAndHighlight('wish-card');
  }

  newWish = signal('');
  newWishTouched = signal(false);
  adding = signal(false);
  addErrorKey = signal('');

  newWishError = computed(() =>
    this.newWishTouched() && !this.newWish().trim() ? 'wishlist.errEmpty' : '');

  newWishValid = computed(() => this.newWishTouched() && !this.newWishError() && !!this.newWish().trim());

  updateNewWish(value: string): void {
    this.newWish.set(value);
    this.addErrorKey.set('');
  }

  wishPendingDelete = signal<Wish | null>(null);

  constructor() {
    if (this.route.snapshot.queryParamMap.get('tab') === 'fulfilled') this.tab.set('fulfilled');

    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) this.loadAll(() => this.openFromQueryParams());
      else this.loading.set(false);
    });
  }

  loadAll(afterLoad?: () => void): void {
    this.loading.set(true);
    this.wishlistService.getAll().subscribe({
      next: (wishes) => {
        this.wishes.set(wishes);
        this.loading.set(false);
        afterLoad?.();
      },
      error: () => this.loading.set(false)
    });
  }

  private openFromQueryParams(): void {
    const idParam = this.route.snapshot.queryParamMap.get('highlight');
    if (!idParam) return;

    const id = Number(idParam);
    if (!this.wishes().some(w => w.id === id)) return;

    this.select(id);
    scrollAndHighlight('wish-card');
  }

  addWish(): void {
    this.newWishTouched.set(true);
    const title = this.newWish().trim();
    if (!title) {
      this.addErrorKey.set('wishlist.errEmpty');
      return;
    }

    this.adding.set(true);
    this.addErrorKey.set('');

    this.wishlistService.create(title).subscribe({
      next: created => {
        this.adding.set(false);
        this.newWish.set('');
        this.newWishTouched.set(false);
        this.loadAll(() => this.select(created.id));
        this.toast.success('toast.wishAdded');
      },
      error: (err: HttpErrorResponse) => {
        this.adding.set(false);
        const key = err.error?.title ?? 'wishlist.addError';
        this.addErrorKey.set(key);
        this.toast.error(key);
      }
    });
  }

  toggleFulfilled(wish: Wish): void {
    this.wishlistService.toggleFulfilled(wish.id).subscribe({
      next: updated => {
        this.wishes.update(list => list.map(w => w.id === updated.id ? updated : w));
        this.toast.success('toast.wishUpdated');

        if (updated.isFulfilled) {
          this.flashId.set(updated.id);
          setTimeout(() => this.flashId.set(null), FLASH_MS);
        }
      },
      error: () => this.toast.error('toast.actionFailed')
    });
  }

  confirmDelete(wish: Wish): void {
    this.wishPendingDelete.set(wish);
  }

  cancelDelete(): void {
    this.wishPendingDelete.set(null);
  }

  deleteWish(): void {
    const wish = this.wishPendingDelete();
    if (!wish) return;

    this.wishlistService.delete(wish.id).subscribe({
      next: () => {
        this.wishPendingDelete.set(null);
        this.loadAll();
        this.toast.success('toast.wishDeleted');
      },
      error: () => this.toast.error('toast.actionFailed')
    });
  }
}
