import { Component, ElementRef, computed, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';
import { TranslationService } from '../../i18n/translation.service';

interface DayCell {
  iso: string;
  day: number;
  inMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
}

const MONTH_KEYS = [
  'memories.month1', 'memories.month2', 'memories.month3', 'memories.month4',
  'memories.month5', 'memories.month6', 'memories.month7', 'memories.month8',
  'memories.month9', 'memories.month10', 'memories.month11', 'memories.month12',
];

const WEEKDAY_KEYS = [
  'datePicker.weekday1', 'datePicker.weekday2', 'datePicker.weekday3', 'datePicker.weekday4',
  'datePicker.weekday5', 'datePicker.weekday6', 'datePicker.weekday7',
];

function toIso(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function parseIso(value: string): Date | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return null;
  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
}

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './date-picker.html',
  styleUrl: './date-picker.css',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'open.set(false)',
  },
})
export class DatePicker {
  private i18n = inject(TranslationService);
  private host = inject(ElementRef<HTMLElement>);

  value = input<string>('');
  fieldId = input<string | undefined>();

  valueChange = output<string>();

  open = signal(false);
  private viewYear = signal(new Date().getFullYear());
  private viewMonth = signal(new Date().getMonth());

  displayLabel = computed(() => {
    const parsed = parseIso(this.value());
    if (!parsed) return '';
    const dd = String(parsed.getDate()).padStart(2, '0');
    const mm = String(parsed.getMonth() + 1).padStart(2, '0');
    return `${dd}.${mm}.${parsed.getFullYear()}.`;
  });

  monthLabel = computed(() => `${this.i18n.t(MONTH_KEYS[this.viewMonth()])} ${this.viewYear()}`);

  weekdayLabels = computed(() => WEEKDAY_KEYS.map(key => this.i18n.t(key)));

  weeks = computed<DayCell[][]>(() => {
    const year = this.viewYear();
    const month = this.viewMonth();
    const selectedIso = this.value();
    const todayIso = toIso(new Date());

    const firstOfMonth = new Date(year, month, 1);
    const mondayOffset = (firstOfMonth.getDay() + 6) % 7;
    const gridStart = new Date(year, month, 1 - mondayOffset);

    const cells: DayCell[] = Array.from({ length: 42 }, (_, i) => {
      const date = new Date(gridStart);
      date.setDate(date.getDate() + i);
      const iso = toIso(date);
      return {
        iso,
        day: date.getDate(),
        inMonth: date.getMonth() === month,
        isToday: iso === todayIso,
        isSelected: iso === selectedIso,
      };
    });

    return Array.from({ length: 6 }, (_, i) => cells.slice(i * 7, i * 7 + 7));
  });

  toggle(): void {
    if (this.open()) {
      this.open.set(false);
      return;
    }

    const base = parseIso(this.value()) ?? new Date();
    this.viewYear.set(base.getFullYear());
    this.viewMonth.set(base.getMonth());
    this.open.set(true);
  }

  prevMonth(): void {
    this.shiftMonth(-1);
  }

  nextMonth(): void {
    this.shiftMonth(1);
  }

  private shiftMonth(delta: number): void {
    const next = new Date(this.viewYear(), this.viewMonth() + delta, 1);
    this.viewYear.set(next.getFullYear());
    this.viewMonth.set(next.getMonth());
  }

  selectDay(cell: DayCell): void {
    if (!cell.inMonth) {
      const date = parseIso(cell.iso)!;
      this.viewYear.set(date.getFullYear());
      this.viewMonth.set(date.getMonth());
    }
    this.valueChange.emit(cell.iso);
    this.open.set(false);
  }

  selectToday(): void {
    const today = new Date();
    this.viewYear.set(today.getFullYear());
    this.viewMonth.set(today.getMonth());
    this.valueChange.emit(toIso(today));
    this.open.set(false);
  }

  clear(): void {
    this.valueChange.emit('');
    this.open.set(false);
  }

  onDocumentClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}
