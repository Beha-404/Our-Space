import { SKY_CAPACITY, skyDust, skyPoints } from './wish-sky-layout';

describe('skyPoints', () => {
  const ids = Array.from({ length: 60 }, (_, index) => index + 1);

  it('keeps only the newest stars up to the capacity', () => {
    const points = skyPoints(ids);

    expect(points.size).toBe(SKY_CAPACITY);
    expect(points.has(60)).toBe(true);
    expect(points.has(20)).toBe(false);
    expect(points.has(21)).toBe(true);
  });

  it('places every star inside the sky', () => {
    for (const { x, y } of skyPoints(ids).values()) {
      expect(x).toBeGreaterThan(0);
      expect(x).toBeLessThan(100);
      expect(y).toBeGreaterThan(0);
      expect(y).toBeLessThan(100);
    }
  });

  it('never stacks two stars on top of each other', () => {
    const points = [...skyPoints(ids).values()];

    for (let i = 0; i < points.length; i++) {
      for (let j = i + 1; j < points.length; j++) {
        const dx = (points[i].x - points[j].x) * 3;
        const dy = points[i].y - points[j].y;
        expect(Math.hypot(dx, dy)).toBeGreaterThan(4);
      }
    }
  });

  it('is stable: the same wishes always land on the same spots', () => {
    const first = skyPoints([5, 9, 12]);
    const second = skyPoints([12, 5, 9]);

    expect(second.get(9)).toEqual(first.get(9));
  });

  it('adding a wish leaves the earlier stars where they were', () => {
    const before = skyPoints([1, 2, 3]);
    const after = skyPoints([1, 2, 3, 4]);

    expect(after.get(2)).toEqual(before.get(2));
  });
});

describe('skyDust', () => {
  it('returns the requested number of dots with visible opacity', () => {
    const dust = skyDust(30);

    expect(dust).toHaveLength(30);
    expect(dust.every(d => d.opacity > 0 && d.opacity <= 1)).toBe(true);
  });
});
