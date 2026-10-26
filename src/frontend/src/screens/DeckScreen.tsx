import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  DEMO_MEMBER_ID,
  fetchBag,
  fetchDeck,
  fetchNextCard,
  finishSession,
  setShowAnswerOnWrong,
  startSession,
  submitAttempt,
} from '../api/client';
import type { AttemptResultDto, SessionDto, SessionResultDto } from '../api/types';
import { FramingCard } from '../components/FramingCard';

type Phase = 'starting' | 'playing' | 'result' | 'error';

export function DeckScreen() {
  const { deckId = '' } = useParams();
  const queryClient = useQueryClient();

  const deck = useQuery({
    queryKey: ['deck', deckId],
    queryFn: () => fetchDeck(deckId),
    enabled: deckId.length > 0,
  });
  const bag = useQuery({ queryKey: ['bag', DEMO_MEMBER_ID], queryFn: () => fetchBag(DEMO_MEMBER_ID) });

  const [phase, setPhase] = useState<Phase>('starting');
  const [error, setError] = useState<string | null>(null);
  const [session, setSession] = useState<SessionDto | null>(null);
  const [cardId, setCardId] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<AttemptResultDto | null>(null);
  const [selected, setSelected] = useState<string | null>(null);
  const [result, setResult] = useState<SessionResultDto | null>(null);
  const [nudge, setNudge] = useState(0);
  const [revealOpen, setRevealOpen] = useState(false);
  const [showAnswer, setShowAnswer] = useState(true);
  const started = useRef(false);

  useEffect(() => {
    if (bag.data) {
      setShowAnswer(bag.data.showAnswerOnWrong);
    }
  }, [bag.data]);

  useEffect(() => {
    if (started.current || !deckId) {
      return;
    }
    started.current = true;
    void begin(deckId);
    // begin is intentionally created once for the lifetime of the deck screen.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [deckId]);

  async function begin(id: string) {
    try {
      setPhase('starting');
      setError(null);
      const started = await startSession(DEMO_MEMBER_ID, id);
      setSession(started);
      const next = await fetchNextCard(started.id);
      setCardId(next.cardId);
      setPhase('playing');
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'The deck could not be started.');
      setPhase('error');
    }
  }

  async function complete(exitedEarly: boolean, activeSession = session) {
    if (!activeSession) {
      return;
    }

    try {
      const finished = await finishSession(activeSession.id, exitedEarly);
      setResult(finished);
      setCardId(null);
      setPhase('result');
      await queryClient.invalidateQueries({ queryKey: ['bag', DEMO_MEMBER_ID] });
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'The result could not be saved.');
      setPhase('error');
    }
  }

  async function advance() {
    if (!session) {
      return;
    }

    setFeedback(null);
    setSelected(null);

    try {
      const next = await fetchNextCard(session.id);
      if (next.cardId) {
        setCardId(next.cardId);
      } else {
        await complete(false);
      }
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'The next card could not be loaded.');
      setPhase('error');
    }
  }

  async function answerWith(rawAnswer: string) {
    if (!session || !card || feedback) {
      return;
    }

    setSelected(card.choices.some((choice) => choice.id === rawAnswer) ? rawAnswer : null);

    try {
      const attempt = await submitAttempt(session.id, card.id, rawAnswer);
      setFeedback(attempt);
      if (attempt.correct) {
        setNudge((value) => value + 1);
      }
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'That answer could not be saved.');
      setPhase('error');
    }
  }

  async function toggleReveal(next: boolean) {
    setShowAnswer(next);
    try {
      await setShowAnswerOnWrong(DEMO_MEMBER_ID, next);
      await queryClient.invalidateQueries({ queryKey: ['bag', DEMO_MEMBER_ID] });
    } catch {
      setShowAnswer(!next);
    }
  }

  if (deck.isError || phase === 'error') {
    return (
      <section className="deck-screen">
        <div className="state state--error" role="alert">
          <h1>The deck stopped.</h1>
          <p>{error ?? 'The deck could not be loaded. It may still be starting.'}</p>
          <Link className="button-primary" to="/">
            Back to your bag
          </Link>
        </div>
      </section>
    );
  }

  if (phase === 'result' && result) {
    return (
      <section className="deck-screen">
        <div className="result">
          <h1>
            {result.passed ? 'Deck complete.' : result.toolGranted ? 'Deck complete, no pass.' : 'You left early.'}
          </h1>
          <p className="result-score">
            {result.firstTryCorrect} of {result.totalCards} right on the first try — {result.scorePercent}%.
          </p>
          {result.passed ? (
            <p>Passed. The deck is done and the work counts. Head home — it lands in your bag.</p>
          ) : result.toolGranted ? (
            <p>
              The deck is complete, so it still lands in your bag. A pass needs 80% on the first
              try.
            </p>
          ) : (
            <p>Nothing was completed this time. The deck is waiting when you are.</p>
          )}
          <Link className="button-primary" to="/">
            Back to your bag
          </Link>
        </div>
      </section>
    );
  }

  const card = deck.data?.cards.find((candidate) => candidate.id === cardId) ?? null;

  if (phase === 'starting' || !deck.data || !card) {
    return (
      <section className="deck-screen">
        <p className="quiet-load" role="status">
          Dealing the deck…
        </p>
      </section>
    );
  }

  const promptParts = card.prompt.split('___');
  const hasBlank = promptParts.length > 1;
  const correctLabel = card.choices.find((choice) => choice.id === feedback?.correctChoiceId)?.label;
  const pickedLabel = card.choices.find((choice) => choice.id === selected)?.label;
  const blankText = feedback
    ? feedback.correct
      ? (pickedLabel ?? '')
      : showAnswer
        ? (correctLabel ?? '')
        : ''
    : '';
  const isFraming = card.type === 'framing';

  return (
    <section className="deck-screen">
      <div className="deck-bar">
        <Link className="text-link" to="/">
          Back to your bag
        </Link>
        <button
          type="button"
          className="icon-button"
          aria-expanded={revealOpen}
          aria-controls="card-settings"
          onClick={() => setRevealOpen((open) => !open)}
        >
          <GearIcon />
          <span className="sr-only">Card settings</span>
        </button>
      </div>

      {revealOpen && (
        <div id="card-settings" className="settings-pop">
          <label className="switch-row">
            <input
              type="checkbox"
              checked={showAnswer}
              onChange={(event) => void toggleReveal(event.target.checked)}
            />
            <span>Show the answer when I get it wrong</span>
          </label>
          <p>This is your setting, not the deck&apos;s.</p>
        </div>
      )}

      <article className="card" aria-labelledby="card-prompt">
        <span key={nudge} className={`nudge${nudge > 0 ? ' nudge--play' : ''}`} aria-hidden="true">
          <CarabinerIcon />
        </span>

        {!isFraming && (
          <div className="card-media">
            {card.imageRef && <img src={card.imageRef} alt="" />}
          </div>
        )}

        <div className="card-body">
          <p className="card-count">
            {deck.data.name}, {session?.remaining ?? 0} left
          </p>
          <h1 id="card-prompt" className="card-prompt">
            {hasBlank ? (
              <>
                {promptParts[0]}
                <span className={`blank${blankText ? ' is-filled' : ''}`}>{blankText || '\u00a0'}</span>
                {promptParts.slice(1).join('___')}
              </>
            ) : (
              card.prompt
            )}
          </h1>

          {isFraming ? (
            <FramingCard
              key={card.id}
              card={card}
              feedback={feedback}
              showAnswer={showAnswer}
              onSubmit={(value) => void answerWith(value)}
            />
          ) : (
            <div className="choices">
              {card.choices.map((choice) => {
                const isSelected = choice.id === selected;
                const isCorrect = feedback?.correctChoiceId === choice.id;
                const state = feedback
                  ? isCorrect
                    ? 'is-right'
                    : isSelected
                      ? 'is-wrong'
                      : ''
                  : isSelected
                    ? 'is-picked'
                    : '';

                return (
                  <button
                    key={choice.id}
                    type="button"
                    className={`choice ${state}`.trim()}
                    disabled={feedback !== null}
                    aria-pressed={isSelected}
                    onClick={() => void answerWith(choice.id)}
                  >
                    <span>{choice.label}</span>
                    {feedback && isCorrect && <small className="choice-tag">right answer</small>}
                    {feedback && isSelected && !isCorrect && <small className="choice-tag">your answer</small>}
                  </button>
                );
              })}
            </div>
          )}

          {feedback && (
            <Feedback
              feedback={feedback}
              correctLabel={correctLabel}
              pickedLabel={pickedLabel}
              showAnswer={showAnswer}
              onNext={() => void advance()}
            />
          )}
        </div>
      </article>

      <button type="button" className="text-button leave" onClick={() => void complete(true)}>
        Leave the deck
      </button>
    </section>
  );
}

type FeedbackProps = {
  feedback: AttemptResultDto;
  correctLabel?: string;
  pickedLabel?: string;
  showAnswer: boolean;
  onNext: () => void;
};

function Feedback({ feedback, correctLabel, pickedLabel, showAnswer, onNext }: FeedbackProps) {
  return (
    <div className={`feedback ${feedback.correct ? 'feedback--right' : 'feedback--wrong'}`} role="status">
      <p className="feedback-verdict">{feedback.correct ? 'Right.' : 'Not quite — try again.'}</p>

      {feedback.correct ? (
        <p>{feedback.explanation}</p>
      ) : showAnswer ? (
        <>
          <p>
            {correctLabel && <strong>{correctLabel} </strong>}
            {feedback.explanation}
          </p>
          {feedback.wrongReason && (
            <p className="feedback-reason">
              You picked {pickedLabel}: {feedback.wrongReason}
            </p>
          )}
          <p className="feedback-return">This card comes back in a moment.</p>
        </>
      ) : (
        <p className="feedback-return">This card comes back in a moment.</p>
      )}

      {feedback.overlap != null && (
        <p className="feedback-overlap">
          {Math.round(feedback.overlap * 100)}% overlap.
          {!feedback.correct && ' 70% counts as close enough.'}
        </p>
      )}

      <button type="button" className="button-primary" onClick={onNext}>
        Next card
      </button>
    </div>
  );
}

function GearIcon() {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.7" aria-hidden="true">
      <circle cx="12" cy="12" r="3.2" />
      <path d="M12 3.5v2.2M12 18.3v2.2M3.5 12h2.2M18.3 12h2.2M6 6l1.6 1.6M16.4 16.4L18 18M18 6l-1.6 1.6M7.6 16.4L6 18" />
    </svg>
  );
}

function CarabinerIcon() {
  return (
    <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M16 5.5a5 5 0 1 0 0 13" />
      <path d="M16 5.5h3v13h-3" />
      <path d="M12 8v8" />
    </svg>
  );
}
