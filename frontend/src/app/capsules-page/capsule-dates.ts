export type OpenPreset = 'month' | 'sixMonths' | 'year' | 'fiveYears' | 'custom' | 'anytime';

const PRESET_MONTHS: Record<'month' | 'sixMonths' | 'year' | 'fiveYears', number> = {
  month: 1,
  sixMonths: 6,
  year: 12,
  fiveYears: 60,
};

export function toIsoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function addMonthsClamped(from: Date, months: number): Date {
  const target = new Date(from.getFullYear(), from.getMonth() + months, 1);
  const lastDay = new Date(target.getFullYear(), target.getMonth() + 1, 0).getDate();
  target.setDate(Math.min(from.getDate(), lastDay));
  return target;
}

export function presetDate(preset: OpenPreset, from: Date = new Date()): string | null {
  if (preset === 'custom' || preset === 'anytime') return null;
  return toIsoDate(addMonthsClamped(from, PRESET_MONTHS[preset]));
}

export function daysUntil(date: string, from: Date = new Date()): number {
  const diffMs = new Date(date).setHours(0, 0, 0, 0) - new Date(from).setHours(0, 0, 0, 0);
  return Math.round(diffMs / (1000 * 60 * 60 * 24));
}

export function capsuleOpenedOn(capsule: { openedAt: string | null; openAt: string | null; createdAt: string }): string {
  return capsule.openedAt ?? capsule.openAt ?? capsule.createdAt;
}
