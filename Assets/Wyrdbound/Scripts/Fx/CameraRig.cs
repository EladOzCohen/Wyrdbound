using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Tilted perspective follow camera (see View3D): room clamping, look-ahead, trauma shake, kicks and
    /// zoom punches. Target and bounds are floor-plane (XY) coordinates, as in all gameplay code.
    /// </summary>
    [DefaultExecutionOrder(800)]
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public Vector2 Target;
        public Vector2 LookAhead;
        public Rect Bounds = new Rect(-20, -15, 40, 30);
        /// <summary>Half the height of the floor area in view, in world units.</summary>
        public float BaseSize = 6.6f;

        Vector2 pos;
        float trauma;
        Vector2 kick;
        float zoom;
        float seed;
        static readonly Plane Floor = new Plane(Vector3.back, 0f);

        float SinP => Mathf.Sin(View3D.Pitch * Mathf.Deg2Rad);
        float TanA => Mathf.Tan(View3D.Fov * 0.5f * Mathf.Deg2Rad);

        /// <summary>Half extents of the visible floor around the view centre.</summary>
        public float HalfH => Mathf.Max(1f, BaseSize - zoom);
        public float HalfW => HalfH * SinP * Cam.aspect;
        /// <summary>The floor point at the centre of the view.</summary>
        public Vector2 Center => pos;

        void Awake()
        {
            Cam = gameObject.AddComponent<Camera>();
            Cam.orthographic = false;
            Cam.fieldOfView = View3D.Fov;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = Color.black;
            Cam.nearClipPlane = 2f;
            Cam.farClipPlane = 200f;
            Cam.renderingPath = RenderingPath.DeferredShading;
            gameObject.AddComponent<AudioListener>();
            seed = Random.value * 100f;
            Apply(Vector2.zero, 0f);
        }

        public void Snap(Vector2 p)
        {
            Target = p;
            pos = Clamp(p);
            Apply(Vector2.zero, 0f);
        }

        public void Shake(float amount) => trauma = Mathf.Min(1f, trauma + amount);
        public void Kick(Vector2 dir, float amount) => kick += dir.Norm(Vector2.zero) * amount;
        public void Punch(float amount) => zoom = Mathf.Max(zoom, amount);

        Vector2 Clamp(Vector2 p)
        {
            float hh = HalfH, hw = HalfW;
            float margin = 1.5f;
            float x0 = Bounds.xMin - margin + hw, x1 = Bounds.xMax + margin - hw;
            float y0 = Bounds.yMin - margin + hh * 0.8f, y1 = Bounds.yMax + margin - hh * 0.8f;
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
            float shk = SaveSystem.Prefs != null ? SaveSystem.Prefs.shake : 1f;
            float sh = trauma * trauma * shk;
            float t = Time.unscaledTime * 26f;
            var off = new Vector2(Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(seed + 9f, t) - 0.5f) * sh * 1.1f;
            float rot = trauma * trauma * (Mathf.PerlinNoise(seed + 30f, Time.unscaledTime * 20f) - 0.5f) * 3f * shk;
            Apply(off + kick * shk, rot);
        }

        void Apply(Vector2 off, float roll)
        {
            float p = View3D.Pitch * Mathf.Deg2Rad;
            var fwd = new Vector3(0f, Mathf.Cos(p), Mathf.Sin(p));
            float dist = HalfH * SinP / TanA;
            var look = new Vector3(pos.x + off.x, pos.y + off.y, 0f);
            var rot = Quaternion.LookRotation(fwd, Vector3.back) * Quaternion.Euler(0f, 0f, roll);
            transform.SetPositionAndRotation(look - fwd * dist, rot);
            View3D.CamRot = rot;
            View3D.CamPos = transform.position;
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            var ray = Cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            if (Floor.Raycast(ray, out float d))
            {
                var w = ray.GetPoint(d);
                return new Vector2(w.x, w.y);
            }
            return pos;
        }

        public Vector2 WorldToScreen(Vector2 world)
        {
            var s = Cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            return new Vector2(s.x, s.y);
        }
    }
}
