let active = false, last = 0, elapsed = 0;
function eligible() { return !document.hidden && document.hasFocus(); }
function tick() { if (active && last) elapsed += Math.max(0, Math.min(performance.now() - last, 60000)); last = eligible() ? performance.now() : 0; }
function visibility() { tick(); }
export function start() { stop(); elapsed = 0; active = true; last = eligible() ? performance.now() : 0; document.addEventListener("visibilitychange", visibility); window.addEventListener("blur", visibility); window.addEventListener("focus", visibility); }
export function stop() { if (active) tick(); active = false; last = 0; document.removeEventListener("visibilitychange", visibility); window.removeEventListener("blur", visibility); window.removeEventListener("focus", visibility); return Math.min(120, Math.floor(elapsed / 60000)); }
