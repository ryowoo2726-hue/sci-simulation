// Run from any directory: node simulations/heart-3d/check-web.cjs
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const zlib = require('node:zlib');
const root = __dirname;
const host = fs.readFileSync(path.join(root, '../blood-3d/index.html'), 'utf8');
const code = host.match(/<script>([\s\S]*?)<\/script>/)[1];

function exercise(query, mobile = false) {
  const elements = {};
  for (const id of ['workbench','blood-panel','heart-panel','blood-frame','heart-frame','view-hint']) {
    elements['#'+id] = {dataset:{src:id.startsWith('blood')?'blood-simulation.html?embed=1':'../heart-3d/?embed=1'},
      classList:{toggle(){}},hasAttribute(name){return this[name]!==undefined;}};
  }
  const buttons = ['compare','blood','heart'].map(view=>({dataset:{view},setAttribute(key,value){this[key]=value;},addEventListener(){}}));
  const context = vm.createContext({URL, location:{href:'https://example.com/sci-simulation/simulations/blood-3d/'+query},
    history:{replaceState(){}}, window:{matchMedia:()=>({matches:mobile})},
    document:{querySelector:s=>elements[s],querySelectorAll:()=>buttons}});
  vm.runInContext(code,context);
  return {elements,buttons,show:view=>vm.runInContext(`show(${JSON.stringify(view)})`,context)};
}
let test=exercise('?view=heart');
assert.equal(test.elements['#blood-panel'].hidden,true);
assert.equal(test.elements['#blood-frame'].src,undefined,'single view must not download the other app');
assert.ok(test.elements['#heart-frame'].src);
test.show('compare');
assert.equal(test.elements['#blood-panel'].hidden,false);
assert.ok(test.elements['#blood-frame'].src);
test.show('blood');
assert.equal(test.elements['#heart-panel'].hidden,true);
assert.equal(test.buttons[1]['aria-pressed'],'true');
test=exercise('',true);
assert.equal(test.elements['#heart-frame'].src,undefined,'mobile starts with one app');
test=exercise('?view=invalid');
assert.equal(test.elements['#heart-panel'].hidden,false);

for (const name of ['data','wasm','framework.js']) {
  const file=fs.readFileSync(path.join(root,`Build/HeartWeb.${name}.unityweb`));
  const raw=zlib.gunzipSync(file);
  assert.ok(raw.length>file.length);
  if(name==='wasm')assert.deepEqual([...raw.subarray(0,4)],[0,97,115,109]);
}
for(const file of ['Build/HeartWeb.loader.js','ATTRIBUTION.txt','Z-Anatomy-LICENSE.txt','NotoSansKR-LICENSE.txt','../blood-3d/blood-simulation.html'])
  assert.ok(fs.statSync(path.join(root,file)).size>0,file);
console.log('PASS: view switching, lazy loading, mobile default, compressed assets and licenses');
