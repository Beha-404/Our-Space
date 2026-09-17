export type PluralForm = 'One' | 'Few' | 'Many';

export function pluralForm(count: number, lang: string): PluralForm {
  const abs = Math.abs(count);

  if (lang !== 'bs') return abs === 1 ? 'One' : 'Many';

  const mod10 = abs % 10;
  const mod100 = abs % 100;

  if (mod10 === 1 && mod100 !== 11) return 'One';
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return 'Few';
  return 'Many';
}

export function pluralKey(baseKey: string, count: number, lang: string): string {
  return baseKey + pluralForm(count, lang);
}
