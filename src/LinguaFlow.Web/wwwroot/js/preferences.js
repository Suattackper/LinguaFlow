window.linguaFlow = {
 get: key => { try { return localStorage.getItem("linguaflow." + key); } catch { return null; } },
 set: (key, value) => { try { localStorage.setItem("linguaflow." + key, value); } catch {} if (key === "sidebarCollapsed") document.documentElement.dataset.sidebar = value === "true" ? "collapsed" : "expanded"; },
 scrollToSection: id => document.getElementById(id)?.scrollIntoView({ block: "start" }),
 positionSelect: root => { const menu = root?.querySelector('.app-select-menu'); if (!menu) return; root.classList.remove('opens-up'); if (menu.getBoundingClientRect().bottom > innerHeight - 12 && root.getBoundingClientRect().top > menu.getBoundingClientRect().height + 12) root.classList.add('opens-up'); },
 copyText: value => navigator.clipboard.writeText(value),
 apply: (theme, reducedMotion, largeText, language) => {
  const systemDark = window.matchMedia("(prefers-color-scheme: dark)").matches; document.documentElement.dataset.theme = theme === "system" ? (systemDark ? "dark" : "light") : theme;
  document.documentElement.dataset.reducedMotion = reducedMotion ? "true" : "false";
  document.documentElement.dataset.largeText = largeText ? "true" : "false";
  document.documentElement.lang = language; return systemDark;
 }
};
