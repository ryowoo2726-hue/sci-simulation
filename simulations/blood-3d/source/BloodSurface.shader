Shader "Blood/SoftCell"
{
    Properties
    {
        _Color ("세포 색상", Color) = (0.52,0.012,0.018,1)
        _Scatter ("역광 산란 근사", Range(0,1)) = 0.32
        _Smoothness ("매끄러움", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma surface surf Blood fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        half _Scatter, _Smoothness;
        struct Input { float3 worldPos; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            float n = sin(IN.worldPos.x * 23.0) * sin(IN.worldPos.y * 19.0) * sin(IN.worldPos.z * 21.0);
            o.Albedo = _Color.rgb * (0.98 + 0.02 * n);
            o.Specular = _Smoothness;
            o.Gloss = .15;
            o.Alpha = 1;
        }
        half4 LightingBlood(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half diffuse = saturate(dot(s.Normal, lightDir));
            half back = saturate(-dot(s.Normal, lightDir));
            half forwardScatter = pow(saturate(dot(viewDir, -lightDir)), 3) * back * _Scatter;
            half3 h = normalize(lightDir + viewDir);
            half specular = pow(saturate(dot(s.Normal,h)), lerp(8,64,s.Specular)) * s.Gloss;
            half3 result = (s.Albedo * (diffuse + forwardScatter) + specular) * _LightColor0.rgb * atten;
            return half4(result, s.Alpha);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
