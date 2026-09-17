export type PageSlot = number | '…';

const SHOW_ALL_UP_TO = 7;

export function pageNumbers(total: number, current: number): PageSlot[] {
  if (total <= SHOW_ALL_UP_TO) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const keep = new Set<number>([1, total, current - 1, current, current + 1]);
  const sorted = [...keep].filter(p => p >= 1 && p <= total).sort((a, b) => a - b);

  const result: PageSlot[] = [];
  let previous = 0;
  for (const page of sorted) {
    if (previous && page - previous > 1) result.push('…');
    result.push(page);
    previous = page;
  }
  return result;
}
