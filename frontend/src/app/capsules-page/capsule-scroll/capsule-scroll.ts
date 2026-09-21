import { Component, computed, input, output, signal } from '@angular/core';
import { LocalDatePipe } from '../../i18n/local-date.pipe';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { Capsule } from '../../interfaces/capsule';
import { Parchment } from '../../shared/parchment/parchment';

@Component({
  selector: 'app-capsule-scroll',
  imports: [LocalDatePipe, TranslatePipe, Parchment],
  templateUrl: './capsule-scroll.html',
  styleUrl: './capsule-scroll.css',
  host: {
    '[class.open]': 'pinned()',
  },
})
export class CapsuleScroll {
  capsule = input.required<Capsule>();
  canDelete = input(false);

  deleteRequested = output<void>();

  pinned = signal(false);

  openedOn = computed(() => this.capsule().openedAt ?? this.capsule().openAt ?? this.capsule().createdAt);

  toggle(): void {
    this.pinned.update(open => !open);
  }
}
