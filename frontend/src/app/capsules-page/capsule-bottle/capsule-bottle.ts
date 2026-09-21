import { Component, DestroyRef, ElementRef, afterNextRender, computed, inject, input, output, signal } from '@angular/core';
import { LocalDatePipe } from '../../i18n/local-date.pipe';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { Capsule } from '../../interfaces/capsule';
import { daysUntil } from '../capsule-dates';

export const UNCORK_MS = 450;

const TILTS = [-14, 9, -6, 12, -10, 5];

function prefersReducedMotion(): boolean {
  return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
}

@Component({
  selector: 'app-capsule-bottle',
  imports: [LocalDatePipe, TranslatePipe],
  templateUrl: './capsule-bottle.html',
  styleUrl: './capsule-bottle.css',
  host: {
    '[class.available]': 'available()',
    '[class.arriving]': 'arriving()',
    '[class.paused]': '!visible()',
    '[style.--tilt]': "tilt() + 'deg'",
    '[style.--bob-duration]': "bobDuration() + 's'",
    '[style.--bob-delay]': "bobDelay() + 's'",
  },
})
export class CapsuleBottle {
  capsule = input.required<Capsule>();
  canDelete = input(false);
  opening = input(false);
  arriving = input(false);

  openRequested = output<void>();
  deleteRequested = output<void>();

  uncorking = signal(false);
  visible = signal(true);

  available = computed(() => this.capsule().canOpenNow);
  daysLeft = computed(() => {
    const openAt = this.capsule().openAt;
    return openAt ? Math.max(daysUntil(openAt), 0) : 0;
  });
  tilt = computed(() => TILTS[this.capsule().id % TILTS.length]);
  bobDuration = computed(() => 5 + (this.capsule().id % 4) * 0.8);
  bobDelay = computed(() => -(this.capsule().id % 5) * 0.9);

  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    const destroyRef = inject(DestroyRef);
    const host: HTMLElement = inject(ElementRef).nativeElement;

    destroyRef.onDestroy(() => {
      if (this.timer) clearTimeout(this.timer);
    });

    afterNextRender(() => {
      if (typeof IntersectionObserver !== 'function') return;

      const observer = new IntersectionObserver(
        ([entry]) => this.visible.set(entry.isIntersecting),
        { rootMargin: '160px' },
      );
      observer.observe(host);
      destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  uncork(): void {
    if (this.uncorking() || this.opening()) return;

    this.uncorking.set(true);
    this.timer = setTimeout(() => {
      this.timer = null;
      this.uncorking.set(false);
      this.openRequested.emit();
    }, prefersReducedMotion() ? 0 : UNCORK_MS);
  }
}
