import { TestBed } from '@angular/core/testing';
import { LocalDatePipe } from './local-date.pipe';
import { TranslationService } from './translation.service';

describe('LocalDatePipe', () => {
  let pipe: LocalDatePipe;
  let i18n: TranslationService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [LocalDatePipe] });
    pipe = TestBed.inject(LocalDatePipe);
    i18n = TestBed.inject(TranslationService);
  });

  it('uses Bosnian month names when the app is in Bosnian', () => {
    i18n.setLang('bs');
    expect(pipe.transform('2026-10-20T12:00:00', 'longDate')).toBe('20. oktobar 2026.');
    expect(pipe.transform('2026-10-20T12:00:00', 'MMM')).toBe('okt');
  });

  it('follows a language change without a reload', () => {
    i18n.setLang('en');
    expect(pipe.transform('2026-10-20T12:00:00', 'longDate')).toBe('October 20, 2026');

    i18n.setLang('es');
    expect(pipe.transform('2026-10-20T12:00:00', 'longDate')).toBe('20 de octubre de 2026');
  });

  it('returns null for a missing date', () => {
    expect(pipe.transform(null)).toBeNull();
    expect(pipe.transform(undefined)).toBeNull();
  });
});
