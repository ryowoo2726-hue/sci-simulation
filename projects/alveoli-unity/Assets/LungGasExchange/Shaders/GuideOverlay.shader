Shader "Heart/GuideOverlay" {
 Properties { _Color("Color",Color)=(.2,1,.8,1) }
 SubShader {Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent"}
 Pass {ZTest Always ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _Color;
 struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct V{float4 positionCS:SV_POSITION;float3 normal:TEXCOORD0;};
 V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.normal=TransformObjectToWorldNormal(a.normalOS);return o;}
 half4 frag(V i):SV_Target{return half4(_Color.rgb*(.65+.35*abs(dot(normalize(i.normal),normalize(float3(-.4,.7,-.8))))),_Color.a);}
 ENDHLSL }
 }
}
