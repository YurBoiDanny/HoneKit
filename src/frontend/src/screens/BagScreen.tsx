import type { CSSProperties } from 'react';
import { useQuery } from '@tanstack/react-query';
import { BagArt } from '../components/BagArt';
import { DEMO_MEMBER_ID, DEMO_ROLE, fetchBag, fetchTeam } from '../api/client';
import type { MemberDto } from '../api/types';

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

export function BagScreen() {
  const team = useQuery({ queryKey: ['team', DEMO_ROLE], queryFn: () => fetchTeam(DEMO_ROLE) });
  const bag = useQuery({ queryKey: ['bag', DEMO_MEMBER_ID], queryFn: () => fetchBag(DEMO_MEMBER_ID) });

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
              <h2>Your bag didn't open.</h2>
              <p>The roster service didn't answer. It may still be starting.</p>
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
                  label={`${bag.data.name}'s camera bag, empty`}
                />
              </figure>

              <div className="bag-caption">
                <h2 id="bag-heading">Nothing in it yet.</h2>
                <p>
                  The Camera Foundations deck is what puts the first tool in. Finish it well and
                  the tool arrives honed.
                </p>
                <p className="deck-hint">Camera Foundations, four cards</p>
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
