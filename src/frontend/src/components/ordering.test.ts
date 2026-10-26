import { describe, expect, it } from 'vitest';
import type { OrderingStepDto } from '../api/types';
import { moveItem, shuffleSteps } from './ordering';

const steps: OrderingStepDto[] = ['a', 'b', 'c', 'd', 'e'].map((id) => ({
  id,
  label: `Step ${id.toUpperCase()}`,
}));

const ids = (items: OrderingStepDto[]) => items.map((item) => item.id);

describe('shuffleSteps', () => {
  it('is deterministic for the same card', () => {
    expect(ids(shuffleSteps(steps, 'card-42'))).toEqual(ids(shuffleSteps(steps, 'card-42')));
  });

  it('never shows the stored order, even when the shuffle lands on it', () => {
    for (let index = 0; index < 250; index += 1) {
      const shuffled = ids(shuffleSteps(steps, `card-${index}`));

      expect(shuffled).toHaveLength(steps.length);
      expect(shuffled).not.toEqual(ids(steps));
      expect([...shuffled].sort()).toEqual(ids(steps).sort());
    }
  });

  it('leaves a single step alone', () => {
    expect(ids(shuffleSteps([steps[0]], 'single'))).toEqual(['a']);
  });
});

describe('moveItem', () => {
  it('moves an item down and up', () => {
    expect(moveItem(['a', 'b', 'c'], 0, 2)).toEqual(['b', 'c', 'a']);
    expect(moveItem(['a', 'b', 'c'], 2, 0)).toEqual(['c', 'a', 'b']);
  });

  it('returns the same list for a no-op or an out-of-range move', () => {
    const items = ['a', 'b', 'c'];

    expect(moveItem(items, 1, 1)).toBe(items);
    expect(moveItem(items, -1, 2)).toBe(items);
    expect(moveItem(items, 0, 3)).toBe(items);
  });
});
