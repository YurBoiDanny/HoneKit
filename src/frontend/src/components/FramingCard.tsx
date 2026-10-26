import { useState } from 'react';
import type { AttemptResultDto, CardDto, RectDto } from '../api/types';
import { FramingCanvas } from './FramingCanvas';

type FramingCardProps = {
  card: CardDto;
  feedback: AttemptResultDto | null;
  showAnswer: boolean;
  onSubmit: (answer: string) => void;
};

const startRect: RectDto = { x: 0.3, y: 0.24, width: 0.4, height: 0.5 };

export function FramingCard({ card, feedback, showAnswer, onSubmit }: FramingCardProps) {
  const [rect, setRect] = useState<RectDto>(startRect);

  if (!card.imageRef) {
    return null;
  }

  const target = feedback && !feedback.correct && showAnswer ? card.targetRect : null;

  return (
    <div className="framing-block">
      <FramingCanvas
        imageRef={card.imageRef}
        rect={rect}
        onChange={setRect}
        disabled={feedback !== null}
        target={target}
      />

      {!feedback && (
        <div className="framing-actions">
          <button type="button" className="button-primary" onClick={() => onSubmit(JSON.stringify(rect))}>
            Submit frame
          </button>
          <p className="frame-hint">Drag to move the frame. Drag a corner to resize it.</p>
        </div>
      )}
    </div>
  );
}
