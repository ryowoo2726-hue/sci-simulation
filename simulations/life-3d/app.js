import * as T from 'three';
import {OrbitControls} from 'three/addons/controls/OrbitControls.js';
import {createModel} from './models.js';
import {parts} from './data.js';
import {loadRefinedModel} from './refined-models.js';
import {RoomEnvironment} from './vendor/RoomEnvironment.js';

const $=id=>document.getElementById(id);
const meta={
  cell:{title:'동물세포',category:'세포 · CELL',point:'세포가 생명 활동에 필요한 에너지를 얻는 과정에서 미토콘드리아는 어떤 역할을 할까요?',tip:'세포 소기관은 서로 협력합니다. 미토콘드리아와 핵을 찾아 기능을 비교해 보세요.'},
  muscle:{title:'근육 조직',category:'조직 · TISSUE',point:'골격근 섬유 하나는 여러 핵을 가진 긴 세포입니다. 여러 근섬유가 모여 근육 조직을 이룹니다.',tip:'근육 수축에 필요한 에너지는 ATP가 공급합니다. 근원섬유 사이의 미토콘드리아를 찾아보세요.'},
  heart:{title:'심장',category:'기관 · ORGAN',point:'온몸 → 대정맥 → 우심방 → 우심실 → 폐동맥 → 폐 → 폐정맥 → 좌심방 → 좌심실 → 대동맥 → 온몸',tip:'정면에서 보면 해부학적 오른쪽은 화면 왼쪽입니다. 빨강·파랑은 산소량을 구분하는 표현이며 실제 혈액은 모두 붉습니다.'}
};
let renderer,scene,camera,controls,model,current='cell',selected=null,hovered=null,showLabels=false,inside=true;
let labelNodes=[],pointerDown=null,lastTime=0,modelRequest=0;
const viewport=$('viewport'),tooltip=$('tooltip'),raycaster=new T.Raycaster(),pointer=new T.Vector2();

function init(){
  renderer=new T.WebGLRenderer({antialias:true,alpha:true});
  renderer.setPixelRatio(Math.min(devicePixelRatio,2));renderer.setClearColor(0x000000,0);
  renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.ACESFilmicToneMapping;renderer.toneMappingExposure=1.22;
  renderer.shadowMap.enabled=true;renderer.shadowMap.type=T.PCFSoftShadowMap;
  renderer.domElement.setAttribute('aria-hidden','true');viewport.prepend(renderer.domElement);
  scene=new T.Scene();camera=new T.PerspectiveCamera(36,1,.1,100);
  controls=new OrbitControls(camera,renderer.domElement);controls.enableDamping=true;controls.dampingFactor=.08;controls.enablePan=false;controls.minDistance=5.2;controls.maxDistance=22;controls.autoRotateSpeed=.7;
  const room=new RoomEnvironment(),pmrem=new T.PMREMGenerator(renderer);
  scene.environment=pmrem.fromScene(room,.04).texture;scene.environmentIntensity=.68;
  room.dispose();pmrem.dispose();
  scene.add(new T.HemisphereLight(0xffffff,0x84917b,1.25));
  const key=new T.DirectionalLight(0xfff0dc,2.6);key.position.set(-4,6,9);key.castShadow=true;
  key.shadow.mapSize.set(2048,2048);key.shadow.camera.left=-5;key.shadow.camera.right=5;key.shadow.camera.top=5;key.shadow.camera.bottom=-5;key.shadow.camera.near=.5;key.shadow.camera.far=24;key.shadow.normalBias=.025;key.shadow.bias=-.0001;scene.add(key);
  const fill=new T.DirectionalLight(0xd9edff,1.2);fill.position.set(5,2,-4);scene.add(fill);
  const bounce=new T.DirectionalLight(0xfff6ee,.65);bounce.position.set(2,-1,8);scene.add(bounce);
  const contact=new T.Mesh(new T.PlaneGeometry(30,30),new T.ShadowMaterial({opacity:.12}));contact.rotation.x=-Math.PI/2;contact.position.y=-2.84;contact.receiveShadow=true;scene.add(contact);
  // A quiet ground ring gives scale without competing with the structure.
  const ground=new T.Mesh(new T.RingGeometry(2.7,2.72,96),new T.MeshBasicMaterial({color:0xc3cebb,transparent:true,opacity:.55,side:T.DoubleSide}));ground.rotation.x=-Math.PI/2;ground.position.y=-2.85;scene.add(ground);
  new ResizeObserver(resize).observe(viewport);
  renderer.domElement.addEventListener('pointermove',onMove);
  renderer.domElement.addEventListener('pointerleave',()=>{hovered=null;tooltip.hidden=true;highlight();});
  renderer.domElement.addEventListener('pointerdown',e=>{pointerDown={x:e.clientX,y:e.clientY,time:performance.now()};tooltip.hidden=true;});
  renderer.domElement.addEventListener('pointerup',e=>{if(pointerDown&&Math.hypot(e.clientX-pointerDown.x,e.clientY-pointerDown.y)<6&&performance.now()-pointerDown.time<700){const hit=pick(e);if(hit)selectPart(hit.id);else clearSelection();}pointerDown=null;});
  renderer.domElement.addEventListener('webglcontextlost',e=>{e.preventDefault();$('error').textContent='3D 화면 연결이 끊겼습니다. 페이지를 새로고침해 주세요.';$('error').hidden=false;});
  const requestedModel=new URLSearchParams(location.search).get('model');
  loadModel(Object.hasOwn(meta,requestedModel)?requestedModel:'cell');resize();requestAnimationFrame(animate);
}
function resize(){if(!renderer)return;const w=viewport.clientWidth,h=viewport.clientHeight;renderer.setSize(w,h,false);camera.aspect=w/h;camera.updateProjectionMatrix();if(model){const min=minimumViewDistance();if(camera.position.distanceTo(controls.target)<min)camera.position.sub(controls.target).setLength(min).add(controls.target);}}
function minimumViewDistance(){return Math.max(11.6,(current==='heart'?9:12.3)/camera.aspect);}
function resetCamera(){camera.position.set(0,.4,minimumViewDistance());controls.target.set(0,current==='heart'?.2:0,0);controls.update();}
function loadModel(kind){
  if(model){scene.remove(model.root);disposeModel(model);}
  current=kind;selected=null;hovered=null;inside=true;model=createModel(kind);scene.add(model.root);
  model.covers.forEach(m=>m.visible=false);$('inside').setAttribute('aria-pressed','true');
  $('model-title').textContent=meta[kind].title;$('search').value='';
  document.querySelectorAll('[data-model]').forEach(b=>{const active=b.dataset.model===kind;b.classList.toggle('active',active);b.setAttribute('aria-pressed',String(active));});
  labelNodes=[];$('labels').replaceChildren();
  parts[kind].forEach(p=>{const b=document.createElement('button');b.className='part-label';b.textContent=p.name.split(' (')[0];b.addEventListener('click',()=>selectPart(p.id));b.hidden=true;$('labels').append(b);labelNodes.push({id:p.id,el:b});});
  tooltip.hidden=true;renderList();showDefault();resetCamera();
  const request=++modelRequest,tag=document.querySelector('.tag');tag.textContent='정교한 모형 불러오는 중…';
  loadRefinedModel(kind).then(refined=>{
    if(request!==modelRequest){disposeModel(refined);return;}
    scene.remove(model.root);disposeModel(model);model=refined;scene.add(model.root);
    const maxAnisotropy=renderer.capabilities.getMaxAnisotropy();
    model.root.traverse(o=>{if(o.isMesh){o.castShadow=true;o.receiveShadow=true;for(const key of ['map','normalMap','roughnessMap'])if(o.material[key])o.material[key].anisotropy=Math.min(8,maxAnisotropy);}});
    setInside(inside);highlight();tag.textContent='정밀 구조 · 조직 질감';
  }).catch(error=>{console.warn('Refined model unavailable; using the basic model.',error);if(request===modelRequest)tag.textContent='기본 교육용 모형';});
}
function disposeModel(value){
  const textures=new Set(),images=new Set();
  value.root.traverse(o=>{o.geometry?.dispose();if(o.material){for(const v of Object.values(o.material))if(v?.isTexture)textures.add(v);o.material.dispose();}});
  for(const t of textures){if(t.source?.data)images.add(t.source.data);t.dispose();}
  for(const i of images)i.close?.();
}
function renderList(){
  const list=$('parts-list'),q=$('search').value.trim().toLowerCase();list.replaceChildren();
  const found=parts[current].filter(p=>(p.name+' '+p.english).toLowerCase().includes(q));
  for(const p of found){const b=document.createElement('button');b.className='part-button';b.dataset.part=String(p.id);b.classList.toggle('selected',selected===p.id);b.setAttribute('aria-pressed',String(selected===p.id));const dot=document.createElement('span');dot.className='swatch';dot.style.background=p.color;const text=document.createElement('span');text.textContent=p.name;b.append(dot,text);b.addEventListener('click',()=>selectPart(p.id));list.append(b);}
  if(!found.length){const p=document.createElement('p');p.className='empty';p.textContent='일치하는 부위가 없습니다.';list.append(p);}
  $('part-count').textContent=parts[current].length+'개 부위';
}
function showDefault(){
  $('detail-category').textContent=meta[current].category;$('part-name').textContent='궁금한 부위를 선택하세요';$('part-english').textContent='SELECT A STRUCTURE';$('part-desc').textContent='모형에 마우스를 올리면 이름이 나타납니다. 부위를 누르거나 아래 목록에서 선택해 보세요.';$('location-box').hidden=true;$('connection-text').textContent=meta[current].point;
}
function selectPart(id){
  const p=parts[current].find(p=>p.id===id);if(!p)return;
  selected=id;
  if(!inside)setInside(true);
  controls.autoRotate=false;$('spin').setAttribute('aria-pressed','false');
  $('part-name').textContent=p.name;$('part-english').textContent=p.english;$('part-desc').textContent=p.description;$('location-box').hidden=false;$('part-location').textContent=p.location;$('connection-text').textContent=meta[current].tip;
  $('detail-category').textContent=meta[current].category+' / '+String(id).padStart(2,'0');
  document.querySelectorAll('.part-button').forEach(b=>{const active=Number(b.dataset.part)===id;b.classList.toggle('selected',active);b.setAttribute('aria-pressed',String(active));});
  highlight();tooltip.hidden=true;
}
function clearSelection(){selected=null;hovered=null;showDefault();renderList();highlight();tooltip.hidden=true;}
function setInside(value){inside=value;model.covers.forEach(m=>m.visible=!inside);$('inside').setAttribute('aria-pressed',String(inside));}
function highlight(){
  model.root.traverse(o=>{if(!o.material||!o.userData.part)return;const id=o.userData.part.id;o.material.emissive.set(id===selected?0x80672b:id===hovered?0x426a53:0x000000);o.material.emissiveIntensity=id===selected?.42:.28;});
}
function pick(e){
  const r=renderer.domElement.getBoundingClientRect();pointer.set((e.clientX-r.left)/r.width*2-1,-(e.clientY-r.top)/r.height*2+1);raycaster.setFromCamera(pointer,camera);
  const hits=raycaster.intersectObject(model.root,true).filter(h=>{let o=h.object;while(o){if(!o.visible)return false;o=o.parent;}return h.object.userData.part;});
  return hits[0]?.object.userData.part;
}
function onMove(e){
  if(e.buttons){tooltip.hidden=true;return;}
  const p=pick(e),next=p?.id??null;
  if(next!==hovered){hovered=next;highlight();}
  renderer.domElement.style.cursor=p?'pointer':'grab';
  tooltip.hidden=!p;
  if(p){tooltip.textContent=p.name;const r=viewport.getBoundingClientRect();tooltip.style.left=Math.max(8,Math.min(e.clientX-r.left+14,r.width-tooltip.offsetWidth-8))+'px';tooltip.style.top=Math.max(55,Math.min(e.clientY-r.top-36,r.height-40))+'px';}
}
function updateLabels(){
  if(!model)return;
  const w=viewport.clientWidth,h=viewport.clientHeight,occupied=[];
  // Selected label has priority; other labels avoid overlap.
  const nodes=[...labelNodes].sort((a,b)=>(b.id===selected)-(a.id===selected));
  for(const {id,el}of nodes){
    if((!showLabels&&id!==selected)||(!inside&&id!==selected)){el.hidden=true;continue;}
    const pos=model.anchors.get(id).clone();model.root.localToWorld(pos);pos.project(camera);
    const x=(pos.x*.5+.5)*w,y=(-pos.y*.5+.5)*h;
    if(pos.z>1||pos.z< -1||x<30||x>w-30||y<58||y>h-35){el.hidden=true;continue;}
    const textWidth=Math.min(180,el.textContent.length*10+18),rect={x:x-textWidth/2,y:y-12,w:textWidth,h:24};
    if(id!==selected&&occupied.some(r=>Math.abs(r.x+r.w/2-x)<(r.w+rect.w)/2+4&&Math.abs(r.y+r.h/2-y)<26)){el.hidden=true;continue;}
    el.hidden=false;el.style.left=x+'px';el.style.top=y+'px';el.classList.toggle('selected',id===selected);occupied.push(rect);
  }
}
function animate(time){requestAnimationFrame(animate);const delta=Math.min((time-lastTime)/1000,.05);lastTime=time;controls.update(delta);renderer.render(scene,camera);updateLabels();}

document.querySelectorAll('[data-model]').forEach(b=>b.addEventListener('click',()=>{if(renderer)loadModel(b.dataset.model);}));
$('search').addEventListener('input',renderList);$('clear-selection').addEventListener('click',clearSelection);
$('reset').addEventListener('click',resetCamera);
$('labels-toggle').addEventListener('click',()=>{showLabels=!showLabels;$('labels-toggle').setAttribute('aria-pressed',String(showLabels));});
$('inside').addEventListener('click',()=>{clearSelection();setInside(!inside);});
$('spin').addEventListener('click',()=>{controls.autoRotate=!controls.autoRotate;$('spin').setAttribute('aria-pressed',String(controls.autoRotate));});
$('fullscreen').addEventListener('click',async()=>{try{if(document.fullscreenElement)await document.exitFullscreen();else await document.querySelector('.workspace').requestFullscreen();}catch{$('fullscreen').textContent='전체 화면 미지원';}});
$('reference').addEventListener('click',()=>{$('reference-title').textContent=meta[current].title+' · 원본 도안';$('reference-image').src='./references/'+current+'.png';$('reference-dialog').showModal();});
$('close-reference').addEventListener('click',()=>$('reference-dialog').close());
$('reference-dialog').addEventListener('click',e=>{if(e.target===$('reference-dialog')){const r=e.target.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)e.target.close();}});
try{init();}catch(error){console.error(error);$('error').hidden=false;$('error').textContent='3D 화면을 시작할 수 없습니다. 최신 Chrome 또는 Edge에서 열고 브라우저의 그래픽 가속 설정을 확인해 주세요.';document.querySelectorAll('.toolbar button').forEach(b=>b.disabled=true);}
