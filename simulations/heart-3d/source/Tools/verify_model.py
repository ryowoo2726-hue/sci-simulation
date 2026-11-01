"""Validate the delivered atlas binary and exported mesh; standard library only."""
from pathlib import Path
from math import isfinite
import hashlib
import struct

project=Path(__file__).resolve().parents[1]
asset=project/'Assets/Resources/Atlas/Heart.bytes'
data=asset.read_bytes();offset=0

def integer():
    global offset
    value=struct.unpack_from('<i',data,offset)[0];offset+=4;return value

parts={}
for _ in range(integer()):
    length=integer();assert 0<length<256
    name=data[offset:offset+length].decode().removesuffix('.001');offset+=length
    count=integer();assert 0<count<2_000_000
    vertices=struct.unpack_from('<'+'f'*(count*3),data,offset);offset+=count*12
    assert all(isfinite(x) for x in vertices)
    indices=integer();assert indices%3==0 and indices>0
    triangles=struct.unpack_from('<'+'i'*indices,data,offset);offset+=indices*4
    assert all(0<=i<count for i in triangles)
    assert name not in parts
    parts[name]=(count,indices//3)
assert offset==len(data) and len(parts)==33
for name in ('Right atrium','Right ventricle','Left atrium','Left ventricle'):
    assert parts[name][0]>5000
for name in ('Left coronary leaflet','Right coronary leaflet','Non-coronary leaflet','Anterior semilunar leaflet of pulmonary valve','Left semilunar leaflet of pulmonary valve','Right semilunar leaflet of pulmonary valve'):
    assert name in parts
obj=project/'Assets/Models/HeartMyocardium.obj'
assert obj.exists() and obj.stat().st_size>1_000_000
print(f'ATLAS_PASS: {len(parts)} named structures, {sum(p[0] for p in parts.values()):,} vertices, {sum(p[1] for p in parts.values()):,} triangles.')
print('SHA256:',hashlib.sha256(data).hexdigest())
