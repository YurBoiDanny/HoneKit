import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { BagArt } from '../components/BagArt';
import { BagSwitch } from '../components/BagSwitch';
import { CrewRail } from '../components/CrewRail';
import { DEMO_ROLE, fetchTeam, fetchTeamBag } from '../api/client';

export function TeamBagScreen() {
  const teamBag = useQuery({ queryKey: ['team-bag', DEMO_ROLE], queryFn: () => fetchTeamBag(DEMO_ROLE) });
  const team = useQuery({ queryKey: ['team', DEMO_ROLE], queryFn: () => fetchTeam(DEMO_ROLE) });

  const tools = teamBag.data?.tools ?? [];
  const earned = tools.map((tool) => ({
    skillId: tool.skillId,
    name: tool.name,
    honed: false,
    acknowledged: true,
  }));
  const fills = Object.fromEntries(tools.map((tool) => [tool.skillId, tool.percent]));

  const filled = tools.reduce((sum, tool) => sum + tool.filledLessons, 0);
  const total = tools.reduce((sum, tool) => sum + tool.totalLessons, 0);
  const overall = total === 0 ? 0 : Math.round((filled / total) * 100);

  return (
    <>
      <BagSwitch />

      <section className="intro">
        <h1>{teamBag.data?.name ?? 'Camera'}</h1>
        <p className="lede">
          The whole crew&apos;s bag. Each tool fills with the share of that skill the crew has
          actually finished.
        </p>
      </section>

      <div className="bag-layout">
        <section className="bag-stage" aria-labelledby="team-bag-heading">
          {teamBag.isPending ? (
            <div className="state" role="status">
              <p className="quiet-load">Reading the crew&apos;s bag…</p>
            </div>
          ) : teamBag.isError ? (
            <div className="state state--error" role="alert">
              <h2>The team bag didn&apos;t open.</h2>
              <p>The roster service didn&apos;t answer. It may still be starting.</p>
              <button type="button" onClick={() => teamBag.refetch()}>
                Try again
              </button>
            </div>
          ) : (
            <>
              <figure className="bag-figure">
                <BagArt
                  colour="powder"
                  tools={earned}
                  fills={fills}
                  label={`The camera crew's bag. ${tools
                    .map((tool) => `${tool.name} ${Math.round(tool.percent)}%`)
                    .join(', ')}.`}
                />
              </figure>

              <div className="bag-caption">
                <h2 id="team-bag-heading">
                  {overall}% of the crew&apos;s lessons are in.
                </h2>
                <p>
                  {teamBag.data.wear === 'worn'
                    ? 'One bag has gone quiet. The wall shows whose.'
                    : 'Everyone has answered recently.'}
                </p>

                <details className="team-numbers">
                  <summary>Read the numbers</summary>
                  <ul>
                    {tools.map((tool) => (
                      <li key={tool.skillId}>
                        <span>{tool.name}</span>
                        <span>
                          {tool.filledLessons} of {tool.totalLessons} lessons
                        </span>
                        <span>{Math.round(tool.percent)}%</span>
                      </li>
                    ))}
                  </ul>
                </details>

                <Link className="button-primary team-wall-link" to="/crew">
                  Open the bag wall
                </Link>
              </div>
            </>
          )}
        </section>

        <CrewRail
          members={team.data?.members}
          isPending={team.isPending}
          isError={team.isError}
          onRetry={() => team.refetch()}
        />
      </div>
    </>
  );
}
