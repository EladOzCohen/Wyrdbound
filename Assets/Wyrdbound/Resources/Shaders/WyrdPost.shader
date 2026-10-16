// Post-processing for Wyrdbound: dual-filter bloom, colour grade, vignette and filmic tonemap.
Shader "Hidden/Wyrd/Post"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }
    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    sampler2D _BloomTex;
    float _Threshold, _Knee, _Intensity, _Vignette, _Saturation, _Contrast, _Exposure;
    float4 _Tint;

    half3 Box4(float2 uv, float2 d)
    {
        float4 o = _MainTex_TexelSize.xyxy * float4(-d.x, -d.y, d.x, d.y);
        half3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }

    half3 Prefilter(half3 c)
    {
        half b = max(c.r, max(c.g, c.b));
        half soft = clamp(b - _Threshold + _Knee, 0, 2 * _Knee);
        soft = soft * soft / (4 * _Knee + 1e-4);
        half contrib = max(soft, b - _Threshold) / max(b, 1e-4);
        return c * contrib;
    }

    half4 fragPrefilter(v2f_img i) : SV_Target { return half4(Prefilter(Box4(i.uv, float2(1, 1))), 1); }
    half4 fragDown(v2f_img i) : SV_Target { return half4(Box4(i.uv, float2(1, 1)), 1); }
    half4 fragUp(v2f_img i) : SV_Target { return half4(Box4(i.uv, float2(0.5, 0.5)), 1); }

    half3 ACES(half3 x)
    {
        const half a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
        return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
    }

    half4 fragCombine(v2f_img i) : SV_Target
    {
        half3 c = tex2D(_MainTex, i.uv).rgb;
        half3 bloom = tex2D(_BloomTex, i.uv).rgb;
        c += bloom * _Intensity;
        c *= _Exposure * _Tint.rgb;
        half l = dot(c, half3(0.2126, 0.7152, 0.0722));
        c = lerp(half3(l, l, l), c, _Saturation);
        c = (c - 0.18) * _Contrast + 0.18;
        c = max(c, 0);
        c = ACES(c * 1.15);
        float2 v = i.uv - 0.5;
        half vig = saturate(1 - dot(v, v) * _Vignette);
        c *= vig;
        return half4(c, 1);
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass { CGPROGRAM
               #pragma vertex vert_img
               #pragma fragment fragPrefilter
               ENDCG }
        Pass { CGPROGRAM
               #pragma vertex vert_img
               #pragma fragment fragDown
               ENDCG }
        Pass { Blend One One
               CGPROGRAM
               #pragma vertex vert_img
               #pragma fragment fragUp
               ENDCG }
        Pass { CGPROGRAM
               #pragma vertex vert_img
               #pragma fragment fragCombine
               ENDCG }
    }
}
