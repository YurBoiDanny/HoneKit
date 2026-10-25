import { useEffect, type CSSProperties } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { BagArt } from '../components/BagArt';
import { acknowledgeTool, DEMO_MEMBER_ID, DEMO_ROLE, fetchBag, fetchDecks, fetchTeam } from '../api/client';
import type { EarnedToolDto, MemberDto } from '../api/types';

function quietFor(member: MemberDto): string | null {
  if (!member.worn) {
    return null;
  }

  if (!member.lastActiveAt) {
    return 'quiet for a while';
  }

  const days = Math.floor((Date.now() - new Date(member.lastActiveAt).getTime()) / 86_400_000);
  return days >= 1 ? `quiet ${days} days` : 'quiet today';
}

function memberStyle(member: MemberDto): CSSProperties {
  return { '--member-colour': `var(--${member.colour})` } as CSSProperties;
}

function toolLabel(tool: EarnedToolDto): string {
  return tool.honed ? `${tool.name} (honed)` : tool.name;
}

export function BagScreen() {
  const queryClient = useQueryClient();
  const team = useQuery({ queryKey: ['team', DEMO_ROLE], queryFn: () => fetchTeam(DEMO_ROLE) });
  const bag = useQuery({ queryKey: ['bag', DEMO_MEMBER_ID], queryFn: () => fetchBag(DEMO_MEMBER_ID) });
  const decks = useQuery({ queryKey: ['decks', DEMO_ROLE], queryFn: () => fetchDecks(DEMO_ROLE) });

  const deck = decks.data?.[0];
  const tools = bag.data?.tools ?? [];
  const dropping = tools.find((tool) => !tool.acknowledged) ?? null;

  // A tool that has not been seen yet plays its drop once, then is acknowledged so it stays put.
  useEffect(() => {
    if (!dropping) {
      return;
    }

    const timer = window.setTimeout(() => {
      void acknowledgeTool(DEMO_MEMBER_ID, dropping.skillId)
        .then(() => queryClient.invalidateQueries({ queryKey: ['bag', DEMO_MEMBER_ID] }))
        .catch(() => undefined);
    }, 1000);

    return () => window.clearTimeout(timer);
  }, [dropping?.skillId, queryClient]);

  return (
    <>
      <section className="intro">
        <h1>Your bag</h1>
        <p className="lede">
          What you learn on the camera crew turns into gear. Every card you get right brings
          something closer to the bag.
        </p>
      </section>

      <div className="bag-layout">
        <section className="bag-stage" aria-labelledby="bag-heading">
          {bag.isPending ? (
            <div className="state" role="status">
              <p className="quiet-load">Opening your bag…</p>
            </div>
          ) : bag.isError ? (
            <div className="state state--error" role="alert">
              <h2>Your bag didn&apos;t open.</h2>
              <p>The roster service didn&apos;t answer. It may still be starting.</p>
              <button type="button" onClick={() => bag.refetch()}>
                Try again
              </button>
            </div>
          ) : (
            <>
              <figure className="bag-figure">
                <BagArt
                  colour={bag.data.colour}
                  worn={bag.data.worn}
                  tools={tools}
                  droppingSkillId={dropping?.skillId}
                  label={
                    tools.length === 0
                      ? `${bag.data.name}'s camera bag, empty`
                      : `${bag.data.name}'s camera bag, ${tools.map(toolLabel).join(', ')} earned`
                  }
                />
              </figure>

              {dropping && (
                <p className="sr-only" role="status">
                  {dropping.name} landed in your bag.
                </p>
              )}

              <div className="bag-caption">
                <h2 id="bag-heading">
                  {tools.length === 0
                    ? 'Nothing in it yet.'
                    : tools.length === 1
                      ? `${tools[0].name} is in the bag${tools[0].honed ? ', honed' : ''}.`
                      : `${tools.length} tools in the bag.`}
                </h2>
                <p>
                  {tools.length === 0
                    ? 'The Camera Foundations deck is what puts the first tool in.'
                    : `Earned: ${tools.map(toolLabel).join(', ')}. Running the deck again is how you keep them sharp.`}
                </p>
                {deck && (
                  <Link className="button-primary" to={`/deck/${deck.id}`}>
                    {tools.length === 0 ? `Start ${deck.name}` : `Run ${deck.name} again`}
                  </Link>
                )}
              </div>
            </>
          )}
        </section>

        <aside className="crew" aria-labelledby="crew-heading">
          <h2 id="crew-heading">The camera crew</h2>
          <p className="crew-sub">Everyone who serves on camera, primary or second role.</p>

          {team.isPending ? (
            <p className="quiet-load">Loading the crew…</p>
          ) : team.isError ? (
            <div role="alert">
              <p className="quiet-load">The crew list is unavailable right now.</p>
              <button type="button" onClick={() => team.refetch()} className="text-button">
                Try again
              </button>
            </div>
          ) : (
            <ul className="crew-list">
              {team.data.members.map((member) => {
                const isYou = member.id === DEMO_MEMBER_ID;
                const note = isYou ? 'you' : quietFor(member);

                return (
                  <li
                    key={member.id}
                    className={[
                      'crew-member',
                      isYou ? 'crew-member--you' : '',
                      member.worn ? 'crew-member--worn' : '',
                    ]
                      .filter(Boolean)
                      .join(' ')}
                    style={memberStyle(member)}
                  >
                    <span className="swatch" aria-hidden="true" />
                    <span className="name">{member.name}</span>
                    <span className="note">{note}</span>
                  </li>
                );
              })}
            </ul>
          )}
        </aside>
      </div>
    </>
  );
}
