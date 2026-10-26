import type { OrderingStepDto } from '../api/types';

/** Stable 32-bit FNV-1a hash so a card always shuffles the same way. */
export function hashSeed(value: string): number {
  let hash = 2166136261;

  for (let index = 0; index < value.length; index += 1) {
    hash ^= value.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }

  return hash >>> 0;
}

function mulberry32(seed: number): () => number {
  let state = seed;

  return () => {
    state = (state + 0x6d2b79f5) | 0;
    let value = Math.imul(state ^ (state >>> 15), 1 | state);
    value = (value + Math.imul(value ^ (value >>> 7), 61 | value)) ^ value;
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * Shuffles the steps deterministically from the card id. The stored order is the answer,
 * so an unlucky shuffle that happened to match it is rotated one place to keep it hidden.
 */
export function shuffleSteps(steps: OrderingStepDto[], cardId: string): OrderingStepDto[] {
  const shuffled = [...steps];
  const random = mulberry32(hashSeed(cardId));

  for (let index = shuffled.length - 1; index > 0; index -= 1) {
    const swapWith = Math.floor(random() * (index + 1));
    [shuffled[index], shuffled[swapWith]] = [shuffled[swapWith], shuffled[index]];
  }

  const isStoredOrder = shuffled.every((step, index) => step.id === steps[index]?.id);

  if (shuffled.length > 1 && isStoredOrder) {
    return [...shuffled.slice(1), shuffled[0]];
  }

  return shuffled;
}

/** Moves one item, used by the up/down controls and by drag and drop. */
export function moveItem<T>(items: T[], from: number, to: number): T[] {
  if (from === to || from < 0 || to < 0 || from >= items.length || to >= items.length) {
    return items;
  }

  const next = [...items];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item);

  return next;
}
