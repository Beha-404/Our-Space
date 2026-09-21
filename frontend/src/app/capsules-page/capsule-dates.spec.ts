import { addMonthsClamped, daysUntil, presetDate, toIsoDate } from './capsule-dates';

describe('capsule dates', () => {
  it('formats a local date without shifting the day', () => {
    expect(toIsoDate(new Date(2027, 0, 5))).toBe('2027-01-05');
  });

  it('adds whole months', () => {
    expect(toIsoDate(addMonthsClamped(new Date(2026, 8, 21), 12))).toBe('2027-09-21');
    expect(toIsoDate(addMonthsClamped(new Date(2026, 8, 21), 60))).toBe('2031-09-21');
  });

  it('clamps to the last day of a shorter month', () => {
    expect(toIsoDate(addMonthsClamped(new Date(2026, 0, 31), 1))).toBe('2026-02-28');
    expect(toIsoDate(addMonthsClamped(new Date(2027, 7, 31), 6))).toBe('2028-02-29');
  });

  it('turns each preset into a date and leaves custom and anytime empty', () => {
    const from = new Date(2026, 8, 21);

    expect(presetDate('month', from)).toBe('2026-10-21');
    expect(presetDate('sixMonths', from)).toBe('2027-03-21');
    expect(presetDate('year', from)).toBe('2027-09-21');
    expect(presetDate('fiveYears', from)).toBe('2031-09-21');
    expect(presetDate('custom', from)).toBeNull();
    expect(presetDate('anytime', from)).toBeNull();
  });

  it('counts calendar days until a date', () => {
    const from = new Date(2026, 8, 21, 23, 30);

    expect(daysUntil('2026-09-21', from)).toBe(0);
    expect(daysUntil('2026-09-22', from)).toBe(1);
    expect(daysUntil('2027-09-21', from)).toBe(365);
  });
});
