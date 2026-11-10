let drawer;
let trigger;
let listener;

export function open() {
  close(false);
  drawer = document.getElementById('mobile-navigation');
  trigger = document.querySelector('.mobile-menu');
  if (!drawer || !trigger) return;
  listener = event => {
    if (event.key === 'Escape') {
      event.preventDefault();
      trigger.click();
      return;
    }
    if (event.key !== 'Tab') return;
    const focusable = [...drawer.querySelectorAll('a[href],button:not([disabled]),input:not([disabled]),select:not([disabled])')]
      .filter(element => element.getClientRects().length > 0);
    if (!focusable.length) { event.preventDefault(); return; }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  };
  document.addEventListener('keydown', listener);
  drawer.querySelector('a[href],button:not([disabled])')?.focus();
}

export function close(restoreFocus = true) {
  if (listener) document.removeEventListener('keydown', listener);
  if (restoreFocus && drawer && trigger) trigger.focus();
  listener = undefined;
  drawer = undefined;
  trigger = undefined;
}
