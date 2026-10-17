using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// The 3D view of a 2D game. All gameplay still happens on the XY plane (the floor), exactly as before;
    /// height above the floor is -Z, so the world is seen by a tilted perspective camera looking "north
    /// and down". Models stand up out of the plane, flat sprites lie on it as decals, and upright sprites
    /// (2D creatures, props without a model) turn to face the camera.
    /// </summary>
    public static class View3D
    {
        /// <summary>Camera pitch below the horizon, and vertical field of view.</summary>
        public const float Pitch = 52f, Fov = 24f;

        /// <summary>The camera's rotation, kept current by CameraRig (billboards copy it).</summary>
        public static Quaternion CamRot = Quaternion.LookRotation(new Vector3(0f, Mathf.Cos(Pitch * Mathf.Deg2Rad), Mathf.Sin(Pitch * Mathf.Deg2Rad)), Vector3.back);
        public static Vector3 CamPos;

        /// <summary>Ground point plus height (height is up, i.e. -Z).</summary>
        public static Vector3 At(Vector2 p, float height = 0f) => new Vector3(p.x, p.y, -height);

        /// <summary>The floor point that appears on screen where a point at this height does (for 2D logic).</summary>
        public static Vector2 Flatten(Vector3 w) => new Vector2(w.x, w.y - w.z / Mathf.Tan(Pitch * Mathf.Deg2Rad));

        /// <summary>Slide a point along its line of sight toward the camera (same pixel, nearer). shrink keeps its on-screen size.</summary>
        public static Vector3 TowardCam(Vector3 p, float d, out float shrink)
        {
            var to = CamPos - p;
            float dist = to.magnitude;
            d = Mathf.Min(d, dist * 0.6f);
            shrink = dist > 0.01f ? (dist - d) / dist : 1f;
            return p + to / Mathf.Max(0.01f, dist) * d;
        }

        /// <summary>Rotation that stands a Y-up model on the floor, facing a floor direction.</summary>
        public static Quaternion Face(Vector2 dir)
        {
            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.down;
            return Quaternion.LookRotation(new Vector3(dir.x, dir.y, 0f), Vector3.back);
        }

        /// <summary>Turn a sprite (and its children) to face the camera every frame.</summary>
        public static Billboard Stand(Transform t)
        {
            if (t == null) return null;
            var b = t.GetComponent<Billboard>();
            if (b == null) b = t.gameObject.AddComponent<Billboard>();
            t.rotation = CamRot;
            return b;
        }

        /// <summary>Put the upright parts of an object under one camera-facing child.</summary>
        public static Transform UprightChild(Transform parent, string name = "upright")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Stand(go.transform);
            return go.transform;
        }
    }

    /// <summary>Keeps a transform facing the camera (runs after the camera has moved).</summary>
    [DefaultExecutionOrder(900)]
    public sealed class Billboard : MonoBehaviour
    {
        void LateUpdate() => transform.rotation = View3D.CamRot;
    }

    /// <summary>
    /// Mirrors a 2D light sprite with a real point light, so 3D models and the floor are lit by the
    /// same torches, souls and spells that light the sprites.
    /// </summary>
    [DefaultExecutionOrder(950)]
    public sealed class LightSync : MonoBehaviour
    {
        public SpriteRenderer Src;
        public Light L;
        public float Height = 1.4f, Gain = 2.4f, RangeK = 0.62f;

        public static LightSync Attach(SpriteRenderer src, float height = 1.4f, bool shadows = false)
        {
            if (src == null) return null;
            var existing = src.GetComponentInChildren<LightSync>(true);
            if (existing != null) return existing;
            var go = new GameObject("pointLight");
            go.transform.SetParent(src.transform, false);
            var ls = go.AddComponent<LightSync>();
            ls.Src = src;
            ls.Height = height;
            ls.L = go.AddComponent<Light>();
            ls.L.type = LightType.Point;
            ls.L.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            ls.L.shadowStrength = 0.75f;
            ls.L.renderMode = LightRenderMode.ForcePixel;
            ls.Sync();
            return ls;
        }

        void LateUpdate() => Sync();

        public void Sync()
        {
            if (Src == null || L == null) return;
            var c = Src.color;
            bool on = Src.enabled && Src.gameObject.activeInHierarchy && Src.gameObject.layer == Lights.Layer && c.a > 0.01f && (SaveSystem.Prefs == null || SaveSystem.Prefs.lighting);
            L.enabled = on;
            if (!on) return;
            L.color = new Color(c.r, c.g, c.b, 1f);
            L.intensity = c.a * Gain;
            L.range = Mathf.Max(0.5f, Mathf.Abs(Src.transform.lossyScale.x) * RangeK);
            var p = Src.transform.position;
            transform.position = new Vector3(p.x, p.y, -Height);
            transform.rotation = Quaternion.identity;
        }
    }
}
