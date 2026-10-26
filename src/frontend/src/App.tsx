import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { BagScreen } from './screens/BagScreen';
import { CrewScreen } from './screens/CrewScreen';
import { DeckScreen } from './screens/DeckScreen';
import { PlanScreen } from './screens/PlanScreen';
import { TeamBagScreen } from './screens/TeamBagScreen';
import { TeammateBagScreen } from './screens/TeammateBagScreen';

export default function App() {
  const { pathname } = useLocation();
  const inBagFlow =
    pathname === '/' ||
    pathname.startsWith('/team') ||
    pathname.startsWith('/me') ||
    pathname.startsWith('/deck') ||
    pathname.startsWith('/crew');
  const onPlan = pathname.startsWith('/plan');

  return (
    <div className="page">
      <header className="topbar">
        <Link className="wordmark" to="/">
          Hone<em>Kit</em>
        </Link>

        <nav className="nav" aria-label="Main">
          <Link to="/team" className={inBagFlow ? 'is-here' : ''} aria-current={inBagFlow ? 'page' : undefined}>
            Bag
          </Link>
          <span className="is-later" aria-disabled="true" title="Later in the build">
            Deck building
          </span>
          <Link to="/plan" className={onPlan ? 'is-here' : ''} aria-current={onPlan ? 'page' : undefined}>
            Meeting plan
          </Link>
          {['Settings', 'Profile'].map((item) => (
            <span key={item} className="is-later" aria-disabled="true" title="Later in the build">
              {item}
            </span>
          ))}
        </nav>

        <Link className="identity" to="/me">
          <span className="swatch" aria-hidden="true" />
          <span>Danny</span>
        </Link>
      </header>

      <Routes>
        <Route path="/" element={<Navigate to="/team" replace />} />
        <Route path="/team" element={<TeamBagScreen />} />
        <Route path="/me" element={<BagScreen />} />
        <Route path="/deck/:deckId" element={<DeckScreen />} />
        <Route path="/crew" element={<CrewScreen />} />
        <Route path="/crew/:memberId" element={<TeammateBagScreen />} />
        <Route path="/plan" element={<PlanScreen />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>

      <nav className="mobile-nav" aria-label="Main, small screens">
        <Link to="/team" className={inBagFlow ? 'is-here' : ''} aria-current={inBagFlow ? 'page' : undefined}>
          Bag
        </Link>
        <span className="is-later" aria-disabled="true">
          Decks
        </span>
        <Link to="/plan" className={onPlan ? 'is-here' : ''} aria-current={onPlan ? 'page' : undefined}>
          Plan
        </Link>
        {['Settings', 'Profile'].map((item) => (
          <span key={item} className="is-later" aria-disabled="true">
            {item}
          </span>
        ))}
      </nav>
    </div>
  );
}
