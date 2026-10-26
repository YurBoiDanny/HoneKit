import type { CSSProperties } from 'react';
import { Link } from 'react-router-dom';
import { DEMO_MEMBER_ID } from '../api/client';
import { quietFor } from '../lib/member';
import type { MemberDto } from '../api/types';

type CrewRailProps = {
  members: MemberDto[] | undefined;
  isPending: boolean;
  isError: boolean;
  onRetry: () => void;
  wallLink?: boolean;
};

function memberStyle(member: MemberDto): CSSProperties {
  return { '--member-colour': `var(--${member.colour})` } as CSSProperties;
}

export function CrewRail({
  members,
  isPending,
  isError,
  onRetry,
  wallLink = true,
}: CrewRailProps) {
  return (
    <aside className="crew" aria-labelledby="crew-heading">
      <h2 id="crew-heading">The camera crew</h2>
      <p className="crew-sub">Everyone who serves on camera, primary or second role.</p>

      {isPending ? (
        <p className="quiet-load">Loading the crew…</p>
      ) : isError || !members ? (
        <div role="alert">
          <p className="quiet-load">The crew list is unavailable right now.</p>
          <button type="button" onClick={onRetry} className="text-button">
            Try again
          </button>
        </div>
      ) : (
        <>
          <ul className="crew-list">
            {members.map((member) => {
              const isYou = member.id === DEMO_MEMBER_ID;
              const note = isYou ? 'you' : quietFor(member);

              return (
                <li key={member.id}>
                  <Link
                    className={[
                      'crew-member',
                      isYou ? 'crew-member--you' : '',
                      member.worn ? 'crew-member--worn' : '',
                    ]
                      .filter(Boolean)
                      .join(' ')}
                    style={memberStyle(member)}
                    to={isYou ? '/me' : `/crew/${member.id}`}
                  >
                    <span className="swatch" aria-hidden="true" />
                    <span className="name">{member.name}</span>
                    <span className="note">{note}</span>
                  </Link>
                </li>
              );
            })}
          </ul>

          {wallLink && (
            <Link className="text-link crew-wall-link" to="/crew">
              See every bag on the wall
            </Link>
          )}
        </>
      )}
    </aside>
  );
}
