let listener;
let activeOwner;
let frame;
let settlingTimer;
let settlingChecks = 0;

function positionAboveFooter() {
  const footer = document.querySelector('.footer');
  if (!footer) return;
  const pager = document.querySelector('.reader-pager');
  const footerOverlap = Math.max(0, innerHeight - footer.getBoundingClientRect().top);
  const pagerBox = pager?.getBoundingClientRect();
  const pagerOverlap = pagerBox && pagerBox.bottom > 0 && pagerBox.top < innerHeight
    ? Math.max(0, innerHeight - pagerBox.top) : 0;
  const overlap = Math.max(footerOverlap, pagerOverlap);
  document.documentElement.style.setProperty('--back-to-top-bottom', `max(${Math.max(24, Math.ceil(overlap + 16))}px, env(safe-area-inset-bottom))`);
}

export function observe(owner) {
  dispose();
  activeOwner = owner;
  listener = () => {
    document.documentElement.dataset.backToTop = window.scrollY > 560 ? 'visible' : 'hidden';
    if (frame) cancelAnimationFrame(frame);
    frame = requestAnimationFrame(positionAboveFooter);
  };
  window.addEventListener('scroll', listener, { passive: true });
  window.addEventListener('hashchange', listener);
  window.addEventListener('resize', listener);
  settlingChecks = 0;
  settlingTimer = setInterval(() => {
    listener?.();
    if (++settlingChecks >= 20) { clearInterval(settlingTimer); settlingTimer = undefined; }
  }, 500);
  listener();
}

export function dispose(owner) {
  if (owner && owner !== activeOwner) return;
  if (listener) window.removeEventListener('scroll', listener);
  if (listener) window.removeEventListener('hashchange', listener);
  if (listener) window.removeEventListener('resize', listener);
  if (frame) cancelAnimationFrame(frame);
  if (settlingTimer) clearInterval(settlingTimer);
  listener = undefined;
  activeOwner = undefined;
  settlingTimer = undefined;
  delete document.documentElement.dataset.backToTop;
  document.documentElement.style.removeProperty('--back-to-top-bottom');
}
