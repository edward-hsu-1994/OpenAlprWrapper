import { useState } from 'react';
import { highlight } from '../highlight.js';

export default function CodeBlock({ lang = 'text', title, code, copyLabel = 'Copy', copiedLabel = 'Copied' }) {
  const [copied, setCopied] = useState(false);

  async function onCopy() {
    try {
      await navigator.clipboard.writeText(code);
    } catch {
      const ta = document.createElement('textarea');
      ta.value = code;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand('copy');
      ta.remove();
    }
    setCopied(true);
    setTimeout(() => setCopied(false), 1600);
  }

  return (
    <figure className="codeblock">
      <figcaption className="codeblock-bar">
        <span className="codeblock-title">{title ?? lang}</span>
        <button type="button" className="codeblock-copy" onClick={onCopy}>
          {copied ? copiedLabel : copyLabel}
        </button>
      </figcaption>
      <pre>
        <code dangerouslySetInnerHTML={{ __html: highlight(code.trimEnd(), lang) }} />
      </pre>
    </figure>
  );
}
