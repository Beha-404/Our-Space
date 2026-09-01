import { Component, ElementRef, computed, input, output, viewChildren } from '@angular/core';

@Component({
  selector: 'app-otp-input',
  standalone: true,
  imports: [],
  templateUrl: './otp-input.html',
  styleUrl: './otp-input.css',
})
export class OtpInput {
  value = input<string>('');
  length = input<number>(6);
  fieldId = input<string | undefined>();
  invalid = input<boolean>(false);
  disabled = input<boolean>(false);

  valueChange = output<string>();
  blurred = output<void>();

  private boxRefs = viewChildren<ElementRef<HTMLInputElement>>('boxRef');

  indices = computed(() => Array.from({ length: this.length() }, (_, i) => i));
  digits = computed(() => {
    const v = this.value();
    return this.indices().map(i => v[i] ?? '');
  });

  onInput(index: number, event: Event): void {
    const el = event.target as HTMLInputElement;
    const digit = el.value.replace(/\D/g, '').slice(-1);

    const current = this.digits().slice();
    current[index] = digit;
    this.valueChange.emit(current.join(''));

    if (digit && index < this.length() - 1) {
      this.focusBox(index + 1);
    }
  }

  onKeydown(index: number, event: KeyboardEvent): void {
    const el = event.target as HTMLInputElement;
    if (event.key === 'Backspace' && !el.value && index > 0) {
      event.preventDefault();
      const current = this.digits().slice();
      current[index - 1] = '';
      this.valueChange.emit(current.join(''));
      this.focusBox(index - 1);
    } else if (event.key === 'ArrowLeft' && index > 0) {
      event.preventDefault();
      this.focusBox(index - 1);
    } else if (event.key === 'ArrowRight' && index < this.length() - 1) {
      event.preventDefault();
      this.focusBox(index + 1);
    }
  }

  onPaste(event: ClipboardEvent): void {
    const pasted = event.clipboardData?.getData('text') ?? '';
    const digits = pasted.replace(/\D/g, '').slice(0, this.length());
    if (!digits) return;
    event.preventDefault();
    this.valueChange.emit(digits);
    this.focusBox(Math.min(digits.length, this.length() - 1));
  }

  onFocus(event: FocusEvent): void {
    (event.target as HTMLInputElement).select();
  }

  onBlur(event: FocusEvent): void {
    const related = event.relatedTarget as HTMLElement | null;
    const stillInside = !!related && this.boxRefs().some(ref => ref.nativeElement === related);
    if (!stillInside) {
      this.blurred.emit();
    }
  }

  private focusBox(index: number): void {
    this.boxRefs()[index]?.nativeElement.focus();
  }
}
