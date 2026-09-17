import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { TranslationService } from '../../i18n/translation.service';
import { YearTeaser as YearTeaserData } from '../../services/home.service';
import { PhotoService } from '../../services/photo.service';
import { pluralKey } from '../plural';

const SLOTS = 4;

@Component({
  selector: 'app-year-teaser',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './year-teaser.html',
  styleUrl: './year-teaser.css',
})
export class YearTeaser {
  private i18n = inject(TranslationService);
  private photoService = inject(PhotoService);

  teaser = input.required<YearTeaserData>();

  photos = computed<(string | null)[]>(() => {
    const urls = this.teaser().photoUrls.map(url => this.photoService.fullUrl(url));
    return Array.from({ length: SLOTS }, (_, i) => urls[i] ?? null);
  });

  memoriesKey = computed(() =>
    pluralKey('recap.memoriesWord', this.teaser().memories, this.i18n.lang()));

  monthKey = computed(() => {
    const month = this.teaser().busiestMonth;
    return month ? `recap.inMonth${month}` : null;
  });
}
