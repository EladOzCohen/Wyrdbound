using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Follow camera with room clamping, look-ahead, trauma shake, kicks and zoom punches.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public Vector2 Target;
        public Vector2 LookAhead;
        public Rect Bounds = new Rect(-20, -15, 40, 30);
        public float BaseSize = 7.2f;

        Vector2 pos;
        float trauma;
        Vector2 kick;
        float zoom;
        float seed;

        void Awake()
        {
            Cam = gameObject.AddComponent<Camera>();
            Cam.orthographic = true;
            Cam.orthographicSize = BaseSize;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Color.black;
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 100f;
            transform.position = new Vector3(0, 0, -10);
            gameObject.AddComponent<AudioListener>();
            seed = Random.value * 100f;
        }

        public void Snap(Vector2 p)
        {
            Target = p;
            pos = Clamp(p);
            Apply(Vector2.zero);
        }

        public void Shake(float amount) => trauma = Mathf.Min(1f, trauma + amount);
        public void Kick(Vector2 dir, float amount) => kick += dir.Norm(Vector2.zero) * amount;
        public void Punch(float amount) => zoom = Mathf.Max(zoom, amount);

        Vector2 Clamp(Vector2 p)
        {
            float hh = Cam.orthographicSize, hw = hh * Cam.aspect;
            float margin = 1.5f;
            float x0 = Bounds.xMin - margin + hw, x1 = Bounds.xMax + margin - hw;
            float y0 = Bounds.yMin - margin + hh, y1 = Bounds.yMax + margin - hh;
            p.x = x0 > x1 ? Bounds.center.x : Mathf.Clamp(p.x, x0, x1);
            p.y = y0 > y1 ? Bounds.center.y : Mathf.Clamp(p.y, y0, y1);
            return p;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            var want = Clamp(Target + LookAhead);
            pos = U.Damp(pos, want, 7f, dt);
            trauma = Mathf.Max(0f, trauma - dt * 1.6f);
            kick = U.Damp(kick, Vector2.zero, 14f, dt);
            zoom = U.Damp(zoom, 0f, 6f, dt);
            float sh = trauma * trauma * (SaveSystem.Prefs != null ? SaveSystem.Prefs.shake : 1f);
            float t = Time.unscaledTime * 26f;
            var off = new Vector2(Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(seed + 9f, t) - 0.5f) * sh * 1.1f;
            Apply(off + kick * (SaveSystem.Prefs != null ? SaveSystem.Prefs.shake : 1f));
            Cam.orthographicSize = BaseSize - zoom;
        }

        void Apply(Vector2 off)
        {
            transform.position = new Vector3(pos.x + off.x, pos.y + off.y, -10f);
            float rot = trauma * trauma * (Mathf.PerlinNoise(seed + 30f, Time.unscaledTime * 20f) - 0.5f) * 3f;
            transform.rotation = Quaternion.Euler(0, 0, rot * (SaveSystem.Prefs != null ? SaveSystem.Prefs.shake : 1f));
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            var w = Cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            return new Vector2(w.x, w.y);
        }

        public Vector2 WorldToScreen(Vector2 world)
        {
            var s = Cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(s.x, s.y);
        }
    }
}
