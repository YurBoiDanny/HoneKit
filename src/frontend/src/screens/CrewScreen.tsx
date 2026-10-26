import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BagArt } from '../components/BagArt';
import { DEMO_MEMBER_ID, DEMO_ROLE, fetchBagWall } from '../api/client';
import { quietFor } from '../lib/member';
import type { BagWallMemberDto, EarnedToolDto } from '../api/types';

function wallTools(member: BagWallMemberDto): EarnedToolDto[] {
  return member.tools.map((name) => ({
    skillId: name.toLowerCase(),
    name,
    honed: false,
    acknowledged: true,
  }));
}

export function CrewScreen() {
  const wall = useQuery({ queryKey: ['bag-wall', DEMO_ROLE], queryFn: () => fetchBagWall(DEMO_ROLE) });

  return (
    <section className="wall-screen">
      <div className="deck-bar">
        <Link className="text-link" to="/">
          Back to your bag
        </Link>
      </div>

      <header className="wall-head">
        <h1>The camera crew</h1>
        <p>Every bag, side by side. Open one to see what they carry.</p>
      </header>

      {wall.isPending ? (
        <p className="quiet-load" role="status">
          Laying the bags out…
        </p>
      ) : wall.isError ? (
        <div className="state state--error" role="alert">
          <h2>The wall didn&apos;t open.</h2>
          <p>The roster service didn&apos;t answer. It may still be starting.</p>
          <button type="button" onClick={() => wall.refetch()}>
            Try again
          </button>
        </div>
      ) : (
        <ul className="wall-grid">
          {wall.data.members.map((member) => {
            const isYou = member.id === DEMO_MEMBER_ID;
            const note = isYou ? 'your bag' : quietFor(member);
            const count = member.tools.length;

            return (
              <li key={member.id}>
                <Link
                  className="wall-member"
                  to={isYou ? '/' : `/crew/${member.id}`}
                  aria-label={isYou ? 'Your bag' : `${member.name}'s bag`}
                >
                  <BagArt
                    colour={member.colour}
                    worn={member.worn}
                    tools={wallTools(member)}
                    label={`${member.name}'s bag, ${
                      count === 0 ? 'empty' : `${count} ${count === 1 ? 'tool' : 'tools'}`
                    }`}
                  />
                  <div className="wall-meta">
                    <span className="wall-name">{member.name}</span>
                    <span className="wall-count">
                      {count === 0 ? 'empty' : `${count} ${count === 1 ? 'tool' : 'tools'}`}
                    </span>
                  </div>
                  {note && <span className="wall-note">{note}</span>}
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
