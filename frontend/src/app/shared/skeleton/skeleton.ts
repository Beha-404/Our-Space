import { Component, computed, input } from '@angular/core';

type SkeletonVariant = 'post' | 'row' | 'block';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  template: `
    <div class="skeleton-group" aria-busy="true" [attr.aria-label]="label()">
      @for (i of slots(); track i) {
        @switch (variant()) {
          @case ('post') {
            <div class="card skeleton-card">
              <div class="skeleton-head">
                <span class="sk sk-avatar"></span>
                <span class="skeleton-head-text">
                  <span class="sk sk-line sk-line-title"></span>
                  <span class="sk sk-line sk-line-meta"></span>
                </span>
              </div>
              <span class="sk sk-media"></span>
            </div>
          }
          @case ('row') {
            <div class="card skeleton-card skeleton-row">
              <span class="sk sk-badge"></span>
              <span class="skeleton-head-text">
                <span class="sk sk-line sk-line-title"></span>
                <span class="sk sk-line sk-line-meta"></span>
              </span>
            </div>
          }
          @default {
            <div class="card skeleton-card">
              <span class="sk sk-line sk-line-title"></span>
              <span class="sk sk-line sk-line-meta"></span>
            </div>
          }
        }
      }
    </div>
  `,
  styles: [`
    .skeleton-group {
      display: flex;
      flex-direction: column;
      gap: 18px;
    }

    .skeleton-card {
      padding: 16px 18px;
      display: flex;
      flex-direction: column;
      gap: 14px;
    }

    .skeleton-row {
      flex-direction: row;
      align-items: center;
      gap: 12px;
    }

    .skeleton-head {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .skeleton-head-text {
      display: flex;
      flex-direction: column;
      gap: 8px;
      flex: 1;
      min-width: 0;
    }

    .sk {
      display: block;
      border-radius: var(--radius-sm);
      background: linear-gradient(
        90deg,
        var(--color-surface) 25%,
        var(--color-surface-strong) 37%,
        var(--color-surface) 63%
      );
      background-size: 400% 100%;
      animation: skShimmer 1.4s ease infinite;
    }

    @keyframes skShimmer {
      from { background-position: 100% 50%; }
      to { background-position: 0 50%; }
    }

    .sk-avatar,
    .sk-badge {
      width: 34px;
      height: 34px;
      border-radius: 50%;
      flex: none;
    }

    .sk-line {
      height: 11px;
    }

    .sk-line-title {
      width: 58%;
    }

    .sk-line-meta {
      width: 34%;
      height: 9px;
    }

    .sk-media {
      width: 100%;
      height: 190px;
      border-radius: var(--radius-md);
    }

    @media (prefers-reduced-motion: reduce) {
      .sk {
        animation: none;
      }
    }
  `],
})
export class Skeleton {
  count = input<number>(3);
  variant = input<SkeletonVariant>('post');
  label = input<string>('Loading');

  slots = computed(() => Array.from({ length: this.count() }, (_, i) => i));
}
