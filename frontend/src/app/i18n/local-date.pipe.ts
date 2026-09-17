import { formatDate, registerLocaleData } from '@angular/common';
import localeBs from '@angular/common/locales/bs';
import localeEs from '@angular/common/locales/es';
import { inject, Pipe, PipeTransform } from '@angular/core';
import { Lang } from './translations';
import { TranslationService } from './translation.service';

registerLocaleData(localeBs);
registerLocaleData(localeEs);

const LOCALES: Record<Lang, string> = {
  bs: 'bs',
  en: 'en-US',
  es: 'es',
};

@Pipe({
  name: 'localDate',
  standalone: true,
  pure: false,
})
export class LocalDatePipe implements PipeTransform {
  private i18n = inject(TranslationService);

  transform(value: string | Date | null | undefined, format = 'mediumDate'): string | null {
    if (value === null || value === undefined || value === '') return null;
    return formatDate(value, format, LOCALES[this.i18n.lang()]);
  }
}
