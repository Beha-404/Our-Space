import { Component, ElementRef, inject, OnDestroy, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { LanguageSwitcher } from '../i18n/language-switcher/language-switcher';
import { LocalDatePipe } from '../i18n/local-date.pipe';
import { TranslatePipe } from '../i18n/translate.pipe';
import { AppNotification } from '../interfaces/notification';
import { NotificationService } from '../services/notification.service';
import { UserService } from '../services/user.service';
import { Avatar } from '../shared/avatar/avatar';

const TYPE_MESSAGE_KEYS: Record<AppNotification['type'], string> = {
  EventCreated: 'notifications.msgEventCreated',
  EventUpdated: 'notifications.msgEventUpdated',
  EventDeleted: 'notifications.msgEventDeleted',
  PhotoAdded: 'notifications.msgPhotoAdded',
  AudioAdded: 'notifications.msgAudioAdded',
  WishAdded: 'notifications.msgWishAdded',
  CapsuleSealed: 'notifications.msgCapsuleSealed',
  CapsuleUnlocked: 'notifications.msgCapsuleUnlocked',
};

@Component({
  imports: [LocalDatePipe, RouterLink, RouterLinkActive, TranslatePipe, LanguageSwitcher, Avatar],
  selector: 'app-navbar',
  styleUrl: './navbar.css',
  templateUrl: './navbar.html',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'notificationsOpen.set(false)',
  },
})
export class Navbar implements OnDestroy {
  userService = inject(UserService);
  notificationService = inject(NotificationService);
  private router = inject(Router);
  private host = inject(ElementRef<HTMLElement>);

  notificationsOpen = signal(false);

  constructor() {
    this.userService.ensureCurrentUser().subscribe();
    this.notificationService.startPolling();
  }

  ngOnDestroy(): void {
    this.notificationService.stopPolling();
  }

  toggleNotifications(): void {
    const opening = !this.notificationsOpen();
    this.notificationsOpen.set(opening);
    if (opening) this.notificationService.loadRecent();
  }

  onDocumentClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.notificationsOpen.set(false);
    }
  }

  messageKeyFor(notification: AppNotification): string {
    return TYPE_MESSAGE_KEYS[notification.type];
  }

  onNotificationClick(notification: AppNotification): void {
    this.notificationService.markRead(notification.id);
    this.notificationsOpen.set(false);

    if (notification.entityType === 'event') {
      const queryParams = notification.type === 'EventDeleted' ? {} : { highlight: notification.entityId };
      this.router.navigate(['/events'], { queryParams });
    } else if (notification.entityType === 'photo' || notification.entityType === 'audio') {
      this.router.navigate(['/memories'], { queryParams: { highlight: notification.entityId, type: notification.entityType } });
    } else if (notification.entityType === 'wish') {
      this.router.navigate(['/wishlist'], { queryParams: { highlight: notification.entityId } });
    } else if (notification.entityType === 'capsule') {
      this.router.navigate(['/capsules'], { queryParams: { highlight: notification.entityId } });
    }
  }
}
