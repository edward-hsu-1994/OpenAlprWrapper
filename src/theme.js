// Theme state lives on <html data-theme="light|dark">, persisted in localStorage.
// index.html applies the stored/system choice before first paint (no flash).

export function currentTheme() {
  return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light';
}

export function setTheme(theme) {
  document.documentElement.dataset.theme = theme;
  try {
    localStorage.setItem('ow-theme', theme);
  } catch {
    /* private mode: session-only theme is fine */
  }
}

export function toggleTheme() {
  const next = currentTheme() === 'dark' ? 'light' : 'dark';
  setTheme(next);
  return next;
}
