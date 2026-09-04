import { Component, computed, input, output, signal } from '@angular/core';
import { TranslatePipe } from '../../i18n/translate.pipe';

@Component({
  selector: 'app-lightbox',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="lightbox-backdrop" (click)="closed.emit()">
      <div class="lightbox-bar" (click)="$event.stopPropagation()">
        <span class="lightbox-title">{{ title() }}</span>

        @if (downloadUrl(); as url) {
          <a class="btn btn-ghost btn-sm" [href]="url">{{ 'memories.downloadButton' | translate }}</a>
        }

        <button class="btn btn-ghost btn-sm" (click)="closed.emit()">{{ 'memories.closeButton' | translate }}</button>
      </div>

      <div class="lightbox-stage" (click)="closed.emit()">
        <img [src]="displayedUrl()" [alt]="title()" class="lightbox-image"
          [class.zoomed]="zoomed()"
          (click)="toggleZoom(); $event.stopPropagation()">
      </div>
    </div>
  `,
  styles: [`
    .lightbox-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(5, 6, 14, 0.92);
      backdrop-filter: blur(6px);
      -webkit-backdrop-filter: blur(6px);
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 14px;
      padding: 18px;
      z-index: 200;
    }

    .lightbox-bar {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 100%;
      max-width: 1100px;
    }

    .lightbox-title {
      flex: 1;
      min-width: 0;
      font-family: var(--font-display);
      font-size: 0.95rem;
      color: var(--color-text);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .lightbox-stage {
      flex: 1;
      min-height: 0;
      width: 100%;
      max-width: 1100px;
      display: flex;
      align-items: center;
      justify-content: center;
      overflow: auto;
    }

    .lightbox-image {
      max-width: 100%;
      max-height: 100%;
      object-fit: contain;
      border-radius: var(--radius-md);
      cursor: zoom-in;
    }

    .lightbox-image.zoomed {
      max-width: none;
      max-height: none;
      cursor: zoom-out;
      border-radius: 0;
    }
  `],
  host: { '(document:keydown.escape)': 'closed.emit()' },
})
export class Lightbox {
  imageUrl = input.required<string>();
  previewUrl = input<string | null>(null);
  title = input<string>('');
  downloadUrl = input<string | null>(null);

  closed = output<void>();

  zoomed = signal(false);

  displayedUrl = computed(() =>
    this.zoomed() ? this.imageUrl() : this.previewUrl() ?? this.imageUrl());

  toggleZoom(): void {
    this.zoomed.update(value => !value);
  }
}
