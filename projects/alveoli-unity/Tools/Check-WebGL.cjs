const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const site = path.resolve(process.argv[2]);
const html = fs.readFileSync(path.join(site, 'index.html'), 'utf8');
const elements = new Map();
let active = 0, maximum = 0, retries = 0;
const context = vm.createContext({
  Blob, Response, URL, DecompressionStream, AbortSignal, console,
  document: {
    querySelector: selector => { if (!elements.has(selector)) elements.set(selector, {hidden: true}); return elements.get(selector); },
    createElement: () => ({}), body: {appendChild() {}}
  },
  fetch: async url => {
    active++; maximum = Math.max(maximum, active);
    await new Promise(resolve => setTimeout(resolve, 1));
    try {
      if (!retries++) return new Response('', {status: 503});
      return new Response(fs.readFileSync(path.join(site, url)));
    } finally { active--; }
  }
});
vm.runInContext(html.match(/<script>([\s\S]*?)<\/script>/)[1], context);
(async () => {
  const files = vm.runInContext('files', context);
  const urls = await Promise.all(files.map(file => context.loadBuildFile(file)));
  assert(maximum <= 4, 'Download concurrency exceeded four');
  for (let i = 0; i < files.length; i++) {
    const file = files[i];
    const response = await fetch(urls[i]);
    assert.equal(response.headers.get('content-type'), file.type);
    const actual = Buffer.from(await response.arrayBuffer());
    const parts = Array.from({length: file.count}, (_, j) => fs.readFileSync(path.join(site, 'Build', file.name + '.gz.' + String(j).padStart(2, '0'))));
    assert(actual.equals(zlib.gunzipSync(Buffer.concat(parts))), 'Restored build bytes differ');
    if (file.type === 'application/wasm') assert(WebAssembly.validate(actual));
    URL.revokeObjectURL(urls[i]);
  }
  assert.equal(elements.get('#progress').value, .75);
  context.fetch = async () => new Response('', {status: 404});
  await assert.rejects(() => context.loadBuildFile(files[0]), /HTTP 404/);
  context.fail(new Error('test failure'));
  assert.equal(elements.get('#retry').hidden, false);
  console.log('PASS: retry after interrupted download; at most four downloads; exact build bytes; valid WebAssembly; progress; error and retry UI');
})().catch(error => { console.error(error.message); process.exitCode = 1; });
