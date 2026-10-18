import * as T from 'three';
import {GLTFLoader} from './vendor/GLTFLoader.js';
import {parts} from './data.js';

export function prepareRefinedModel(kind,scene){
  const root=new T.Group(),groups=new Map(),covers=[],meshes=[];
  for(const p of parts[kind]){const g=new T.Group();g.userData.part=p;root.add(g);groups.set(p.id,g);}
  scene.updateMatrixWorld(true);
  scene.traverse(o=>{if(o.isMesh)meshes.push(o);});
  for(const mesh of meshes){
    let node=mesh;while(node&&!node.userData.partId)node=node.parent;
    const id=Number(node?.userData.partId),p=parts[kind].find(p=>p.id===id);
    if(!p)throw new Error('Missing educational part ID in '+kind+': '+mesh.name);
    const cover=node.userData.cover===true;
    mesh.userData.part=p;mesh.material=mesh.material.clone();mesh.material.side=T.DoubleSide;
    groups.get(id).attach(mesh);
    if(cover)covers.push(mesh);
  }
  if(!covers.length||[...groups.values()].some(g=>!g.children.length))throw new Error('Incomplete refined model: '+kind);
  root.updateMatrixWorld(true);
  const anchors=new Map();
  for(const [id,g]of groups){const c=new T.Box3().setFromObject(g).getCenter(new T.Vector3());root.worldToLocal(c);anchors.set(id,c);}
  return{root,groups,covers,anchors,refined:true};
}
export async function loadRefinedModel(kind){
  const response=await fetch(new URL('./assets/models/'+kind+'-hq.glb',import.meta.url));
  if(!response.ok)throw new Error('Model download failed: '+response.status);
  const gltf=await new GLTFLoader().parseAsync(await response.arrayBuffer(),'');
  return prepareRefinedModel(kind,gltf.scene);
}
