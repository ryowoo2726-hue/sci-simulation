import * as T from 'three';
import {parts} from './data.js';

// 도안의 색과 배치를 참고한 절개형 교육 모형. 치수는 학습용 상대 크기입니다.
export function createModel(kind) {
  const root=new T.Group(), groups=new Map(), covers=[];
  const metadata=parts[kind];
  for(const p of metadata){const g=new T.Group();g.userData.part=p;root.add(g);groups.set(p.id,g);}
  const mat=(color,extra={})=>new T.MeshStandardMaterial({color,roughness:.62,metalness:.02,...extra});
  const add=(id,geo,pos=[0,0,0],scale=[1,1,1],color,extra={})=>{
    const m=new T.Mesh(geo,mat(color||metadata[id-1].color,extra));m.position.set(...pos);m.scale.set(...scale);m.userData.part=metadata[id-1];groups.get(id).add(m);return m;
  };
  const ball=(id,pos,scale,color)=>add(id,new T.SphereGeometry(1,32,24),pos,scale,color);
  const tube=(id,points,radius=.06,color)=>add(id,new T.TubeGeometry(new T.CatmullRomCurve3(points.map(p=>new T.Vector3(...p))),48,radius,9,false),[0,0,0],[1,1,1],color);
  const ring=(id,pos,r,thick,scale=[1,1,1],color)=>add(id,new T.TorusGeometry(r,thick,12,64),pos,scale,color);
  const rod=(id,a,b,r,color)=>{const start=new T.Vector3(...a),end=new T.Vector3(...b),d=end.clone().sub(start);const m=add(id,new T.CylinderGeometry(r,r,d.length(),16),start.clone().add(end).multiplyScalar(.5).toArray(),[1,1,1],color);m.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),d.normalize());return m;};
  const shell=(id,pos,scale,color,front=false)=>add(id,new T.SphereGeometry(1,64,40,front?0:Math.PI,Math.PI),pos,scale,color,{side:T.DoubleSide});

  if(kind==='cell'){
    shell(17,[0,0,0],[3.55,2.5,1.65]);
    shell(18,[0,0,0.025],[3.42,2.37,1.5],'#e7dcb1');
    ring(17,[0,0,.025],1,.033,[3.55,2.5,1]);
    covers.push(shell(17,[0,0,0],[3.55,2.5,1.65],null,true));
    // 核: back shell exposes the nucleolus and chromatin.
    shell(4,[-.85,.48,.43],[1.12,1.02,.85]);
    shell(1,[-.85,.48,.46],[1.04,.94,.78]);
    ring(4,[-.85,.48,.46],1,.045,[1.1,1,1]);
    ball(2,[-.94,.45,.6],[.39,.36,.33]);
    for(let j=0;j<3;j++)tube(3,Array.from({length:14},(_,i)=>[-.85+Math.cos(i*.8+j*2)*(.61+j*.06),.48+Math.sin(i*.8+j*2)*(.59+j*.04),.53+Math.sin(i*1.2)*.08]),.025);
    for(let j=0;j<5;j++){
      const y=.95-j*.33;
      tube(5,[[-.25,y,-.04],[.45,y+.28,.15],[1.05,y+.07,.3],[1.45,y-.19,.16]],.13);
      for(let k=0;k<5;k++)ball(7,[.05+k*.26,y+.14+Math.sin(k)*.12,.33],[.046,.046,.046]);
    }
    for(let j=0;j<4;j++)tube(6,[[1.2,.8-j*.25,.1],[1.7,1.12-j*.24,.12],[2.1,.83-j*.22,.14],[2.4,1.1-j*.28,.12]],.075);
    for(let j=0;j<5;j++)tube(8,[[-2.45,-.92-j*.17,.1],[-2,-.75-j*.17,.35],[-1.6,-.95-j*.17,.32],[-1.32,-.86-j*.17,.12]],.095);
    [[-2.5,-1.3,.3],[-1.28,-1.55,.3],[-1.5,-1.8,.2]].forEach(p=>ball(9,p,[.13,.13,.13]));
    const mito=(p,rot)=>{const holder=new T.Group();holder.position.set(...p);holder.rotation.z=rot;groups.get(10).add(holder);const m=ball(10,[0,0,0],[.58,.27,.24]);holder.add(m);const points=Array.from({length:17},(_,i)=>[-.44+i*.055,Math.sin(i*1.6)*.13,.21]);const cr=tube(10,points,.035,'#f6cd8c');holder.add(cr);};
    mito([1.2,-1.35,.37],-.45);mito([-2.3,1.05,.1],.5);mito([2.5,-.1,.1],.8);
    rod(11,[.04,-.9,.28],[.55,-.9,.28],.12);rod(11,[.28,-.65,.3],[.28,-.65,.78],.12);
    for(let j=0;j<5;j++)rod(11,[.05+j*.1,-1.02,.27],[.05+j*.1,-.79,.27],.014,'#edcc83');
    ball(12,[2.05,-.78,.35],[.35,.32,.3]);
    for(let j=0;j<9;j++)ball(12,[2.05+Math.cos(j*2.4)*.2,-.78+Math.sin(j*2.4)*.18,.6],[.03,.03,.025],'#a84e80');
    ball(13,[.4,1.73,.16],[.29,.26,.25]);ball(14,[-.35,-1.82,.16],[.46,.28,.2]);
    ball(15,[2.94,-.78,.08],[.22,.2,.16]);
    for(let j=0;j<5;j++)tube(16,[[-2.9+j*.13,-.3-j*.14,-.35],[-.3,-1.6+j*.2,-.3],[2.5,-.65+j*.4,-.25]],.025);
    for(let j=0;j<23;j++){const x=Math.sin(j*2.399)*2.8,y=Math.cos(j*2.399)*1.9;if((x+.85)**2+(y-.48)**2>1.4)ball(7,[x,y,.08],[.04,.04,.04]);}
    root.rotation.x=-.11;
  }
  if(kind==='muscle'){
    // Long axis = x. Open front half of membrane exposes myofibrils.
    const outer=add(10,new T.CylinderGeometry(1.38,1.38,5.8,64,1,true,Math.PI/2,Math.PI),[0,0,0]);outer.rotation.z=Math.PI/2;
    outer.material.side=T.DoubleSide;
    const membrane=add(9,new T.CylinderGeometry(1.28,1.28,5.85,64,1,true,Math.PI/2,Math.PI),[0,0,.015]);membrane.rotation.z=Math.PI/2;membrane.material.side=T.DoubleSide;
    const cover=add(10,new T.CylinderGeometry(1.38,1.38,5.8,64,1,true,-Math.PI/2,Math.PI),[0,0,0]);cover.rotation.z=Math.PI/2;cover.material.side=T.DoubleSide;covers.push(cover);
    for(const x of [-2.9,2.9]){const r=ring(9,[x,0,0],1.29,.055);r.rotation.y=Math.PI/2;}
    const centers=[[-.66,.1],[0,.1],[.65,.1],[-.35,.71],[.36,.71],[-.4,-.65],[.4,-.65]];
    centers.forEach(([y,z],i)=>{
      const end=i===4?3.4:2.76;
      rod(1,[-2.77,y,z],[end,y,z],.25);
      for(let x=-2.65;x<end;x+=.34){const r=ring(3,[x,y,z],.255,.026,[1,1,1],Math.round(x*10)%2?'#b96754':'#f3ba94');r.rotation.y=Math.PI/2;}
      // Thin and thick filaments extend from the end of one myofibril.
      if(i===4)for(let n=0;n<7;n++){const a=n*Math.PI*2/7;rod(2,[end,y+Math.cos(a)*.15,z+Math.sin(a)*.15],[end+.48,y+Math.cos(a)*.15,z+Math.sin(a)*.15],.022,n%2?'#a2554b':'#eeb296');}
    });
    for(const cy of [-.64,0,.64]){
      tube(4,Array.from({length:100},(_,i)=>{const t=i/99;return[-2.65+t*5.3,cy+Math.cos(t*Math.PI*16)*.32,.12+Math.sin(t*Math.PI*16)*.32];}),.035);
    }
    for(const x of [-1.15,1.05]){
      const t=ring(5,[x,0,0],1.12,.055);t.rotation.y=Math.PI/2;
      for(const dx of [-.13,.13]){const r=ring(6,[x+dx,0,0],1.1,.075);r.rotation.y=Math.PI/2;}
    }
    for(const x of [-1.8,.3,1.9]){ball(7,[x,-.93,.65],[.32,.14,.18]);tube(7,[[x-.2,-.94,.8],[x-.1,-.87,.82],[x,-1,.82],[x+.1,-.87,.82],[x+.2,-.94,.8]],.018,'#f2b395');}
    ball(8,[-1.8,1.14,.12],[.53,.16,.19]);ball(8,[1.75,1.14,.12],[.48,.16,.19]);
    tube(11,[[2.4,2.2,-.2],[1.7,1.95,.15],[.9,1.8,.35],[.5,1.25,.85]],.07,'#c49a40');
    for(let i=0;i<5;i++){const m=ball(11,[2.24-i*.26,2.12-i*.09,-.14+i*.12],[.17,.12,.12]);m.rotation.z=.3;}
    for(let j=0;j<4;j++){const x=.2+j*.25;tube(12,[[.5,1.25,.85],[x,1,.99],[x-.16,.8,1.03]],.045);ball(12,[x-.16,.8,1.03],[.12,.07,.09]);}
    root.rotation.y=-.32;root.rotation.z=-.12;
  }
  if(kind==='heart'){
    // Anatomical right is the viewer's left in the initial anterior view.
    const shape=new T.Shape();shape.moveTo(0,-2.5);shape.bezierCurveTo(-.6,-2.3,-2,-1.3,-2,0);shape.bezierCurveTo(-2.2,1.8,-.8,2.05,0,1.45);shape.bezierCurveTo(1,2.2,2.05,1.3,1.9,.15);shape.bezierCurveTo(1.9,-1.2,.8,-2.2,0,-2.5);
    const wall=add(13,new T.ExtrudeGeometry(shape,{depth:.58,bevelEnabled:true,bevelThickness:.16,bevelSize:.13,bevelSegments:4,steps:1,curveSegments:30}),[0,0,-.7]);
    const front=add(13,new T.ExtrudeGeometry(shape,{depth:.38,bevelEnabled:true,bevelThickness:.25,bevelSize:.12,bevelSegments:5,steps:1,curveSegments:30}),[0,0,.18]);covers.push(front);
    const chambers=[{id:1,p:[-1.04,.72,.11],s:[.65,.72,.58]},{id:3,p:[-.83,-.8,.1],s:[.72,1.05,.56]},{id:7,p:[.79,.84,.12],s:[.62,.64,.5]},{id:9,p:[.72,-.77,.12],s:[.62,1.2,.55]}];
    chambers.forEach(({id,p,s})=>{
      shell(id,p,s,metadata[id-1].color);
      // Recessed cavity surface sits just ahead of the continuous back wall.
      ball(id,[p[0],p[1],p[2]+.03],[s[0]*.94,s[1]*.95,.08]);
      ring(id,p,1,id===9?.13:.075,[s[0],s[1],.65],'#dfaaa0');
    });
    tube(13,[[.04,1.12,.26],[-.05,.2,.3],[.02,-.8,.3],[.13,-1.9,.22]],.13,'#d4a09a');
    tube(12,[[-1.25,2.6,-.15],[-1.32,1.8,-.05],[-1.14,1.12,.05]],.29);
    tube(12,[[-1.35,-2,-.15],[-1.45,-.8,-.13],[-1.24,.4,-.1]],.25);
    tube(5,[[-.75,-.1,.1],[-.3,.7,.65],[-.25,1.75,.8],[.15,2.03,.7],[1.8,2.03,.35]],.25);
    tube(5,[[.05,1.96,.63],[-.8,2.03,.7],[-1.98,1.78,.38]],.2);
    tube(11,[[.43,-.14,-.01],[.4,1.2,.15],[.55,2.45,.13],[1.15,2.65,-.2],[1.55,2.25,-.53],[1.5,.6,-.6]],.29);
    for(let i=0;i<3;i++)tube(11,[[.54+i*.3,2.46,-.1],[.51+i*.34,3.06,-.13]],.11);
    for(const y of [.8,1.24]){tube(6,[[1.91,y,-.05],[1.35,y,-.05],[.98,.9,-.02]],.15);tube(6,[[-1.9,y,-.55],[-.55,y,-.6],[.68,.94,-.4]],.13);}
    const valve=(id,cx,cy,count,r)=>{
      const rim=ring(id,[cx,cy,.39],r,.035,[1,.52,1]);
      for(let j=0;j<count;j++){const a=j*2*Math.PI/count;const m=ball(id,[cx+Math.cos(a)*r*.52,cy+Math.sin(a)*r*.27,.43],[r*.55,r*.28,.075]);m.rotation.z=a;}
    };
    valve(2,-.96,-.02,3,.46);valve(8,.78,.02,2,.42);valve(4,-.29,1.2,3,.19);valve(10,.43,1.48,3,.19);
    for(const [cx,id] of [[-.96,2],[.78,8]])for(let j=0;j<5;j++)tube(id,[[cx-.28+j*.14,-.05,.41],[cx+(j-2)*.055,-.7,.36]],.011,'#eee0c8');
    root.rotation.y=-.13;
  }
  root.updateMatrixWorld(true);
  const anchors=new Map();for(const [id,g]of groups){const box=new T.Box3().setFromObject(g);const c=box.getCenter(new T.Vector3());root.worldToLocal(c);anchors.set(id,c);}
  return {root,groups,covers,anchors};
}
