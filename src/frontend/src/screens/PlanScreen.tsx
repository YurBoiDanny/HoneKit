import { useEffect, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  ApiError,
  confirmRequirementSet,
  createManualRequirementSet,
  DEMO_ROLE,
  deleteRequirementSet,
  fetchAiStatus,
  fetchRequirementSet,
  fetchRequirementSets,
  fetchSkills,
  parseBrief,
  updateRequirementSet,
} from '../api/client';
import { Recommendations } from '../components/Recommendations';
import { RequirementEditor } from '../components/RequirementEditor';
import type {
  ManualRequirementSetRequest,
  RequirementSetDto,
  RequirementSkillDto,
  UpdateRequirementSetRequest,
} from '../api/types';

type Mode = 'parsed' | 'manual' | 'confirmed';

function manualDraft(sourceName: string, sourceText: string): RequirementSetDto {
  return {
    id: '',
    sourceName: sourceName || 'Typed brief',
    sourceText,
    purpose: '',
    criticality: 'medium',
    provider: 'manual',
    model: '',
    parsedAt: new Date().toISOString(),
    confirmedAt: null,
    edited: false,
    skills: [],
    outsideRole: [],
  };
}

function toInput(skill: RequirementSkillDto) {
  return {
    skillId: skill.skillId,
    minLevel: skill.minLevel,
    headcount: skill.headcount,
    rationale: skill.rationale,
  };
}

export function PlanScreen() {
  const queryClient = useQueryClient();
  const catalog = useQuery({ queryKey: ['skills', DEMO_ROLE], queryFn: () => fetchSkills(DEMO_ROLE) });
  const sets = useQuery({ queryKey: ['requirements'], queryFn: fetchRequirementSets });

  const [draft, setDraft] = useState<RequirementSetDto | null>(null);
  const [mode, setMode] = useState<Mode>('parsed');
  const [file, setFile] = useState<File | null>(null);
  const [text, setText] = useState('');
  const [reading, setReading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const cancelled = useRef(false);
  const [ai, setAi] = useState<import('../api/types').AiStatusDto | null>(null);
  const [checking, setChecking] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null);

  useEffect(() => {
    fetchAiStatus().then(setAi).catch(() => setAi(null));
  }, []);

  async function checkReader() {
    setChecking(true);
    try {
      setAi(await fetchAiStatus(true));
    } catch {
      setAi(null);
    } finally {
      setChecking(false);
    }
  }

  async function readBrief() {
    if (!file && !text.trim()) {
      setErrors(['Choose a file or paste the brief first.']);
      return;
    }

    cancelled.current = false;
    setReading(true);
    setErrors([]);

    try {
      const result = await parseBrief(file, text);
      if (cancelled.current) {
        return;
      }

      if (result.ok) {
        setDraft(result.set);
        setMode(result.set.confirmedAt ? 'confirmed' : 'parsed');
      } else {
        setDraft(manualDraft(result.failure.sourceName, result.failure.sourceText));
        setMode('manual');
        setErrors([result.failure.detail, ...result.failure.errors].filter(Boolean));
      }
    } catch (exception) {
      setErrors([exception instanceof Error ? exception.message : 'The brief could not be read.']);
    } finally {
      setReading(false);
    }
  }

  function typeInstead() {
    cancelled.current = true;
    setReading(false);
    setDraft(manualDraft(file?.name ?? 'Typed brief', text));
    setMode('manual');
    setErrors([]);
  }

  async function confirm() {
    if (!draft) {
      return;
    }

    setSaving(true);
    setErrors([]);

    try {
      let saved = draft;

      if (mode === 'manual' || !draft.id) {
        const body: ManualRequirementSetRequest = {
          purpose: draft.purpose,
          criticality: draft.criticality,
          skills: draft.skills.map(toInput),
          outsideRole: draft.outsideRole,
          sourceName: draft.sourceName,
          sourceText: draft.sourceText,
        };
        saved = await createManualRequirementSet(body);
      } else {
        const body: UpdateRequirementSetRequest = {
          purpose: draft.purpose,
          criticality: draft.criticality,
          skills: draft.skills.map(toInput),
          outsideRole: draft.outsideRole,
        };
        saved = await updateRequirementSet(draft.id, body);
      }

      const confirmed = await confirmRequirementSet(saved.id);
      setDraft(confirmed);
      setMode('confirmed');
      await queryClient.invalidateQueries({ queryKey: ['requirements'] });
    } catch (exception) {
      if (exception instanceof ApiError && exception.details.length > 0) {
        setErrors(exception.details);
      } else {
        setErrors([exception instanceof Error ? exception.message : 'The requirements were not saved.']);
      }
    } finally {
      setSaving(false);
    }
  }

  async function removeDraft(id: string) {
    try {
      await deleteRequirementSet(id);
      setConfirmDelete(null);
      if (draft?.id === id) {
        setDraft(null);
        setMode('parsed');
      }
      setErrors([]);
      await queryClient.invalidateQueries({ queryKey: ['requirements'] });
    } catch (exception) {
      setErrors([exception instanceof Error ? exception.message : 'The draft was not deleted.']);
    }
  }

  async function loadSet(id: string) {
    setErrors([]);
    try {
      const set = await fetchRequirementSet(id);
      setDraft(set);
      setMode(set.confirmedAt ? 'confirmed' : 'parsed');
    } catch (exception) {
      setErrors([exception instanceof Error ? exception.message : 'That set could not be loaded.']);
    }
  }

  return (
    <section className="plan-screen">
      <header className="plan-head">
        <h1>Meeting plan</h1>
        <p>
          Turn the brief you were sent into requirements you own. Read it, correct it, then
          confirm it.
        </p>
      </header>

      {ai && (
        <div className={`ai-status ai-status--${ai.mode}`}>
          <span className="ai-dot" aria-hidden="true" />
          <span>{ai.detail}</span>
          <button type="button" className="text-button" onClick={() => void checkReader()} disabled={checking}>
            {checking ? 'Checking…' : 'Check the reader'}
          </button>
        </div>
      )}

      <div className="plan-grid">
        <section className="brief-panel" aria-labelledby="brief-heading">
          <h2 id="brief-heading">The brief</h2>

          <label className="field">
            <span>Upload a .txt, .md or .pdf</span>
            <input
              type="file"
              accept=".txt,.md,.pdf"
              onChange={(event) => setFile(event.target.files?.[0] ?? null)}
            />
          </label>

          <label className="field">
            <span>Or paste the text</span>
            <textarea
              rows={4}
              value={text}
              placeholder="Paste the meeting brief…"
              onChange={(event) => setText(event.target.value)}
            />
          </label>

          <div className="brief-actions">
            <button type="button" className="button-primary" onClick={() => void readBrief()} disabled={reading}>
              {reading ? 'Reading the brief…' : 'Read the brief'}
            </button>
            {reading && (
              <button type="button" className="text-button" onClick={typeInstead}>
                Type them instead
              </button>
            )}
          </div>

          {draft?.sourceText && (
            <div className="brief-source">
              <h3>{draft.sourceName}</h3>
              <pre>{draft.sourceText}</pre>
            </div>
          )}

          {sets.data && sets.data.length > 0 && (
            <div className="saved-sets">
              <h3>Saved sets</h3>
              <ul>
                {sets.data.map((set) => (
                  <li key={set.id}>
                    <button type="button" className="text-button saved-title" onClick={() => void loadSet(set.id)}>
                      {set.purpose || set.sourceName}
                    </button>
                    <span className="saved-state">{set.confirmedAt ? 'confirmed' : 'draft'}</span>
                    {!set.confirmedAt &&
                      (confirmDelete === set.id ? (
                        <span className="draft-confirm">
                          <button type="button" className="text-button" onClick={() => void removeDraft(set.id)}>
                            Delete
                          </button>
                          <button type="button" className="text-button" onClick={() => setConfirmDelete(null)}>
                            Keep
                          </button>
                        </span>
                      ) : (
                        <button
                          type="button"
                          className="text-button draft-delete"
                          onClick={() => setConfirmDelete(set.id)}
                        >
                          Delete
                        </button>
                      ))}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </section>

        <section className="req-panel" aria-labelledby="requirements-heading">
          <h2 id="requirements-heading">Requirements</h2>

          {draft ? (
            <RequirementEditor
              draft={draft}
              catalog={catalog.data ?? []}
              mode={mode}
              saving={saving}
              errors={errors}
              onChange={setDraft}
              onConfirm={() => void confirm()}
            />
          ) : (
            <p className="quiet-load">
              {reading
                ? 'Reading the brief…'
                : 'Upload the brief and its requirements will appear here, ready to edit.'}
            </p>
          )}
        </section>
      </div>

      {mode === 'confirmed' && draft?.id && <Recommendations requirementId={draft.id} />}
    </section>
  );
}
