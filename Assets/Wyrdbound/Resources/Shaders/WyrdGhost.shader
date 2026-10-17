// Additive silhouette for 3D afterimages (dash trails, phantom echoes). Brightest at the rim.
Shader "Wyrd/Ghost"
{
    Properties
    {
        _Color ("Color", Color) = (0.5,1,0.8,0.5)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Back
        ZWrite Off
        ZTest LEqual
        Blend One One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float rim : TEXCOORD0; };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 n = normalize(UnityObjectToWorldNormal(v.normal));
                float3 vd = normalize(WorldSpaceViewDir(v.vertex));
                o.rim = 1.0 - saturate(dot(n, vd));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float k = 0.25 + 0.75 * i.rim * i.rim;
                return fixed4(_Color.rgb * _Color.a * k, 0);
            }
            ENDCG
        }
    }
}
