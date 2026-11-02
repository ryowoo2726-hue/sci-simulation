Shader "Heart/AnatomyGhost" {
 Properties { _BaseColor("Color",Color)=(.55,.70,.75,.09) }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass {ZWrite Off Cull Back Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _BaseColor;
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};struct V{float4 positionCS:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
 V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);return o;}
 half4 frag(V i):SV_Target {float rim=pow(1-saturate(abs(dot(normalize(i.normal),normalize(GetWorldSpaceViewDir(i.world))))),2);return half4(_BaseColor.rgb,_BaseColor.a*(.35+rim*2));}
 ENDHLSL }
 }
}
