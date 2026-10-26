import { describe, expect, it } from 'vitest';
import { clampRect, MIN_FRAME_SIZE, moveRect, rectStyle, resizeRect } from './framing';

const near = (actual: number, expected: number) => expect(actual).toBeCloseTo(expected, 6);

describe('clampRect', () => {
  it('pulls a rectangle that runs off the right and bottom back inside', () => {
    const rect = clampRect({ x: 0.8, y: 0.9, width: 0.4, height: 0.3 });

    near(rect.x, 0.6);
    near(rect.y, 0.7);
    near(rect.width, 0.4);
    near(rect.height, 0.3);
  });

  it('enforces the minimum size', () => {
    const rect = clampRect({ x: 0.5, y: 0.5, width: 0.001, height: 0.001 });

    near(rect.width, MIN_FRAME_SIZE);
    near(rect.height, MIN_FRAME_SIZE);
  });
});

describe('moveRect', () => {
  it('moves inside the frame', () => {
    const rect = moveRect({ x: 0.2, y: 0.2, width: 0.4, height: 0.4 }, 0.1, 0.05);

    near(rect.x, 0.3);
    near(rect.y, 0.25);
  });

  it('stops at the edge instead of leaving the photograph', () => {
    const rect = moveRect({ x: 0.5, y: 0.5, width: 0.3, height: 0.3 }, 0.9, 0.9);

    near(rect.x, 0.7);
    near(rect.y, 0.7);
  });
});

describe('resizeRect', () => {
  it('keeps the opposite corner fixed when dragging north-west', () => {
    const rect = resizeRect({ x: 0.4, y: 0.4, width: 0.2, height: 0.2 }, 'nw', -0.1, -0.1);

    near(rect.x, 0.3);
    near(rect.y, 0.3);
    near(rect.width, 0.3);
    near(rect.height, 0.3);
  });

  it('never shrinks below the minimum', () => {
    const rect = resizeRect({ x: 0.4, y: 0.4, width: 0.2, height: 0.2 }, 'se', -1, -1);

    near(rect.width, MIN_FRAME_SIZE);
    near(rect.height, MIN_FRAME_SIZE);
  });

  it('does not run past the edge when dragged outwards', () => {
    const rect = resizeRect({ x: 0.7, y: 0.7, width: 0.2, height: 0.2 }, 'se', 0.5, 0.5);

    near(rect.x + rect.width, 1);
    near(rect.y + rect.height, 1);
  });
});

describe('rectStyle', () => {
  it('turns normalised values into percentages', () => {
    expect(rectStyle({ x: 0.25, y: 0.5, width: 0.5, height: 0.25 })).toEqual({
      left: '25%',
      top: '50%',
      width: '50%',
      height: '25%',
    });
  });
});

