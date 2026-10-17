using UnityEngine;
using UnityEngine.Rendering;

namespace Wyrdbound
{
    /// <summary>
    /// Realm-wide 3D lighting: the ambient the dead world is bathed in, and one high, cold "moon" that casts
    /// the long soft shadows grounding every model. Point lights (torches, souls, spells) come from LightSync.
    /// </summary>
    public static class Atmosphere
    {
        static Light moon;

        public static void Init()
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 60f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.pixelLightCount = 8;
            QualitySettings.antiAliasing = 0;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = 0.2f;

            if (moon == null)
            {
                var go = new GameObject("Moon");
                Object.DontDestroyOnLoad(go);
                moon = go.AddComponent<Light>();
                moon.type = LightType.Directional;
                moon.shadows = LightShadows.Soft;
                moon.shadowStrength = 0.7f;
                moon.shadowBias = 0.04f;
                moon.shadowNormalBias = 0.3f;
                // shining down (+Z) from the upper left, a little toward the camera so faces catch it
                go.transform.rotation = Quaternion.LookRotation(new Vector3(0.45f, -0.25f, 0.86f), Vector3.back);
            }
        }

        public static void Apply(Biome b)
        {
            Init();
            RenderSettings.ambientLight = b.Ambient * 0.85f;
            moon.color = Color.Lerp(b.Fog, Color.white, 0.55f);
            moon.intensity = b.Index == 4 ? 0.25f : 0.42f;
        }
    }
}
