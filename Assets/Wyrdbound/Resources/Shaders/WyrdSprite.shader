// Premultiplied-alpha sprite shader with a per-renderer hit flash and 2D lighting.
// All procedural textures in Wyrdbound are generated premultiplied.
// Lighting: a light buffer (_WyrdLightTex, rendered by the light camera) is sampled in screen space
// and multiplied in. _WyrdLightOn (global) enables it; _Lit (per material) opts a material out.
Shader "Wyrd/Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Flash ("Flash", Range(0,1)) = 0
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _Lit ("Lit", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 screen : TEXCOORD1; };

            sampler2D _MainTex;
            sampler2D _WyrdLightTex;
            float _WyrdLightOn;
            float _WyrdLightFlip;
            float _Flash;
            fixed4 _FlashColor;
            float _Lit;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                float4 c;
                c.a = t.a * i.color.a;
                c.rgb = t.rgb * i.color.rgb * i.color.a;
                float lit = _WyrdLightOn * _Lit;
                if (lit > 0.001)
                {
                    float2 suv = i.screen.xy / max(i.screen.w, 1e-5);
                    if (_WyrdLightFlip > 0.5) suv.y = 1.0 - suv.y;
                    float3 L = tex2D(_WyrdLightTex, suv).rgb;
                    c.rgb *= lerp(float3(1, 1, 1), L, lit);
                }
                c.rgb = lerp(c.rgb, _FlashColor.rgb * c.a, _Flash);
                return c;
            }
            ENDCG
        }
    }
}
