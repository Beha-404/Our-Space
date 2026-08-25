import { Component, computed, input } from '@angular/core';
import { config } from '../../config';

@Component({
  selector: 'app-avatar',
  standalone: true,
  template: `
    @if (pictureUrl()) {
      <img class="avatar" [src]="fullUrl()" [alt]="name()"
        [style.width.px]="size()" [style.height.px]="size()">
    } @else {
      <div class="avatar" [style.width.px]="size()" [style.height.px]="size()" [style.fontSize.px]="size() * 0.42">
        {{ initials() }}
      </div>
    }
  `,
})
export class Avatar {
  name = input.required<string>();
  pictureUrl = input<string | null>(null);
  size = input<number>(36);

  initials = computed(() => this.name().charAt(0).toUpperCase());
  fullUrl = computed(() => `${config.mediaUrl}${this.pictureUrl()}`);
}
