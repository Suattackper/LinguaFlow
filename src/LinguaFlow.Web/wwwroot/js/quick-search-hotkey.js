// The shortcut is registered before Blazor starts, so it also works after culture reloads.
document.addEventListener('keydown', event => {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
    event.preventDefault();
    document.querySelector('.quick-search-trigger')?.click();
    return;
  }
  const overlay = document.querySelector('.command-overlay');
  if (!overlay) return;
  const results = [...overlay.querySelectorAll('.command-result')];
  if (event.key === 'Escape') {
    event.preventDefault();
    overlay.querySelector('.command-close')?.click();
    document.querySelector('.quick-search-trigger')?.focus();
  } else if (results.length && ['ArrowDown', 'ArrowUp'].includes(event.key)) {
    event.preventDefault();
    const index = results.indexOf(document.activeElement);
    results[event.key === 'ArrowDown' ? (index + 1) % results.length : (index + results.length - 1) % results.length].focus();
  } else if (event.key === 'Tab') {
    const controls = [...overlay.querySelectorAll('input, button')];
    const first = controls[0], last = controls.at(-1);
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }
});
document.documentElement.dataset.quickSearchReady = 'true';

// Language menu behavior stays available through a full culture reload.
document.addEventListener('keydown', event => {
  const root = document.activeElement?.closest('.language-switcher,.app-select');
  if (!root) return;
  const trigger = root.querySelector('.language-trigger,.app-select-trigger');
  const options = [...root.querySelectorAll('.language-option,.app-select-option')];
  if (event.key === 'Escape' && options.length) {
    event.preventDefault(); trigger?.click(); trigger?.focus();
  } else if (options.length && ['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) {
    event.preventDefault();
    const index = options.indexOf(document.activeElement);
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? options.length - 1 :
      event.key === 'ArrowDown' ? (index + 1) % options.length : (index + options.length - 1) % options.length;
    options[next].focus();
  }
});
document.addEventListener('pointerdown', event => {
  for (const menu of document.querySelectorAll('.language-menu,.app-select-menu')) {
    const root = menu.closest('.language-switcher,.app-select');
    if (root && !root.contains(event.target)) root.querySelector('.language-trigger,.app-select-trigger')?.click();
  }
});
