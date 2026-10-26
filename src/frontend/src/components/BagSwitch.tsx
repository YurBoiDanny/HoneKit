import { Link, useLocation } from 'react-router-dom';

export function BagSwitch() {
  const { pathname } = useLocation();
  const onTeam = pathname === '/team';

  return (
    <div className="switcher-row">
      <div className="bag-switch" role="group" aria-label="Bag view">
        <Link to="/team" className={onTeam ? 'is-on' : ''} aria-current={onTeam ? 'page' : undefined}>
          Team bag
        </Link>
        <Link to="/me" className={!onTeam ? 'is-on' : ''} aria-current={!onTeam ? 'page' : undefined}>
          My bag
        </Link>
      </div>
    </div>
  );
}
