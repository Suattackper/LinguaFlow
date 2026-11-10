import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { spawn } from 'node:child_process';
import { existsSync, mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve, extname } from 'node:path';
import { createHash } from 'node:crypto';

const [siteArgument, base = '/LinguaFlow/'] = process.argv.slice(2);
if (!siteArgument || !base.startsWith('/') || !base.endsWith('/')) throw new Error('Usage: smoke-publish.mjs <published-wwwroot> </project/>');
const site = resolve(siteArgument);
const chrome = process.env.CHROME_BIN || (process.platform === 'win32' ? 'C:/Program Files/Google/Chrome/Application/chrome.exe' : '/usr/bin/google-chrome');
if (!existsSync(chrome)) throw new Error(`Chrome unavailable: ${chrome}`);
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json', '.webmanifest': 'application/manifest+json', '.css': 'text/css', '.wasm': 'application/wasm', '.png': 'image/png', '.svg': 'image/svg+xml', '.pdf': 'application/pdf', '.zip': 'application/zip', '.dat': 'application/octet-stream' };
const overrides = new Map();
const requests = [];
const server = createServer((req, res) => {
  const pathname = new URL(req.url, 'http://localhost').pathname;
  requests.push(req.url);
  const relative = pathname.startsWith(base) ? pathname.slice(base.length) || 'index.html' : null;
  const file = relative && !relative.includes('..') ? resolve(site, relative) : null;
  const selected = file && file.startsWith(site) && existsSync(file) ? file : join(site, '404.html');
  res.writeHead(selected.endsWith('404.html') ? 404 : 200, { 'Content-Type': mime[extname(selected)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
  res.end(overrides.get(pathname) || readFileSync(selected));
});
await new Promise(ok => server.listen(0, '127.0.0.1', ok));
const port = server.address().port;
const profile = mkdtempSync(join(tmpdir(), 'linguaflow-pwa-'));
const child = spawn(chrome, ['--headless=new', '--no-sandbox', '--disable-gpu', '--no-first-run', '--no-default-browser-check', `--user-data-dir=${profile}`, '--remote-debugging-port=0', `http://127.0.0.1:${port}${base}`], { stdio: 'ignore' });

const delay = ms => new Promise(ok => setTimeout(ok, ms));
async function waitFor(fn, timeout = 45000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) { const result = await Promise.resolve().then(fn).catch(() => null); if (result) return result; await delay(300); }
  throw new Error('Timed out waiting for browser state.');
}
class Cdp {
  constructor(ws) { this.ws = ws; this.next = 1; this.pending = new Map(); this.events = []; ws.onmessage = event => { const item = JSON.parse(event.data); if (item.method === 'Runtime.exceptionThrown' || item.method === 'Runtime.consoleAPICalled' || item.method === 'Log.entryAdded') this.events.push(item); if (item.id && this.pending.has(item.id)) { const { resolve, reject } = this.pending.get(item.id); this.pending.delete(item.id); item.error ? reject(new Error(item.error.message)) : resolve(item.result); } }; }
  send(method, params = {}) { const id = this.next++; return new Promise((resolve, reject) => { const timer = setTimeout(() => { this.pending.delete(id); reject(new Error(`CDP ${method} timed out`)); }, 45000); this.pending.set(id, { resolve: value => { clearTimeout(timer); resolve(value); }, reject: error => { clearTimeout(timer); reject(error); } }); this.ws.send(JSON.stringify({ id, method, params })); }); }
  async eval(expression) { const result = await this.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }); if (result.exceptionDetails) throw new Error(result.exceptionDetails.text + ': ' + result.result?.description); return result.result?.value; }
}
let cdp;
try {
  const devtools = await waitFor(() => { const file = join(profile, 'DevToolsActivePort'); return existsSync(file) && readFileSync(file, 'utf8').split('\n')[0]; });
  const targets = await waitFor(async () => { const list = await (await fetch(`http://127.0.0.1:${devtools}/json`)).json(); return list.find(x => x.type === 'page')?.webSocketDebuggerUrl; });
  const ws = new WebSocket(targets);
  await new Promise((ok, fail) => { ws.onopen = ok; ws.onerror = fail; });
  cdp = new Cdp(ws);
  await cdp.send('Runtime.enable'); await cdp.send('Page.enable');
  try { await waitFor(async () => await cdp.eval("!!document.querySelector('.topbar') && !document.querySelector('.boot-loading')")); }
  catch (error) { console.error(await cdp.eval("[location.href, document.body.innerText.slice(0,1000), document.querySelector('#blazor-error-ui')?.outerHTML, performance.getEntriesByType('resource').filter(x => x.responseStatus >= 400).map(x => [x.name,x.responseStatus]).slice(0,20)]")); console.error('Browser events:', JSON.stringify(cdp.events.slice(-12), null, 2)); throw error; }
  const registered = await waitFor(() => cdp.eval(`(async () => { const r = await navigator.serviceWorker.ready; return r.active?.scriptURL.endsWith('${base}service-worker.js') && !!navigator.serviceWorker.controller; })()`));
  assert.equal(registered, true, 'Published service worker should control the page');
  console.log('Service worker active');
  const manifest = await cdp.eval("(async () => { const m = await (await fetch('manifest.webmanifest')).json(); return [m.name, m.display, m.scope]; })()");
  assert.deepEqual(manifest, ['LinguaFlow', 'standalone', './']);
  const runtimeFile = readFileSync(join(site, 'index.html'), 'utf8').match(/dotnet\.[a-z0-9]{8,}\.js/)?.[0];
  assert(runtimeFile, 'Published import map must identify the .NET runtime module');
  try { console.log('Runtime fetch:', await cdp.eval(`(async () => { const r = await fetch('_framework/${runtimeFile}'); return [r.status, r.headers.get('content-type')]; })()`)); }
  catch (error) { console.error('Runtime URL:', `${base}_framework/${runtimeFile}`, 'exists:', existsSync(join(site, '_framework', runtimeFile)), 'recent requests:', requests.slice(-20)); console.error('Browser events:', JSON.stringify(cdp.events.slice(-8), null, 2)); throw error; }
  assert.equal(await cdp.eval('document.documentElement.lang'), 'vi-VN', 'Clean browser should default to Vietnamese');
  async function openRoute(path, marker) {
    await cdp.send('Page.navigate', { url: `http://127.0.0.1:${port}${base}${path}` });
    try { await waitFor(() => cdp.eval(`location.pathname.endsWith(${JSON.stringify('/' + path)}) && !!document.querySelector(${JSON.stringify(marker)}) && !document.querySelector('.boot-loading')`)); }
    catch (error) { console.error('Route failure', path, await cdp.eval("[location.href, document.body.innerText.slice(0,500), document.querySelector('#blazor-error-ui')?.outerHTML]")); console.error('Last requests:', requests.slice(-15)); console.error('Browser events:', JSON.stringify(cdp.events.slice(-8), null, 2)); throw error; }
    assert.equal(await cdp.eval("getComputedStyle(document.querySelector('#blazor-error-ui')).display === 'none'"), true, `Blazor error on ${path}`);
  }
  await openRoute('settings', '.settings-card');
  await cdp.eval("document.querySelector('.settings-card .language-trigger').click()");
  await waitFor(() => cdp.eval("document.querySelectorAll('.settings-card .language-option').length === 2"));
  await cdp.eval("document.querySelectorAll('.settings-card .language-option')[1].click()");
  await waitFor(() => cdp.eval("document.documentElement.lang === 'en-US' && document.querySelector('.settings-card .language-trigger')?.innerText.includes('EN')"));
  await cdp.eval("document.querySelector('.settings-card .language-trigger').click()");
  await waitFor(() => cdp.eval("document.querySelectorAll('.settings-card .language-option').length === 2"));
  await cdp.eval("document.querySelectorAll('.settings-card .language-option')[0].click()");
  await waitFor(() => cdp.eval("document.documentElement.lang === 'vi-VN' && document.querySelector('.settings-card .language-trigger')?.innerText.includes('VI')"));
  for (const [path, marker] of [
    ['languages/en', '.course-list'],
    ['courses/english-life-office-it', '.course-detail'],
    ['courses/english-life-office-it/roadmap', '.roadmap-grid'],
    ['courses/english-life-office-it/units/unit-01', '.lesson-section'],
    ['courses/english-life-office-it/practice', '.practice-wizard'],
    ['courses/english-life-office-it/materials', '.materials-grid'],
    ['help', '.help-center, .help-layout, .help-search'],
    ['about', '.about-page, .about-hero, .info-panel'],
    ['privacy', '.info-panel, .privacy-page'],
  ]) await openRoute(path, marker);
  await openRoute('courses/english-life-office-it/practice', '.practice-wizard');
  await waitFor(() => cdp.eval("!!document.querySelector('.practice-wizard > section.info-panel button.button.course-action:not(:disabled)')"));
  await cdp.eval("document.querySelector('.practice-wizard > section.info-panel button.button.course-action').click()");
  await waitFor(() => cdp.eval("!!document.querySelector('pre.practice-preview')"));
  const packagePreview = await cdp.eval("document.querySelector('pre.practice-preview').textContent");
  assert(packagePreview.includes('linguaflow-practice-request') && packagePreview.includes('linguaflow-practice-result'), 'Practice package needs request and result contracts');
  await cdp.eval("document.querySelectorAll('.practice-wizard > section.info-panel')[1].querySelector('button.button.course-action').click()");
  const session = await waitFor(() => cdp.eval(`(async () => { const records = await (await import('./js/learning-store.js')).getCourse('english-life-office-it'); return records.find(x => x.kind === 11 && x.data?.Status === 1)?.data; })()`));
  assert.equal(session.Request.Format, 'linguaflow-practice-request');
  assert.equal(session.Request.UnitId, 'unit-01');
  assert(session.PackageMarkdown.includes(session.Request.RequestId));
  const request = session.Request;
  const result = {
    format: 'linguaflow-practice-result', schemaVersion: 1, requestId: request.RequestId,
    courseId: request.CourseId, courseContentVersion: request.CourseContentVersion, unitId: request.UnitId,
    taskId: request.TaskId, skill: request.Skill, rubricId: request.RubricId, rubricVersion: request.RubricVersion,
    completedAt: new Date().toISOString(), evaluator: 'external-ai:ChatGPT',
    evidence: { modality: 'text-only', description: 'Deterministic browser QA fixture: text only' },
    criterionAssessments: request.AssessmentCriteria.map(c => {
      const audio = /audio/i.test(c.EvidenceRequirement);
      return { criterionId: c.CriterionId, awardedPoints: audio ? null : c.MaxPoints,
        maxPoints: c.MaxPoints, evidenceLevel: audio ? 'insufficient' : 'sufficient',
        evidenceSummary: audio ? 'No audio observed' : 'Text fixture response' };
    }),
    strengths: ['Text response recorded'], mistakes: [], vocabularySuggestions: [], improvementSuggestions: [], summary: 'Browser test fixture.'
  };
  await openRoute('courses/english-life-office-it/practice/import', '.practice-wizard');
  await cdp.eval(`(() => { const field = document.querySelector('.practice-wizard textarea'); field.value = ${JSON.stringify(JSON.stringify(result))}; field.dispatchEvent(new Event('input', { bubbles: true })); document.querySelector('.practice-wizard button.button.course-action').click(); })()`);
  await waitFor(() => cdp.eval("!!document.querySelectorAll('.practice-wizard > section.info-panel')[1]?.querySelector('button.button.course-action') || !!document.querySelector('.practice-wizard [role=alert]')"));
  const importError = await cdp.eval("document.querySelector('.practice-wizard [role=alert]')?.textContent || null");
  assert.equal(importError, null, `Practice fixture rejected: ${importError}`);
  await cdp.eval("document.querySelectorAll('.practice-wizard > section.info-panel')[1].querySelector('button.button.course-action').click()");
  await waitFor(() => cdp.eval("location.pathname.includes('/practice/results/')"));
  assert.equal(await cdp.eval(`(async () => (await (await import('./js/learning-store.js')).getCourse('english-life-office-it')).some(x => x.kind === 12 && x.data?.RequestId === '${request.RequestId}'))()`), true);
  const resultUrl = await cdp.eval('location.href');
  await cdp.send('Page.navigate', { url: resultUrl });
  await waitFor(() => cdp.eval("location.pathname.includes('/practice/results/') && !document.querySelector('.boot-loading')"));
  assert.equal(await cdp.eval(`(async () => (await (await import('./js/learning-store.js')).getCourse('english-life-office-it')).some(x => x.kind === 12 && x.data?.RequestId === '${request.RequestId}'))()`), true, 'Imported result must survive refresh');
  console.log('Practice package exported and matching deterministic assessment imported');
  for (const width of [360, 390, 768, 1024, 1440]) {
    await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 844, deviceScaleFactor: 1, mobile: width < 768 });
    assert.equal(await cdp.eval('document.documentElement.scrollWidth <= innerWidth + 1'), true, `Horizontal overflow at ${width}px`);
  }
  await cdp.send('Emulation.clearDeviceMetricsOverride');
  await openRoute('', '.hero');
  console.log('Routes, language switching, and five viewport widths validated');
  await cdp.send('Emulation.setDeviceMetricsOverride', { width: 390, height: 844, deviceScaleFactor: 2, mobile: true });
  const mobile = await cdp.eval("[document.documentElement.scrollWidth <= innerWidth + 1, getComputedStyle(document.querySelector('.mobile-menu')).display !== 'none']");
  assert.deepEqual(mobile, [true, true], 'Mobile navigation should fit without horizontal overflow');
  await cdp.eval("document.querySelector('.mobile-menu').click()");
  await waitFor(() => cdp.eval("!!document.querySelector('#mobile-navigation') && !!document.activeElement.closest('#mobile-navigation')"));
  await cdp.eval("document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))");
  await waitFor(() => cdp.eval("!document.querySelector('#mobile-navigation') && document.activeElement === document.querySelector('.mobile-menu')"));
  await cdp.send('Emulation.clearDeviceMetricsOverride');
  const course = 'english-life-office-it';
  const resources = await cdp.eval(`(async () => { const m = await (await fetch('Courses/English/${course}/manifest.json')).json(); const root = new URL('Courses/English/${course}/manifest.json', document.baseURI); return Promise.all(m.downloads.filter(x => x.available).map(async x => { const r = await fetch(new URL(x.path, root)); return [r.ok, (await r.arrayBuffer()).byteLength === x.fileSize]; })); })()`);
  assert(resources.length >= 2 && resources.every(x => x[0] && x[1]), 'Published course downloads must resolve under the project subpath');
  const download = await cdp.eval(`(async () => { const m = await (await fetch('Courses/English/${course}/manifest.json')).json(); const offline = await import('./js/offline.js'); return offline.download('${course}', 'English', new URL('Courses/English/${course}/manifest.json', document.baseURI).href, m.contentFingerprint, false, null); })()`);
  assert.equal(download.status, 'ready'); assert.equal(download.cachedUnits, 36);
  console.log('Course downloaded');
  const status = await cdp.eval(`(async () => (await import('./js/offline.js')).status('${course}'))()`);
  assert.equal(status.available, true); assert.equal(status.cachedUnits, 36);
  const courseRoot = join(site, 'Courses', 'English', course);
  const originalManifest = JSON.parse(readFileSync(join(courseRoot, 'manifest.json'), 'utf8'));
  const originalUnit = JSON.parse(readFileSync(join(courseRoot, 'Units', 'unit-01.json'), 'utf8'));
  function publishRevision(version, unit) {
    const unitBytes = Buffer.from(JSON.stringify(unit));
    const paths = [...originalManifest.units, ...originalManifest.vocabulary, ...originalManifest.assessments, ...originalManifest.aiPractice].map(x => x.path).sort();
    const hash = createHash('sha256');
    for (const path of paths) {
      const bytes = path === 'Units/unit-01.json' ? unitBytes : readFileSync(join(courseRoot, ...path.split('/')));
      hash.update(path); hash.update(Buffer.from([0])); hash.update(String(bytes.length)); hash.update(Buffer.from([0])); hash.update(bytes);
    }
    const manifest = { ...originalManifest, contentVersion: version, contentFingerprint: `sha256:${hash.digest('hex')}` };
    overrides.set(`${base}Courses/English/${course}/manifest.json`, Buffer.from(JSON.stringify(manifest)));
    overrides.set(`${base}Courses/English/${course}/Units/unit-01.json`, unitBytes);
    return manifest;
  }
  const compatibleUnit = structuredClone(originalUnit);
  const explanation = compatibleUnit.sections.flatMap(x => x.blocks).find(x => x.text && !x.exerciseId);
  assert(explanation, 'Expected a non-exercise lesson block');
  explanation.text += '\nUpdated explanation.';
  const compatible = publishRevision('1.1.1', compatibleUnit);
  const revisionCall = (fingerprint, allow) => `(async () => (await import('./js/offline.js')).download('${course}', 'English', new URL('Courses/English/${course}/manifest.json', document.baseURI).href, '${fingerprint}', ${allow}, null))()`;
  const pending = await cdp.eval(revisionCall(compatible.contentFingerprint, false));
  assert.equal(pending.status, 'changed');
  console.log('Changed course detected');
  const update = await cdp.eval(revisionCall(compatible.contentFingerprint, true)).catch(async error => { console.error('Offline stage:', await cdp.eval('[globalThis.__linguaFlowOfflineStage,globalThis.__linguaFlowOfflineProgress]').catch(() => 'unavailable')); console.error('Last requests:', requests.slice(-12)); throw error; });
  assert.equal(update.status, 'ready');
  assert.equal((await cdp.eval(`(async () => (await import('./js/offline.js')).status('${course}'))()`)).contentFingerprint, compatible.contentFingerprint);
  console.log('Compatible course update validated');
  const incompatibleUnit = structuredClone(compatibleUnit);
  const exercise = incompatibleUnit.sections.flatMap(x => x.blocks).find(x => x.exerciseId);
  assert(exercise, 'Expected a source exercise');
  exercise.prompt += ' Changed requirement.';
  const incompatible = publishRevision('1.1.2', incompatibleUnit);
  const rejected = await cdp.eval(`(async () => { try { await ${revisionCall(incompatible.contentFingerprint, true)}; return false; } catch { return true; } })()`);
  assert.equal(rejected, true, 'Changed exercise content must not replace offline course without migration');
  assert.equal((await cdp.eval(`(async () => (await import('./js/offline.js')).status('${course}'))()`)).contentFingerprint, compatible.contentFingerprint);
  await cdp.send('Network.emulateNetworkConditions', { offline: true, latency: 0, downloadThroughput: 0, uploadThroughput: 0 });
  const cached = await cdp.eval(`(async () => (await (await fetch('Courses/English/${course}/Units/unit-01.json')).json()).unitId)()`);
  assert.equal(cached, 'unit-01');
  await cdp.send('Page.navigate', { url: `http://127.0.0.1:${port}${base}courses/${course}/units/unit-01` });
  try { await waitFor(async () => await cdp.eval("!!document.querySelector('.lesson-section')")); }
  catch (error) { console.error('Offline navigation:', await cdp.eval("[location.href,document.body.innerText.slice(0,800),navigator.serviceWorker.controller?.scriptURL,performance.getEntriesByType('resource').filter(x => x.responseStatus >= 400).map(x => [x.name,x.responseStatus]).slice(-12)]").catch(() => 'unavailable')); throw error; }
  console.log('Offline lesson loaded');
  const exerciseId = await cdp.eval("(() => { const item = [...document.querySelectorAll('.interactive-exercise')].find(x => x.querySelector('input[type=radio], textarea.answer-input')); if (!item) throw new Error('No interactive exercise'); const radio = item.querySelector('input[type=radio]'); if (radio) radio.click(); else { const field = item.querySelector('textarea.answer-input'); field.value = 'I work in software.'; field.dispatchEvent(new Event('input', { bubbles: true })); } return item.id; })()");
  await waitFor(async () => await cdp.eval(`!document.getElementById('${exerciseId}')?.querySelector('.exercise-submit')?.disabled`));
  await cdp.eval(`document.getElementById('${exerciseId}').querySelector('.exercise-submit').click()`);
  console.log('Exercise submitted', exerciseId);
  try { await waitFor(async () => await cdp.eval(`(async () => { const store = await import('./js/learning-store.js'); return (await store.getCourse('${course}')).some(x => x.kind === 3 && x.exerciseId === '${exerciseId}'); })()`)); }
  catch (error) { console.error(await cdp.eval(`(async () => { const store = await import('./js/learning-store.js'); return [document.getElementById('${exerciseId}')?.innerText.slice(-500), (await store.getCourse('${course}')).slice(-5)]; })()`)); throw error; }
  const removed = await cdp.eval(`(async () => { const offline = await import('./js/offline.js'); await offline.remove('${course}'); const store = await import('./js/learning-store.js'); return [await offline.status('${course}'), (await store.getCourse('${course}')).some(x => x.kind === 3 && x.exerciseId === '${exerciseId}')]; })()`);
  assert.equal(removed[0].available, false); assert.equal(removed[1], true);
  console.log('Production browser smoke passed: manifest, service worker, subpath route, 36 cached Units, offline Unit, IndexedDB, update and removal.');
} finally {
  cdp?.ws.close(); child.kill(); await new Promise(ok => server.close(ok));
  try { rmSync(profile, { recursive: true, force: true }); } catch { }
}
