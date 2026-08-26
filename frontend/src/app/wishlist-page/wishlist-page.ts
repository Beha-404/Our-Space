import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Navbar } from '../navbar/navbar';
import { Wish } from '../interfaces/wish';
import { UserService } from '../services/user.service';
import { WishlistService } from '../services/wishlist.service';

@Component({
  imports: [Navbar, DatePipe, TranslatePipe],
  selector: 'app-wishlist-page',
  styleUrl: './wishlist-page.css',
  templateUrl: './wishlist-page.html',
})
export class WishlistPage {
  private wishlistService = inject(WishlistService);
  private userService = inject(UserService);

  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  wishes = signal<Wish[]>([]);
  loading = signal(true);

  newWish = signal('');
  adding = signal(false);
  addErrorKey = signal('');

  wishPendingDelete = signal<Wish | null>(null);

  constructor() {
    this.userService.refreshCurrentUser().subscribe();
    this.loadAll();
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
        this.loadAll();
      },
      error: (err: HttpErrorResponse) => {
        this.adding.set(false);
        this.addErrorKey.set(err.error?.title ?? 'wishlist.addError');
      }
    });
  }

  toggleFulfilled(wish: Wish): void {
    this.wishlistService.toggleFulfilled(wish.id).subscribe(() => this.loadAll());
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

    this.wishlistService.delete(wish.id).subscribe(() => {
      this.wishPendingDelete.set(null);
      this.loadAll();
    });
  }
}
