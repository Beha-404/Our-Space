import { Component, inject } from '@angular/core';
import { LANG_LABELS, Lang, SUPPORTED_LANGS } from '../translations';
import { TranslationService } from '../translation.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  template: `
    <div class="lang-switcher">
      @for (lang of langs; track lang) {
        <button
          type="button"
          class="lang-btn"
          [class.active]="i18n.lang() === lang"
          (click)="i18n.setLang(lang)">
          {{ labels[lang] }}
        </button>
      }
    </div>
  `,
  styles: [`
    .lang-switcher {
      display: inline-flex;
      gap: 2px;
      padding: 3px;
      border-radius: 100px;
      background: var(--color-surface-strong);
      border: 1px solid var(--color-border);
    }
    .lang-btn {
      border: none;
      background: transparent;
      color: var(--color-text-muted);
      font-size: 0.72rem;
      font-weight: 600;
      padding: 5px 9px;
      border-radius: 100px;
      cursor: pointer;
      transition: background 0.15s ease, color 0.15s ease;
    }
    .lang-btn:hover {
      color: var(--color-text);
    }
    .lang-btn.active {
      background: var(--gradient-primary);
      color: #0a0c18;
    }
  `],
})
export class LanguageSwitcher {
  i18n = inject(TranslationService);
  langs = SUPPORTED_LANGS;
  labels = LANG_LABELS;
}
