import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { BagArt } from '../components/BagArt';
import { BagHub } from '../components/BagHub';
import { DEMO_ROLE, fetchBag, fetchTeam } from '../api/client';
import { quietSentence } from '../lib/member';

export function TeammateBagScreen() {
  const { memberId = '' } = useParams();
  const bag = useQuery({
    queryKey: ['bag', memberId],
    queryFn: () => fetchBag(memberId),
    enabled: memberId.length > 0,
  });
  const team = useQuery({ queryKey: ['team', DEMO_ROLE], queryFn: () => fetchTeam(DEMO_ROLE) });
  const [hubOpen, setHubOpen] = useState(false);
  const member = team.data?.members.find((candidate) => candidate.id === memberId);

  return (
    <section className="deck-screen teammate">
      <div className="deck-bar">
        <Link className="text-link" to="/crew">
          Back to the crew
        </Link>
      </div>

      {bag.isPending ? (
        <p className="quiet-load" role="status">
          Opening the bag…
        </p>
      ) : bag.isError ? (
        <div className="state state--error" role="alert">
          <h2>That bag didn&apos;t open.</h2>
          <p>The roster service didn&apos;t answer. It may still be starting.</p>
          <button type="button" onClick={() => bag.refetch()}>
            Try again
          </button>
        </div>
      ) : (
        <>
          <header className="teammate-head">
            <h1>{bag.data.name}&apos;s bag</h1>
            <p className="teammate-note">
              {member
                ? (quietSentence(member) ?? 'Serving on camera.')
                : bag.data.worn
                  ? 'Has been quiet for a while.'
                  : 'Serving on camera.'}
            </p>
          </header>

          <div className="teammate-layout">
            <figure className="teammate-bag">
              <button
                type="button"
                className="bag-open"
                aria-label={`Open ${bag.data.name}'s bag`}
                onClick={() => setHubOpen(true)}
              >
                <BagArt
                  colour={bag.data.colour}
                  worn={bag.data.worn}
                  tools={bag.data.tools}
                  label={`${bag.data.name}'s camera bag, ${
                    bag.data.tools.length === 0
                      ? 'empty'
                      : `${bag.data.tools.length} tools earned`
                  }`}
                />
              </button>
            </figure>

            <div className="teammate-kit">
              <h2>{bag.data.tools.length === 0 ? 'Nothing earned yet.' : 'In the bag'}</h2>
              {bag.data.tools.length === 0 ? (
                <p>An empty bag is where everyone starts.</p>
              ) : (
                <ul>
                  {bag.data.tools.map((tool) => (
                    <li key={tool.skillId}>
                      <span>{tool.name}</span>
                      {tool.honed && <small>honed</small>}
                    </li>
                  ))}
                </ul>
              )}
              <p className="read-only">Read-only.</p>
            </div>
          </div>
        </>
      )}

      <BagHub
        open={hubOpen}
        target={{ kind: 'member', memberId, editable: false }}
        onClose={() => setHubOpen(false)}
      />
    </section>
  );
}
