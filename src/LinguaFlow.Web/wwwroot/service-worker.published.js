importScripts('./service-worker-assets.js');

const shellName = `linguaflow-shell-${self.assetsManifest.version}`;
const runtimeName = 'linguaflow-runtime-v1';
const metaName = 'linguaflow-course-meta-v1';
const shellFiles = self.assetsManifest.assets
  .filter(asset => (!asset.url.startsWith('Courses/') || asset.url === 'Courses/catalog.json') && !/\.(?:pdf|zip|pdb|map|br|gz)$/i.test(asset.url))
  .filter(asset => /\.(?:html|css|js|json|wasm|dat|png|webmanifest|woff2?|ttf|svg|ico|webp)$/i.test(asset.url) || asset.url.startsWith('_framework/'))
  .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));

self.addEventListener('install', event => event.waitUntil((async () => {
  const cache = await caches.open(shellName);
  await cache.addAll(shellFiles);
})()));

self.addEventListener('activate', event => event.waitUntil((async () => {
  for (const key of await caches.keys()) {
    if (key.startsWith('linguaflow-shell-') && key !== shellName) await caches.delete(key);
  }
  await self.clients.claim();
})()));

function inScope(url) { return url.origin === self.location.origin && url.pathname.startsWith(new URL(self.registration.scope).pathname); }
async function courseCacheFor(url) {
  const path = url.pathname.slice(new URL(self.registration.scope).pathname.length);
  const match = /^Courses\/(English|Japan|China)\/([a-z0-9-]+)\//.exec(path);
  if (!match) return null;
  const meta = await caches.open(metaName);
  const record = await meta.match(`__offline/${match[2]}.json`);
  if (!record) return null;
  try { return caches.open((await record.json()).cacheName); } catch { return null; }
}

self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (!inScope(url)) return;
  if (url.searchParams.has('offline-refresh')) return;
  if (request.mode === 'navigate') {
    event.respondWith((async () => (await caches.open(shellName)).match('index.html') || fetch(request))());
    return;
  }
  const path = url.pathname.slice(new URL(self.registration.scope).pathname.length);
  if (path.startsWith('Courses/')) {
    event.respondWith((async () => {
      const selected = await courseCacheFor(url);
      const stored = selected && await selected.match(request);
      if (stored) return stored;
      if (selected && path.endsWith('.json')) return Response.error();
      const runtime = await caches.open(runtimeName);
      try {
        const fresh = await fetch(request);
        if (fresh.ok && fresh.type !== 'opaque' && fresh.headers.get('content-type')?.includes('application/json')) await runtime.put(request, fresh.clone());
        return fresh;
      } catch {
        return await runtime.match(request) || (path === 'Courses/catalog.json' ? await (await caches.open(shellName)).match(request) : null) || Response.error();
      }
    })());
    return;
  }
  event.respondWith((async () => (await caches.open(shellName)).match(request) || fetch(request))());
});

const metaKey = id => new URL(`__offline/${id}.json`, self.registration.scope);
const validId = id => typeof id === 'string' && /^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(id);
const validPath = path => typeof path === 'string' && path.split('/').every(x => /^[A-Za-z0-9][A-Za-z0-9._-]*$/.test(x) && x !== '.' && x !== '..') && !path.includes('\\');

async function getRecord(id) {
  if (!validId(id)) throw new Error('Invalid course ID.');
  const response = await (await caches.open(metaName)).match(metaKey(id));
  return response ? response.json() : null;
}

async function getStatus(id) {
  const record = await getRecord(id);
  if (!record) return { available: false, cachedUnits: 0, contentBytes: 0 };
  const cache = await caches.open(record.cacheName);
  const keys = await cache.keys();
  const cachedUnits = keys.filter(x => /\/Units\/[^/]+\.json$/.test(new URL(x.url).pathname)).length;
  return { available: cachedUnits === record.unitCount, cachedUnits, unitCount: record.unitCount,
    contentBytes: record.contentBytes, contentVersion: record.contentVersion, contentFingerprint: record.contentFingerprint };
}

async function freshJson(url, expectedCourse) {
  const freshUrl = new URL(url);
  freshUrl.searchParams.set('offline-refresh', Date.now().toString());
  const response = await fetch(freshUrl, { cache: 'no-store' });
  if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error(`Course file unavailable: ${url.pathname}`);
  const bytes = new Uint8Array(await response.arrayBuffer());
  const data = JSON.parse(new TextDecoder('utf-8', { fatal: true }).decode(bytes));
  if (!data || data.courseId !== expectedCourse) throw new Error(`Course reference mismatch: ${url.pathname}`);
  return { url, bytes, data, response: new Response(bytes, { headers: { 'Content-Type': 'application/json; charset=utf-8' } }) };
}

async function freshAsset(url) {
  const freshUrl = new URL(url);
  freshUrl.searchParams.set('offline-refresh', Date.now().toString());
  const response = await fetch(freshUrl, { cache: 'no-store' });
  if (!response.ok || response.headers.get('content-type')?.includes('text/html')) throw new Error(`Media unavailable: ${url.pathname}`);
  const bytes = new Uint8Array(await response.arrayBuffer());
  return { url, bytes, data: null, response: new Response(bytes, { headers: { 'Content-Type': response.headers.get('content-type') || 'application/octet-stream' } }) };
}

function idGraph(unit) {
  return [unit.unitId, ...(unit.sections || []).flatMap(s => [s.sectionId, ...(s.blocks || []).flatMap(b => [b.blockId, b.exerciseId].filter(Boolean))])].join('|');
}
function exerciseGraph(unit) {
  return JSON.stringify((unit.sections || []).flatMap(s => (s.blocks || []).filter(b => b.exerciseId).map(b => [b.exerciseId, b])));
}

async function downloadCourse(message, port) {
  const { courseId, manifestUrl, expectedFingerprint, allowChanged } = message;
  if (!validId(courseId) || typeof expectedFingerprint !== 'string') throw new Error('Invalid course request.');
  const manifestAddress = new URL(manifestUrl);
  const scope = new URL(self.registration.scope);
  if (manifestAddress.origin !== scope.origin || !manifestAddress.pathname.startsWith(scope.pathname) ||
      !manifestAddress.pathname.endsWith(`/Courses/${message.languageDirectory}/${courseId}/manifest.json`) ||
      !['English', 'Japan', 'China'].includes(message.languageDirectory)) throw new Error('Manifest is outside this course.');
  const manifestFile = await freshJson(manifestAddress, courseId);
  const manifest = manifestFile.data;
  if (manifest.schemaVersion !== 1 || manifest.contentFingerprint !== expectedFingerprint || !Array.isArray(manifest.units) || !manifest.units.length)
    throw new Error('Course version changed. Reload the page before downloading.');
  const old = await getRecord(courseId);
  if (old && old.contentFingerprint !== manifest.contentFingerprint && !allowChanged)
    return { status: 'changed', previousVersion: old.contentVersion, newVersion: manifest.contentVersion };
  const refs = [manifestFile];
  port.postMessage({ event: 'stage', stage: 'fetching' });
  const paths = [...manifest.units, ...(manifest.vocabulary || []), ...(manifest.assessments || []), ...(manifest.aiPractice || []), ...(manifest.mediaAssets || [])].map(x => x.path);
  if (!paths.every(validPath) || paths.some(x => !/^(Units|Vocabulary|Assessments|Prompts|Assets)\//.test(x)) || new Set(paths).size !== paths.length)
    throw new Error('Invalid course file references.');
  const root = new URL('./', manifestAddress);
  const files = [];
  for (let i = 0; i < paths.length; i += 4) {
    const batch = await Promise.all(paths.slice(i, i + 4).map(async path => ({ path, ...(path.startsWith('Assets/') ? await freshAsset(new URL(path, root)) : await freshJson(new URL(path, root), courseId)) })));
    files.push(...batch);
    port.postMessage({ event: 'progress', done: files.length, total: paths.length });
  }
  const byPath = new Map(files.map(x => [x.path, x]));
  for (const unit of manifest.units) if (byPath.get(unit.path)?.data.unitId !== unit.unitId) throw new Error(`Unit reference mismatch: ${unit.unitId}`);
  const source = files.filter(x => /^(Units|Vocabulary|Assessments|Prompts)\//.test(x.path)).sort((a, b) => a.path < b.path ? -1 : a.path > b.path ? 1 : 0);
  const encoder = new TextEncoder();
  const chunks = source.flatMap(x => [encoder.encode(x.path), new Uint8Array([0]), encoder.encode(String(x.bytes.length)), new Uint8Array([0]), x.bytes]);
  const joined = new Uint8Array(chunks.reduce((sum, x) => sum + x.length, 0));
  let offset = 0; for (const chunk of chunks) { joined.set(chunk, offset); offset += chunk.length; }
  const digest = [...new Uint8Array(await crypto.subtle.digest('SHA-256', joined))].map(x => x.toString(16).padStart(2, '0')).join('');
  if (`sha256:${digest}` !== manifest.contentFingerprint) throw new Error('Course fingerprint check failed. Existing offline copy was kept.');
  port.postMessage({ event: 'stage', stage: 'validated' });
  if (old && old.contentFingerprint !== manifest.contentFingerprint) {
    const previous = await caches.open(old.cacheName);
    const oldManifest = await previous.match(manifestAddress);
    if (!oldManifest) throw new Error('Previous course manifest missing; migration cannot be checked.');
    const oldData = await oldManifest.json();
    if (oldData.units.length !== manifest.units.length || oldData.units.some((u, i) => u.unitId !== manifest.units[i].unitId))
      throw new Error('Unit IDs changed; no content migration is defined. Existing offline copy was kept.');
    for (const unit of manifest.units) {
      const oldUnit = await previous.match(new URL(unit.path, root));
      const previousUnit = oldUnit && await oldUnit.json();
      if (!previousUnit || idGraph(previousUnit) !== idGraph(byPath.get(unit.path).data) || exerciseGraph(previousUnit) !== exerciseGraph(byPath.get(unit.path).data))
        throw new Error('Exercise content or stable IDs changed; no content migration is defined. Existing offline copy was kept.');
    }
    if (JSON.stringify(oldData.completionRules) !== JSON.stringify(manifest.completionRules))
      throw new Error('Completion rules changed; no content migration is defined. Existing offline copy was kept.');
    for (const path of [...manifest.assessments, ...manifest.aiPractice].map(x => x.path)) {
      const prior = await previous.match(new URL(path, root));
      if (!prior || JSON.stringify(await prior.json()) !== JSON.stringify(byPath.get(path).data))
        throw new Error('Assessment rules changed; no content migration is defined. Existing offline copy was kept.');
    }
    for (const path of manifest.vocabulary.map(x => x.path)) {
      const prior = await previous.match(new URL(path, root));
      const beforeIds = prior && (await prior.json()).entries?.map(x => x.vocabularyId);
      const afterIds = byPath.get(path).data.entries?.map(x => x.vocabularyId);
      if (!beforeIds || JSON.stringify(beforeIds) !== JSON.stringify(afterIds))
        throw new Error('Vocabulary IDs changed; no content migration is defined. Existing offline copy was kept.');
    }
  }
  const cacheName = `linguaflow-course-${courseId}-${crypto.randomUUID()}`;
  port.postMessage({ event: 'stage', stage: 'storing' });
  const cache = await caches.open(cacheName);
  try {
    const payloads = refs.concat(files);
    for (let i = 0; i < payloads.length; i++) {
      await cache.put(payloads[i].url, payloads[i].response.clone());
      port.postMessage({ event: 'stage', stage: `stored ${i + 1}/${payloads.length}` });
    }
    const record = { cacheName, courseId, contentVersion: manifest.contentVersion, contentFingerprint: manifest.contentFingerprint,
      unitCount: manifest.units.length, contentBytes: refs.concat(files).reduce((sum, x) => sum + x.bytes.length, 0) };
    await (await caches.open(metaName)).put(metaKey(courseId), new Response(JSON.stringify(record), { headers: { 'Content-Type': 'application/json' } }));
    port.postMessage({ event: 'stage', stage: 'stored' });
    if (old) await caches.delete(old.cacheName);
    return { status: 'ready', ...await getStatus(courseId) };
  } catch (error) { await caches.delete(cacheName); throw error; }
}

async function removeCourse(id) {
  const old = await getRecord(id);
  if (old) {
    await (await caches.open(metaName)).delete(metaKey(id));
    await caches.delete(old.cacheName);
  }
  const prefix = `/Courses/`;
  const runtime = await caches.open(runtimeName);
  for (const key of await runtime.keys()) {
    const path = new URL(key.url).pathname;
    if (path.includes(prefix) && path.includes(`/${id}/`)) await runtime.delete(key);
  }
  return { status: 'removed' };
}

self.addEventListener('message', event => {
  const port = event.ports[0];
  if (!port) return;
  event.waitUntil((async () => {
    try {
      const message = event.data;
      const result = message.action === 'status' ? await getStatus(message.courseId) :
        message.action === 'download' ? await downloadCourse(message, port) :
        message.action === 'remove' ? await removeCourse(message.courseId) : null;
      if (!result) throw new Error('Unknown offline action.');
      port.postMessage({ event: 'result', result });
    } catch (error) { port.postMessage({ event: 'error', message: error?.message || 'Offline operation failed.' }); }
  })());
});
