using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Tiny mesh library for creatures built in code (spheres, cones, lathed shapes).</summary>
    public static class ProcMesh
    {
        static Mesh sphere, cone;

        public static Mesh Sphere
        {
            get
            {
                if (sphere != null) return sphere;
                const int lat = 10, lon = 16;
                var v = new List<Vector3>();
                var t = new List<int>();
                for (int i = 0; i <= lat; i++)
                {
                    float a = Mathf.PI * i / lat;
                    for (int j = 0; j <= lon; j++)
                    {
                        float b = Mathf.PI * 2f * j / lon;
                        v.Add(new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b)) * 0.5f);
                    }
                }
                for (int i = 0; i < lat; i++)
                    for (int j = 0; j < lon; j++)
                    {
                        int p = i * (lon + 1) + j, q = p + lon + 1;
                        t.Add(p); t.Add(p + 1); t.Add(q);
                        t.Add(p + 1); t.Add(q + 1); t.Add(q);
                    }
                sphere = Build("sphere", v, t, false);
                return sphere;
            }
        }

        /// <summary>A cone of height 1 pointing up +Y from a unit-diameter base.</summary>
        public static Mesh Cone
        {
            get
            {
                if (cone != null) return cone;
                var prof = new[] { new Vector2(0.5f, 0f), new Vector2(0.001f, 1f) };
                cone = Lathe(prof, 10, "cone");
                return cone;
            }
        }

        /// <summary>Spin a (radius, height) profile around +Y.</summary>
        public static Mesh Lathe(Vector2[] profile, int n, string name)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < profile.Length; i++)
                for (int j = 0; j <= n; j++)
                {
                    float b = Mathf.PI * 2f * j / n;
                    v.Add(new Vector3(Mathf.Cos(b) * profile[i].x, profile[i].y, Mathf.Sin(b) * profile[i].x));
                }
            for (int i = 0; i < profile.Length - 1; i++)
                for (int j = 0; j < n; j++)
                {
                    int p = i * (n + 1) + j, q = p + n + 1;
                    t.Add(p); t.Add(q); t.Add(p + 1);
                    t.Add(p + 1); t.Add(q); t.Add(q + 1);
                }
            // wind outward whichever way the profile runs (top-down profiles need the opposite order)
            bool down = profile[profile.Length - 1].y < profile[0].y;
            return Build(name, v, t, down);
        }

        static Mesh Build(string name, List<Vector3> v, List<int> t, bool flip)
        {
            var m = new Mesh { name = name };
            m.SetVertices(v);
            if (flip) for (int i = 0; i < t.Count; i += 3) { int k = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = k; }
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return mr;
        }

        public static Material Mat(Color body, Color rim, float rimAmt, float gloss = 0.25f)
        {
            var m = Mats.NewLit(null, "proc");
            m.SetColor("_Color", body);
            m.SetColor("_RimColor", rim);
            m.SetFloat("_Rim", rimAmt);
            m.SetFloat("_Gloss", gloss);
            return m;
        }

        public static Material Glow(Color c)
        {
            var m = Mats.NewLit(null, "procGlow");
            m.SetColor("_Color", Color.black);
            m.SetColor("_Emission", c * 2.5f);
            m.SetFloat("_Rim", 0f);
            return m;
        }
    }

    /// <summary>A creature built in code and posed every frame from RigInput (see Model3D).</summary>
    public abstract class ProcBody
    {
        public readonly List<Renderer> Renderers = new List<Renderer>();
        protected Transform root;
        protected ModelSpec spec;
        protected float t;

        public static ProcBody Create(string kind, Transform root, ModelSpec spec)
        {
            ProcBody b;
            switch (kind)
            {
                case "bell": b = new BellBody(); break;
                case "dragon": b = new DragonHeadBody(); break;
                default: b = new SerpentBody(); break;
            }
            b.root = root;
            b.spec = spec;
            b.Build();
            return b;
        }

        protected abstract void Build();

        /// <summary>Pose for this frame. ground = the entity's floor position; sink = how far the corpse has sunk.</summary>
        public abstract void Tick(in RigInput r, Vector2 facing, Vector2 ground, float sink, float dt);

        protected MeshRenderer Add(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var mr = ProcMesh.Part(parent, name, mesh, mat, pos, scale);
            Renderers.Add(mr);
            return mr;
        }
    }

    /// <summary>Serpent-spawn and Níð-larvae: a head and a tapering body that slithers along its own trail.</summary>
    public sealed class SerpentBody : ProcBody
    {
        readonly List<Transform> segs = new List<Transform>();
        readonly List<Vector2> trail = new List<Vector2>();
        Transform head, jaw;
        float r0, spacing;
        int count;

        protected override void Build()
        {
            bool larva = spec.Proc == "larva";
            count = larva ? 6 : 9;
            r0 = spec.Height * (larva ? 0.3f : 0.2f);
            spacing = r0 * (larva ? 1.05f : 1.25f);
            var skin = ProcMesh.Mat(spec.Tint, spec.Rim, spec.RimAmt, larva ? 0.55f : 0.35f);
            var belly = ProcMesh.Mat(Color.Lerp(spec.Tint, Color.white, 0.35f), spec.Rim, spec.RimAmt * 0.5f, 0.3f);
            for (int i = 0; i < count; i++)
            {
                var s = new GameObject("seg" + i).transform;
                s.SetParent(root, false);
                float k = 1f - i / (float)count;
                float rr = r0 * Mathf.Lerp(0.45f, 1f, k);
                Add(s, "body", ProcMesh.Sphere, i % 2 == 0 ? skin : belly, Vector3.zero, new Vector3(rr * 2.1f, rr * 1.8f, rr * 2.4f));
                if (!larva && i % 2 == 1) Add(s, "spine", ProcMesh.Cone, skin, new Vector3(0f, rr * 0.75f, 0f), new Vector3(rr * 0.5f, rr * 0.9f, rr * 0.5f));
                segs.Add(s);
            }
            head = new GameObject("head").transform;
            head.SetParent(root, false);
            Add(head, "skull", ProcMesh.Sphere, skin, new Vector3(0f, 0f, r0 * 0.3f), new Vector3(r0 * 2.2f, r0 * 1.6f, r0 * 2.8f));
            jaw = new GameObject("jaw").transform;
            jaw.SetParent(head, false);
            jaw.localPosition = new Vector3(0f, -r0 * 0.35f, r0 * 0.1f);
            Add(jaw, "jaw", ProcMesh.Sphere, belly, new Vector3(0f, 0f, r0 * 0.7f), new Vector3(r0 * 1.7f, r0 * 0.6f, r0 * 2.2f));
            var eye = ProcMesh.Glow(spec.Eyes);
            Add(head, "eyeL", ProcMesh.Sphere, eye, new Vector3(-r0 * 0.55f, r0 * 0.45f, r0 * 1.0f), Vector3.one * r0 * 0.42f);
            Add(head, "eyeR", ProcMesh.Sphere, eye, new Vector3(r0 * 0.55f, r0 * 0.45f, r0 * 1.0f), Vector3.one * r0 * 0.42f);
        }

        Vector2 Along(float dist)
        {
            if (trail.Count == 0) return Vector2.zero;
            float acc = 0f;
            for (int i = trail.Count - 1; i > 0; i--)
            {
                float d = Vector2.Distance(trail[i], trail[i - 1]);
                if (acc + d >= dist) return Vector2.Lerp(trail[i], trail[i - 1], (dist - acc) / Mathf.Max(1e-4f, d));
                acc += d;
            }
            // trail too short: extend straight back
            var back = trail.Count > 1 ? (trail[0] - trail[1]).normalized : Vector2.down;
            if (back.sqrMagnitude < 0.5f) back = Vector2.down;
            return trail[0] + back * (dist - acc);
        }

        public override void Tick(in RigInput r, Vector2 facing, Vector2 ground, float sink, float dt)
        {
            t += dt;
            var f = facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector2.down;
            if (trail.Count == 0)
            {
                for (int i = 0; i < 24; i++) trail.Add(ground - f * (24 - i) * spacing * 0.5f);
                trail.Add(ground);
            }
            if (Vector2.Distance(trail[trail.Count - 1], ground) > spacing * 0.3f)
            {
                trail.Add(ground);
                if (trail.Count > 64) trail.RemoveAt(0);
            }
            else trail[trail.Count - 1] = ground;

            float moving = Mathf.Clamp01(r.Speed / Mathf.Max(0.5f, r.MaxSpeed));
            float k = Mathf.Clamp01(r.ActionT);
            float rear = 0f, lunge = 0f, open = 0.1f + 0.05f * Mathf.Sin(t * 3f);
            switch (r.Action)
            {
                case RigAction.Windup:
                case RigAction.SlamWindup:
                case RigAction.Cast:
                case RigAction.Shoot:
                    rear = U.OutCubic(k) * r0 * 2.2f; open = 0.6f * k; break;
                case RigAction.Strike:
                case RigAction.Lunge:
                case RigAction.SlamStrike:
                    lunge = Mathf.Sin(Mathf.Clamp01(k * 1.5f) * Mathf.PI) * r0 * 2.5f; open = 0.9f; break;
            }
            float emerge = (1f - Mathf.Clamp01(r.Spawn)) * r0 * 4f;
            bool dead = r.Dead;

            var side = new Vector2(-f.y, f.x);
            for (int i = 0; i < segs.Count; i++)
            {
                float d = (i + 1) * spacing;
                var p = Along(d);
                float wig = Mathf.Sin(t * (4f + moving * 6f) - i * 0.9f) * r0 * (0.25f + moving * 0.35f) * (i / (float)segs.Count + 0.3f);
                p += side * wig;
                float rr = r0 * Mathf.Lerp(0.45f, 1f, 1f - i / (float)segs.Count);
                float h = dead ? rr * 0.5f : rr * 0.85f + Mathf.Max(0f, rear * (1f - i / 3f)) * (i < 3 ? 0.5f : 0f);
                var next = Along(d - spacing);
                segs[i].SetPositionAndRotation(new Vector3(p.x, p.y, -h + emerge + sink), View3D.Face(next - p));
            }
            float hh = dead ? r0 * 0.5f : r0 * 1.1f + rear + Mathf.Sin(t * 2.4f) * r0 * 0.1f;
            var hp = ground + f * lunge;
            var look = Quaternion.AngleAxis(dead ? 0f : -rear * 25f, Vector3.Cross(new Vector3(f.x, f.y, 0f), Vector3.back));
            head.SetPositionAndRotation(new Vector3(hp.x, hp.y, -hh + emerge + sink), look * View3D.Face(f));
            jaw.localRotation = Quaternion.Euler(Mathf.Clamp01(open) * 35f, 0f, 0f);
        }
    }

    /// <summary>The Wailing Bell: a hovering bronze bell that swings to toll its curse.</summary>
    public sealed class BellBody : ProcBody
    {
        Transform pivot, clapper;
        float swing, swingV;

        protected override void Build()
        {
            float H = spec.Height;
            var prof = new[]
            {
                new Vector2(0.001f, 1f), new Vector2(0.14f, 0.99f), new Vector2(0.22f, 0.93f), new Vector2(0.26f, 0.8f),
                new Vector2(0.28f, 0.62f), new Vector2(0.31f, 0.45f), new Vector2(0.37f, 0.28f), new Vector2(0.45f, 0.14f),
                new Vector2(0.52f, 0.05f), new Vector2(0.53f, 0f), new Vector2(0.47f, 0.02f)
            };
            var mesh = ProcMesh.Lathe(prof, 28, "bell");
            var bronze = ProcMesh.Mat(spec.Tint, spec.Rim, spec.RimAmt, 0.6f);
            pivot = new GameObject("pivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(0f, H * 1.55f, 0f);
            Add(pivot, "bell", mesh, bronze, new Vector3(0f, -H, 0f), new Vector3(H, H, H));
            Add(pivot, "crown", ProcMesh.Sphere, bronze, new Vector3(0f, 0.02f * H, 0f), new Vector3(0.2f, 0.16f, 0.2f) * H);
            var band = ProcMesh.Glow(spec.Eyes);
            Add(pivot, "rune", ProcMesh.Lathe(new[] { new Vector2(0.385f, 0.26f), new Vector2(0.4f, 0.22f) }, 28, "band"), band, new Vector3(0f, -H, 0f), new Vector3(H, H, H));
            clapper = new GameObject("clapper").transform;
            clapper.SetParent(pivot, false);
            Add(clapper, "ball", ProcMesh.Sphere, bronze, new Vector3(0f, -H * 0.85f, 0f), Vector3.one * H * 0.16f);
        }

        public override void Tick(in RigInput r, Vector2 facing, Vector2 ground, float sink, float dt)
        {
            t += dt;
            float target = Mathf.Sin(t * 1.3f) * 6f;
            float k = Mathf.Clamp01(r.ActionT);
            if (r.Action != RigAction.None) target = Mathf.Sin(t * 9f) * (12f + 22f * k);
            if (r.Dead) target = 70f;
            swingV += (target - swing) * 40f * dt;
            swingV *= Mathf.Exp(-4f * dt);
            swing += swingV * dt;
            float bob = Mathf.Sin(t * 1.7f) * 0.08f * spec.Height;
            pivot.localPosition = new Vector3(0f, spec.Height * 1.55f + bob - (r.Dead ? Mathf.Min(1.2f, r.DeadT) * spec.Height : 0f) - sink, 0f);
            pivot.localRotation = Quaternion.Euler(swing, 0f, swing * 0.3f);
            clapper.localRotation = Quaternion.Euler(-swing * 0.6f, 0f, 0f);
        }
    }

    /// <summary>Níðhöggr's head: a horned skull with a working jaw, rearing on its neck above the root-rot.</summary>
    public sealed class DragonHeadBody : ProcBody
    {
        Transform head, jaw;

        protected override void Build()
        {
            float s = spec.Height;
            var scale = ProcMesh.Mat(spec.Tint, spec.Rim, spec.RimAmt, 0.4f);
            var bone = ProcMesh.Mat(U.Hex("c8b48a"), spec.Rim, spec.RimAmt * 0.5f, 0.2f);
            var eye = ProcMesh.Glow(spec.Eyes);
            Add(root, "neck", ProcMesh.Sphere, scale, new Vector3(0f, s * 0.35f, -s * 0.25f), new Vector3(0.55f, 0.75f, 0.6f) * s);
            head = new GameObject("head").transform;
            head.SetParent(root, false);
            head.localPosition = new Vector3(0f, s * 0.7f, 0f);
            Add(head, "skull", ProcMesh.Sphere, scale, Vector3.zero, new Vector3(0.62f, 0.5f, 0.75f) * s);
            Add(head, "snout", ProcMesh.Sphere, scale, new Vector3(0f, -0.04f * s, 0.42f * s), new Vector3(0.42f, 0.3f, 0.6f) * s);
            Add(head, "eyeL", ProcMesh.Sphere, eye, new Vector3(-0.2f * s, 0.1f * s, 0.25f * s), Vector3.one * 0.09f * s);
            Add(head, "eyeR", ProcMesh.Sphere, eye, new Vector3(0.2f * s, 0.1f * s, 0.25f * s), Vector3.one * 0.09f * s);
            for (int i = -1; i <= 1; i += 2)
            {
                var horn = Add(head, "horn", ProcMesh.Cone, bone, new Vector3(0.2f * s * i, 0.2f * s, -0.15f * s), new Vector3(0.12f, 0.45f, 0.12f) * s);
                horn.transform.localRotation = Quaternion.Euler(-55f, 0f, -18f * i);
            }
            for (int i = 0; i < 3; i++)
            {
                var sp = Add(head, "crest", ProcMesh.Cone, bone, new Vector3(0f, 0.22f * s, (-0.1f - 0.14f * i) * s), new Vector3(0.08f, 0.22f, 0.08f) * s);
                sp.transform.localRotation = Quaternion.Euler(-30f, 0f, 0f);
            }
            jaw = new GameObject("jaw").transform;
            jaw.SetParent(head, false);
            jaw.localPosition = new Vector3(0f, -0.15f * s, 0.05f * s);
            Add(jaw, "jaw", ProcMesh.Sphere, scale, new Vector3(0f, 0f, 0.35f * s), new Vector3(0.38f, 0.16f, 0.62f) * s);
            for (int i = -1; i <= 1; i += 2)
            {
                var fang = Add(jaw, "fang", ProcMesh.Cone, bone, new Vector3(0.12f * s * i, 0.06f * s, 0.55f * s), new Vector3(0.05f, 0.14f, 0.05f) * s);
                fang.transform.localRotation = Quaternion.identity;
            }
        }

        public override void Tick(in RigInput r, Vector2 facing, Vector2 ground, float sink, float dt)
        {
            t += dt;
            float k = Mathf.Clamp01(r.ActionT);
            float rear = 0f, open = 0.15f + 0.08f * Mathf.Sin(t * 2f), dip = 0f;
            switch (r.Action)
            {
                case RigAction.Windup:
                case RigAction.SlamWindup: rear = U.OutCubic(k); open = 0.3f + 0.5f * k; break;
                case RigAction.Strike:
                case RigAction.Lunge:
                case RigAction.SlamStrike: dip = Mathf.Sin(Mathf.Clamp01(k * 1.5f) * Mathf.PI); open = 1f; break;
                case RigAction.Cast:
                case RigAction.Shoot: open = 0.8f + 0.2f * Mathf.Sin(t * 14f); rear = 0.4f; break;
            }
            float s = spec.Height;
            float emerge = (1f - Mathf.Clamp01(r.Spawn)) * s * 1.2f + (r.Underground ? s * 1.5f : 0f);
            head.localPosition = new Vector3(0f, s * (0.7f + 0.25f * rear - 0.3f * dip) + Mathf.Sin(t * 1.6f) * 0.05f * s - emerge - sink, s * (0.15f * dip - 0.1f * rear));
            head.localRotation = Quaternion.Euler(-20f * rear + 25f * dip + Mathf.Sin(t * 1.1f) * 4f, 0f, Mathf.Sin(t * 0.8f) * 5f);
            jaw.localRotation = Quaternion.Euler(Mathf.Clamp01(open) * 38f, 0f, 0f);
            if (r.Dead) head.localRotation = Quaternion.Euler(40f, 0f, 30f);
        }
    }
}
