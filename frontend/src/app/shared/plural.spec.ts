import { pluralForm, pluralKey } from './plural';

describe('pluralForm', () => {
  it('follows the Bosnian one/few/many rule, including the teens', () => {
    expect(pluralForm(1, 'bs')).toBe('One');
    expect(pluralForm(2, 'bs')).toBe('Few');
    expect(pluralForm(4, 'bs')).toBe('Few');
    expect(pluralForm(5, 'bs')).toBe('Many');
    expect(pluralForm(11, 'bs')).toBe('Many');
    expect(pluralForm(12, 'bs')).toBe('Many');
    expect(pluralForm(14, 'bs')).toBe('Many');
    expect(pluralForm(21, 'bs')).toBe('One');
    expect(pluralForm(22, 'bs')).toBe('Few');
    expect(pluralForm(25, 'bs')).toBe('Many');
    expect(pluralForm(111, 'bs')).toBe('Many');
    expect(pluralForm(112, 'bs')).toBe('Many');
    expect(pluralForm(0, 'bs')).toBe('Many');
  });

  it('treats only exactly one as singular in English and Spanish', () => {
    expect(pluralForm(1, 'en')).toBe('One');
    expect(pluralForm(21, 'en')).toBe('Many');
    expect(pluralForm(2, 'en')).toBe('Many');
    expect(pluralForm(0, 'es')).toBe('Many');
    expect(pluralForm(1, 'es')).toBe('One');
  });

  it('builds the translation key from the base key and the form', () => {
    expect(pluralKey('home.yearAgo', 3, 'bs')).toBe('home.yearAgoFew');
    expect(pluralKey('home.yearAgo', 3, 'en')).toBe('home.yearAgoMany');
  });
});
