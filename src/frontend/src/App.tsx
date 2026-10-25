import { Link, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { BagScreen } from './screens/BagScreen';
import { DeckScreen } from './screens/DeckScreen';

const laterNav = ['Deck building', 'Meeting plan', 'Settings', 'Profile'];
const laterNavShort = ['Decks', 'Plan', 'Settings', 'Profile'];

export default function App() {
  const { pathname } = useLocation();
  const inBagFlow = pathname === '/' || pathname.startsWith('/deck');

  return (
    <div className="page">
      <header className="topbar">
        <Link className="wordmark" to="/">
          Hone<em>Kit</em>
        </Link>

        <nav className="nav" aria-label="Main">
          <Link to="/" className={inBagFlow ? 'is-here' : ''} aria-current={inBagFlow ? 'page' : undefined}>
            Bag
          </Link>
          {laterNav.map((item) => (
            <span key={item} className="is-later" aria-disabled="true" title="Later in the build">
              {item}
            </span>
          ))}
        </nav>

        <div className="identity">
          <span className="swatch" aria-hidden="true" />
          <span>Danny</span>
        </div>
      </header>

      <Routes>
        <Route path="/" element={<BagScreen />} />
        <Route path="/deck/:deckId" element={<DeckScreen />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>

      <nav className="mobile-nav" aria-label="Main, small screens">
        <Link to="/" className={inBagFlow ? 'is-here' : ''} aria-current={inBagFlow ? 'page' : undefined}>
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
