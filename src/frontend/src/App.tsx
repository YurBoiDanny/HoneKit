import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { BagScreen } from './screens/BagScreen';
import { CrewScreen } from './screens/CrewScreen';
import { DeckScreen } from './screens/DeckScreen';
import { TeamBagScreen } from './screens/TeamBagScreen';
import { TeammateBagScreen } from './screens/TeammateBagScreen';

const laterNav = ['Deck building', 'Meeting plan', 'Settings', 'Profile'];
const laterNavShort = ['Decks', 'Plan', 'Settings', 'Profile'];

export default function App() {
  const { pathname } = useLocation();
  const inBagFlow =
    pathname === '/' ||
    pathname.startsWith('/team') ||
    pathname.startsWith('/me') ||
    pathname.startsWith('/deck') ||
    pathname.startsWith('/crew');

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
          {laterNav.map((item) => (
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
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>

      <nav className="mobile-nav" aria-label="Main, small screens">
        <Link to="/team" className={inBagFlow ? 'is-here' : ''} aria-current={inBagFlow ? 'page' : undefined}>
          Bag
        </Link>
        {laterNavShort.map((item) => (
          <span key={item} className="is-later" aria-disabled="true">
            {item}
          </span>
        ))}
      </nav>
    </div>
  );
}
