import { Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageSwitcher } from '../i18n/language-switcher/language-switcher';
import { TranslatePipe } from '../i18n/translate.pipe';
import { Lightbox } from '../shared/lightbox/lightbox';

@Component({
  imports: [RouterLink, TranslatePipe, LanguageSwitcher, Lightbox],
  selector: 'app-landing-page',
  styleUrl: './landing-page.css',
  templateUrl: './landing-page.html',
})
export class LandingPage {
  readonly previewImageUrl = '/preview/couple.jpg';

  previewOpen = signal(false);
}
