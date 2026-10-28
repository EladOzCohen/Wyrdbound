// Lit shader for every 3D surface in Wyrdbound: KayKit models, the chamber floor and its cliffs.
// Albedo = texture x vertex colour, optionally recoloured toward _Tint (keeps the texture's shading).
// Alpha-tested, double-sided surfaces (hair cards) via _Cutoff / _Cull.
// Adds a cool rim light (the silhouette read Hades gets from its painted outlines), an emissive
// term, and the same per-renderer hit flash the sprite shader has.
Shader "Wyrd/Lit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Tint ("Tint", Color) = (1,1,1,1)
        _TintAmt ("Tint Amount", Range(0,1)) = 0
        _Gloss ("Smoothness", Range(0,1)) = 0.15
        _RimColor ("Rim Color", Color) = (0.55,0.7,0.9,1)
        _Rim ("Rim", Range(0,2)) = 0.6
        _Emission ("Emission", Color) = (0,0,0,0)
        _Flash ("Flash", Range(0,1)) = 0
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _Dither ("Dither Fade", Range(0,1)) = 0
        _Cutoff ("Alpha Cutoff (0 = opaque)", Range(0,1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull [_Cull]

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.5

        sampler2D _MainTex;
        fixed4 _Color, _Tint, _RimColor, _Emission, _FlashColor;
        half _TintAmt, _Gloss, _Rim, _Flash, _Dither, _Cutoff;

        struct Input
        {
            float2 uv_MainTex;
            float4 vcol;
            float3 viewDir;
            float4 screenPos;
        };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vcol = v.color;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // screen-door fade so a pillar in front of Narfi never hides him
            if (_Dither > 0.001)
            {
                float2 sp = floor(IN.screenPos.xy / max(IN.screenPos.w, 1e-5) * _ScreenParams.xy);
                float4x4 bayer = float4x4(0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5) / 16.0;
                float d = bayer[(uint)sp.x % 4][(uint)sp.y % 4];
                clip(d + 0.03 - _Dither);
            }
            fixed4 t = tex2D(_MainTex, IN.uv_MainTex) * _Color * IN.vcol;
            // hair cards, brows and lashes are alpha-tested
            if (_Cutoff > 0.001) clip(t.a - _Cutoff);
            half lum = dot(t.rgb, half3(0.3, 0.55, 0.15));
            half3 tinted = lerp(t.rgb, lum * _Tint.rgb * 1.6, _TintAmt);
            half rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), 3.0) * _Rim;
            o.Albedo = lerp(tinted, _FlashColor.rgb, _Flash * 0.6);
            o.Metallic = 0;
            o.Smoothness = _Gloss;
            o.Emission = _Emission.rgb + _RimColor.rgb * rim * (0.35 + 0.65 * lum) + _FlashColor.rgb * _Flash * 1.4;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
