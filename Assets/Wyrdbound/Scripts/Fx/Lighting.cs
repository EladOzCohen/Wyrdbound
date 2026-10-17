using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// 2D lighting. Light sprites live on a dedicated layer; a second camera renders them additively over
    /// the realm's ambient colour into a low-resolution HDR buffer, which every lit sprite multiplies in.
    /// </summary>
    public static class Lights
    {
        public const int Layer = 30;

        /// <summary>A light that follows a transform.</summary>
        public static SpriteRenderer Attach(Transform parent, Vector2 offset, float radius, Color color, float intensity)
        {
            var sr = Mats.SR(parent, "light", SpriteLib.Light, 0, color.A(intensity), true);
            sr.gameObject.layer = Layer;
            sr.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            sr.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            LightSync.Attach(sr, 1.2f + Mathf.Max(0f, offset.y));
            return sr;
        }

        /// <summary>A static light placed in the current chamber.</summary>
        public static SpriteRenderer At(Vector2 pos, float radius, Color color, float intensity)
        {
            var sr = Attach(World.Root, Vector2.zero, radius, color, intensity);
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            return sr;
        }

        public static void ToLayer(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.layer = Layer;
            LightSync.Attach(sr);
        }
    }

    /// <summary>Owns the light camera and its buffer. Sits on the main camera.</summary>
    public sealed class LightingRig : MonoBehaviour
    {
        public static LightingRig I;
        public Color Ambient = new Color(0.45f, 0.45f, 0.5f);
        Camera main, lightCam;
        RenderTexture rt;
        int w, h;
        public bool On = true;

        void Awake()
        {
            I = this;
            main = GetComponent<Camera>();
            main.cullingMask &= ~(1 << Lights.Layer);
            var go = new GameObject("LightCamera");
            go.transform.SetParent(transform, false);
            lightCam = go.AddComponent<Camera>();
            lightCam.orthographic = main.orthographic;
            lightCam.fieldOfView = main.fieldOfView;
            lightCam.cullingMask = 1 << Lights.Layer;
            lightCam.clearFlags = CameraClearFlags.SolidColor;
            lightCam.depth = main.depth - 1f;
            lightCam.nearClipPlane = main.nearClipPlane;
            lightCam.farClipPlane = main.farClipPlane;
            lightCam.allowHDR = true;
            lightCam.allowMSAA = false;
        }

        void Resize()
        {
            int nw = Mathf.Max(64, Screen.width / 3), nh = Mathf.Max(64, Screen.height / 3);
            if (rt != null && nw == w && nh == h) return;
            if (rt != null) { lightCam.targetTexture = null; rt.Release(); Destroy(rt); }
            w = nw; h = nh;
            rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBHalf) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "WyrdLight" };
            rt.Create();
            lightCam.targetTexture = rt;
            Shader.SetGlobalTexture("_WyrdLightTex", rt);
        }

        void LateUpdate()
        {
            bool on = On && SaveSystem.Prefs.lighting && (Game.I == null || Game.I.Mode == GameMode.Hub || Game.I.Mode == GameMode.Run);
            lightCam.enabled = on;
            Shader.SetGlobalFloat("_WyrdLightOn", on ? 1f : 0f);
            Shader.SetGlobalFloat("_WyrdLightFlip", 0f);
            if (!on) return;
            Resize();
            lightCam.orthographic = main.orthographic;
            lightCam.orthographicSize = main.orthographicSize;
            lightCam.fieldOfView = main.fieldOfView;
            lightCam.nearClipPlane = main.nearClipPlane;
            lightCam.farClipPlane = main.farClipPlane;
            lightCam.aspect = main.aspect;
            lightCam.backgroundColor = Ambient;
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
        }
    }
}
