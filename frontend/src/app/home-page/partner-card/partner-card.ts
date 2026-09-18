import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalDatePipe } from '../../i18n/local-date.pipe';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { TranslationService } from '../../i18n/translation.service';
import { PartnerDto } from '../../interfaces/user';
import { HomeService, PartnerActivity } from '../../services/home.service';
import { PhotoService } from '../../services/photo.service';
import { Avatar } from '../../shared/avatar/avatar';
import { pluralKey } from '../../shared/plural';

@Component({
  selector: 'app-partner-card',
  imports: [LocalDatePipe, RouterLink, TranslatePipe, Avatar],
  templateUrl: './partner-card.html',
  styleUrl: './partner-card.css',
})
export class PartnerCard {
  private homeService = inject(HomeService);
  private photoService = inject(PhotoService);
  private i18n = inject(TranslationService);

  partner = input.required<PartnerDto>();
  daysTogether = input<number | null>(null);
  totalMemories = input<number | null>(null);

  partnerOpen = signal(false);
  partnerActivity = signal<PartnerActivity | null>(null);
  partnerActivityFailed = signal(false);
  private partnerActivityLoading = false;

  partnerStats = computed(() => {
    const activity = this.partnerActivity();
    if (!activity) return [];

    const lang = this.i18n.lang();
    const stat = (count: number, key: string, link: string, filter?: string) => ({
      count,
      labelKey: pluralKey(`partner.${key}`, count, lang),
      link,
      queryParams: filter ? { filter } : null,
    });

    return [
      stat(activity.photos, 'photos', '/memories', 'photo'),
      stat(activity.voiceLetters, 'letters', '/memories', 'audio'),
      stat(activity.wishes, 'wishes', '/wishlist'),
      stat(activity.events, 'events', '/events'),
      stat(activity.capsules, 'capsules', '/capsules'),
    ];
  });

  togglePartner(): void {
    const open = !this.partnerOpen();
    this.partnerOpen.set(open);
    if (open && !this.partnerActivity()) this.loadPartnerActivity();
  }

  loadPartnerActivity(): void {
    if (this.partnerActivityLoading) return;

    this.partnerActivityLoading = true;
    this.partnerActivityFailed.set(false);
    this.homeService.getPartnerActivity().subscribe({
      next: activity => {
        this.partnerActivityLoading = false;
        this.partnerActivity.set(activity);
      },
      error: () => {
        this.partnerActivityLoading = false;
        this.partnerActivityFailed.set(true);
      },
    });
  }

  partnerPhotoUrl(path: string): string {
    return this.photoService.fullUrl(path);
  }
}
