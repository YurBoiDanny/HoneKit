import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { BagArt } from '../components/BagArt';
import { BagHub } from '../components/BagHub';
import {
  acknowledgeTool,
  DEMO_MEMBER_ID,
  DEMO_ROLE,
  fetchBag,
  fetchDecks,
  fetchTeam,
  updateBagStyle,
} from '../api/client';
import type { BagDto, EarnedToolDto } from '../api/types';
import { BagSwitch } from '../components/BagSwitch';
import { CrewRail } from '../components/CrewRail';

const colours = ['sage', 'rose', 'powder', 'butter', 'brass', 'teal'] as const;
const styles = ['backpack', 'duffel', 'sling'] as const;

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

  const [savingStyle, setSavingStyle] = useState(false);
  const [hubOpen, setHubOpen] = useState(false);

  async function personalize(colour: string, style: string) {
    if (!bag.data || savingStyle) {
      return;
    }

    const previous = { colour: bag.data.colour, style: bag.data.style };
    setSavingStyle(true);
    queryClient.setQueryData<BagDto>(['bag', DEMO_MEMBER_ID], (old) =>
      old ? { ...old, colour, style } : old,
    );

    try {
      await updateBagStyle(DEMO_MEMBER_ID, colour, style);
    } catch {
      queryClient.setQueryData<BagDto>(['bag', DEMO_MEMBER_ID], (old) =>
        old ? { ...old, ...previous } : old,
      );
    } finally {
      setSavingStyle(false);
    }
  }

  return (
    <>
      <BagSwitch />

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
                <button
                  type="button"
                  className="bag-open"
                  aria-label="Open your bag"
                  onClick={() => setHubOpen(true)}
                >
                  <BagArt
                    colour={bag.data.colour}
                    worn={bag.data.worn}
                    style={bag.data.style}
                    tools={tools}
                    droppingSkillId={dropping?.skillId}
                    label={
                      tools.length === 0
                        ? `${bag.data.name}'s camera bag, empty`
                        : `${bag.data.name}'s camera bag, ${tools.map(toolLabel).join(', ')} earned`
                    }
                  />
                </button>
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

                <div className="personalise">
                  <h2>Make it yours</h2>
                  <p>Colour and style follow your bag into every view.</p>

                  <div className="colour-row" role="group" aria-label="Bag colour">
                    {colours.map((colour) => (
                      <button
                        key={colour}
                        type="button"
                        className={`colour-dot${bag.data.colour === colour ? ' is-on' : ''}`}
                        style={{ background: `var(--${colour})` }}
                        aria-label={colour.charAt(0).toUpperCase() + colour.slice(1)}
                        aria-pressed={bag.data.colour === colour}
                        onClick={() => void personalize(colour, bag.data.style)}
                      />
                    ))}
                  </div>

                  <div className="style-row" role="group" aria-label="Bag style">
                    {styles.map((style) => (
                      <button
                        key={style}
                        type="button"
                        className={`style-choice${bag.data.style === style ? ' is-on' : ''}`}
                        aria-pressed={bag.data.style === style}
                        onClick={() => void personalize(bag.data.colour, style)}
                      >
                        {style}
                      </button>
                    ))}
                  </div>

                  {savingStyle && (
                    <p className="sr-only" role="status">
                      Saving your bag.
                    </p>
                  )}
                </div>
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

      <BagHub
        open={hubOpen}
        target={{ kind: 'member', memberId: DEMO_MEMBER_ID, editable: true }}
        onClose={() => setHubOpen(false)}
      />
    </>
  );
}
