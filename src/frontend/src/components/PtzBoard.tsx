import { useState } from 'react';
import type { AttemptResultDto, CardDto } from '../api/types';
import { describePtzAction, formatPtzAction, readPtzAction, type PtzVerb } from './ptz';

type PtzBoardProps = {
  card: CardDto;
  feedback: AttemptResultDto | null;
  onSubmit: (answer: string) => void;
};

type ViewfinderView = {
  scale: number;
  x: number;
  y: number;
};

const cameras = [1, 2, 3, 4, 5, 6];
const presets = Array.from({ length: 13 }, (_, index) => index + 1);
const traces = [1, 2, 3, 4];

const cuedPresets: Record<number, string> = {
  1: 'Preset 1: stages the camera on the lectern',
  10: 'Preset 10: opens the wide shot on the crowd',
};

/** Preset 1 is the close stage mark, 10 is the crowd wide; the rest move a little. */
function viewForPreset(preset: number | null): ViewfinderView {
  if (preset === null) {
    return { scale: 1.04, x: 0, y: 0 };
  }

  if (preset === 1) {
    return { scale: 1.32, x: -8, y: 4 };
  }

  if (preset === 10) {
    return { scale: 1, x: 0, y: 0 };
  }

  const x = ((preset * 13) % 17) - 8;
  const y = ((preset * 11) % 13) - 6;

  // The scale has to cover the shift or the photograph's edge would show.
  return {
    scale: 1.02 + Math.max(Math.abs(x), Math.abs(y)) * 0.024,
    x,
    y,
  };
}

export function PtzBoard({ card, feedback, onSubmit }: PtzBoardProps) {
  const [camera, setCamera] = useState(1);
  const [actions, setActions] = useState<string[]>([]);
  const [sweep, setSweep] = useState(0);
  const [status, setStatus] = useState('Camera 1 selected.');
  const locked = feedback !== null;

  // The log is the source of truth, so undo and clear put the viewfinder back too.
  const recalled = actions.reduce<Record<number, number>>((current, action) => {
    const parsed = readPtzAction(action);

    if (parsed?.verb === 'preset') {
      current[parsed.camera] = parsed.number;
    }

    return current;
  }, {});
  const preset = recalled[camera] ?? null;
  const view = viewForPreset(preset);

  function chooseCamera(next: number) {
    if (locked) {
      return;
    }

    setCamera(next);
    setStatus(`Camera ${next} selected.`);
  }

  function addAction(verb: PtzVerb, number: number) {
    if (locked) {
      return;
    }

    setActions((current) => [...current, formatPtzAction(camera, verb, number)]);

    if (verb === 'trace') {
      setSweep((current) => current + 1);
    }

    setStatus(`Camera ${camera}, ${verb} ${number}.`);
  }

  function undo() {
    if (locked || actions.length === 0) {
      return;
    }

    setActions((current) => current.slice(0, -1));
    setStatus('Last action undone.');
  }

  function clear() {
    if (locked || actions.length === 0) {
      return;
    }

    setActions([]);
    setStatus('Action log cleared.');
  }

  function submit() {
    if (locked || actions.length === 0) {
      return;
    }

    onSubmit(JSON.stringify(actions));
  }

  return (
    <div className={`ptz-board${locked ? ' is-locked' : ''}`}>
      <div className="ptz-head">
        <div>
          <p className="ptz-brand">Camera control</p>
          <p className="ptz-sub">Presets and traces run on the selected camera.</p>
        </div>

        <div className="ptz-cams" role="group" aria-label="Select camera">
          {cameras.map((number) => (
            <button
              key={number}
              type="button"
              className="ptz-cam"
              aria-label={`Camera ${number}`}
              aria-pressed={camera === number}
              disabled={locked}
              onClick={() => chooseCamera(number)}
            >
              {number}
            </button>
          ))}
        </div>
      </div>

      <div className="ptz-body">
        <div className="ptz-monitor">
          <div className="ptz-viewfinder">
            {card.imageRef ? (
              <div
                className="ptz-frame"
                style={{ transform: `translate(${view.x}%, ${view.y}%) scale(${view.scale})` }}
              >
                <img src={card.imageRef} alt="" draggable={false} />
              </div>
            ) : (
              <p className="ptz-nosignal">No camera image</p>
            )}

            <span className="ptz-hud" aria-hidden="true" />
            <span className="ptz-rec" aria-hidden="true">
              CAM {camera}
            </span>
            {sweep > 0 && <span key={sweep} className="ptz-sweep" aria-hidden="true" />}
          </div>

          <p className="ptz-monitor-caption">
            {preset === null
              ? `Camera ${camera}, no preset recalled`
              : `Preset ${preset} on camera ${camera}`}
          </p>
        </div>

        <div className="ptz-console">
          <div className="ptz-group">
            <p className="ptz-verb" id="ptz-preset-label">
              PRESET
            </p>
            <div className="ptz-grid ptz-presets" role="group" aria-labelledby="ptz-preset-label">
              {presets.map((number) => (
                <button
                  key={number}
                  type="button"
                  className={`ptz-key${cuedPresets[number] ? ' ptz-key--cued' : ''}${
                    preset === number ? ' is-recalled' : ''
                  }`}
                  aria-label={cuedPresets[number] ?? `Preset ${number}`}
                  aria-pressed={preset === number}
                  title={cuedPresets[number] ?? `Preset ${number}`}
                  disabled={locked}
                  onClick={() => addAction('preset', number)}
                >
                  {number}
                </button>
              ))}
            </div>
          </div>

          <div className="ptz-group">
            <p className="ptz-verb" id="ptz-trace-label">
              TRACE
            </p>
            <div className="ptz-grid ptz-traces" role="group" aria-labelledby="ptz-trace-label">
              {traces.map((number) => (
                <button
                  key={number}
                  type="button"
                  className="ptz-key ptz-key--trace"
                  aria-label={`Trace ${number}`}
                  disabled={locked}
                  onClick={() => addAction('trace', number)}
                >
                  {number}
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="ptz-foot">
        <div className="ptz-log">
          <p className="ptz-log-label">
            Action log <span>{actions.length}</span>
          </p>

          {actions.length === 0 ? (
            <p className="ptz-empty">No actions yet. Recall a preset or play a trace.</p>
          ) : (
            <ol className="ptz-log-list">
              {actions.map((action, index) => (
                <li key={`${action}-${index}`}>
                  <span className="ptz-log-num" aria-hidden="true">
                    {index + 1}
                  </span>
                  <span>{describePtzAction(action)}</span>
                </li>
              ))}
            </ol>
          )}
        </div>

        <div className="ptz-controls">
          <button
            type="button"
            className="ptz-button"
            disabled={locked || actions.length === 0}
            onClick={undo}
          >
            Undo
          </button>
          <button
            type="button"
            className="ptz-button"
            disabled={locked || actions.length === 0}
            onClick={clear}
          >
            Clear
          </button>
          <button
            type="button"
            className="ptz-button ptz-button--submit"
            disabled={locked || actions.length === 0}
            onClick={submit}
          >
            Submit actions
          </button>
        </div>
      </div>

      <p className="sr-only" role="status" aria-live="polite">
        {status}
      </p>
    </div>
  );
}
