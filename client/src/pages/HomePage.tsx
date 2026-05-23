import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export function HomePage() {
  const { user } = useAuth();
  const displayName = [user?.firstName, user?.lastName].filter(Boolean).join(' ') || 'Putniče';

  return (
    <div className="home-page">
      <div className="home-page-bg" aria-hidden>
        <span className="home-blob home-blob-1" />
        <span className="home-blob home-blob-2" />
        <span className="home-blob home-blob-3" />
      </div>

      <div className="home-page-inner">
        <header className="home-hero-header">
          <p className="home-kicker">Planiranje putovanja</p>
          <h1 className="home-title">Dobrodošao/la, {displayName}</h1>
        </header>

        <div className="home-grid">
          <section className="home-card home-card-main glass-panel">
            <div className="home-card-accent" aria-hidden />
            <h2 className="home-card-heading">Tvoj nalog</h2>
            <p className="home-card-lead">
              Ulogovan/a si kao <strong>{displayName}</strong>
              {user?.email ? (
                <>
                  {' '}
                  <span className="home-email">({user.email})</span>
                </>
              ) : null}
              .
            </p>
            <div className="home-role-row">
              <span className="muted">Uloga</span>
              <span className="pill pill-lg">{user?.role}</span>
            </div>
            <ul className="home-features">
              <li>Kreiranje i uređivanje planova putovanja</li>
              <li>Pregled datuma, budžeta i napomena na jednom mestu</li>
              <li>Lista i detalji vezani isključivo za tvoj nalog</li>
            </ul>
            <div className="home-cta-row">
              <Link to="/plans" className="btn btn-glow primary btn-lg">
                Otvori planove putovanja
              </Link>
            </div>
          </section>

          <aside className="home-side" aria-hidden>
            <div className="home-side-card glass-panel">
              <span className="home-side-icon">✈</span>
              <p className="home-side-text">Sledeća destinacija čeka samo jedan klik.</p>
            </div>
            <div className="home-side-metrics">
              <div className="home-metric">
                <span className="home-metric-value">100%</span>
                <span className="home-metric-label">Tvoji podaci</span>
              </div>
              <div className="home-metric">
                <span className="home-metric-value">24/7</span>
                <span className="home-metric-label">Pristup planovima</span>
              </div>
            </div>
          </aside>
        </div>

        <div className="home-grid" style={{ marginTop: 18 }}>
          <section className="home-card glass-panel" style={{ position: 'relative' }}>
            <div className="home-card-accent" aria-hidden />
            <h2 className="home-card-heading">Deljeni planovi</h2>
            <p className="home-card-lead">
              Pregled planova koji su <strong>povezani</strong> sa tvojim nalogom nakon učitavanja QR koda.
            </p>
            <div className="home-cta-row">
              <Link to="/shared-plans" className="btn btn-glow primary btn-lg">
                Otvori deljene planove
              </Link>
            </div>
          </section>

          <section className="home-card glass-panel" style={{ position: 'relative' }}>
            <div className="home-card-accent" aria-hidden />
            <h2 className="home-card-heading">Učitaj QR</h2>
            <p className="home-card-lead">
              Učitaj PNG/JPG snimak QR koda da <strong>povežeš</strong> plan sa svojim nalogom (claim) i odmah ga otvoriš.
            </p>
            <div className="home-cta-row">
              <Link to="/share/qr" className="btn btn-glow primary btn-lg">
                Učitaj QR
              </Link>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
