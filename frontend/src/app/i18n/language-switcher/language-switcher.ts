import { Component, ElementRef, inject, signal } from '@angular/core';
import { LANG_LABELS, Lang, SUPPORTED_LANGS } from '../translations';
import { TranslationService } from '../translation.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  template: `
    <div class="lang-switcher">
      <button type="button" class="lang-trigger" [class.open]="open()"
        (click)="toggle()" aria-haspopup="listbox" [attr.aria-expanded]="open()" aria-label="Language">
        <span class="lang-globe">🌐</span>
        <span class="lang-current">{{ labels[i18n.lang()] }}</span>
        <span class="lang-caret"></span>
      </button>

      @if (open()) {
        <ul class="lang-menu" role="listbox">
          @for (lang of langs; track lang) {
            <li>
              <button type="button" class="lang-option" role="option"
                [class.selected]="lang === i18n.lang()"
                [attr.aria-selected]="lang === i18n.lang()"
                (click)="selectLang(lang)">
                <span>{{ labels[lang] }}</span>
                @if (lang === i18n.lang()) {
                  <span class="lang-check">✓</span>
                }
              </button>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .lang-switcher {
      position: relative;
      display: inline-block;
    }

    .lang-trigger {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      font-family: var(--font-body);
      font-size: 0.82rem;
      font-weight: 600;
      color: var(--color-text);
      background: var(--color-surface-strong);
      border: 1px solid var(--color-border);
      border-radius: 100px;
      padding: 8px 14px;
      cursor: pointer;
      outline: none;
      transition: border-color 0.15s ease, background 0.15s ease;
    }

    .lang-trigger:hover,
    .lang-trigger.open {
      border-color: var(--color-border-strong);
      background: var(--color-surface);
    }

    .lang-trigger:focus-visible {
      border-color: var(--color-purple);
      box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.18);
    }

    .lang-globe {
      font-size: 0.9rem;
      line-height: 1;
    }

    .lang-caret {
      width: 0;
      height: 0;
      border-left: 4px solid transparent;
      border-right: 4px solid transparent;
      border-top: 5px solid var(--color-text-muted);
      transition: transform 0.18s ease;
    }

    .lang-trigger.open .lang-caret {
      transform: rotate(180deg);
    }

    .lang-menu {
      position: absolute;
      top: calc(100% + 8px);
      right: 0;
      min-width: 160px;
      margin: 0;
      padding: 6px;
      list-style: none;
      background: var(--color-bg-2);
      border: 1px solid var(--color-border-strong);
      border-radius: var(--radius-md);
      box-shadow: var(--shadow-card);
      z-index: 50;
      animation: langMenuIn 0.14s ease;
    }

    @keyframes langMenuIn {
      from { opacity: 0; transform: translateY(-4px); }
      to { opacity: 1; transform: none; }
    }

    .lang-option {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      width: 100%;
      font-family: var(--font-body);
      font-size: 0.85rem;
      font-weight: 500;
      text-align: left;
      color: var(--color-text-muted);
      background: none;
      border: none;
      border-radius: var(--radius-sm);
      padding: 9px 12px;
      cursor: pointer;
      transition: background 0.12s ease, color 0.12s ease;
    }

    .lang-option:hover {
      background: var(--color-surface-strong);
      color: var(--color-text);
    }

    .lang-option.selected {
      color: var(--color-text);
      font-weight: 600;
    }

    .lang-check {
      color: var(--color-purple);
      font-size: 0.8rem;
    }

    @media (max-width: 560px) {
      .lang-current {
        display: none;
      }
    }
  `],
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'open.set(false)',
  },
})
export class LanguageSwitcher {
  i18n = inject(TranslationService);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private host = inject(ElementRef<HTMLElement>);

  langs = SUPPORTED_LANGS;
  labels = LANG_LABELS;
  open = signal(false);

  toggle(): void {
    this.open.update(value => !value);
  }

  onDocumentClick(event: MouseEvent): void {
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  selectLang(lang: Lang): void {
    this.open.set(false);
    this.i18n.setLang(lang);

    if (this.authService.isLoggedIn()) {
      this.userService.updateLanguage(lang).subscribe({ error: () => {} });
    }
  }
}
