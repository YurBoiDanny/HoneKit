import { useRef, useState } from 'react';
import type { DragEvent as ReactDragEvent } from 'react';
import type { AttemptResultDto, CardDto } from '../api/types';
import { moveItem, shuffleSteps } from './ordering';

type OrderingCardProps = {
  card: CardDto;
  feedback: AttemptResultDto | null;
  onSubmit: (answer: string) => void;
};

export function OrderingCard({ card, feedback, onSubmit }: OrderingCardProps) {
  const steps = card.steps ?? [];
  const [order, setOrder] = useState(() => shuffleSteps(steps, card.id));
  const [announcement, setAnnouncement] = useState('');
  const dragFrom = useRef<number | null>(null);
  const locked = feedback !== null;

  if (steps.length === 0) {
    return <p className="quiet-load">This ordering card has no steps yet.</p>;
  }

  function move(index: number, delta: number) {
    const target = index + delta;

    if (locked || target < 0 || target >= order.length) {
      return;
    }

    setOrder((current) => moveItem(current, index, target));
    setAnnouncement(`${order[index].label} is now step ${target + 1}.`);
  }

  function startDrag(index: number, event: ReactDragEvent<HTMLSpanElement>) {
    if (locked) {
      return;
    }

    dragFrom.current = index;
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', order[index]?.id ?? '');
  }

  function dropAt(index: number) {
    const from = dragFrom.current;
    dragFrom.current = null;

    if (locked || from === null || from === index) {
      return;
    }

    setOrder((current) => moveItem(current, from, index));
    setAnnouncement(`${order[from].label} is now step ${index + 1}.`);
  }

  return (
    <div className="ordering-block">
      <p className="ordering-hint">Drag a slip, or use the arrows. The order you leave is your answer.</p>

      <ol
        className={`ordering-list${locked ? ' is-locked' : ''}`}
        aria-label="Steps in the order you choose"
      >
        {order.map((step, index) => (
          <li
            key={step.id}
            className="ordering-row"
            onDragOver={(event) => {
              if (!locked && dragFrom.current !== null) {
                event.preventDefault();
              }
            }}
            onDrop={(event) => {
              event.preventDefault();
              dropAt(index);
            }}
          >
            <span
              className="ordering-grip"
              draggable={!locked}
              onDragStart={(event) => startDrag(index, event)}
              onDragEnd={() => {
                dragFrom.current = null;
              }}
              aria-hidden="true"
            >
              <GripIcon />
            </span>

            <span className="ordering-num" aria-hidden="true">
              {index + 1}
            </span>

            <span className="ordering-label">{step.label}</span>

            <span className="ordering-move">
              <button
                type="button"
                className="ordering-move-button"
                disabled={locked || index === 0}
                aria-label={`Move "${step.label}" up`}
                onClick={() => move(index, -1)}
              >
                <ArrowUpIcon />
              </button>
              <button
                type="button"
                className="ordering-move-button"
                disabled={locked || index === order.length - 1}
                aria-label={`Move "${step.label}" down`}
                onClick={() => move(index, 1)}
              >
                <ArrowDownIcon />
              </button>
            </span>
          </li>
        ))}
      </ol>

      <p className="sr-only" role="status" aria-live="polite">
        {announcement}
      </p>

      {!locked && (
        <div className="ordering-actions">
          <button
            type="button"
            className="button-primary"
            disabled={order.length === 0}
            onClick={() => onSubmit(JSON.stringify(order.map((step) => step.id)))}
          >
            Submit order
          </button>
          <p className="frame-hint">The order above is what gets checked.</p>
        </div>
      )}
    </div>
  );
}

function GripIcon() {
  return (
    <svg viewBox="0 0 20 20" width="16" height="16" fill="currentColor" aria-hidden="true">
      <circle cx="6" cy="5" r="1.4" />
      <circle cx="14" cy="5" r="1.4" />
      <circle cx="6" cy="10" r="1.4" />
      <circle cx="14" cy="10" r="1.4" />
      <circle cx="6" cy="15" r="1.4" />
      <circle cx="14" cy="15" r="1.4" />
    </svg>
  );
}

function ArrowUpIcon() {
  return (
    <svg viewBox="0 0 20 20" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
      <path d="M10 15V5M5.5 9.5 10 5l4.5 4.5" />
    </svg>
  );
}

function ArrowDownIcon() {
  return (
    <svg viewBox="0 0 20 20" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
      <path d="M10 5v10M5.5 10.5 10 15l4.5-4.5" />
    </svg>
  );
}
