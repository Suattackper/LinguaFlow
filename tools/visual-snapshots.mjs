import { spawn } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';

const [target, output] = process.argv.slice(2);
if (!target || !output) throw new Error('Usage: node tools/visual-snapshots.mjs <app-url> <output-dir>');
const chrome = process.env.CHROME_BIN || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
if (!existsSync(chrome)) throw new Error('Chrome is unavailable');
const folder = resolve(output);
mkdirSync(folder, { recursive: true });
const profile = mkdtempSync(join(tmpdir(), 'linguaflow-visual-'));
const child = spawn(chrome, ['--headless=new', '--no-sandbox', '--disable-gpu', '--no-first-run', '--no-default-browser-check', `--user-data-dir=${profile}`, '--remote-debugging-port=0', target], { stdio: 'ignore' });
const pause = ms => new Promise(resolve => setTimeout(resolve, ms));
async function until(fn, timeout = 50000) { const end = Date.now() + timeout; while (Date.now() < end) { const result = await Promise.resolve().then(fn).catch(() => null); if (result) return result; await pause(250); } throw new Error('Browser timed out'); }
let ws;
try {
  const port = await until(() => { const path = join(profile, 'DevToolsActivePort'); return existsSync(path) && readFileSync(path, 'utf8').split('\n')[0]; });
  const targetWs = await until(async () => (await (await fetch(`http://127.0.0.1:${port}/json`)).json()).find(x => x.type === 'page')?.webSocketDebuggerUrl);
  ws = new WebSocket(targetWs);
  await new Promise((resolve, reject) => { ws.onopen = resolve; ws.onerror = reject; });
  let next = 1;
  const pending = new Map();
  const browserEvents = [];
  ws.onmessage = event => { const message = JSON.parse(event.data); if (message.method === 'Runtime.exceptionThrown' || message.method === 'Runtime.consoleAPICalled' || message.method === 'Log.entryAdded') browserEvents.push(message); if (!pending.has(message.id)) return; const { resolve, reject } = pending.get(message.id); pending.delete(message.id); message.error ? reject(new Error(message.error.message)) : resolve(message.result); };
  const send = (method, params = {}) => new Promise((resolve, reject) => { const id = next++; pending.set(id, { resolve, reject }); ws.send(JSON.stringify({ id, method, params })); });
  const evaluate = async expression => (await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true })).result?.value;
  await send('Runtime.enable'); await send('Page.enable');
  try { await until(() => evaluate("!!document.querySelector('.topbar') && !document.querySelector('.boot-loading')")); }
  catch (error) { console.error('Startup state:', await evaluate("[location.href, document.body.innerText.slice(0,500), document.querySelector('#blazor-error-ui')?.outerHTML, performance.getEntriesByType('resource').filter(x=>x.responseStatus>=400).map(x=>[x.name,x.responseStatus]).slice(-12)]").catch(()=>null)); console.error('Browser events:',JSON.stringify(browserEvents.slice(-8),null,2)); throw error; }
  if (process.argv.includes('--light')) await evaluate("localStorage.setItem('linguaflow.theme', 'light'); localStorage.setItem('linguaflow.language', 'vi-VN')");
  if (process.argv.includes('--english')) await evaluate("localStorage.setItem('linguaflow.language', 'en-US')");
  for (const [name, path, width, height] of [
    ['home-desktop', '', 1692, 1100], ['unit-desktop', 'courses/english-life-office-it/units/unit-01', 1692, 1100],
    ['home-mobile', '', 390, 900], ['unit-mobile', 'courses/english-life-office-it/units/unit-01', 390, 900],
  ]) {
    await send('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile: width < 700 });
    await send('Page.navigate', { url: new URL(path, target).href });
    const marker = path.includes('/units/') ? '.reader .lesson-section' : '.hero';
    try { await until(() => evaluate(`location.pathname.endsWith(${JSON.stringify('/' + path)}) && !document.querySelector('.boot-loading') && !!document.querySelector(${JSON.stringify(marker)}) && !document.querySelector('.state-message.loading')`)); }
    catch (error) { console.error(name, await evaluate("[location.href, document.body.innerText.slice(0,500), document.querySelector('#blazor-error-ui')?.outerHTML]")); throw error; }
    await evaluate('(async () => { await document.fonts.ready; return true; })()');
    await evaluate('window.scrollTo(0,0)');
    const metrics = await evaluate("({viewport:innerWidth, page:document.documentElement.scrollWidth, main:Math.round(document.querySelector('.content-wrap').getBoundingClientRect().width), reader:Math.round(document.querySelector('.reader-main')?.getBoundingClientRect().width||0)})");
    const screenshot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder, `${name}.png`), Buffer.from(screenshot.data, 'base64'));
    console.log(name, JSON.stringify(metrics));
  }
  if (process.argv.includes('--step16')) {
    await send('Emulation.setDeviceMetricsOverride', { width: 390, height: 900, deviceScaleFactor: 1, mobile: true });
    await send('Page.navigate', { url: new URL('', target).href });
    await until(() => evaluate("!!document.querySelector('.language-trigger') && !document.querySelector('.boot-loading')"));
    await evaluate("document.querySelector('.language-trigger').click()");
    await until(() => evaluate("document.querySelectorAll('.language-option').length===2"));
    const languages = await evaluate("[...document.querySelectorAll('.language-option')].map(x=>x.textContent.trim())");
    const menuShot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder, 'language-menu-mobile.png'), Buffer.from(menuShot.data, 'base64'));
    await evaluate("document.querySelectorAll('.language-option')[1].click()");
    await until(() => evaluate("document.documentElement.lang==='en-US' && document.querySelector('.language-trigger')?.innerText.includes('EN')"));
    await evaluate("document.querySelector('.language-trigger').click()");
    await until(() => evaluate("document.querySelectorAll('.language-option').length===2"));
    await evaluate("document.querySelector('.language-trigger').focus()");
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowDown',bubbles:true}))");
    const arrowFocused = await evaluate("document.activeElement?.classList.contains('language-option')");
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))");
    await until(() => evaluate("!document.querySelector('.language-menu')"));
    const languageFocusReturned = await evaluate("document.activeElement?.classList.contains('language-trigger')");
    try { await until(() => evaluate("document.documentElement.dataset.quickSearchReady==='true'"), 8000); }
    catch (error) { console.error('Shortcut state', await evaluate("({html:document.querySelector('.quick-search-trigger')?.outerHTML,lang:document.documentElement.lang,error:document.querySelector('#blazor-error-ui')?.outerHTML})"), JSON.stringify(browserEvents.slice(-10),null,2)); throw error; }
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'k',ctrlKey:true,bubbles:true}))");
    try { await until(() => evaluate("!!document.querySelector('.command-overlay')")); }
    catch (error) { console.error('Palette state', await evaluate("({html:document.querySelector('.quick-search-trigger')?.outerHTML,lang:document.documentElement.lang,error:document.querySelector('#blazor-error-ui')?.outerHTML})"), JSON.stringify(browserEvents.slice(-10),null,2)); throw error; }
    await pause(350);
    const paletteShot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder, 'command-palette-mobile.png'), Buffer.from(paletteShot.data, 'base64'));
    await evaluate("document.querySelector('.command-search').value='Unit 01';document.querySelector('.command-search').dispatchEvent(new Event('input',{bubbles:true}))");
    await until(() => evaluate("[...document.querySelectorAll('.command-result')].some(x=>x.textContent.includes('Unit 01'))"));
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))");
    await until(() => evaluate("!document.querySelector('.command-overlay')"));
    const state = await evaluate("({lang:document.documentElement.lang,languages:localStorage.getItem('linguaflow.language'),paletteClosed:!document.querySelector('.command-overlay'),focusReturned:document.activeElement?.classList.contains('quick-search-trigger'),overflow:document.documentElement.scrollWidth>innerWidth+1,animeLoaded:performance.getEntriesByType('resource').some(x=>x.name.includes('anime.esm.min.')),error:getComputedStyle(document.querySelector('#blazor-error-ui')).display!=='none'})");
    await send('Page.navigate', { url: new URL('courses/english-life-office-it/units/unit-01', target).href });
    await until(() => evaluate("!!document.querySelector('.reader-toolbar') && !document.querySelector('.state-message.loading')"));
    await evaluate("[...document.querySelectorAll('.reader-toolbar button')].find(x=>x.textContent.includes('Focus'))?.click()");
    await until(() => evaluate("document.querySelector('.reader-focus') && getComputedStyle(document.querySelector('.topbar')).display==='none'"));
    const focus = await evaluate("({on:!!document.querySelector('.reader-focus'),sidebarHidden:getComputedStyle(document.querySelector('.side-panel')).display==='none',exitVisible:[...document.querySelectorAll('.reader-toolbar button')].some(x=>x.textContent.includes('Exit'))})");
    await evaluate("[...document.querySelectorAll('.reader-toolbar button')].find(x=>x.textContent.includes('Exit'))?.click()");
    await until(() => evaluate("!document.querySelector('.reader-focus') && getComputedStyle(document.querySelector('.topbar')).display!=='none'"));
    await send('Emulation.setDeviceMetricsOverride', { width: 1692, height: 1000, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: new URL('languages/en', target).href });
    await until(() => evaluate("!!document.querySelector('.topbar') && !document.querySelector('.boot-loading')"));
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'k',ctrlKey:true,bubbles:true}))");
    await until(() => evaluate("!!document.querySelector('.command-panel')"));
    await pause(300);
    const paletteOnTop = await evaluate("(() => { const r=document.querySelector('.command-panel').getBoundingClientRect(); return !!document.elementFromPoint(r.left+30,r.top+90)?.closest('.command-panel'); })()");
    const desktopPalette = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder, 'command-palette-desktop.png'), Buffer.from(desktopPalette.data, 'base64'));
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))");
    await send('Page.navigate', { url: new URL('settings', target).href });
    await until(() => evaluate("!!document.querySelector('.settings-card .app-select-trigger') && !document.querySelector('.boot-loading')"));
    await evaluate("document.querySelector('.settings-card .app-select-trigger').click()");
    await until(() => evaluate("document.querySelectorAll('.settings-card .app-select-option').length===3"));
    const selectOnTop = await evaluate("(() => { const r=document.querySelector('.settings-card .app-select-option').getBoundingClientRect(); return !!document.elementFromPoint(r.left+20,r.top+r.height/2)?.closest('.app-select-option'); })()");
    const selectShot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder, 'settings-select-desktop.png'), Buffer.from(selectShot.data, 'base64'));
    await evaluate("document.querySelectorAll('.settings-card .app-select-option')[2].click()");
    await until(() => evaluate("document.documentElement.dataset.theme==='dark'"));
    await evaluate("document.querySelector('.side-panel>.sidebar-toggle').click()");
    await until(() => evaluate("document.querySelector('.app-shell').classList.contains('sidebar-collapsed')"));
    await pause(350);
    const shell = await evaluate("({sidebar:Math.round(document.querySelector('.side-panel').getBoundingClientRect().width),centerLogo:!!document.querySelector('.top-brand'),toggleInsideSidebar:document.querySelector('.side-panel').contains(document.querySelector('.sidebar-toggle')),overflow:document.documentElement.scrollWidth>innerWidth+1})");
    writeFileSync(join(folder, 'step16-smoke.json'), JSON.stringify({languages,arrowFocused,languageFocusReturned,state,focus,paletteOnTop,selectOnTop,shell},null,2));
    console.log('step16',JSON.stringify({languages,arrowFocused,languageFocusReturned,state,focus,paletteOnTop,selectOnTop,shell}));
    if (languages.length!==2 || !arrowFocused || !languageFocusReturned || state.lang!=='en-US' || state.languages!=='en-US' || !state.paletteClosed || !state.focusReturned || state.overflow || !state.animeLoaded || state.error || !focus.on || !focus.sidebarHidden || !focus.exitVisible || !paletteOnTop || !selectOnTop || shell.centerLogo || !shell.toggleInsideSidebar || shell.sidebar>80 || shell.overflow) throw new Error('Step 16 interaction audit failed');
  }
  if (process.argv.includes('--step15')) {
    const states = [];
    for (const theme of ['light', 'dark']) {
      await evaluate(`localStorage.setItem('linguaflow.theme', '${theme}')`);
      for (const width of [390, 768, 1024, 1440, 1692]) {
        await send('Emulation.setDeviceMetricsOverride', { width, height: 950, deviceScaleFactor: 1, mobile: width < 700 });
        await send('Page.navigate', { url: new URL('settings', target).href });
        await until(() => evaluate("!!document.querySelector('.settings-grid') && !document.querySelector('.boot-loading')"));
        await evaluate('(async () => { await document.fonts.ready; return true; })()');
        const state = await evaluate("({viewport:innerWidth,scroll:document.documentElement.scrollWidth,theme:document.documentElement.dataset.theme,language:document.documentElement.lang,selects:[...document.querySelectorAll('.settings-grid select')].map(x=>({height:Math.round(x.getBoundingClientRect().height),paddingRight:getComputedStyle(x).paddingRight,value:x.value})),sidebar:Math.round(document.querySelector('.side-panel').getBoundingClientRect().width),error:getComputedStyle(document.querySelector('#blazor-error-ui')).display!=='none'})");
        states.push({expectedTheme:theme,width,...state});
        const shot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
        writeFileSync(join(folder, `settings-${theme}-${width}.png`), Buffer.from(shot.data, 'base64'));
        console.log('settings',theme,width,JSON.stringify(state));
      }
    }
    await send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 950, deviceScaleFactor: 1, mobile: false });
    await send('Page.navigate', { url: new URL('settings', target).href });
    await until(() => evaluate("!!document.querySelector('.sidebar-toggle') && !document.querySelector('.boot-loading')"));
    await evaluate("document.querySelector('.sidebar-toggle').click()");
    await until(() => evaluate("document.documentElement.dataset.sidebar==='collapsed'"));
    await pause(350);
    const collapsed = await evaluate("({sidebar:Math.round(document.querySelector('.side-panel').getBoundingClientRect().width),stored:localStorage.getItem('linguaflow.sidebarCollapsed'),main:Math.round(document.querySelector('.content-wrap').getBoundingClientRect().width)})");
    const collapsedShot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder, 'settings-collapsed.png'), Buffer.from(collapsedShot.data, 'base64'));
    await send('Page.navigate', { url: new URL('settings', target).href });
    await until(() => evaluate("!!document.querySelector('.sidebar-toggle') && !document.querySelector('.boot-loading')"));
    const persisted = await evaluate("document.querySelector('.app-shell').classList.contains('sidebar-collapsed')");
    await send('Emulation.setDeviceMetricsOverride', { width: 390, height: 850, deviceScaleFactor: 1, mobile: true });
    await evaluate("document.querySelector('.mobile-menu').click()");
    await until(() => evaluate("!!document.querySelector('#mobile-navigation')"));
    const mobileLabels = await evaluate("[...document.querySelectorAll('#mobile-navigation .nav-text')].every(x=>getComputedStyle(x).display!=='none')");
    await evaluate("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}))");
    await send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 950, deviceScaleFactor: 1, mobile: false });
    await evaluate("document.querySelector('.sidebar-toggle').click()");
    await until(() => evaluate("document.documentElement.dataset.sidebar==='expanded'"));
    writeFileSync(join(folder, 'step15-audit.json'), JSON.stringify({states,collapsed,persisted,mobileLabels},null,2));
    if (states.some(x => x.scroll > x.viewport + 1 || x.error || x.theme !== x.expectedTheme || x.selects.some(s => s.height < 44)) || collapsed.sidebar > 80 || collapsed.stored !== 'true' || !persisted || !mobileLabels) throw new Error('Step 15 visual audit failed');
    await evaluate("(async()=>{const html=await(await fetch('index.html')).text();const boot=new DOMParser().parseFromString(html,'text/html').querySelector('.boot-loading');boot.style.cssText='position:fixed;inset:0;z-index:9999';document.body.append(boot);return true})()");
    const bootShot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder, 'first-load.png'), Buffer.from(bootShot.data, 'base64'));
  }
  if (process.argv.includes('--controls')) {
    const controls = [];
    await evaluate("localStorage.setItem('linguaflow.theme', 'dark'); localStorage.setItem('linguaflow.language', 'vi-VN')");
    await send('Emulation.setDeviceMetricsOverride', { width: 390, height: 850, deviceScaleFactor: 1, mobile: true });
    await send('Page.navigate', { url: new URL('settings', target).href });
    await until(() => evaluate("!!document.querySelector('.settings-group .language-trigger') && !document.querySelector('.boot-loading')"));
    await evaluate("document.querySelector('.settings-group .language-trigger').scrollIntoView({block:'center'}); document.querySelector('.settings-group .language-trigger').click()");
    await until(() => evaluate("!!document.querySelector('.settings-group .language-option')"));
    await pause(700);
    const settingsLanguageMenu = await evaluate("(() => { const options=[...document.querySelectorAll('.settings-group .language-option')], menu=document.querySelector('.settings-group .language-menu'), card=document.querySelector('.settings-group'), next=card.nextElementSibling, m=menu.getBoundingClientRect(); return {visible:options.every(option=>{const r=option.getBoundingClientRect(); return !!document.elementFromPoint(r.left+Math.min(40,r.width/2),r.top+r.height/2)?.closest('.language-option')}),hits:options.map(option=>{const r=option.getBoundingClientRect(); return document.elementFromPoint(r.left+Math.min(40,r.width/2),r.top+r.height/2)?.className}),cardZ:getComputedStyle(card).zIndex,nextZ:getComputedStyle(next).zIndex,menuX:m.left,menuBeyondCard:m.bottom>card.getBoundingClientRect().bottom,overlapsNext:m.bottom>next.getBoundingClientRect().top,viewport:innerWidth,scroll:document.documentElement.scrollWidth}; })()");
    const languageShot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder,'settings-language-mobile.png'),Buffer.from(languageShot.data,'base64'));
    await send('Page.navigate', { url: new URL('', target).href });
    await until(() => evaluate("!!document.querySelector('.hero') && !document.querySelector('.boot-loading')"));
    await evaluate("if (!document.querySelector('.dashboard-catalog-link')) { const link=document.createElement('a'); link.className='reader-control dashboard-catalog-link'; link.textContent='Danh mục khóa học →'; document.querySelector('.hero').before(link); }");
    const homeSpacing = await evaluate("(() => { const link=document.querySelector('.dashboard-catalog-link').getBoundingClientRect(), hero=document.querySelector('.hero').getBoundingClientRect(); return {gap:Math.round(hero.top-link.bottom),overlap:hero.top<link.bottom,viewport:innerWidth,scroll:document.documentElement.scrollWidth}; })()");
    const homeShot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
    writeFileSync(join(folder,'home-dashboard-mobile.png'),Buffer.from(homeShot.data,'base64'));
    console.log('mobile layout',JSON.stringify({settingsLanguageMenu,homeSpacing}));
    if (!settingsLanguageMenu.visible || settingsLanguageMenu.overlapsNext || settingsLanguageMenu.scroll>settingsLanguageMenu.viewport+1 || homeSpacing.overlap || homeSpacing.gap<20 || homeSpacing.scroll>homeSpacing.viewport+1) throw new Error('Mobile language menu or dashboard spacing failed');
    const paths = ['settings', 'courses/english-life-office-it/practice', 'courses/english-life-office-it/vocabulary', 'courses/english-life-office-it/goals', 'courses/english-life-office-it/workspace'];
    for (const [theme, language, width] of [['light', 'vi-VN', 390], ['dark', 'en-US', 1692]]) {
      await evaluate(`localStorage.setItem('linguaflow.theme', '${theme}'); localStorage.setItem('linguaflow.language', '${language}')`);
      await send('Emulation.setDeviceMetricsOverride', { width, height: 960, deviceScaleFactor: 1, mobile: width < 700 });
      for (const path of paths) {
        await send('Page.navigate', { url: new URL(path, target).href });
        await until(() => evaluate("!!document.querySelector('.app-select-trigger') && !document.querySelector('.boot-loading') && !document.querySelector('.state-message.loading')"));
        await evaluate("document.querySelector('.app-select-trigger').scrollIntoView({block:'center'})");
        await evaluate("document.querySelector('.app-select-trigger').click()");
        await until(() => evaluate("!!document.querySelector('.app-select-option')"));
        await pause(220);
        const result = await evaluate("(() => { const option=document.querySelector('.app-select-option'), r=option.getBoundingClientRect(), trigger=document.querySelector('.app-select-trigger'); return {width:innerWidth,scroll:document.documentElement.scrollWidth,theme:document.documentElement.dataset.theme,lang:document.documentElement.lang,selects:document.querySelectorAll('.app-select-trigger').length,triggerText:trigger.innerText,optionOnTop:!!document.elementFromPoint(r.left+Math.min(20,r.width/2),r.top+r.height/2)?.closest('.app-select-option'),error:getComputedStyle(document.querySelector('#blazor-error-ui')).display!=='none'} })()");
        controls.push({path,expectedWidth:width,expectedTheme:theme,expectedLanguage:language,...result});
        const name = `controls-${theme}-${width}-${path.replaceAll('/','-')}.png`;
        const shot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false });
        writeFileSync(join(folder,name),Buffer.from(shot.data,'base64'));
        console.log('controls',path,theme,width,JSON.stringify(result));
      }
    }
    writeFileSync(join(folder,'controls-audit.json'),JSON.stringify(controls,null,2));
    if (controls.some(x => x.width>x.expectedWidth+1 || x.scroll>x.expectedWidth+1 || x.error || !x.optionOnTop || x.theme!==x.expectedTheme || x.lang!==x.expectedLanguage)) throw new Error('Control overlap audit failed');
  }
  if (process.argv[4] !== '--quick') {
  const routes = ['', 'languages/en', 'courses/english-life-office-it', 'courses/english-life-office-it/roadmap',
    'courses/english-life-office-it/units/unit-01', 'courses/english-life-office-it/vocabulary',
    'courses/english-life-office-it/flashcards', 'courses/english-life-office-it/review',
    'courses/english-life-office-it/practice', 'courses/english-life-office-it/practice/import',
    'courses/english-life-office-it/progress', 'courses/english-life-office-it/backup',
    'courses/english-life-office-it/materials', 'courses/english-life-office-it/offline',
    'courses/english-life-office-it/goals', 'courses/english-life-office-it/workspace',
    'courses/english-life-office-it/mistakes', 'settings', 'help', 'about', 'privacy'];
  const problems = [];
  for (const [theme, language] of [['light', 'vi-VN'], ['dark', 'en-US']]) {
    await evaluate(`localStorage.setItem('linguaflow.theme', '${theme}'); localStorage.setItem('linguaflow.language', '${language}')`);
    for (const width of [360, 390, 768, 1024, 1366, 1692, 1920]) {
      await send('Emulation.setDeviceMetricsOverride', { width, height: 1000, deviceScaleFactor: 1, mobile: width < 700 });
      for (const path of width === 390 || width === 1692 ? routes : ['', 'courses/english-life-office-it/units/unit-01']) {
        await send('Page.navigate', { url: new URL(path, target).href });
        await until(() => evaluate(`location.pathname.endsWith(${JSON.stringify('/' + path)}) && !document.querySelector('.boot-loading') && !!document.querySelector('.topbar') && !document.querySelector('.state-message.loading')`));
        const state = await evaluate("(async () => { await document.fonts.ready; return { width:innerWidth, scroll:document.documentElement.scrollWidth, theme:document.documentElement.dataset.theme, lang:document.documentElement.lang, error:getComputedStyle(document.querySelector('#blazor-error-ui')).display !== 'none' }; })()");
        if (state.scroll > state.width + 1 || state.theme !== theme || state.lang !== language || state.error) problems.push({ theme, language, width, path, state });
      }
      console.log(`audit ${theme}/${language} ${width}px complete`);
    }
  }
  writeFileSync(join(folder, 'audit.json'), JSON.stringify({ routes: routes.length, widths: [360,390,768,1024,1366,1692,1920], combinations: 2, problems }, null, 2));
  if (problems.length) throw new Error(`${problems.length} visual audit problems: ${JSON.stringify(problems.slice(0,5))}`);
  }
} finally { ws?.close(); child.kill(); try { rmSync(profile, { recursive: true, force: true }); } catch {} }
