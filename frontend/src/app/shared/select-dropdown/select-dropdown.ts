import { Component, computed, ElementRef, inject, input, output, signal } from '@angular/core';

export interface SelectOption {
  value: number | 'all';
  label: string;
}

@Component({
  selector: 'app-select-dropdown',
  standalone: true,
  templateUrl: './select-dropdown.html',
  styleUrl: './select-dropdown.css',
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'open.set(false)',
  },
})
export class SelectDropdown {
  options = input.required<SelectOption[]>();
  value = input.required<number | 'all'>();
  selectionChange = output<number | 'all'>();

  open = signal(false);
  private host = inject(ElementRef<HTMLElement>);

  currentLabel = computed(() => this.options().find(o => o.value === this.value())?.label ?? '');

  toggle(): void {
    this.open.update(v => !v);
  }

  select(value: number | 'all'): void {
    this.open.set(false);
    this.selectionChange.emit(value);
  }

  onDocumentClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}
