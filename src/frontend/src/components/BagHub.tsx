import { useEffect, useRef, useState, type CSSProperties } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BagArt } from './BagArt';
import {
  DEMO_ROLE,
  fetchBag,
  fetchTeamBag,
  updateBagStyle,
} from '../api/client';
import type { EarnedToolDto } from '../api/types';

export type HubTarget =
  | { kind: 'member'; memberId: string; editable: boolean }
  | { kind: 'team' };

type Panel = 'tools' | 'personalize' | 'numbers' | null;
type Phase = 'unzip' | 'zoom' | 'hub';

type BagHubProps = {
  open: boolean;
  target: HubTarget;
  onClose: () => void;
};

const colours = ['sage', 'rose', 'powder', 'butter', 'brass', 'teal'] as const;
const styles = ['backpack', 'duffel', 'sling'] as const;

export function BagHub({ open, target, onClose }: BagHubProps) {
  const navigate = useNavigate();
  const dialog = useRef<HTMLDivElement>(null);
  const closeButton = useRef<HTMLButtonElement>(null);
  const [phase, setPhase] = useState<Phase>('unzip');
  const [panel, setPanel] = useState<Panel>(null);
  const [opened, setOpened] = useState(false);
  const memberId = target.kind === 'member' ? target.memberId : '';

  const bag = useQuery({
    queryKey: ['bag', memberId],
    queryFn: () => fetchBag(memberId),
    enabled: open && target.kind === 'member',
  });
  const teamBag = useQuery({
    queryKey: ['team-bag', DEMO_ROLE],
    queryFn: () => fetchTeamBag(DEMO_ROLE),
    enabled: open && target.kind === 'team',
  });

  useEffect(() => {
    if (!open) {
      return;
    }

    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    setPanel(null);
    setOpened(false);
    setPhase(reduced ? 'hub' : 'unzip');
    const toOpen = window.setTimeout(() => setOpened(true), reduced ? 0 : 80);
    document.body.classList.add('hub-open');

    const toZoom = window.setTimeout(() => setPhase('zoom'), reduced ? 0 : 520);
    const toHub = window.setTimeout(() => setPhase('hub'), reduced ? 0 : 900);

    return () => {
      window.clearTimeout(toOpen);
      window.clearTimeout(toZoom);
      window.clearTimeout(toHub);
      document.body.classList.remove('hub-open');
    };
  }, [open]);

  useEffect(() => {
    if (open && phase === 'hub') {
      closeButton.current?.focus();
    }
  }, [open, phase]);

  useEffect(() => {
    if (!open) {
      return;
    }

    function onKey(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose();
      }
    }

    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, onClose]);

  if (!open) {
    return null;
  }

  const earning: EarnedToolDto[] =
    target.kind === 'team'
      ? (teamBag.data?.tools ?? []).map((tool) => ({
          skillId: tool.skillId,
          name: tool.name,
          honed: false,
          acknowledged: true,
        }))
      : (bag.data?.tools ?? []);

  const fills =
    target.kind === 'team'
      ? Object.fromEntries((teamBag.data?.tools ?? []).map((tool) => [tool.skillId, tool.percent]))
      : undefined;

  const coloursForBag = bag.data?.colour ?? 'sage';
  const worn = target.kind === 'member' ? bag.data?.worn : false;
  const title =
    target.kind === 'team' ? 'The camera crew' : bag.data ? `${bag.data.name}'s bag` : 'Your bag';

  function trapFocus(event: React.KeyboardEvent<HTMLDivElement>) {
    if (event.key !== 'Tab') {
      return;
    }

    const nodes = dialog.current?.querySelectorAll<HTMLElement>(
      'button:not([disabled]), a[href], input, [tabindex]:not([tabindex="-1"])',
    );

    if (!nodes || nodes.length === 0) {
      return;
    }

    const first = nodes[0];
    const last = nodes[nodes.length - 1];

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  function go(path: string) {
    onClose();
    navigate(path);
  }

  function personalize(colour: string, style: string) {
    if (target.kind !== 'member') {
      return;
    }

    void updateBagStyle(target.memberId, colour, style).then(() => bag.refetch());
  }

  return (
    <div
      className={`hub hub--${phase}`}
      role="dialog"
      aria-modal="true"
      aria-labelledby="hub-title"
      ref={dialog}
      onKeyDown={trapFocus}
    >
      <div className="hub-bg" aria-hidden="true" />
      <p className="sr-only" role="status">
        {phase === 'unzip'
          ? 'Unzipping the bag.'
          : phase === 'zoom'
            ? 'Moving in close.'
            : 'Bag menu open.'}
      </p>

      <button
        type="button"
        className="hub-close"
        ref={closeButton}
        aria-label="Close the bag"
        onClick={onClose}
      >
        <svg viewBox="0 0 24 24" width="22" height="22" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
          <path d="M5 5l14 14M19 5L5 19" />
        </svg>
      </button>

      <h2 className="hub-title" id="hub-title">
        {title}
      </h2>

      <div className="hub-stage">
        <BagArt
          colour={target.kind === 'team' ? 'powder' : coloursForBag}
          worn={worn}
          tools={earning}
          fills={fills}
          state={opened ? 'open' : 'idle'}
          label={`${title}, open`}
        />
      </div>

      {phase === 'hub' && (
        <div className="hub-options">
          {target.kind === 'member' ? (
            <>
              <button
                type="button"
                className="hub-card"
                style={{ '--i': 0, '--r': '-4deg' } as CSSProperties}
                onClick={() => setPanel(panel === 'tools' ? null : 'tools')}
                aria-expanded={panel === 'tools'}
              >
                <span className="hub-card-num">01</span>
                <strong>Tools</strong>
                <small>What you carry</small>
              </button>

              <button
                type="button"
                className="hub-card"
                style={{ '--i': 1, '--r': '3deg' } as CSSProperties}
                onClick={() => go('/crew')}
              >
                <span className="hub-card-num">02</span>
                <strong>The crew</strong>
                <small>Every bag on the wall</small>
              </button>

              {target.editable ? (
                <button
                  type="button"
                  className="hub-card"
                  style={{ '--i': 2, '--r': '-2.5deg' } as CSSProperties}
                  onClick={() => setPanel(panel === 'personalize' ? null : 'personalize')}
                  aria-expanded={panel === 'personalize'}
                >
                  <span className="hub-card-num">03</span>
                  <strong>Make it yours</strong>
                  <small>Colour and style</small>
                </button>
              ) : (
                <div className="hub-note">Read-only. Their colours, their kit.</div>
              )}
            </>
          ) : (
            <>
              <button
                type="button"
                className="hub-card"
                style={{ '--i': 0, '--r': '-4deg' } as CSSProperties}
                onClick={() => setPanel(panel === 'numbers' ? null : 'numbers')}
                aria-expanded={panel === 'numbers'}
              >
                <span className="hub-card-num">01</span>
                <strong>The numbers</strong>
                <small>How the crew is filling in</small>
              </button>

              <button
                type="button"
                className="hub-card"
                style={{ '--i': 1, '--r': '3deg' } as CSSProperties}
                onClick={() => go('/crew')}
              >
                <span className="hub-card-num">02</span>
                <strong>The crew</strong>
                <small>Every bag on the wall</small>
              </button>

              <button
                type="button"
                className="hub-card"
                style={{ '--i': 2, '--r': '-2.5deg' } as CSSProperties}
                onClick={() => go('/me')}
              >
                <span className="hub-card-num">03</span>
                <strong>My bag</strong>
                <small>Your own gear</small>
              </button>
            </>
          )}
        </div>
      )}

      {panel && (
        <div className="hub-panel">
          {panel === 'tools' && target.kind === 'member' && (
            <>
              <h3>In the bag</h3>
              {earning.length === 0 ? (
                <p className="quiet-load">Nothing in it yet.</p>
              ) : (
                <ul>
                  {earning.map((tool) => (
                    <li key={tool.skillId}>
                      <span>{tool.name}</span>
                      <small>{tool.honed ? 'honed' : 'plain'}</small>
                    </li>
                  ))}
                </ul>
              )}
            </>
          )}

          {panel === 'numbers' && target.kind === 'team' && (
            <>
              <h3>The crew's numbers</h3>
              <ul>
                {(teamBag.data?.tools ?? []).map((tool) => (
                  <li key={tool.skillId}>
                    <span>{tool.name}</span>
                    <small>
                      {tool.filledLessons} of {tool.totalLessons} lessons,{' '}
                      {Math.round(tool.percent)}%
                    </small>
                  </li>
                ))}
              </ul>
            </>
          )}

          {panel === 'personalize' && target.kind === 'member' && bag.data && (
            <>
              <h3>Make it yours</h3>
              <div className="colour-row" role="group" aria-label="Bag colour">
                {colours.map((colour) => (
                  <button
                    key={colour}
                    type="button"
                    className={`colour-dot${bag.data.colour === colour ? ' is-on' : ''}`}
                    style={{ background: `var(--${colour})` }}
                    aria-label={colour.charAt(0).toUpperCase() + colour.slice(1)}
                    aria-pressed={bag.data.colour === colour}
                    onClick={() => personalize(colour, bag.data.style)}
                  />
                ))}
              </div>
              <div className="style-row" role="group" aria-label="Bag style">
                {styles.map((style) => (
                  <button
                    key={style}
                    type="button"
                    className={`style-choice${bag.data.style === style ? ' is-on' : ''}`}
                    aria-pressed={bag.data.style === style}
                    onClick={() => personalize(bag.data.colour, style)}
                  >
                    {style}
                  </button>
                ))}
              </div>
            </>
          )}

          <button type="button" className="text-button" onClick={() => setPanel(null)}>
            Back to the bag
          </button>
        </div>
      )}
    </div>
  );
}
