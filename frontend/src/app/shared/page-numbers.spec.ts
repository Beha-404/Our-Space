import { pageNumbers } from './page-numbers';

describe('pageNumbers', () => {
  it('lists every page when there are only a few', () => {
    expect(pageNumbers(4, 2)).toEqual([1, 2, 3, 4]);
  });

  it('returns nothing when there are no pages', () => {
    expect(pageNumbers(0, 1)).toEqual([]);
  });

  it('keeps the first, last and neighbouring pages with gaps between', () => {
    expect(pageNumbers(20, 10)).toEqual([1, '…', 9, 10, 11, '…', 20]);
  });

  it('has no gap when the current page is next to an edge', () => {
    expect(pageNumbers(20, 2)).toEqual([1, 2, 3, '…', 20]);
  });
});
