Shader "Heart/LivingTissue"
{
 Properties {
  _BaseColor("Tissue",Color)=(.48,.12,.13,1)
  _Roughness("Roughness",Range(.05,1))=.2
  _CoatMask("Wet coat",Range(0,1))=.8
  _Subsurface("Subsurface scattering approximation",Range(0,1))=.45
  _Cuttable("Section enabled",Float)=1
  _Deform("Heartbeat deformation",Float)=0
  _Fiber("Muscle fiber detail",Float)=1
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor; float _Roughness,_CoatMask,_Subsurface,_Cuttable,_Deform,_Fiber;
   CBUFFER_END
   float _SectionEnabled,_SectionDepth,_AtrialContraction,_VentricularContraction;
   struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float3 local:TEXCOORD2;};
   float hash3(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
   float tissueNoise(float3 p){
    float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
    float x0=lerp(hash3(a),hash3(a+float3(1,0,0)),f.x),x1=lerp(hash3(a+float3(0,1,0)),hash3(a+float3(1,1,0)),f.x);
    float x2=lerp(hash3(a+float3(0,0,1)),hash3(a+float3(1,0,1)),f.x),x3=lerp(hash3(a+float3(0,1,1)),hash3(a+float3(1,1,1)),f.x);
    return lerp(lerp(x0,x1,f.y),lerp(x2,x3,f.y),f.z);
   }
   Varyings vert(Attributes a){
    Varyings o;float3 p=a.positionOS.xyz;o.local=p;
    float atrium=smoothstep(.05,.45,p.y);
    float squeeze=lerp(_VentricularContraction*.09,_AtrialContraction*.055,atrium)*_Deform;
    p.xz*=1-squeeze;p.y=lerp(p.y,p.y*.965,squeeze*8);
    o.world=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);return o;
   }
   half4 frag(Varyings i, bool front:SV_IsFrontFace):SV_Target {
    if(_Cuttable>0&&_SectionEnabled>.5)clip(i.local.z-_SectionDepth);
    float3 n=normalize(i.normal)*(front?1:-1),v=normalize(GetWorldSpaceViewDir(i.world));
    float grain=tissueNoise(i.local*210),coarse=tissueNoise(i.local*24);
    n=normalize(n+_Fiber*.07*float3(grain-.5,tissueNoise(i.local*210+17)-.5,tissueNoise(i.local*210+43)-.5));
    float variation=1+_Fiber*(.05*(grain-.5)+.14*(coarse-.5));
    float3 color=_BaseColor.rgb*variation;
    Light light=GetMainLight();float3 l=normalize(light.direction),h=normalize(l+v);
    float wrap=saturate((dot(n,l)+.38)/1.38);
    float back=pow(saturate(dot(v,-l+n*.3)),3)*_Subsurface;
    float3 diffuse=color*(.22+wrap*light.color*.90+back*float3(.7,.13,.08));
    float3 fill=normalize(float3(-.8,.3,-.9));diffuse+=color*saturate(dot(n,fill))*.33;
    float power=lerp(160,12,_Roughness);
    float spec=pow(saturate(dot(n,h)),power)*_CoatMask*.50;
    float fillSpec=pow(saturate(dot(n,normalize(v+fill))),power)*.18*_CoatMask;
    float rim=pow(1-saturate(dot(n,v)),3)*.08;
    return half4(diffuse+(spec+fillSpec)*float3(1,.86,.79)+rim*color,1);
   }
   ENDHLSL
  }
 }
}
