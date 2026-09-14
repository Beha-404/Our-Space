import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Wish } from '../interfaces/wish';
import { UserService } from '../services/user.service';
import { WishlistService } from '../services/wishlist.service';
import { Skeleton } from '../shared/skeleton/skeleton';
import { ToastService } from '../shared/toast/toast.service';

@Component({
  imports: [DatePipe, TranslatePipe, Skeleton, RouterLink],
  selector: 'app-wishlist-page',
  styleUrl: './wishlist-page.css',
  templateUrl: './wishlist-page.html',
})
export class WishlistPage {
  private wishlistService = inject(WishlistService);
  private userService = inject(UserService);
  private toast = inject(ToastService);

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  wishes = signal<Wish[]>([]);
  loading = signal(true);

  private static readonly WISH_PER_PAGE = 5;

  wishPage = signal(1);
  statusFilter = signal<'all' | 'fulfilled' | 'ongoing'>('all');

  filteredWishes = computed(() => {
    const filter = this.statusFilter();
    if (filter === 'all') return this.wishes();
    return this.wishes().filter(w => filter === 'fulfilled' ? w.isFulfilled : !w.isFulfilled);
  });

  totalWishPages = computed(() => Math.max(1, Math.ceil(this.filteredWishes().length / WishlistPage.WISH_PER_PAGE)));

  currentWishPage = computed(() => Math.min(this.wishPage(), this.totalWishPages()));

  pagedWishes = computed(() => {
    const start = (this.currentWishPage() - 1) * WishlistPage.WISH_PER_PAGE;
    return this.filteredWishes().slice(start, start + WishlistPage.WISH_PER_PAGE);
  });

  goToWishPage(page: number): void {
    this.wishPage.set(Math.min(Math.max(page, 1), this.totalWishPages()));
  }

  setStatusFilter(filter: 'all' | 'fulfilled' | 'ongoing'): void {
    this.statusFilter.set(filter);
    this.wishPage.set(1);
  }

  wishPageNumbers = computed<(number | '…')[]>(() => {
    const total = this.totalWishPages();
    const current = this.currentWishPage();

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
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) this.loadAll();
      else this.loading.set(false);
    });
  }

  loadAll(): void {
    this.loading.set(true);
    this.wishlistService.getAll().subscribe({
      next: (wishes) => {
        this.wishes.set(wishes);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
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
      next: () => {
        this.adding.set(false);
        this.newWish.set('');
        this.newWishTouched.set(false);
        this.loadAll();
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
