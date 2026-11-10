import { readFileSync, writeFileSync, readdirSync, existsSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { createHash } from 'node:crypto';

const [root, base] = process.argv.slice(2);
if (!root || !/^\/[A-Za-z0-9._/-]*\/$/.test(base) || base.includes('..') || base.includes('//')) throw new Error('Usage: prepare-pages.mjs <published-wwwroot> </project/>');
let index = readFileSync(join(root, 'index.html'), 'utf8');
if (!index.includes(`<base href="${base}" />`)) throw new Error('Published base href does not match requested path.');
// Some SDK builds retain the script placeholder even though the runtime file is fingerprinted.
// Resolve it from the actual published file, then update the service-worker integrity record.
if (index.includes('blazor.webassembly#[.{fingerprint}].js')) {
  const runtime = readdirSync(join(root, '_framework')).filter(name => /^blazor\.webassembly\.[a-z0-9]+\.js$/.test(name));
  if (runtime.length !== 1) throw new Error('Could not uniquely identify the published Blazor runtime script.');
  index = index.replace('blazor.webassembly#[.{fingerprint}].js', runtime[0]);
}
if (index.includes('<script type="importmap"></script>')) {
  const modules = readdirSync(join(root, '_framework')).filter(name => /^(dotnet(?:\.runtime|\.native)?)\.[a-z0-9]+\.js$/.test(name));
  const imports = {};
  for (const name of modules) {
    const match = /^(dotnet(?:\.runtime|\.native)?)\.[a-z0-9]+\.js$/.exec(name);
    imports[`./_framework/${match[1]}.js`] = `./_framework/${name}`;
  }
  if (Object.keys(imports).length !== 3) throw new Error('Could not map all published .NET runtime modules.');
  index = index.replace('<script type="importmap"></script>', `<script type="importmap">${JSON.stringify({ imports })}</script>`);
}
if (!existsSync(join(root, 'LinguaFlow.Web.styles.css'))) index = index.replace('<link href="LinguaFlow.Web.styles.css" rel="stylesheet" />', '');
writeFileSync(join(root, 'index.html'), index);
const assetsFile = join(root, 'service-worker-assets.js');
const prefix = 'self.assetsManifest = ';
const source = readFileSync(assetsFile, 'utf8');
if (!source.startsWith(prefix)) throw new Error('Unexpected Blazor service-worker asset manifest.');
const assets = JSON.parse(source.slice(prefix.length).trim().replace(/;$/, ''));
const entry = assets.assets.find(item => item.url === 'index.html');
if (!entry) throw new Error('index.html missing from service-worker asset manifest.');
entry.hash = 'sha256-' + createHash('sha256').update(readFileSync(join(root, 'index.html'))).digest('base64');
assets.version = createHash('sha256').update(source + index).digest('hex').slice(0, 12);
writeFileSync(assetsFile, prefix + JSON.stringify(assets, null, 2) + ';\n');
for (const name of ['index.html.br', 'index.html.gz', 'service-worker-assets.js.br', 'service-worker-assets.js.gz']) rmSync(join(root, name), { force: true });
writeFileSync(join(root, '.nojekyll'), '');
writeFileSync(join(root, '404.html'), `<!doctype html><html lang="en"><meta charset="utf-8"><title>LinguaFlow</title><script>
const base = ${JSON.stringify(base)};
const path = location.pathname.startsWith(base) ? '/' + location.pathname.slice(base.length) + location.search + location.hash : '/';
location.replace(base + '?p=' + encodeURIComponent(path));
</script><a href="${base}">Open LinguaFlow</a></html>`);
