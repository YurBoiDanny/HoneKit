import { useRef } from 'react';
import type { PointerEvent as ReactPointerEvent, KeyboardEvent as ReactKeyboardEvent } from 'react';
import type { RectDto } from '../api/types';
import { moveRect, rectStyle, resizeRect, type Handle } from './framing';

type FramingCanvasProps = {
  imageRef: string;
  rect: RectDto;
  onChange: (rect: RectDto) => void;
  disabled: boolean;
  target?: RectDto | null;
};

type Drag = {
  mode: 'move' | Handle;
  startX: number;
  startY: number;
  rect: RectDto;
  pointerId: number;
};

const corners: Handle[] = ['nw', 'ne', 'sw', 'se'];

export function FramingCanvas({ imageRef, rect, onChange, disabled, target }: FramingCanvasProps) {
  const wrapper = useRef<HTMLDivElement>(null);
  const drag = useRef<Drag | null>(null);

  function beginDrag(mode: 'move' | Handle, event: ReactPointerEvent<HTMLElement>) {
    if (disabled) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    wrapper.current?.setPointerCapture(event.pointerId);
    drag.current = {
      mode,
      startX: event.clientX,
      startY: event.clientY,
      rect,
      pointerId: event.pointerId,
    };
  }

  function onPointerMove(event: ReactPointerEvent<HTMLDivElement>) {
    const current = drag.current;
    const box = wrapper.current?.getBoundingClientRect();

    if (!current || !box || box.width === 0 || box.height === 0) {
      return;
    }

    const dx = (event.clientX - current.startX) / box.width;
    const dy = (event.clientY - current.startY) / box.height;

    onChange(
      current.mode === 'move'
        ? moveRect(current.rect, dx, dy)
        : resizeRect(current.rect, current.mode, dx, dy),
    );
  }

  function endDrag(event: ReactPointerEvent<HTMLDivElement>) {
    if (!drag.current) {
      return;
    }

    wrapper.current?.releasePointerCapture(event.pointerId);
    drag.current = null;
  }

  function onKeyDown(event: ReactKeyboardEvent<HTMLDivElement>) {
    if (disabled) {
      return;
    }

    const step = event.shiftKey ? 0.02 : 0.01;
    const dx = event.key === 'ArrowLeft' ? -step : event.key === 'ArrowRight' ? step : 0;
    const dy = event.key === 'ArrowUp' ? -step : event.key === 'ArrowDown' ? step : 0;

    if (dx === 0 && dy === 0) {
      return;
    }

    event.preventDefault();

    if (event.shiftKey) {
      const handle = `${dy < 0 ? 'n' : dy > 0 ? 's' : ''}${dx < 0 ? 'w' : dx > 0 ? 'e' : ''}` as Handle;
      onChange(resizeRect(rect, handle, dx, dy));
    } else {
      onChange(moveRect(rect, dx, dy));
    }
  }

  const description = `Your frame: ${Math.round(rect.x * 100)}% from the left, ${Math.round(
    rect.y * 100,
  )}% from the top, ${Math.round(rect.width * 100)}% wide and ${Math.round(rect.height * 100)}% tall.`;

  return (
    <div
      className="framing-canvas"
      ref={wrapper}
      onPointerMove={onPointerMove}
      onPointerUp={endDrag}
      onPointerCancel={endDrag}
    >
      <img src={imageRef} alt="" draggable={false} />
      <div
        className={`frame${disabled ? ' frame--locked' : ' frame--active'}`}
        style={rectStyle(rect)}
        role="group"
        tabIndex={disabled ? -1 : 0}
        aria-label={description}
        aria-describedby="frame-instructions"
        onPointerDown={(event) => beginDrag('move', event)}
        onKeyDown={onKeyDown}
      >
        {!disabled &&
          corners.map((handle) => (
            <span
              key={handle}
              className={`frame-handle frame-handle--${handle}`}
              onPointerDown={(event) => beginDrag(handle, event)}
            />
          ))}
      </div>

      {target && (
        <div className="frame frame--target" style={rectStyle(target)} aria-hidden="true">
          <span className="frame-target-label">the frame we wanted</span>
        </div>
      )}

      <p className="sr-only" id="frame-instructions">
        Drag anywhere in the frame to move it. Drag a corner to resize. With a keyboard, use the
        arrow keys to move and hold shift with the arrow keys to resize.
      </p>
    </div>
  );
}
