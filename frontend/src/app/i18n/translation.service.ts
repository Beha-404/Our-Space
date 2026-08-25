import { Injectable, signal } from '@angular/core';
import { Lang, SUPPORTED_LANGS, translations } from './translations';

const STORAGE_KEY = 'lang';
const DEFAULT_LANG: Lang = 'bs';

@Injectable({ providedIn: 'root' })
export class TranslationService {
  readonly lang = signal<Lang>(this.readStoredLang());

  setLang(lang: Lang): void {
    this.lang.set(lang);
    localStorage.setItem(STORAGE_KEY, lang);
  }

  t(key: string): string {
    return this.lookup(translations[this.lang()], key)
      ?? this.lookup(translations[DEFAULT_LANG], key)
      ?? key;
  }

  private lookup(dict: Record<string, unknown>, key: string): string | undefined {
    const value = key.split('.').reduce<unknown>((acc, part) => {
      if (acc && typeof acc === 'object') {
        return (acc as Record<string, unknown>)[part];
      }
      return undefined;
    }, dict);

    return typeof value === 'string' ? value : undefined;
  }

  private readStoredLang(): Lang {
    const stored = localStorage.getItem(STORAGE_KEY) as Lang | null;
    return stored && SUPPORTED_LANGS.includes(stored) ? stored : DEFAULT_LANG;
  }
}
