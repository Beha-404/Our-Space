import { Component, inject } from '@angular/core';
import { LANG_LABELS, Lang, SUPPORTED_LANGS } from '../translations';
import { TranslationService } from '../translation.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  template: `
    <select class="lang-select" [value]="i18n.lang()" (change)="selectLang($any($event.target).value)" aria-label="Language">
      @for (lang of langs; track lang) {
        <option [value]="lang">{{ labels[lang] }}</option>
      }
    </select>
  `,
  styles: [`
    .lang-select {
      font-family: var(--font-body);
      font-size: 0.82rem;
      font-weight: 600;
      color: var(--color-text);
      background: var(--color-surface-strong);
      border: 1px solid var(--color-border);
      border-radius: 100px;
      padding: 7px 12px;
      cursor: pointer;
      outline: none;
      transition: border-color 0.15s ease, background 0.15s ease;
    }
    .lang-select:hover {
      border-color: var(--color-border-strong);
    }
    .lang-select:focus {
      border-color: var(--color-purple);
      box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.18);
    }
    .lang-select option {
      background: var(--color-bg-2);
      color: var(--color-text);
    }
  `],
})
export class LanguageSwitcher {
  i18n = inject(TranslationService);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  langs = SUPPORTED_LANGS;
  labels = LANG_LABELS;

  selectLang(lang: Lang): void {
    this.i18n.setLang(lang);
    if (this.authService.isLoggedIn()) {
      this.userService.updateLanguage(lang).subscribe({ error: () => {} });
    }
  }
}
