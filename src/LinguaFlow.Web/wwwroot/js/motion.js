const targets = '.hero, .language-card, .course-tile, .roadmap-card, .dashboard-metrics article, .info-panel, .lesson-section, .vocabulary-tile, .material-card';
let root;
let intersection;
let mutations;
let seen = new WeakSet();

function reduced() {
  return document.documentElement.dataset.reducedMotion === 'true' ||
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

function animate(element) {
  if (reduced() || !element.isConnected) return;
  element.animate([
    { opacity: .72, transform: 'translateY(12px)' },
    { opacity: 1, transform: 'translateY(0)' }
  ], { duration: 420, easing: 'cubic-bezier(.2,.8,.2,1)', fill: 'none' });
}

function scan() {
  if (!root || !intersection) return;
  for (const element of root.querySelectorAll(targets)) {
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
      animate(entry.target);
    }
  }, { threshold: .08, rootMargin: '0px 0px -24px 0px' });
  mutations = new MutationObserver(scan);
  mutations.observe(root, { childList: true, subtree: true });
  scan();
}

export function dispose() {
  mutations?.disconnect();
  intersection?.disconnect();
  mutations = undefined;
  intersection = undefined;
  root = undefined;
  seen = new WeakSet();
}
