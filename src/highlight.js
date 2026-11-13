// Minimal regex-based highlighter for csharp / bash / json. Escapes HTML first.

const CS_KEYWORDS = new Set(
  ('using var new public private internal protected sealed static class record struct interface enum if else return throw try catch finally when await async foreach for while do string int long float double bool byte void object null true false readonly get set init event delegate namespace nameof out in is not and or switch case default this base params ref unsafe fixed const partial override virtual abstract typeof as'
  ).split(' '),
);
const CS_TYPES = new Set(
  ('Task ReadOnlyMemory Memory Span CancellationToken CancellationTokenSource IEnumerable IList List IReadOnlyList Exception ArgumentException ObjectDisposedException OperationCanceledException FileNotFoundException DirectoryNotFoundException ArgumentNullException ArgumentOutOfRangeException InvalidOperationException IDisposable EventHandler Console File Directory Path AppContext IntPtr Guid DateTime TimeSpan String StringBuilder OpenAlprWrapper OpenAlprOptions OpenAlprRecognizer IPlateRecognizer OpenAlprException PlateRecognitionResult RecognizedPlate PlateCandidate PlatePoint BatchRecognitionResult BatchError PlateRecognitionEventArgs PlateErrorEventArgs PlateCompletedEventArgs NativeRecognitionResult'
  ).split(' '),
);

function escapeHtml(s) {
  return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

// Tokenizes one line into spans. States: normal / in-string.
function highlightCSharp(code) {
  const out = [];
  // Order: comment, string, number, identifier, other
  const re = /(\/\/.*$)|("(?:[^"\\]|\\.)*"?)|(\$"(?:[^"\\]|\\.)*"?)|('(?:[^'\\]|\\.)*'?)|(\b\d+(?:\.\d+)?f?\b)|([A-Za-z_][A-Za-z0-9_<>?]*)/gm;
  let last = 0;
  let m;
  while ((m = re.exec(code)) !== null) {
    if (m.index > last) out.push(escapeHtml(code.slice(last, m.index)));
    const [tok, comment, str, interpStr, chr, num, ident] = m;
    if (comment) out.push(`<span class="tok-com">${escapeHtml(tok)}</span>`);
    else if (str || interpStr || chr) out.push(`<span class="tok-str">${escapeHtml(tok)}</span>`);
    else if (num) out.push(`<span class="tok-num">${escapeHtml(tok)}</span>`);
    else if (ident) {
      const bare = ident.replace(/[<>?]/g, '');
      if (CS_KEYWORDS.has(bare)) out.push(`<span class="tok-kw">${escapeHtml(tok)}</span>`);
      else if (CS_TYPES.has(bare) || /^[A-Z]/.test(bare)) out.push(`<span class="tok-type">${escapeHtml(tok)}</span>`);
      else out.push(escapeHtml(tok));
    }
    last = m.index + tok.length;
  }
  if (last < code.length) out.push(escapeHtml(code.slice(last)));
  return out.join('');
}

function highlightBash(code) {
  return code
    .split('\n')
    .map((line) => {
      const ci = line.indexOf('#');
      if (ci === 0) return `<span class="tok-com">${escapeHtml(line)}</span>`;
      let body = ci > 0 ? line.slice(0, ci) : line;
      const com = ci > 0 ? `<span class="tok-com">${escapeHtml(line.slice(ci))}</span>` : '';
      body = escapeHtml(body)
        .replace(/(&quot;.*?&quot;|".*?"|'.*?')/g, '<span class="tok-str">$1</span>')
        .replace(/(^|\s)(-{1,2}[a-zA-Z][a-zA-Z0-9-]*)/g, '$1<span class="tok-kw">$2</span>');
      return body + com;
    })
    .join('\n');
}

function highlightJson(code) {
  return escapeHtml(code)
    .replace(/(&quot;(?:[^&]|&(?!quot;))*?&quot;)(\s*:)/g, '<span class="tok-kw">$1</span>$2')
    .replace(/: (&quot;.*?&quot;)/g, ': <span class="tok-str">$1</span>')
    .replace(/\b(true|false|null|\d+(?:\.\d+)?)\b/g, '<span class="tok-num">$1</span>');
}

export function highlight(code, lang) {
  if (lang === 'csharp' || lang === 'cs') return highlightCSharp(code);
  if (lang === 'bash' || lang === 'sh' || lang === 'powershell') return highlightBash(code);
  if (lang === 'json') return highlightJson(code);
  return escapeHtml(code);
}
