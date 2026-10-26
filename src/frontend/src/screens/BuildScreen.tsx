import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { FramingCanvas } from '../components/FramingCanvas';
import {
  addCard,
  assignDeck,
  createDeck,
  DEMO_ROLE,
  fetchLessons,
  fetchSkills,
  fetchTeam,
} from '../api/client';
import type {
  CardDto,
  CreateCardRequest,
  DeckSummaryDto,
  RectDto,
} from '../api/types';

const images = [
  { value: '/seed/stage-01.jpg', label: 'Stage from the lectern side' },
  { value: '/seed/stage-02.png', label: 'Lit auditorium' },
  { value: '/seed/stage-03.jpg', label: 'Sanctuary wide' },
];

type CardType = 'fill-in-the-blank' | 'framing';

type ChoiceDraft = { label: string; reason: string };

export function BuildScreen() {
  const catalog = useQuery({ queryKey: ['skills', DEMO_ROLE], queryFn: () => fetchSkills(DEMO_ROLE) });
  const lessons = useQuery({ queryKey: ['lessons', DEMO_ROLE], queryFn: () => fetchLessons(DEMO_ROLE) });
  const team = useQuery({ queryKey: ['team', DEMO_ROLE], queryFn: () => fetchTeam(DEMO_ROLE) });

  const [deck, setDeck] = useState<DeckSummaryDto | null>(null);
  const [cards, setCards] = useState<CardDto[]>([]);
  const [deckName, setDeckName] = useState('');
  const [skillId, setSkillId] = useState('framing');
  const [errors, setErrors] = useState<string[]>([]);
  const [notice, setNotice] = useState<string | null>(null);
  const [assigned, setAssigned] = useState(false);

  const [type, setType] = useState<CardType>('fill-in-the-blank');
  const [lessonId, setLessonId] = useState('');
  const [imageRef, setImageRef] = useState(images[0].value);
  const [prompt, setPrompt] = useState('');
  const [explanation, setExplanation] = useState('');
  const [choices, setChoices] = useState<ChoiceDraft[]>([
    { label: '', reason: '' },
    { label: '', reason: '' },
  ]);
  const [correctIndex, setCorrectIndex] = useState(0);
  const [targetRect, setTargetRect] = useState<RectDto>({ x: 0.3, y: 0.25, width: 0.4, height: 0.5 });
  const [saving, setSaving] = useState(false);

  const deckLessons = (lessons.data ?? []).filter((lesson) => lesson.skillId === deck?.skillId);

  useEffect(() => {
    if (deck && !lessonId) {
      setLessonId(deckLessons[0]?.id ?? '');
    }
    // deckLessons changes when lessons load; only seed the first value once.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [deck, lessons.data]);

  async function create() {
    setErrors([]);
    setNotice(null);

    if (!deckName.trim()) {
      setErrors(['Give the deck a name.']);
      return;
    }

    setSaving(true);
    try {
      const created = await createDeck({ role: DEMO_ROLE, name: deckName.trim(), skillId });
      setDeck(created);
      setCards([]);
    } catch (exception) {
      setErrors([exception instanceof Error ? exception.message : 'The deck was not created.']);
    } finally {
      setSaving(false);
    }
  }

  function updateChoice(index: number, patch: Partial<ChoiceDraft>) {
    setChoices((current) => current.map((choice, i) => (i === index ? { ...choice, ...patch } : choice)));
  }

  function removeChoice(index: number) {
    setChoices((current) => current.filter((_, i) => i !== index));
    setCorrectIndex((current) => (current >= index && current > 0 ? current - 1 : current));
  }

  async function add() {
    if (!deck) {
      return;
    }

    const problems: string[] = [];
    if (!lessonId) problems.push('Choose a lesson for this card.');
    if (!prompt.trim()) problems.push('Write the prompt.');
    if (!explanation.trim()) problems.push('Explain the right answer.');
    if (type === 'fill-in-the-blank') {
      if (!prompt.includes('___')) problems.push('Put ___ where the missing word goes.');
      if (choices.filter((choice) => choice.label.trim()).length < 2) {
        problems.push('A fill-in-the-blank needs at least two choices.');
      }
    }

    if (problems.length > 0) {
      setErrors(problems);
      return;
    }

    const body: CreateCardRequest = {
      lessonId,
      type,
      imageRef,
      prompt: prompt.trim(),
      choices:
        type === 'fill-in-the-blank'
          ? choices.map((choice, index) => ({ id: `c-${index}`, label: choice.label.trim() }))
          : [],
      correctChoiceId: type === 'fill-in-the-blank' ? `c-${correctIndex}` : null,
      targetRect: type === 'framing' ? targetRect : null,
      explanation: explanation.trim(),
      wrongReasons:
        type === 'fill-in-the-blank'
          ? Object.fromEntries(
              choices
                .map((choice, index) => [`c-${index}`, choice.reason.trim()] as const)
                .filter(([, reason], index) => index !== correctIndex && reason.length > 0),
            )
          : {},
    };

    setSaving(true);
    setErrors([]);
    setNotice(null);

    try {
      const card = await addCard(deck.id, body);
      setCards((current) => [...current, card]);
      setDeck((current) => (current ? { ...current, cardCount: current.cardCount + 1 } : current));
      setPrompt('');
      setExplanation('');
      setChoices([
        { label: '', reason: '' },
        { label: '', reason: '' },
      ]);
      setCorrectIndex(0);
      setNotice('Card added.');
    } catch (exception) {
      setErrors([exception instanceof Error ? exception.message : 'The card was not saved.']);
    } finally {
      setSaving(false);
    }
  }

  async function assign() {
    if (!deck || !team.data) {
      return;
    }

    setErrors([]);
    try {
      await assignDeck(
        deck.id,
        team.data.members.map((member) => member.id),
      );
      setAssigned(true);
    } catch (exception) {
      setErrors([exception instanceof Error ? exception.message : 'The deck was not assigned.']);
    }
  }

  return (
    <section className="build-screen">
      <header className="plan-head">
        <h1>Deck building</h1>
        <p>Make one small deck, add cards of the two types, and send it to the crew.</p>
      </header>

      {!deck ? (
        <div className="build-create">
          <label className="field field--short">
            <span>Deck name</span>
            <input value={deckName} onChange={(event) => setDeckName(event.target.value)} placeholder="Switching basics" />
          </label>

          <label className="field field--short">
            <span>Skill</span>
            <select value={skillId} onChange={(event) => setSkillId(event.target.value)}>
              {(catalog.data ?? []).map((skill) => (
                <option key={skill.id} value={skill.id}>
                  {skill.name}
                </option>
              ))}
            </select>
          </label>

          {errors.length > 0 && (
            <div className="req-errors" role="alert">
              <ul>
                {errors.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </div>
          )}

          <button type="button" className="button-primary" disabled={saving} onClick={() => void create()}>
            {saving ? 'Creating…' : 'Create the deck'}
          </button>
        </div>
      ) : (
        <div className="plan-grid">
          <section className="brief-panel" aria-labelledby="deck-heading">
            <h2 id="deck-heading">{deck.name}</h2>
            <p className="req-provenance">
              {(catalog.data ?? []).find((skill) => skill.id === deck.skillId)?.name ?? deck.skillId}, {cards.length}{' '}
              {cards.length === 1 ? 'card' : 'cards'}
            </p>

            {cards.length === 0 ? (
              <p className="quiet-load">No cards yet. The form on the right adds the first one.</p>
            ) : (
              <ol className="build-cards">
                {cards.map((card) => (
                  <li key={card.id}>
                    <span>{card.type === 'framing' ? 'Framing scenario' : 'Fill in the blank'}</span>
                    <p>{card.prompt}</p>
                  </li>
                ))}
              </ol>
            )}

            <div className="rec-actions">
              <button
                type="button"
                className="button-primary"
                disabled={assigned || cards.length === 0 || !team.data}
                onClick={() => void assign()}
              >
                {assigned ? 'Assigned to the crew' : 'Assign to the camera crew'}
              </button>
            </div>

            {notice && <p className="rec-done">{notice}</p>}
          </section>

          <section className="req-panel" aria-labelledby="add-card-heading">
            <h2 id="add-card-heading">Add a card</h2>

            <div className="strategy-switch" role="group" aria-label="Card type">
              <button
                type="button"
                className={type === 'fill-in-the-blank' ? 'is-on' : ''}
                aria-pressed={type === 'fill-in-the-blank'}
                onClick={() => setType('fill-in-the-blank')}
              >
                Fill in the blank
              </button>
              <button
                type="button"
                className={type === 'framing' ? 'is-on' : ''}
                aria-pressed={type === 'framing'}
                onClick={() => setType('framing')}
              >
                Framing scenario
              </button>
            </div>

            {errors.length > 0 && (
              <div className="req-errors" role="alert">
                <ul>
                  {errors.map((error) => (
                    <li key={error}>{error}</li>
                  ))}
                </ul>
              </div>
            )}

            <label className="field">
              <span>Lesson</span>
              <select value={lessonId} onChange={(event) => setLessonId(event.target.value)}>
                {deckLessons.map((lesson) => (
                  <option key={lesson.id} value={lesson.id}>
                    {lesson.title}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <span>Image</span>
              <select value={imageRef} onChange={(event) => setImageRef(event.target.value)}>
                {images.map((image) => (
                  <option key={image.value} value={image.value}>
                    {image.label}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <span>{type === 'fill-in-the-blank' ? 'Prompt (use ___ for the blank)' : 'Prompt'}</span>
              <textarea rows={2} value={prompt} onChange={(event) => setPrompt(event.target.value)} />
            </label>

            {type === 'fill-in-the-blank' ? (
              <fieldset className="build-choices">
                <legend>Choices</legend>
                {choices.map((choice, index) => (
                  <div className="build-choice" key={index}>
                    <label className="check-row">
                      <input
                        type="radio"
                        name="correct-choice"
                        checked={correctIndex === index}
                        onChange={() => setCorrectIndex(index)}
                      />
                      <span>Right answer</span>
                    </label>
                    <label className="field">
                      <span>Word</span>
                      <input
                        value={choice.label}
                        onChange={(event) => updateChoice(index, { label: event.target.value })}
                      />
                    </label>
                    {correctIndex !== index && (
                      <label className="field">
                        <span>Why it is wrong</span>
                        <input
                          value={choice.reason}
                          onChange={(event) => updateChoice(index, { reason: event.target.value })}
                        />
                      </label>
                    )}
                    {choices.length > 2 && (
                      <button type="button" className="text-button" onClick={() => removeChoice(index)}>
                        Remove
                      </button>
                    )}
                  </div>
                ))}
                <button
                  type="button"
                  className="text-button"
                  disabled={choices.length >= 4}
                  onClick={() => setChoices((current) => [...current, { label: '', reason: '' }])}
                >
                  Add another choice
                </button>
              </fieldset>
            ) : (
              <div className="framing-block">
                <FramingCanvas
                  imageRef={imageRef}
                  rect={targetRect}
                  onChange={setTargetRect}
                  disabled={false}
                />
                <p className="frame-hint">Place the frame where a good answer should land.</p>
              </div>
            )}

            <label className="field">
              <span>Explanation</span>
              <textarea rows={2} value={explanation} onChange={(event) => setExplanation(event.target.value)} />
            </label>

            <div className="rec-actions">
              <button type="button" className="button-primary" disabled={saving} onClick={() => void add()}>
                {saving ? 'Saving…' : 'Add card'}
              </button>
            </div>
          </section>
        </div>
      )}
    </section>
  );
}
