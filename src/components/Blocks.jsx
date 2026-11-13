import CodeBlock from './CodeBlock.jsx';

function inline(text) {
  // `code` spans inside prose
  const parts = text.split(/(`[^`]+`)/g);
  return parts.map((p, i) =>
    p.startsWith('`') && p.endsWith('`') ? <code key={i} className="inline-code">{p.slice(1, -1)}</code> : p,
  );
}

export default function Blocks({ sections, labels }) {
  return (
    <>
      {sections.map((s, i) => {
        switch (s.t) {
          case 'h2':
            return <h2 key={i} id={s.id}>{s.text}</h2>;
          case 'p':
            return <p key={i}>{inline(s.text)}</p>;
          case 'list':
            return (
              <ul key={i}>
                {s.items.map((it, j) => (
                  <li key={j}>{inline(it)}</li>
                ))}
              </ul>
            );
          case 'code':
            return <CodeBlock key={i} lang={s.lang} title={s.title} code={s.code} copyLabel={labels.copy} copiedLabel={labels.copied} />;
          case 'table':
            return (
              <div key={i} className="table-wrap">
                <table>
                  <thead>
                    <tr>{s.head.map((h, j) => <th key={j}>{h}</th>)}</tr>
                  </thead>
                  <tbody>
                    {s.rows.map((r, j) => (
                      <tr key={j}>{r.map((c, k) => <td key={k}>{inline(c)}</td>)}</tr>
                    ))}
                  </tbody>
                </table>
              </div>
            );
          case 'note':
            return (
              <aside key={i} className={`note note-${s.kind ?? 'info'}`}>
                <span className="note-badge">{s.kind === 'warn' ? labels.warning : labels.note}</span>
                <div>{inline(s.text)}</div>
              </aside>
            );
          default:
            return null;
        }
      })}
    </>
  );
}
