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

  readonly width = 900;
  readonly height = 160;
  readonly photoLaneY = 55;
  readonly audioLaneY = 115;
  private readonly padding = 30;

  hoveredItem = signal<TimelineItem | null>(null);
  hoverPos = signal<{ x: number; y: number }>({ x: 0, y: 0 });

  private minDate = computed(() => Math.min(...this.items().map(i => new Date(i.date).getTime())));
  private maxDate = computed(() => Math.max(...this.items().map(i => new Date(i.date).getTime())));

  xFor(dateStr: string): number {
    const t = new Date(dateStr).getTime();
    const min = this.minDate();
    const max = this.maxDate();

    if (!isFinite(min) || !isFinite(max) || min === max) {
      return this.width / 2;
    }

    return this.padding + ((t - min) / (max - min)) * (this.width - this.padding * 2);
  }

  laneYFor(item: TimelineItem): number {
    return item.type === 'photo' ? this.photoLaneY : this.audioLaneY;
  }

  onHover(item: TimelineItem, event: MouseEvent): void {
    const target = event.currentTarget as SVGElement;
    const wrap = target.closest('.timeline-wrap') as HTMLElement | null;
    if (!wrap) return;

    const rect = wrap.getBoundingClientRect();
    this.hoverPos.set({ x: event.clientX - rect.left, y: event.clientY - rect.top });
    this.hoveredItem.set(item);
  }

  onLeave(): void {
    this.hoveredItem.set(null);
  }
}
