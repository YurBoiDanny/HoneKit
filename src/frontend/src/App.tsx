import { BagScreen } from './screens/BagScreen';

const laterNav = ['Deck building', 'Meeting plan', 'Settings', 'Profile'];
const laterNavShort = ['Decks', 'Plan', 'Settings', 'Profile'];

export default function App() {
  return (
    <div className="page">
      <header className="topbar">
        <a className="wordmark" href="/">
          Hone<em>Kit</em>
        </a>

        <nav className="nav" aria-label="Main">
          <span className="is-here" aria-current="page">
            Bag
          </span>
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

      <BagScreen />

      <nav className="mobile-nav" aria-label="Main, small screens">
        <span className="is-here" aria-current="page">
          Bag
        </span>
        {laterNavShort.map((item) => (
          <span key={item} className="is-later" aria-disabled="true">
            {item}
          </span>
        ))}
      </nav>
    </div>
  );
}
