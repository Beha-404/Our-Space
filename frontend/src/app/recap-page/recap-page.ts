import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/translate.pipe';
import { TranslationService } from '../i18n/translation.service';
import { PhotoService } from '../services/photo.service';
import { Recap, RecapService } from '../services/recap.service';
import { UserService } from '../services/user.service';
import { pluralKey } from '../shared/plural';
import { SelectDropdown, SelectOption } from '../shared/select-dropdown/select-dropdown';
import { Skeleton } from '../shared/skeleton/skeleton';

interface MonthBar {
  key: string;
  label: string;
  count: number;
  percent: number;
}

const MONTH_KEYS = [
  'memories.month1', 'memories.month2', 'memories.month3', 'memories.month4',
  'memories.month5', 'memories.month6', 'memories.month7', 'memories.month8',
  'memories.month9', 'memories.month10', 'memories.month11', 'memories.month12',
];

@Component({
  imports: [TranslatePipe, Skeleton, SelectDropdown, RouterLink],
  selector: 'app-recap-page',
  styleUrl: './recap-page.css',
  templateUrl: './recap-page.html',
})
export class RecapPage {
  private recapService = inject(RecapService);
  private photoService = inject(PhotoService);
  private userService = inject(UserService);
  private i18n = inject(TranslationService);

  userLoaded = computed(() => !!this.userService.currentUser());
  isPaired = computed(() => !!this.userService.currentUser()?.partner);

  recap = signal<Recap | null>(null);
  loading = signal(true);
  selectedYear = signal<number>(new Date().getFullYear());

  totalMemories = computed(() => {
    const recap = this.recap();
    return recap ? recap.photos + recap.audioMessages : 0;
  });

  isEmpty = computed(() => {
    const recap = this.recap();
    if (!recap) return false;
    return this.totalMemories() === 0 && recap.events === 0 && recap.capsulesSealed === 0;
  });

  yearOptions = computed<SelectOption[]>(() => {
    const years = this.recap()?.availableYears ?? [];
    const current = new Date().getFullYear();
    const all = years.includes(current) ? years : [current, ...years];

    return all.map(year => ({ value: year, label: String(year) }));
  });

  months = computed<MonthBar[]>(() => {
    const perMonth = this.recap()?.memoriesPerMonth ?? [];
    const peak = Math.max(1, ...perMonth);

    return perMonth
      .map((count, index) => ({
        key: MONTH_KEYS[index],
        label: this.i18n.t(MONTH_KEYS[index]),
        count,
        percent: Math.round((count / peak) * 100),
      }))
      .filter(month => month.count > 0)
      .sort((a, b) => b.count - a.count)
      .slice(0, 5);
  });

  busiestMonth = computed(() => this.months()[0] ?? null);

  memoriesLabelKey = computed(() =>
    pluralKey('recap.memoriesThisYear', this.totalMemories(), this.i18n.lang()));

  memoriesWordKey(count: number): string {
    return pluralKey('recap.memoriesWord', count, this.i18n.lang());
  }

  highlightUrls = computed(() =>
    (this.recap()?.highlights ?? []).map(item =>
      this.photoService.fullUrl(item.thumbnailUrl ?? item.url))
  );

  constructor() {
    this.userService.ensureCurrentUser().subscribe(user => {
      if (user.partner) this.load(this.selectedYear());
      else this.loading.set(false);
    });
  }

  selectYear(year: number): void {
    this.selectedYear.set(year);
    this.load(year);
  }

  private load(year: number): void {
    this.loading.set(true);

    this.recapService.get(year).subscribe({
      next: recap => {
        this.recap.set(recap);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
