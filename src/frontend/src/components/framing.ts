import type { RectDto } from '../api/types';

export type Handle = 'nw' | 'ne' | 'sw' | 'se' | 'n' | 's' | 'e' | 'w';

export const MIN_FRAME_SIZE = 0.06;

const clamp = (value: number, min: number, max: number) => Math.min(max, Math.max(min, value));

/** Keeps a normalised rectangle inside the photograph and above the minimum size. */
export function clampRect(rect: RectDto): RectDto {
  const width = clamp(rect.width, MIN_FRAME_SIZE, 1);
  const height = clamp(rect.height, MIN_FRAME_SIZE, 1);
  const x = clamp(rect.x, 0, 1 - width);
  const y = clamp(rect.y, 0, 1 - height);

  return { x, y, width, height };
}

export function moveRect(rect: RectDto, dx: number, dy: number): RectDto {
  return clampRect({ ...rect, x: rect.x + dx, y: rect.y + dy });
}

/**
 * Resizes from one edge or corner. The opposite edge stays put, and the minimum size
 * is enforced in the direction being dragged.
 */
export function resizeRect(rect: RectDto, handle: Handle, dx: number, dy: number): RectDto {
  let left = rect.x;
  let top = rect.y;
  let right = rect.x + rect.width;
  let bottom = rect.y + rect.height;

  if (handle.includes('w')) {
    left = clamp(left + dx, 0, right - MIN_FRAME_SIZE);
  }
  if (handle.includes('e')) {
    right = clamp(right + dx, left + MIN_FRAME_SIZE, 1);
  }
  if (handle.includes('n')) {
    top = clamp(top + dy, 0, bottom - MIN_FRAME_SIZE);
  }
  if (handle.includes('s')) {
    bottom = clamp(bottom + dy, top + MIN_FRAME_SIZE, 1);
  }

  return { x: left, y: top, width: right - left, height: bottom - top };
}

/** CSS percentages for an absolutely positioned overlay. */
export function rectStyle(rect: RectDto) {
  return {
    left: `${rect.x * 100}%`,
    top: `${rect.y * 100}%`,
    width: `${rect.width * 100}%`,
    height: `${rect.height * 100}%`,
  };
}
