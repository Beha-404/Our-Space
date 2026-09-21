import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { pageNumbers } from '../page-numbers';

@Component({
  selector: 'app-pager',
  imports: [TranslatePipe],
  templateUrl: './pager.html',
  styleUrl: './pager.css',
})
export class Pager {
  page = input.required<number>();
  totalPages = input.required<number>();

  pageChange = output<number>();

  slots = computed(() => pageNumbers(this.totalPages(), this.page()));

  go(target: number): void {
    const clamped = Math.min(Math.max(target, 1), this.totalPages());
    if (clamped !== this.page()) this.pageChange.emit(clamped);
  }
}
