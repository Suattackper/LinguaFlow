import { animate, stagger } from './vendor/anime.esm.min.js';

// One observer owns page entrances as Blazor replaces nodes on route changes.
const selectors = [
  '.hero', '.hero-actions > *', '.language-card', '.course-tile',
  '.dashboard-metrics article', '.dashboard-card', '.info-panel', '.page-heading',
  '.roadmap-card', '.lesson-section', '.reader-toolbar', '.exercise-card',
  '.interactive-exercise', '.exercise-feedback', '.vocabulary-tile',
  '.flashcard', '.practice-wizard > section', '.practice-wizard > .info-panel',
  '.settings-group', '.material-card', '.help-article', '.help-faq details',
  '.unit-progress-chart', '.course-detail', '.state-message'
].join(',');
let root, intersection, mutations;
let seen = new WeakSet();
const active = new Set();
const reduced = () => document.documentElement.dataset.reducedMotion === 'true' ||
  matchMedia('(prefers-reduced-motion: reduce)').matches;

function enter(element) {
  if (reduced() || !element.isConnected) return;
  const animation = animate(element, {
    opacity: [0.78, 1], y: [10, 0], duration: 420, ease: 'out(3)',
    onComplete: () => active.delete(animation)
  });
  active.add(animation);
}

function scan() {
  if (!root || !intersection) return;
  for (const element of root.querySelectorAll(selectors)) {
    if (seen.has(element)) continue;
    seen.add(element);
    intersection.observe(element);
  }
}

export function initialize(selector = '#main') {
  dispose();
  root = document.querySelector(selector);
  if (!root || !('IntersectionObserver' in window)) return;
  intersection = new IntersectionObserver(entries => {
    for (const entry of entries) {
      if (!entry.isIntersecting) continue;
      intersection.unobserve(entry.target);
      enter(entry.target);
    }
  }, { threshold: .06, rootMargin: '0px 0px -16px 0px' });
  mutations = new MutationObserver(scan);
  mutations.observe(root, { childList: true, subtree: true });
  scan();
}

export function revealMenu(element) {
  if (reduced()) return;
  const animation = animate(element, { opacity: [0, 1], y: [-7, 0],
    scale: [.97, 1], duration: 210, ease: 'out(3)' });
  active.add(animation);
  animation.then(() => active.delete(animation));
}

export function revealPalette(element) {
  if (reduced()) return;
  const panel = element.querySelector('.command-panel');
  if (!panel) return;
  const animation = animate(panel, { opacity: [0, 1], y: [-12, 0],
    scale: [.98, 1], duration: 260, ease: 'out(3)' });
  active.add(animation);
  animation.then(() => active.delete(animation));
  const items = element.querySelectorAll('.command-result');
  if (!items.length) return;
  const rows = animate(items, { opacity: [0, 1], y: [6, 0],
    delay: stagger(24), duration: 240, ease: 'out(3)' });
  active.add(rows);
  rows.then(() => active.delete(rows));
}

export function dispose() {
  mutations?.disconnect();
  intersection?.disconnect();
  for (const animation of active) animation.cancel();
  active.clear();
  mutations = undefined;
  intersection = undefined;
  root = undefined;
  seen = new WeakSet();
}
