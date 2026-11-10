import { revealMenu, revealPalette } from './motion.js';

export function showLanguageMenu(menu) {
  if (!menu?.isConnected) return;
  revealMenu(menu);
  menu.querySelector('.language-option[aria-selected="true"]')?.focus();
}

export function showCommandPalette(element) {
  if (!element?.isConnected) return;
  revealPalette(element);
  element.querySelector('.command-search')?.focus();
}
