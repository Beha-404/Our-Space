import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TranslationService } from '../i18n/translation.service';

export const langInterceptor: HttpInterceptorFn = (req, next) => {
  const i18n = inject(TranslationService);

  return next(req.clone({
    setHeaders: { 'Accept-Language': i18n.lang() }
  }));
};
