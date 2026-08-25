import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageSwitcher } from '../i18n/language-switcher/language-switcher';
import { TranslatePipe } from '../i18n/translate.pipe';

@Component({
  imports: [RouterLink, TranslatePipe, LanguageSwitcher],
  selector: 'app-landing-page',
  styleUrl: './landing-page.css',
  templateUrl: './landing-page.html',
})
export class LandingPage {
}
