import { useState } from 'react';
import type { RequirementSetDto, RequirementSkillDto, SkillDto } from '../api/types';

type Mode = 'parsed' | 'manual' | 'confirmed';

type RequirementEditorProps = {
  draft: RequirementSetDto;
  catalog: SkillDto[];
  mode: Mode;
  saving: boolean;
  errors: string[];
  onChange: (next: RequirementSetDto) => void;
  onConfirm: () => void;
};

const levels = [
  { value: 0, label: 'Watching' },
  { value: 0.5, label: 'Working' },
  { value: 1, label: 'At level' },
];

function levelLabel(value: number): string {
  return levels.find((level) => level.value === value)?.label ?? `${value}`;
}

export function RequirementEditor({
  draft,
  catalog,
  mode,
  saving,
  errors,
  onChange,
  onConfirm,
}: RequirementEditorProps) {
  const [toAdd, setToAdd] = useState('');
  const remaining = catalog.filter(
    (skill) => !draft.skills.some((required) => required.skillId === skill.id),
  );

  function patchSkill(skillId: string, patch: Partial<RequirementSkillDto>) {
    onChange({
      ...draft,
      skills: draft.skills.map((skill) =>
        skill.skillId === skillId ? { ...skill, ...patch } : skill,
      ),
    });
  }

  function removeSkill(skillId: string) {
    onChange({ ...draft, skills: draft.skills.filter((skill) => skill.skillId !== skillId) });
  }

  function addSkill() {
    const skill = catalog.find((candidate) => candidate.id === toAdd);
    if (!skill) {
      return;
    }

    onChange({
      ...draft,
      skills: [
        ...draft.skills,
        {
          skillId: skill.id,
          name: skill.name,
          minLevel: 0.5,
          headcount: 1,
          rationale: '',
        },
      ],
    });
    setToAdd('');
  }

  const readDate = new Date(draft.parsedAt).toLocaleString(undefined, {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });

  const provenance =
    mode === 'manual'
      ? `Typed by hand.${draft.sourceName ? ` From ${draft.sourceName}.` : ''}`
      : `Read from ${draft.sourceName} on ${readDate} with ${draft.provider}.`;

  if (mode === 'confirmed') {
    return (
      <div className="req-editor">
        <p className="req-provenance">{provenance}</p>
        <p className="req-confirmed">
          Confirmed. This is the set recommendations are built from.
        </p>

        <dl className="req-summary">
          <div>
            <dt>Purpose</dt>
            <dd>{draft.purpose}</dd>
          </div>
          <div>
            <dt>Criticality</dt>
            <dd className="capitalise">{draft.criticality}</dd>
          </div>
          {draft.skills.map((skill) => (
            <div key={skill.skillId}>
              <dt>{skill.name}</dt>
              <dd>
                {levelLabel(skill.minLevel)}, {skill.headcount}{' '}
                {skill.headcount === 1 ? 'person' : 'people'}
              </dd>
            </div>
          ))}
        </dl>

        {draft.outsideRole.length > 0 && (
          <section className="outside-role">
            <h3>Outside this role, not planned here</h3>
            <ul>
              {draft.outsideRole.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </section>
        )}
      </div>
    );
  }

  return (
    <div className="req-editor">
      <p className="req-provenance">{provenance}</p>

      {mode === 'manual' && (
        <p className="req-note">
          The brief could not be read automatically. Type the requirement set yourself; the
          document stays beside you.
        </p>
      )}

      {errors.length > 0 && (
        <div className="req-errors" role="alert">
          <p>The requirements need a little work:</p>
          <ul>
            {errors.map((error) => (
              <li key={error}>{error}</li>
            ))}
          </ul>
        </div>
      )}

      <label className="field">
        <span>Purpose</span>
        <textarea
          rows={2}
          value={draft.purpose}
          onChange={(event) => onChange({ ...draft, purpose: event.target.value })}
        />
      </label>

      <label className="field field--short">
        <span>Criticality</span>
        <select
          value={draft.criticality}
          onChange={(event) => onChange({ ...draft, criticality: event.target.value })}
        >
          <option value="low">Low</option>
          <option value="medium">Medium</option>
          <option value="high">High</option>
        </select>
      </label>

      <fieldset className="req-skills">
        <legend>Skills this meeting needs</legend>

        {draft.skills.length === 0 && <p className="quiet-load">No skills yet.</p>}

        {draft.skills.map((skill) => (
          <div className="req-skill" key={skill.skillId}>
            <div className="req-skill-head">
              <h3>{skill.name}</h3>
              <button type="button" className="text-button" onClick={() => removeSkill(skill.skillId)}>
                Remove
              </button>
            </div>

            <div className="req-skill-fields">
              <label className="field">
                <span>Minimum</span>
                <select
                  value={skill.minLevel}
                  onChange={(event) =>
                    patchSkill(skill.skillId, { minLevel: Number(event.target.value) })
                  }
                >
                  {levels.map((level) => (
                    <option key={level.value} value={level.value}>
                      {level.label}
                    </option>
                  ))}
                </select>
              </label>

              <label className="field field--tiny">
                <span>People</span>
                <input
                  type="number"
                  min={1}
                  max={20}
                  value={skill.headcount}
                  onChange={(event) =>
                    patchSkill(skill.skillId, { headcount: Number(event.target.value) })
                  }
                />
              </label>
            </div>

            <label className="field">
              <span>Why</span>
              <input
                value={skill.rationale}
                onChange={(event) => patchSkill(skill.skillId, { rationale: event.target.value })}
              />
            </label>
          </div>
        ))}
      </fieldset>

      {remaining.length > 0 && (
        <div className="req-add">
          <label className="field">
            <span>Add a skill</span>
            <select value={toAdd} onChange={(event) => setToAdd(event.target.value)}>
              <option value="">Choose a skill…</option>
              {remaining.map((skill) => (
                <option key={skill.id} value={skill.id}>
                  {skill.name}
                </option>
              ))}
            </select>
          </label>
          <button type="button" className="text-button" disabled={!toAdd} onClick={addSkill}>
            Add skill
          </button>
        </div>
      )}

      {draft.outsideRole.length > 0 && (
        <section className="outside-role">
          <h3>Outside this role, not planned here</h3>
          <ul>
            {draft.outsideRole.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </section>
      )}

      <div className="req-actions">
        <button
          type="button"
          className="button-primary"
          disabled={saving || draft.skills.length === 0 || !draft.purpose.trim()}
          onClick={onConfirm}
        >
          {saving ? 'Confirming…' : mode === 'manual' ? 'Save and confirm' : 'Confirm requirements'}
        </button>
        {draft.skills.length === 0 && <p className="frame-hint">Add at least one skill.</p>}
      </div>
    </div>
  );
}
