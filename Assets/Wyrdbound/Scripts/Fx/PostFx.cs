using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Bloom, colour grade, vignette and tonemapping on the main camera (built-in pipeline).</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class PostFx : MonoBehaviour
    {
        Material mat;
        readonly RenderTexture[] levels = new RenderTexture[6];
        public float Threshold = 1.25f, Knee = 0.6f, Intensity = 0.65f, Vignette = 1.5f, Saturation = 1.1f, Contrast = 1.05f, Exposure = 0.95f;
        public Color Tint = Color.white;

        void Awake()
        {
            var sh = Resources.Load<Shader>("Shaders/WyrdPost");
            if (sh != null && sh.isSupported) mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            else Debug.LogWarning("[Wyrdbound] Post shader unavailable; bloom disabled.");
            var cam = GetComponent<Camera>();
            cam.allowHDR = true;
            cam.allowMSAA = false;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null || !SaveSystem.Prefs.bloom)
            {
                Graphics.Blit(src, dst);
                return;
            }
            mat.SetFloat("_Threshold", Threshold);
            mat.SetFloat("_Knee", Knee);
            mat.SetFloat("_Intensity", Intensity);
            mat.SetFloat("_Vignette", Vignette);
            mat.SetFloat("_Saturation", Saturation);
            mat.SetFloat("_Contrast", Contrast);
            mat.SetFloat("_Exposure", Exposure);
            mat.SetColor("_Tint", Tint);

            var fmt = RenderTextureFormat.ARGBHalf;
            int w = src.width / 2, h = src.height / 2;
            int n = 0;
            levels[0] = RenderTexture.GetTemporary(w, h, 0, fmt);
            Graphics.Blit(src, levels[0], mat, 0);
            n = 1;
            for (; n < levels.Length; n++)
            {
                w /= 2; h /= 2;
                if (w < 8 || h < 8) break;
                levels[n] = RenderTexture.GetTemporary(w, h, 0, fmt);
                Graphics.Blit(levels[n - 1], levels[n], mat, 1);
            }
            for (int i = n - 2; i >= 0; i--)
            {
                Graphics.Blit(levels[i + 1], levels[i], mat, 2);
                RenderTexture.ReleaseTemporary(levels[i + 1]);
                levels[i + 1] = null;
            }
            mat.SetTexture("_BloomTex", levels[0]);
            Graphics.Blit(src, dst, mat, 3);
            RenderTexture.ReleaseTemporary(levels[0]);
            levels[0] = null;
        }
    }
}
