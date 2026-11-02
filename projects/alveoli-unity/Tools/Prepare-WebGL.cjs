const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const source = path.resolve(process.argv[2]);
const destination = path.resolve(process.argv[3]);
assert.notEqual(source, destination, 'Use a separate publication folder.');
const original = fs.readFileSync(path.join(source, 'index.html'), 'utf8');
const nameFor = key => {
  const match = original.match(new RegExp(key + ': buildUrl \\+ "([^"\\n]+)"'));
  assert(match, 'Missing Unity build setting: ' + key);
  return path.basename(match[1]);
};
const loader = original.match(/var loaderUrl = buildUrl \+ "\/([^"\n]+)"/)[1];
const framework = nameFor('frameworkUrl');
const files = ['dataUrl', 'codeUrl'].map(key => nameFor(key));
const build = path.join(destination, 'Build');
fs.mkdirSync(build, {recursive: true});
// This directory contains only generated publication files.
for (const file of fs.readdirSync(build)) fs.unlinkSync(path.join(build, file));
for (const file of [loader, framework]) fs.copyFileSync(path.join(source, 'Build', file), path.join(build, file));
const manifest = files.map((name, index) => {
  const bytes = fs.readFileSync(path.join(source, 'Build', name));
  const compressed = zlib.gzipSync(bytes, {level: 9});
  assert.deepEqual(zlib.gunzipSync(compressed), bytes);
  const size = 256 * 1024, count = Math.ceil(compressed.length / size);
  for (let i = 0; i < count; i++) fs.writeFileSync(path.join(build, name + '.gz.' + String(i).padStart(2, '0')), compressed.subarray(i * size, (i + 1) * size));
  console.log(name + ': ' + bytes.length + ' -> ' + compressed.length + ' bytes (' + count + ' parts)');
  return {name, count, type: index ? 'application/wasm' : 'application/octet-stream'};
});
fs.writeFileSync(path.join(destination, 'index.html'), `<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>허파꽈리 기체 교환 3D</title><link rel="icon" href="data:,"><style>
body{margin:0;min-height:100vh;background:#08131b;color:#e3edf0;font:16px system-ui,sans-serif;display:flex;align-items:center;justify-content:center;flex-direction:column}header,.player{width:min(1440px,calc(100vw - 24px),calc(160vh - 128px))}header{display:flex;justify-content:space-between;align-items:center;margin:12px 0}a{color:#9ddde7}button{font:inherit;padding:8px 14px;cursor:pointer}.player{aspect-ratio:8/5;position:relative;background:#08131b}canvas{width:100%;height:100%;display:block}.loading{position:absolute;inset:0;display:grid;align-content:center;justify-items:center;gap:18px;background:#08131b;text-align:center;padding:24px}.loading h1{font-size:24px;margin:0}progress{width:min(480px,80vw);height:20px;accent-color:#45cada}#status{max-width:650px;line-height:1.6}#retry[hidden]{display:none}
</style></head><body><header><a href="../../">← 과학 실험실</a><button id="fullscreen" disabled>전체 화면</button></header><main class="player"><canvas id="unity-canvas" width="1440" height="900" tabindex="0" aria-label="허파꽈리 기체 교환 3D 시뮬레이션"></canvas><section class="loading" id="loading"><h1>허파꽈리 기체 교환 3D</h1><progress id="progress" max="1" value="0" aria-label="실행 준비 진행률"></progress><div id="status" role="status" aria-live="polite">실행 파일을 준비하고 있습니다.</div><button id="retry" hidden>다시 시도</button></section></main><script>
const files = ${JSON.stringify(manifest)};
const progress = document.querySelector('#progress'), status = document.querySelector('#status');
const total = files.reduce((n, file) => n + file.count, 0);
let downloaded = 0;
function fail(error) {
  status.textContent = '실행 준비를 마치지 못했습니다. 연결을 확인하고 다시 시도해 주세요. ' + error.message;
  document.querySelector('#retry').hidden = false;
}
document.querySelector('#retry').onclick = () => location.reload();
async function loadBuildFile(file) {
  const parts = new Array(file.count);
  let next = 0;
  // Two workers per file keep at most four downloads active.
  await Promise.all([0, 1].map(async () => {
    while (next < file.count) {
      const i = next++;
      for (let attempt = 0; attempt < 3; attempt++) {
        try {
          const response = await fetch('Build/' + file.name + '.gz.' + String(i).padStart(2, '0'), {signal: AbortSignal.timeout(45000)});
          if (!response.ok) throw new Error('HTTP ' + response.status);
          parts[i] = await response.arrayBuffer();
          downloaded++;
          progress.value = downloaded / total * .75;
          status.textContent = '실행 파일 다운로드 중 (' + downloaded + '/' + total + ') · 처음 실행은 시간이 걸릴 수 있습니다.';
          break;
        } catch (error) { if (attempt === 2) throw error; }
      }
    }
  }));
  const stream = new Blob(parts).stream().pipeThrough(new DecompressionStream('gzip'));
  return URL.createObjectURL(new Blob([await new Response(stream).arrayBuffer()], {type: file.type}));
}
const script = document.createElement('script');
script.src = 'Build/${loader}';
script.onerror = () => fail(new Error('실행 도구 다운로드 오류'));
script.onload = async () => {
  try {
    const urls = await Promise.all(files.map(loadBuildFile));
    status.textContent = '3D 화면을 시작하고 있습니다.';
    const instance = await createUnityInstance(document.querySelector('#unity-canvas'), {
      dataUrl: urls[0], codeUrl: urls[1], frameworkUrl: 'Build/${framework}',
      companyName: 'Science Classroom', productName: '허파꽈리 기체 교환', productVersion: '1.1',
      devicePixelRatio: Math.min(window.devicePixelRatio || 1, 1.5),
      showBanner: (message, type) => { if (type === 'error') fail(new Error(message)); }
    }, value => { progress.value = .75 + value * .25; });
    urls.forEach(url => URL.revokeObjectURL(url));
    document.querySelector('#loading').remove();
    document.querySelector('#fullscreen').disabled = false;
    document.querySelector('#fullscreen').onclick = () => instance.SetFullscreen(1);
  } catch (error) { fail(error); }
};
document.body.appendChild(script);
</script></body></html>`);
console.log('PASS: compressed publication generated with versioned build filenames.');
