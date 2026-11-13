import { useEffect } from 'react';
import { Navigate, Route, Routes, useLocation, useParams } from 'react-router-dom';
import { DEFAULT_LANG, getLang } from './i18n.jsx';
import Shell from './components/Shell.jsx';
import Blocks from './components/Blocks.jsx';

function Page() {
  const { lang: langSlug, page: pageSlug } = useParams();
  const lang = getLang(langSlug);
  const location = useLocation();

  useEffect(() => {
    window.scrollTo(0, 0);
  }, [location.pathname]);

  const page = lang.content.pages.find((p) => p.slug === pageSlug);
  useEffect(() => {
    document.documentElement.lang = lang.slug === 'zh' ? 'zh-Hant-TW' : 'en';
    document.title = page ? `${page.title} · OpenALPRWrapper Docs` : 'OpenALPRWrapper Docs';
  }, [lang.slug, page]);

  if (!page) return <Navigate to={`/${lang.slug}/intro`} replace />;

  return (
    <Shell lang={lang}>
      <article className="doc-page">
        <p className="doc-kicker">{page.kicker}</p>
        <h1>{page.title}</h1>
        <p className="doc-intro">{page.intro}</p>
        <Blocks sections={page.sections} labels={lang.content.labels} />
        <footer className="doc-foot">{lang.content.labels.footer}</footer>
      </article>
    </Shell>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to={`/${DEFAULT_LANG}/intro`} replace />} />
      <Route path="/:lang/:page" element={<Page />} />
      <Route path="*" element={<Navigate to={`/${DEFAULT_LANG}/intro`} replace />} />
    </Routes>
  );
}
