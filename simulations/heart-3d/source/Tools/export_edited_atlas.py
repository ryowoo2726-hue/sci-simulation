"""Blender: --background ModelSources/AtlasHeartSource.blend --disable-autoexec
--python Tools/export_edited_atlas.py -- /path/to/Assets/Resources/Atlas/Heart.bytes
Keep the source model's original axes and meter coordinates when editing.
"""
import bpy
import struct
import sys
from pathlib import Path

destination=Path(sys.argv[sys.argv.index('--')+1])
objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
assert 20<=len(objects)<=100, 'Open the provided anatomy source scene first.'
deps=bpy.context.evaluated_depsgraph_get()
destination.parent.mkdir(parents=True,exist_ok=True)
with destination.open('wb') as stream:
    stream.write(struct.pack('<i',len(objects)))
    for obj in sorted(objects,key=lambda o:o.name):
        name=obj.name.removesuffix('.001').encode('utf-8')
        stream.write(struct.pack('<i',len(name)));stream.write(name)
        mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(deps),depsgraph=deps)
        mesh.transform(obj.matrix_world);mesh.calc_loop_triangles()
        stream.write(struct.pack('<i',len(mesh.vertices)))
        for vertex in mesh.vertices:
            p=vertex.co;stream.write(struct.pack('<fff',(p.x-.02)*25,(p.z-1.30)*25,(p.y+.03)*25))
        stream.write(struct.pack('<i',len(mesh.loop_triangles)*3))
        for triangle in mesh.loop_triangles:
            a,b,c=triangle.vertices;stream.write(struct.pack('<iii',a,c,b))
        bpy.data.meshes.remove(mesh)
print('EXPORTED',len(objects),'anatomy parts to',destination)
