const fs=require('node:fs'),path=require('node:path'),zlib=require('node:zlib'),assert=require('node:assert/strict');
const base=process.argv[2]; const chunks={};
for(const name of ['Alveoli-WebGL.data','Alveoli-WebGL.wasm']){
 const source=fs.readFileSync(path.join(base,'Build',name));const zipped=zlib.gzipSync(source,{level:9});assert.deepEqual(zlib.gunzipSync(zipped),source);
 const size=512*1024;chunks[name]=Math.ceil(zipped.length/size);
 for(let i=0;i<chunks[name];i++)fs.writeFileSync(path.join(base,'Build',name+'.gz.'+String(i).padStart(2,'0')),zipped.subarray(i*size,(i+1)*size));
 fs.unlinkSync(path.join(base,'Build',name));console.log(name+': '+source.length+' bytes -> '+zipped.length+' compressed bytes, '+chunks[name]+' parts');
}
const index=path.join(base,'index.html');let html=fs.readFileSync(index,'utf8');
const helper=`      // shortcut: segmented uploads avoid the current upload limit; use whole files when the connection allows it.\n      async function loadBuildFile(name, count, type) {\n        var parts = await Promise.all(Array.from({length: count}, async (_, i) => {\n          for (var attempt = 0; attempt < 3; attempt++) {\n            try {\n              var response = await fetch(buildUrl + '/' + name + '.gz.' + String(i).padStart(2, '0'));\n              if (!response.ok) throw new Error('실행 파일을 불러오지 못했습니다: ' + response.status);\n              return await response.arrayBuffer();\n            } catch (error) { if (attempt === 2) throw error; }\n          }\n        }));\n        var stream = new Blob(parts).stream().pipeThrough(new DecompressionStream('gzip'));\n        return URL.createObjectURL(new Blob([await new Response(stream).arrayBuffer()], {type: type}));\n      }\n\n`;
html=html.replace('      var script = document.createElement("script");',helper+'      var script = document.createElement("script");');
html=html.replace('      script.onload = () => {',`      script.onload = async () => {\n        try {\n          [config.dataUrl, config.codeUrl] = await Promise.all([\n            loadBuildFile('Alveoli-WebGL.data', ${chunks['Alveoli-WebGL.data']}, 'application/octet-stream'),\n            loadBuildFile('Alveoli-WebGL.wasm', ${chunks['Alveoli-WebGL.wasm']}, 'application/wasm')\n          ]);\n        } catch (error) {\n          unityShowBanner(error.message, 'error');\n          return;\n        }`);
fs.writeFileSync(index,html); console.log('PASS: exact compression round-trip and segmented loader generated');
