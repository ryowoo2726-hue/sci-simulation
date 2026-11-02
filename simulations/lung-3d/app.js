/* Simplified teaching model: the branching pattern and breathing amplitude are illustrative. */
(() => {
  const host = document.getElementById('view');
  if (!window.THREE || !THREE.OrbitControls) { host.textContent = '3D 화면을 불러오지 못했습니다.'; return; }
  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(36, 1, .1, 100);
  camera.position.set(0, .65, 12.7);
  const renderer = new THREE.WebGLRenderer({antialias:true, alpha:true});
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.5));
  renderer.outputEncoding = THREE.sRGBEncoding;
  host.appendChild(renderer.domElement);
  const orbit = new THREE.OrbitControls(camera, renderer.domElement);
  orbit.target.set(0, .25, 0); orbit.enableDamping = true; orbit.enablePan = false; orbit.minDistance = 6; orbit.maxDistance = 19;
  scene.add(new THREE.HemisphereLight(0xffffff, 0x8ca89d, .8));
  const sun = new THREE.DirectionalLight(0xffffff, .65); sun.position.set(4,6,8); scene.add(sun);
  const fill = new THREE.DirectionalLight(0xffffff,.25); fill.position.set(-5,2,-4); scene.add(fill);
  const anatomy = new THREE.Group(); scene.add(anatomy);
  const lung = new THREE.Group(), trachea = new THREE.Group(), bronchi = new THREE.Group(), closeup = new THREE.Group();
  anatomy.add(lung,trachea,bronchi); scene.add(closeup);
  const material = (color, options={}) => new THREE.MeshPhongMaterial({color, shininess:45, ...options});
  const lungMaterial = material(0xd98e87,{transparent:true,opacity:.46,depthWrite:false,side:THREE.DoubleSide});
  const windMaterial = material(0xe9bc81), cartilage = material(0xc98b59), branchMaterial = material(0xd0804f), sacMaterial = material(0xe68c86), sacHighlight = material(0xf0a8a0);
  const v = (...p) => new THREE.Vector3(...p);
  function path(group, points, radius, mat, detail=32) {
    const curve = new THREE.CatmullRomCurve3(points.map(p => v(...p)));
    const mesh = new THREE.Mesh(new THREE.TubeGeometry(curve,detail,radius,9,false),mat); group.add(mesh); return curve;
  }
  function ball(group, position, scale, mat) {
    const obj = new THREE.Mesh(new THREE.SphereGeometry(1,24,18), mat); obj.position.set(...position); obj.scale.set(...scale); group.add(obj); return obj;
  }
  // The shell is tapered at the top and broad at the bottom to read as lungs.
  for (const side of [-1,1]) {
    const geometry = new THREE.SphereGeometry(1,48,36);
    const positions = geometry.attributes.position;
    for (let i=0;i<positions.count;i++) {
      const x=positions.getX(i), y=positions.getY(i), z=positions.getZ(i);
      const taper = 1 - .24 * Math.max(y,0) + .06 * Math.max(-y,0);
      positions.setXYZ(i, x*taper + side*.10*(1-y*y), y, z*(.9+.1*(1-y*y)));
    }
    geometry.computeVertexNormals();
    const mesh = new THREE.Mesh(geometry, lungMaterial); mesh.position.set(side*1.24,-.16,0); mesh.scale.set(1.08,1.86,.7); mesh.rotation.z=side*.11; lung.add(mesh);
  }
  path(trachea,[[0,2.85,0],[0,2.2,0],[0,1.52,0],[0,1.04,0]],.22,windMaterial);
  for(let i=0;i<11;i++){
    const ring=new THREE.Mesh(new THREE.TorusGeometry(.222,.025,8,32),cartilage); ring.rotation.x=Math.PI/2; ring.position.y=1.2+i*.145; trachea.add(ring);
  }
  // The main airways feed progressively smaller branches within each lung.
  const curves=[];
  for(const side of [-1,1]) {
    const main=path(bronchi,[[0,1.08,0],[side*.42,.72,0],[side*.82,.4,0],[side*1.14,-.05,0],[side*1.48,-.72,0],[side*1.65,-1.34,0]],.14,branchMaterial,40); curves.push(main);
    const branches=[
      [[side*.53,.62,0],[side*.92,.96,0],[side*1.31,1.33,-.05],[side*1.58,1.5,-.06]],
      [[side*.89,.32,0],[side*1.25,.52,.08],[side*1.73,.65,.12],[side*1.95,.76,.12]],
      [[side*1.16,-.08,0],[side*1.55,-.05,-.05],[side*1.91,.06,-.09]],
      [[side*1.35,-.43,0],[side*1.72,-.46,.08],[side*2,-.65,.12]],
      [[side*1.51,-.78,0],[side*1.82,-.96,-.03],[side*1.98,-1.27,-.08]],
      [[side*1.57,-.95,0],[side*1.31,-1.2,.12],[side*1.12,-1.56,.16]],
      [[side*.85,.35,0],[side*.68,.02,-.10],[side*.57,-.48,-.15]]
    ];
    branches.forEach((points,i)=>{
      path(bronchi,points,.055,branchMaterial,24);
      const end=points[points.length-1], prev=points[points.length-2];
      const dx=end[0]-prev[0],dy=end[1]-prev[1];
      path(bronchi,[prev,end,[end[0]+side*.15,end[1]+.22,end[2]+.04]],.026,windMaterial,16);
      path(bronchi,[prev,end,[end[0]+side*.14,end[1]-.20,end[2]-.05]],.026,windMaterial,16);
    });
  }
  // A separate enlarged, schematic alveolar cluster.
  path(closeup,[[-2.6,.45,0],[-1.65,.45,0],[-.9,.25,0],[-.32,0,0]],.16,windMaterial);
  path(closeup,[[-.8,.26,0],[-.42,.65,0],[-.13,.88,0]],.075,branchMaterial);
  path(closeup,[[-.7,.18,0],[-.38,-.24,0],[-.12,-.58,0]],.075,branchMaterial);
  const cluster=ball(closeup,[.28,.1,0],[.48,.5,.48],sacHighlight);
  const sacs=[];
  const points=[[-.08,.72,.05],[.32,.84,.03],[.75,.68,.02],[1.02,.35,.05],[1.08,-.04,0],[.88,-.44,.02],[.48,-.65,0],[.08,-.58,.06],[-.22,-.32,.12],[-.22,.18,.25],[.20,.28,.45],[.65,.15,.45],[.28,-.25,.45],[.63,-.30,-.35],[.3,.25,-.4]];
  points.forEach((p,i)=>sacs.push(ball(closeup,p,[.36,.34,.34],i%3===0?sacHighlight:sacMaterial)));
  const loopMat=material(0xb87975); for(let i=0;i<3;i++){
    let r=new THREE.Mesh(new THREE.TorusGeometry(.35,.018,6,40),loopMat);r.position.set(.28,.08,.03);r.rotation.set(i*.83,.35+i*.6,.4);closeup.add(r);
  }
  closeup.visible=false;
  const airwayParticles=[];
  for(let i=0;i<4;i++) {let p=ball(anatomy,[0,0,.24],[.06,.06,.06],material(0x79b9aa,{emissive:0x256457,emissiveIntensity:.15})); airwayParticles.push(p)}
  const descriptions={
    lung:['폐','가슴 속에 있는 한 쌍의 기관입니다. 숨을 들이쉴 때 공기가 들어오고, 내쉴 때 공기가 나갑니다.','실제 사람의 오른쪽 폐는 3엽, 왼쪽 폐는 2엽으로 나뉩니다.'],
    trachea:['기관','코와 입으로 들어온 공기가 지나가는 통로입니다. 아래쪽에서 좌우 기관지로 갈라집니다.','기관을 둘러싼 연골은 공기가 지나는 길이 유지되도록 돕습니다.'],
    bronchi:['기관지','기관에서 갈라져 각각의 폐로 이어지는 공기 통로입니다. 폐 안에서 점점 가는 세기관지로 나뉩니다.','기관지와 세기관지는 공기를 허파꽈리까지 전달합니다.'],
    alveoli:['허파꽈리','세기관지 끝에 모여 있는 작은 공기주머니입니다. 이곳에서 허파꽈리와 모세 혈관 사이에 기체 교환이 일어납니다.','산소: 허파꽈리 → 혈액  ·  이산화 탄소: 혈액 → 허파꽈리']
  };
  let selected='lung', running=!matchMedia('(prefers-reduced-motion: reduce)').matches, showTissue=true, clock=0, last=0, visible=true;
  const buttons=[...document.querySelectorAll('.tab')];
  function setView(name) {
    selected=name;
    for(const b of buttons){const on=b.dataset.view===name;b.classList.toggle('active',on);b.setAttribute('aria-pressed',String(on))}
    document.getElementById('name').textContent=descriptions[name][0]; document.getElementById('description').textContent=descriptions[name][1]; document.getElementById('fact').textContent=descriptions[name][2];
    anatomy.visible=name!=='alveoli'; closeup.visible=name==='alveoli';
    document.getElementById('viewlabel').textContent=name==='alveoli'?'허파꽈리 확대 모형 · 실제 크기 비율 아님':'정면에서 보는 폐';
    document.getElementById('tissue').disabled=name==='alveoli'; document.getElementById('tissue').style.opacity=name==='alveoli'?'.45':'1';
    lungMaterial.opacity=name==='lung'?.46:.18; windMaterial.color.setHex(name==='trachea'?0xffc875:0xe9bc81); branchMaterial.color.setHex(name==='bronchi'?0xe27f40:0xd0804f);
    camera.position.set(0,.65,name==='alveoli'?8.5:10.5); orbit.target.set(name==='alveoli'?-.5:0,name==='alveoli'?.1:.25,0); orbit.update();
  }
  buttons.forEach(b=>b.addEventListener('click',()=>setView(b.dataset.view)));
  document.getElementById('reset').addEventListener('click',()=>setView(selected));
  document.getElementById('tissue').addEventListener('click',e=>{showTissue=!showTissue;lung.visible=showTissue;e.currentTarget.setAttribute('aria-pressed',String(showTissue));e.currentTarget.textContent=showTissue?'◉ 폐 겉면 보이기':'○ 폐 겉면 숨기기'});
  function updatePlay(){const b=document.getElementById('play');b.textContent=running?'Ⅱ 호흡 일시 정지':'▶ 호흡 재생';b.setAttribute('aria-pressed',String(running))}
  document.getElementById('play').addEventListener('click',()=>{running=!running;updatePlay()}); updatePlay();
  host.addEventListener('keydown',e=>{if(e.key!=='ArrowLeft' && e.key!=='ArrowRight')return; e.preventDefault(); const angle=e.key==='ArrowLeft'?.16:-.16; camera.position.sub(orbit.target).applyAxisAngle(new THREE.Vector3(0,1,0),angle).add(orbit.target); orbit.update()});
  function resize(){let w=host.clientWidth,h=host.clientHeight;if(!w||!h)return;renderer.setSize(w,h,false);camera.aspect=w/h;camera.updateProjectionMatrix()};window.addEventListener('resize',resize);resize();
  if ('IntersectionObserver' in window) new IntersectionObserver(entries=>{visible=entries[entries.length-1].isIntersecting}).observe(host);
  function frame(t) {
    requestAnimationFrame(frame); const dt=last?Math.min((t-last)/1000,.05):0;last=t;if(!visible)return;
    if(running)clock+=dt;
    const wave=(1-Math.cos(clock*Math.PI/2))/2, inhale=(clock%4)<2;
    lung.scale.set(1+.055*wave,1+.035*wave,1+.04*wave);
    closeup.scale.setScalar(1+.045*wave);
    document.getElementById('phase').textContent=running?(inhale?'숨을 들이쉬는 중':'숨을 내쉬는 중'):'호흡 일시 정지';
    document.getElementById('phase-detail').textContent=running?(inhale?'폐가 부풀어 올라요':'폐가 작아져요'):'재생 버튼을 눌러 보세요';
    airwayParticles.forEach((p,i)=>{let f=(clock*.18+i/4)%1;if(!inhale)f=1-f;const pt=curves[i%2].getPointAt(f);p.position.copy(pt);p.position.z+=.22;p.visible=selected!=='alveoli'&&running});
    orbit.update();renderer.render(scene,camera);
  }
  setView('lung');requestAnimationFrame(frame);
})();

