import { readFileSync, writeFileSync } from 'node:fs';

const [file, base] = process.argv.slice(2);
if (!file || !/^\/[A-Za-z0-9._/-]*\/$/.test(base) || base.includes('..') || base.includes('//')) throw new Error('Usage: set-base.mjs <index.html> </project/>');
const html = readFileSync(file, 'utf8');
if (!html.includes('<base href="/" />')) throw new Error('Expected root base href in source index.html.');
writeFileSync(file, html.replace('<base href="/" />', `<base href="${base}" />`));
