import { Component, computed, input, output } from '@angular/core';
import { LocalDatePipe } from '../../i18n/local-date.pipe';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { Capsule } from '../../interfaces/capsule';
import { capsuleOpenedOn } from '../capsule-dates';

@Component({
  selector: 'app-capsule-scroll',
  imports: [LocalDatePipe, TranslatePipe],
  templateUrl: './capsule-scroll.html',
  styleUrl: './capsule-scroll.css',
})
export class CapsuleScroll {
  capsule = input.required<Capsule>();
  canDelete = input(false);

  deleteRequested = output<void>();

  openedOn = computed(() => capsuleOpenedOn(this.capsule()));
}
