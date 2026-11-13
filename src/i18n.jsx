import en from './content/en.js';
import zhTW from './content/zhTW.js';

export const LANGS = [
  { slug: 'en', label: 'English', content: en },
  { slug: 'zh', label: '繁體中文', content: zhTW },
];

export const DEFAULT_LANG = 'en';

export function getLang(slug) {
  return LANGS.find((l) => l.slug === slug) ?? LANGS[0];
}
