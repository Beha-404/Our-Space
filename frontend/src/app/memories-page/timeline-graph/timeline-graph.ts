import { DatePipe } from '@angular/common';
import { Component, computed, input, signal } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';

export interface TimelineItem {
  id: number;
  type: 'photo' | 'audio';
  date: string;
  caption: string | null;
  thumbnailUrl?: string;
}

@Component({
  selector: 'app-timeline-graph',
  standalone: true,
  imports: [DatePipe, TranslatePipe],
  templateUrl: './timeline-graph.html',
  styleUrl: './timeline-graph.css',
})
export class TimelineGraph {
  items = input.required<TimelineItem[]>();

  hoveredItem = signal<TimelineItem | null>(null);
  hoverPos = signal<{ x: number; y: number }>({ x: 0, y: 0 });

  private minDate = computed(() => Math.min(...this.items().map(i => new Date(i.date).getTime())));
  private maxDate = computed(() => Math.max(...this.items().map(i => new Date(i.date).getTime())));

  percentFor(dateStr: string): number {
    const time = new Date(dateStr).getTime();
    const min = this.minDate();
    const max = this.maxDate();

    if (!isFinite(min) || !isFinite(max) || min === max) {
      return 50;
    }

    return ((time - min) / (max - min)) * 100;
  }

  onHover(item: TimelineItem, event: Event): void {
    const dot = event.currentTarget as HTMLElement;
    const wrap = dot.closest('.timeline-wrap') as HTMLElement | null;
    if (!wrap) return;

    const wrapRect = wrap.getBoundingClientRect();
    const dotRect = dot.getBoundingClientRect();

    this.hoverPos.set({
      x: dotRect.left + dotRect.width / 2 - wrapRect.left,
      y: dotRect.top - wrapRect.top,
    });

    this.hoveredItem.set(item);
  }

  onLeave(): void {
    this.hoveredItem.set(null);
  }
}
