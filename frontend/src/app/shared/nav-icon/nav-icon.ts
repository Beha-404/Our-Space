import { Component, input } from '@angular/core';

export type NavIconName = 'home' | 'memories' | 'map' | 'events' | 'wishlist' | 'capsules';

@Component({
  selector: 'app-nav-icon',
  templateUrl: './nav-icon.html',
  styles: `:host { display: inline-flex; } svg { width: 100%; height: 100%; }`,
  host: { 'aria-hidden': 'true' },
})
export class NavIcon {
  name = input.required<NavIconName>();
}
