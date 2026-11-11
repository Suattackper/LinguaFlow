import { revealMenu, revealPalette } from './motion.js';

let languageListener;
let languageMenu;
let languageTrigger;

export function showLanguageMenu(menu, trigger) {
  if (!menu?.isConnected) return;
  hideLanguageMenu(false);
  languageMenu = menu;
  languageTrigger = trigger;
  revealMenu(menu);
  menu.querySelector('.language-option[aria-selected="true"]')?.focus();
  languageListener = event => {
    if (event.key === 'Escape') { event.preventDefault(); trigger?.click(); return; }
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
    event.preventDefault();
    const options = [...event.currentTarget.querySelectorAll('.language-option')];
    const index = options.indexOf(document.activeElement);
    const target = options[(index + (event.key === 'ArrowDown' ? 1 : -1) + options.length) % options.length];
    queueMicrotask(() => { if (target?.isConnected) target.focus(); });
  };
  menu.addEventListener('keydown', languageListener);
}

export function hideLanguageMenu(restoreFocus = true) {
  if (languageMenu && languageListener) languageMenu.removeEventListener('keydown', languageListener);
  if (restoreFocus && languageTrigger?.isConnected) languageTrigger.focus();
  languageListener = undefined;
  languageMenu = undefined;
  languageTrigger = undefined;
}

let paletteListener;
let paletteTrigger;

export function showCommandPalette(element, trigger) {
  if (!element?.isConnected) return;
  hideCommandPalette(false);
  paletteTrigger = trigger;
  revealPalette(element);
  element.querySelector('.command-search')?.focus();
  paletteListener = event => {
    if (event.key === 'Escape') {
      event.preventDefault();
      element.querySelector('.command-close')?.click();
      return;
    }
    if (event.key !== 'Tab') return;
    const focusable = [...element.querySelectorAll('input:not([disabled]),button:not([disabled]),a[href]')]
      .filter(node => node.getClientRects().length > 0);
    if (!focusable.length) { event.preventDefault(); return; }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  };
  document.addEventListener('keydown', paletteListener);
}

export function hideCommandPalette(restoreFocus = true) {
  if (paletteListener) document.removeEventListener('keydown', paletteListener);
  if (restoreFocus && paletteTrigger?.isConnected) paletteTrigger.focus();
  paletteListener = undefined;
  paletteTrigger = undefined;
}
