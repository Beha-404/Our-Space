import { NominatimPlace, placeLabel, toPickedLocation, uniqueByLabel } from './geocoding';

function place(overrides: Partial<NominatimPlace>): NominatimPlace {
  return { lat: '43.3438', lon: '17.8078', display_name: 'Mostar, Hercegovina, Bosna i Hercegovina', ...overrides };
}

describe('placeLabel', () => {
  it('combines the place name with its country', () => {
    expect(placeLabel(place({ name: 'Mostar', address: { country: 'Bosna i Hercegovina' } })))
      .toBe('Mostar, Bosna i Hercegovina');
  });

  it('falls back to the town when the result has no name', () => {
    expect(placeLabel(place({ name: '', address: { town: 'Konjic', country: 'Bosna i Hercegovina' } })))
      .toBe('Konjic, Bosna i Hercegovina');
  });

  it('does not repeat a country that is itself the result', () => {
    expect(placeLabel(place({ name: 'Hrvatska', address: { country: 'Hrvatska' } }))).toBe('Hrvatska');
  });

  it('uses the start of the full address when nothing else is known', () => {
    expect(placeLabel(place({ name: '', address: {} }))).toBe('Mostar, Hercegovina');
  });
});

describe('toPickedLocation', () => {
  it('turns the text coordinates into numbers', () => {
    const picked = toPickedLocation(place({ name: 'Mostar', address: {} }));

    expect(picked).toEqual({ latitude: 43.3438, longitude: 17.8078, name: 'Mostar' });
  });
});

describe('uniqueByLabel', () => {
  it('keeps only the first of places that would look the same in the list', () => {
    const town = place({ name: 'Jajce', address: { country: 'BiH' } });
    const municipality = place({ name: 'Jajce', lat: '44.34', address: { country: 'BiH' } });
    const other = place({ name: 'Konjic', address: { country: 'BiH' } });

    expect(uniqueByLabel([town, municipality, other])).toEqual([town, other]);
  });
});
