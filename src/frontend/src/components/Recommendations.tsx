import { useEffect, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  assignDeck,
  checkOverrides,
  DEMO_ROLE,
  fetchPlan,
  fetchPlans,
  fetchRecommendations,
  fetchTeam,
  savePlan,
} from '../api/client';
import type {
  OverrideWarningDto,
  PlanDto,
  RecommendationGapDto,
  TeamDto,
} from '../api/types';

type RecommendationsProps = {
  requirementId: string;
};

const strategies = [
  {
    id: 'critical',
    label: 'Critical',
    blurb: 'Cover every required skill. The meeting cannot afford a miss.',
  },
  {
    id: 'balanced',
    label: 'Balanced',
    blurb: 'Pair the strongest member with someone still learning the same skill.',
  },
  {
    id: 'develop',
    label: 'Develop',
    blurb: 'Prefer people below level and show what the training costs.',
  },
];

function names(team: TeamDto | undefined, ids: string[]): string {
  if (!team) {
    return `${ids.length} ${ids.length === 1 ? 'person' : 'people'}`;
  }

  return ids
    .map((id) => team.members.find((member) => member.id === id)?.name)
    .filter(Boolean)
    .join(', ');
}

export function Recommendations({ requirementId }: RecommendationsProps) {
  const queryClient = useQueryClient();
  const [strategy, setStrategy] = useState('critical');
  const [selected, setSelected] = useState<string[]>([]);
  const [warnings, setWarnings] = useState<OverrideWarningDto[]>([]);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState<PlanDto | null>(null);
  const [assigned, setAssigned] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [viewedPlan, setViewedPlan] = useState<PlanDto | null>(null);

  const rec = useQuery({
    queryKey: ['recommendations', requirementId, strategy],
    queryFn: () => fetchRecommendations(requirementId, strategy),
  });
  const team = useQuery({ queryKey: ['team', DEMO_ROLE], queryFn: () => fetchTeam(DEMO_ROLE) });
  const plans = useQuery({ queryKey: ['plans'], queryFn: fetchPlans });

  useEffect(() => {
    if (rec.data) {
      setSelected(rec.data.suggestions.map((person) => person.memberId));
      setWarnings([]);
      setSaved(null);
    }
  }, [rec.data]);

  const blurb = strategies.find((entry) => entry.id === strategy)?.blurb ?? '';

  async function toggle(memberId: string) {
    const next = selected.includes(memberId)
      ? selected.filter((id) => id !== memberId)
      : [...selected, memberId];

    setSelected(next);
    setWarnings([]);

    if (next.length === 0) {
      return;
    }

    try {
      setWarnings(await checkOverrides(requirementId, next));
    } catch {
      setWarnings([]);
    }
  }

  async function save() {
    setSaving(true);
    setError(null);

    try {
      const plan = await savePlan(requirementId, strategy, selected);
      setSaved(plan);
      await queryClient.invalidateQueries({ queryKey: ['plans'] });
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'The plan was not saved.');
    } finally {
      setSaving(false);
    }
  }

  async function assign(gap: RecommendationGapDto) {
    if (!gap.deckId) {
      return;
    }

    try {
      await assignDeck(gap.deckId, gap.suggestedMemberIds);
      setAssigned((current) => [...current, gap.skillId]);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'The deck was not assigned.');
    }
  }

  async function openPlan(id: string) {
    try {
      setViewedPlan(await fetchPlan(id));
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'That plan could not be opened.');
    }
  }

  const warningFor = (memberId: string) =>
    warnings.find((warning) => warning.memberId === memberId)?.warnings ?? [];

  return (
    <section className="recommendations" aria-labelledby="rec-heading">
      <header className="rec-head">
        <div>
          <h2 id="rec-heading">Who serves this meeting</h2>
          <p className="rec-blurb">{blurb}</p>
        </div>
        <div className="strategy-switch" role="group" aria-label="Selection strategy">
          {strategies.map((entry) => (
            <button
              key={entry.id}
              type="button"
              className={strategy === entry.id ? 'is-on' : ''}
              aria-pressed={strategy === entry.id}
              onClick={() => setStrategy(entry.id)}
            >
              {entry.label}
            </button>
          ))}
        </div>
      </header>

      {error && (
        <div className="req-errors" role="alert">
          <p>{error}</p>
        </div>
      )}

      {rec.isPending ? (
        <p className="quiet-load" role="status">
          Reading the crew against the brief…
        </p>
      ) : rec.isError ? (
        <div className="state state--error" role="alert">
          <h2>The roster didn&apos;t load.</h2>
          <p>The recommendations service didn&apos;t answer. It may still be starting.</p>
          <button type="button" onClick={() => rec.refetch()}>
            Try again
          </button>
        </div>
      ) : (
        <>
          <p className="rec-coverage">
            {Math.round(rec.data.coverage)}% of the required skills are covered by this roster.
          </p>

          <ol className="rec-people">
            {rec.data.suggestions.map((person) => (
              <li className="rec-person" key={person.memberId}>
                <div className="rec-person-head">
                  <span
                    className="rec-swatch"
                    style={{ background: `var(--${person.colour})` }}
                    aria-hidden="true"
                  />
                  <h3>{person.name}</h3>
                  <span className="rec-readiness">{Math.round(person.readiness)}%</span>
                </div>
                <ul className="rec-reasons">
                  {person.reasons.map((reason) => (
                    <li key={reason}>{reason}</li>
                  ))}
                </ul>
                {person.warnings.length > 0 && (
                  <ul className="rec-warnings">
                    {person.warnings.map((warning) => (
                      <li key={warning}>{warning}</li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ol>

          {rec.data.gaps.length > 0 && (
            <section className="rec-block">
              <h3>Gaps to close</h3>
              <ul className="rec-gap-list">
                {rec.data.gaps.map((gap) => (
                  <li key={gap.skillId}>
                    <p>
                      <strong>{gap.name}</strong> needs {gap.shortfall} more{' '}
                      {gap.shortfall === 1 ? 'person' : 'people'} at level.
                    </p>
                    <div className="rec-gap-actions">
                      {gap.deckId ? (
                        assigned.includes(gap.skillId) ? (
                          <span className="rec-done">Deck assigned.</span>
                        ) : (
                          <button type="button" className="text-button" onClick={() => void assign(gap)}>
                            Send the deck to {names(team.data, gap.suggestedMemberIds)}
                          </button>
                        )
                      ) : (
                        <span className="quiet-load">No deck for this skill yet.</span>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            </section>
          )}

          {rec.data.sustain.length > 0 && (
            <section className="rec-block">
              <h3>Keep them sharp</h3>
              <ul className="rec-sustain">
                {rec.data.sustain.map((entry) => (
                  <li key={entry.memberId}>
                    <strong>{entry.name}</strong> {entry.reason}
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section className="rec-block">
            <h3>Hand-pick</h3>
            <p className="rec-blurb">
              Change the roster if you like. The app says what each choice costs; it does not block
              you.
            </p>

            <ul className="pick-list">
              {team.data?.members.map((member) => {
                const memberWarnings = warningFor(member.id);
                return (
                  <li key={member.id}>
                    <label className="check-row">
                      <input
                        type="checkbox"
                        checked={selected.includes(member.id)}
                        onChange={() => void toggle(member.id)}
                      />
                      <span>{member.name}</span>
                    </label>
                    {memberWarnings.length > 0 && (
                      <ul className="rec-warnings">
                        {memberWarnings.map((warning) => (
                          <li key={warning}>{warning}</li>
                        ))}
                      </ul>
                    )}
                  </li>
                );
              })}
            </ul>

            <div className="rec-actions">
              <button
                type="button"
                className="button-primary"
                disabled={saving || selected.length === 0}
                onClick={() => void save()}
              >
                {saving ? 'Saving…' : 'Save plan'}
              </button>
              {saved && (
                <p className="rec-done" role="status">
                  Plan saved with {saved.members.length} people.
                </p>
              )}
            </div>
          </section>

          {plans.isSuccess && (
            <section className="rec-block">
              <h3>Saved plans</h3>
              {plans.data.length === 0 ? (
                <p className="quiet-load">No plans saved yet.</p>
              ) : (
                <ul className="saved-plans">
                  {plans.data.map((plan) => (
                    <li key={plan.id}>
                      <button type="button" className="text-button" onClick={() => void openPlan(plan.id)}>
                        {plan.purpose}
                      </button>
                      <span>
                        {plan.strategy}, {plan.memberCount} people
                      </span>
                    </li>
                  ))}
                </ul>
              )}

              {viewedPlan && (
                <div className="rec-viewed">
                  <h4>{viewedPlan.strategy} plan</h4>
                  <ul>
                    {viewedPlan.members.map((member) => (
                      <li key={member.memberId}>
                        <span>{member.name}</span>
                        <span>{member.atLevel ? 'at level' : 'below level'}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </section>
          )}
        </>
      )}
    </section>
  );
}
