import { useState } from 'react';
import { NavLink, useNavigate, useParams } from 'react-router-dom';
import { LANGS } from '../i18n.jsx';
import { currentTheme, toggleTheme } from '../theme.js';

function TopBar({ lang, labels, onMenu }) {
  const navigate = useNavigate();
  const { page } = useParams();
  const [theme, setThemeState] = useState(currentTheme());
  return (
    <header className="topbar">
      <button type="button" className="topbar-menu" aria-label="Menu" onClick={onMenu}>☰</button>
      <NavLink to={`/${lang.slug}/intro`} className="topbar-brand">
        <span className="brand-mark">A</span>
        <span className="brand-name">OpenALPRWrapper</span>
        <span className="brand-badge">v1.0.2</span>
      </NavLink>
      <div className="topbar-spacer" />
      <button
        type="button"
        className="theme-btn"
        aria-label={labels.themeToggle}
        title={labels.themeToggle}
        onClick={() => setThemeState(toggleTheme())}
      >
        {theme === 'dark' ? '☀' : '☾'}
      </button>
      <nav className="topbar-lang" aria-label="Language">
        {LANGS.map((l) => (
          <button
            key={l.slug}
            type="button"
            className={`lang-btn${l.slug === lang.slug ? ' active' : ''}`}
            onClick={() => navigate(`/${l.slug}/${page ?? 'intro'}`)}
          >
            {l.label}
          </button>
        ))}
      </nav>
      <a className="topbar-link" href="https://www.nuget.org/packages/OpenAlprWrapper" target="_blank" rel="noreferrer">NuGet</a>
      <a className="topbar-link" href="https://github.com/edward-hsu-1994/OpenAlprWrapper" target="_blank" rel="noreferrer">GitHub</a>
    </header>
  );
}

function Sidebar({ lang, open, onNavigate }) {
  return (
    <aside className={`sidebar${open ? ' open' : ''}`}>
      <nav>
        {lang.content.nav.map((group) => (
          <div key={group.title} className="nav-group">
            <div className="nav-group-title">{group.title}</div>
            {group.items.map((item) => (
              <NavLink
                key={item.slug}
                to={`/${lang.slug}/${item.slug}`}
                className={({ isActive }) => `nav-item${isActive ? ' active' : ''}`}
                onClick={onNavigate}
              >
                {item.title}
              </NavLink>
            ))}
          </div>
        ))}
      </nav>
      <div className="sidebar-foot">
        <a href="https://github.com/edward-hsu-1994/OpenAlprWrapper/blob/main/LICENSE" target="_blank" rel="noreferrer">AGPL-3.0-only</a>
      </div>
    </aside>
  );
}

export default function Shell({ lang, children }) {
  const [menuOpen, setMenuOpen] = useState(false);
  return (
    <div className="shell">
      <TopBar lang={lang} labels={lang.content.labels} onMenu={() => setMenuOpen((v) => !v)} />
      <div className="shell-body">
        <Sidebar lang={lang} open={menuOpen} onNavigate={() => setMenuOpen(false)} />
        {menuOpen && <div className="sidebar-scrim" onClick={() => setMenuOpen(false)} />}
        <main className="content">{children}</main>
      </div>
    </div>
  );
}
