using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public enum RigAction { None, Windup, Strike, SlamWindup, SlamStrike, Cast, Shoot, Lunge, Dash, Throw }

    /// <summary>What the animator needs to know this frame.</summary>
    public struct RigInput
    {
        public float Speed, MaxSpeed;
        public RigAction Action;
        public float ActionT;
        public bool ArmOverride;
        public float AimLocal;
        public float Hurt;
        public bool Dead;
        public float DeadT;
        public float Spawn;
        public bool Underground;
    }

    /// <summary>
    /// A jointed cut-out character. Bones are a Transform hierarchy (one SpriteRenderer each);
    /// every pose is computed procedurally: gait cycles, breathing, attack anticipation and
    /// follow-through, recoil, collapse, and spring-simulated capes, hair, tails and tatters.
    /// </summary>
    public sealed class Rig : MonoBehaviour
    {
        public RigDef Def;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly Dictionary<string, Vector3> rest = new Dictionary<string, Vector3>();
        readonly List<SpriteRenderer> srs = new List<SpriteRenderer>();
        readonly List<int> orders = new List<int>();
        public SpriteRenderer Main;
        float phase, t, runS, swingVel;
        readonly float[] capeA = new float[6], capeV = new float[6];
        float hairA, hairV, tailA, tailV;
        float lastTorso;
        float jawOpen;

        public static Rig Build(Transform parent, string id)
        {
            var def = RigArt.Get(id);
            var go = new GameObject("rig_" + id);
            go.transform.SetParent(parent, false);
            var rig = go.AddComponent<Rig>();
            rig.Def = def;
            foreach (var p in def.Parts)
            {
                Transform tr;
                if (p.Parent == null) tr = go.transform;
                else
                {
                    var pg = new GameObject(p.Name);
                    tr = pg.transform;
                    tr.SetParent(rig.bones.TryGetValue(p.Parent, out var par) ? par : go.transform, false);
                    tr.localPosition = new Vector3(p.Pos.x, p.Pos.y, 0f);
                    tr.localScale = new Vector3(p.Scale, p.Scale, 1f);
                    if (p.Sprite != null)
                    {
                        var sr = pg.AddComponent<SpriteRenderer>();
                        sr.sprite = p.Sprite;
                        sr.sharedMaterial = Mats.Sprite;
                        rig.srs.Add(sr);
                        rig.orders.Add(p.Order);
                        if (p.Name == "torso" || p.Name == "body") rig.Main = sr;
                    }
                }
                rig.bones[p.Name] = tr;
                rig.rest[p.Name] = tr.localPosition;
            }
            if (rig.Main == null && rig.srs.Count > 0) rig.Main = rig.srs[0];
            rig.t = Random.value * 10f;
            return rig;
        }

        public Transform Bone(string name) => bones.TryGetValue(name, out var b) ? b : null;
        public IReadOnlyList<SpriteRenderer> Renderers => srs;

        public Vector2 HandWorld
        {
            get
            {
                var h = Bone("handN");
                var p = h != null ? h.position : transform.position;
                return new Vector2(p.x, p.y);
            }
        }

        public void SetOrder(int baseOrder)
        {
            for (int i = 0; i < srs.Count; i++) srs[i].sortingOrder = baseOrder + orders[i];
        }

        public void SetBlock(MaterialPropertyBlock mpb)
        {
            foreach (var sr in srs)
            {
                sr.SetPropertyBlock(mpb);
            }
        }

        public void SetColor(Color c)
        {
            foreach (var sr in srs) sr.color = c;
        }

        public void SetVisible(bool v)
        {
            foreach (var sr in srs) sr.enabled = v;
        }

        public void Afterimage(Color c, float life)
        {
            foreach (var sr in srs) Vfx.Afterimage(sr, c, life);
        }

        // ------------------------------------------------------------------ helpers

        void Rot(string bone, float deg)
        {
            if (bones.TryGetValue(bone, out var b)) b.localRotation = Quaternion.Euler(0f, 0f, deg);
        }

        void Off(string bone, float dx, float dy)
        {
            if (bones.TryGetValue(bone, out var b) && rest.TryGetValue(bone, out var r)) b.localPosition = new Vector3(r.x + dx, r.y + dy, 0f);
        }

        static float Spring(ref float a, ref float v, float target, float k, float damp, float dt)
        {
            v += (target - a) * k * dt;
            v *= Mathf.Exp(-damp * dt);
            a += v * dt;
            return a;
        }

        // ------------------------------------------------------------------ animate

        public void Animate(in RigInput inp, float dt)
        {
            if (dt <= 0f) return;
            t += dt;
            float run = Mathf.Clamp01(inp.Speed / Mathf.Max(0.5f, inp.MaxSpeed));
            runS = U.Damp(runS, run, 9f, dt);
            if (runS > 0.04f) phase += dt * Mathf.Lerp(5f, 12f, runS);
            else phase = U.Damp(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, 6f, dt);

            switch (Def.Kind)
            {
                case RigKind.Humanoid: Humanoid(inp, dt); break;
                case RigKind.Quad: Quad(inp, dt); break;
                case RigKind.Bird: Bird(inp, dt); break;
                case RigKind.Serpent: Serpent(inp, dt); break;
                case RigKind.Ghost: Ghost(inp, dt); break;
                case RigKind.Blob: Blob(inp, dt); break;
                case RigKind.Bell: Bell(inp, dt); break;
                default: Static(inp, dt); break;
            }

            // spawn: emerge from the ground
            float sp = Mathf.Clamp01(inp.Spawn);
            float sy = sp >= 1f ? 1f : U.OutBack(sp);
            transform.localScale = new Vector3(1f + (1f - sp) * 0.25f, Mathf.Max(0.02f, sy), 1f);
        }

        void Humanoid(in RigInput inp, float dt)
        {
            var d = Def;
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
            float breath = Mathf.Sin(t * 2.1f);
            float r = runS;
            float k = Mathf.Clamp01(inp.ActionT);

            float torso = -7f * r - d.Hunch * 24f + breath * 1.3f * (1f - r);
            float head = d.Hunch * 16f + breath * 1.4f + Mathf.Sin(t * 0.7f) * 2f * (1f - r);
            float hipY = (Mathf.Abs(c) * 0.05f - 0.035f) * r * d.Scale;
            float hipX = Mathf.Sin(t * 0.9f) * 0.012f * (1f - r);
            float thF = 32f * s * r, thN = -32f * s * r;
            float shF = -50f * Mathf.Max(0f, c) * r, shN = -50f * Mathf.Max(0f, -c) * r;
            float uaF = -thF * 0.75f + Mathf.Sin(t * 1.7f) * 3f * (1f - r) + 4f;
            float uaN = -thN * 0.75f + Mathf.Sin(t * 1.7f + 1.2f) * 3f * (1f - r) - 2f;
            float faF = 14f + 22f * r, faN = 14f + 22f * r;
            float item = 0f;
            float skirt = Mathf.Sin(phase * 2f) * 4f * r + Mathf.Sin(t * 1.3f) * 1.5f;
            float wing = Mathf.Sin(t * 2.4f) * 8f;
            float rootRot = 0f, rootY = 0f;

            if (d.ArmsUp) { uaF = 168f + breath * 2f; uaN = 162f - breath * 2f; faF = faN = -14f; }

            switch (inp.Action)
            {
                case RigAction.Windup:
                    {
                        float e = U.OutCubic(k);
                        uaN = Mathf.Lerp(uaN, 215f, e); faN = Mathf.Lerp(faN, 40f, e);
                        uaF = Mathf.Lerp(uaF, 40f, e);
                        torso += 12f * e; head -= 6f * e; hipY -= 0.05f * e * d.Scale;
                        thN += 8f * e; thF -= 12f * e; shF -= 15f * e;
                        break;
                    }
                case RigAction.Strike:
                    {
                        float e = U.OutCubic(k * 2.5f);
                        uaN = Mathf.Lerp(215f, 55f, e); faN = Mathf.Lerp(40f, 4f, e);
                        uaF = Mathf.Lerp(40f, -40f, e);
                        torso -= 16f * e; head += 6f * e; hipY -= 0.05f * d.Scale;
                        thN += 26f * e; shN -= 10f * e; thF -= 20f * e; shF -= 25f * e;
                        item = 25f * e;
                        break;
                    }
                case RigAction.SlamWindup:
                    {
                        float e = U.OutCubic(k);
                        uaN = Mathf.Lerp(uaN, 190f, e); uaF = Mathf.Lerp(uaF, 200f, e);
                        faN = faF = Mathf.Lerp(faN, -20f, e);
                        torso += 14f * e; head += 10f * e; hipY += 0.03f * e * d.Scale;
                        break;
                    }
                case RigAction.SlamStrike:
                    {
                        float e = U.OutCubic(k * 3f);
                        uaN = Mathf.Lerp(190f, 25f, e); uaF = Mathf.Lerp(200f, 15f, e);
                        faN = faF = 10f;
                        torso -= 24f * e; head -= 8f * e; hipY -= 0.12f * e * d.Scale;
                        thN += 28f * e; thF -= 28f * e; shN -= 35f * e; shF -= 35f * e;
                        break;
                    }
                case RigAction.Cast:
                    {
                        float w = 0.5f + 0.5f * Mathf.Sin(t * 9f);
                        uaN = 150f + 12f * w; uaF = 140f - 10f * w; faN = faF = -25f;
                        torso += 8f; head += 14f; hipY += 0.02f * Mathf.Sin(t * 6f);
                        break;
                    }
                case RigAction.Shoot:
                    uaN = 92f; faN = 2f;
                    uaF = 100f; faF = 145f * (1f - k * 0.3f);
                    torso += 3f; head += 4f;
                    item = -90f;
                    break;
                case RigAction.Lunge:
                case RigAction.Dash:
                    torso -= 28f; head += 12f; hipY -= 0.05f * d.Scale;
                    thN = 38f; shN = -12f; thF = -42f; shF = -34f;
                    uaN = -55f; uaF = -65f; faN = faF = 20f;
                    wing = -35f;
                    break;
                case RigAction.Throw:
                    {
                        float a = k < 0.45f ? Mathf.Lerp(uaN, 225f, U.OutCubic(k / 0.45f)) : Mathf.Lerp(225f, 80f, U.OutCubic((k - 0.45f) / 0.55f));
                        uaN = a; faN = k < 0.45f ? 50f : 8f;
                        torso += k < 0.45f ? 10f : -14f;
                        break;
                    }
            }

            if (inp.ArmOverride)
            {
                float a = inp.AimLocal + 90f;
                float dA = Mathf.DeltaAngle(uaN, a);
                uaN += dA;
                faN = 8f;
                torso += Mathf.Clamp(Mathf.DeltaAngle(0f, inp.AimLocal) * 0.12f, -12f, 12f);
                uaF = Mathf.Lerp(uaF, -25f, 0.6f);
                item = 0f;
            }

            if (inp.Hurt > 0f)
            {
                float h = inp.Hurt;
                torso += 14f * h; head += 22f * h; uaN += 25f * h; uaF -= 25f * h;
                hipX += Mathf.Sin(t * 70f) * 0.03f * h;
            }

            if (inp.Dead)
            {
                float e = U.OutCubic(inp.DeadT / 0.45f);
                rootRot = 82f * e; rootY = -0.05f * e;
                torso += 18f * e; head += 30f * e;
                uaN = Mathf.Lerp(uaN, 160f, e); uaF = Mathf.Lerp(uaF, 140f, e);
                thN += 45f * e; thF += 30f * e; shN -= 75f * e; shF -= 60f * e;
            }

            if (inp.Spawn < 1f)
            {
                float e = 1f - Mathf.Clamp01(inp.Spawn);
                uaN = Mathf.Lerp(uaN, 175f, e); uaF = Mathf.Lerp(uaF, 165f, e);
                torso -= 20f * e;
            }

            Off("hips", hipX, hipY);
            Rot("torso", torso);
            Rot("head", head - torso * 0.35f);
            Rot("thighF", thF); Rot("shinF", shF);
            Rot("thighN", thN); Rot("shinN", shN);
            Rot("upperF", uaF); Rot("foreF", faF);
            Rot("upperN", uaN); Rot("foreN", faN);
            Rot("item", item);
            Rot("skirt", skirt + (inp.Action == RigAction.Lunge || inp.Action == RigAction.Dash ? 12f : 0f));
            Rot("pelvis", -thF * 0.08f);
            Rot("wingF", wing - 6f); Rot("wingN", wing * 1.2f + 4f);
            transform.localRotation = Quaternion.Euler(0f, 0f, rootRot);
            transform.localPosition = new Vector3(0f, rootY, 0f);

            // cape and hair: springs that trail behind motion and react to torso swings
            float torsoVel = (torso - lastTorso) / dt;
            lastTorso = torso;
            if (d.CapeSegs > 0)
            {
                float dash = inp.Action == RigAction.Dash || inp.Action == RigAction.Lunge ? 1f : 0f;
                for (int i = 0; i < d.CapeSegs; i++)
                {
                    float target = (i == 0 ? -(r * 30f + dash * 40f) - torso * 0.6f : -(r * 12f + dash * 18f))
                                   + Mathf.Sin(t * (2.6f + i) + i * 0.9f) * (2.5f + 4f * r) + inp.Hurt * 10f;
                    capeV[i] -= torsoVel * 0.02f;
                    Spring(ref capeA[i], ref capeV[i], target, 70f, 9f, dt);
                    Rot("cape" + i, capeA[i]);
                }
            }
            if (d.Hair)
            {
                Spring(ref hairA, ref hairV, -r * 20f + Mathf.Sin(t * 2.3f) * 3f - torso * 0.4f, 60f, 8f, dt);
                Rot("hair", hairA);
            }
        }

        void Quad(in RigInput inp, float dt)
        {
            var d = Def;
            float r = runS;
            float k = Mathf.Clamp01(inp.ActionT);
            float pA = phase, pB = phase + Mathf.PI;
            float body = Mathf.Sin(phase * 2f) * 1.5f * r + Mathf.Sin(t * 2f) * 0.8f;
            float bodyY = (Mathf.Abs(Mathf.Cos(phase)) * 0.06f - 0.03f) * r * d.Scale;
            float head = Mathf.Sin(phase * 2f) * 4f * r + Mathf.Sin(t * 1.6f) * 2f;
            float upFF = 30f * Mathf.Sin(pB) * r, upFN = 30f * Mathf.Sin(pA) * r;
            float upBF = 30f * Mathf.Sin(pA) * r, upBN = 30f * Mathf.Sin(pB) * r;
            float loFF = 40f * Mathf.Max(0f, Mathf.Cos(pB)) * r, loFN = 40f * Mathf.Max(0f, Mathf.Cos(pA)) * r;
            float loBF = -45f * Mathf.Max(0f, Mathf.Cos(pA)) * r, loBN = -45f * Mathf.Max(0f, Mathf.Cos(pB)) * r;
            float jaw = 0f;
            float rootRot = 0f, rootY = 0f;

            switch (inp.Action)
            {
                case RigAction.Windup:
                case RigAction.SlamWindup:
                case RigAction.Cast:
                    {
                        float e = U.OutCubic(k);
                        body += 7f * e; bodyY -= 0.08f * e * d.Scale; head -= 10f * e; jaw = 12f * e;
                        upBF -= 15f * e; upBN -= 15f * e; loBF -= 25f * e; loBN -= 25f * e;
                        if (inp.Action == RigAction.Cast) { head = 25f; jaw = 35f; }
                        break;
                    }
                case RigAction.Strike:
                case RigAction.Lunge:
                case RigAction.Dash:
                case RigAction.SlamStrike:
                    body -= 9f; head += 6f; jaw = 38f;
                    upFF = upFN = 40f; loFF = loFN = 10f; upBF = upBN = -35f; loBF = loBN = -20f;
                    break;
            }
            if (inp.Hurt > 0f) { body += 8f * inp.Hurt; head += 12f * inp.Hurt; }
            if (inp.Dead)
            {
                float e = U.OutCubic(inp.DeadT / 0.45f);
                rootRot = -12f * e; rootY = -0.12f * e * d.Scale;
                upFF = upFN = upBF = upBN = 55f * e; loFF = loFN = loBF = loBN = -90f * e; head = -20f * e; jaw = 20f * e;
            }

            jawOpen = U.Damp(jawOpen, jaw, 18f, dt);
            Off("body", 0f, bodyY);
            Rot("body", body);
            Rot("head", head - body * 0.5f);
            Rot("jaw", -jawOpen);
            Rot("upfF", upFF); Rot("lofF", loFF);
            Rot("upfN", upFN); Rot("lofN", loFN);
            Rot("upbF", upBF); Rot("lobF", loBF);
            Rot("upbN", upBN); Rot("lobN", loBN);
            Spring(ref tailA, ref tailV, 12f * r + Mathf.Sin(t * 4.5f) * 14f - body * 0.8f, 50f, 7f, dt);
            Rot("tail0", tailA);
            Rot("tail1", tailA * 0.7f + Mathf.Sin(t * 4.5f - 1f) * 10f);
            transform.localRotation = Quaternion.Euler(0f, 0f, rootRot);
            transform.localPosition = new Vector3(0f, rootY, 0f);
        }

        void Bird(in RigInput inp, float dt)
        {
            float flap = Mathf.Sin(t * 15f);
            float wF = 25f + flap * 45f, wN = 15f + Mathf.Sin(t * 15f + 0.4f) * 50f;
            float body = Mathf.Sin(t * 3f) * 4f;
            float y = Mathf.Sin(t * 15f) * 0.04f;
            if (inp.Action == RigAction.Lunge || inp.Action == RigAction.Strike || inp.Action == RigAction.Dash) { wF = -50f; wN = -60f; body = -18f; }
            if (inp.Action == RigAction.Windup) { wF = 80f + flap * 10f; wN = 70f; body = 12f; }
            if (inp.Dead) { float e = inp.DeadT / 0.45f; body = -120f * e; y = -0.5f * e; wF = -20f; wN = 90f; }
            Rot("wingF", wF); Rot("wingN", wN);
            Rot("body", body);
            Off("body", 0f, y);
        }

        void Serpent(in RigInput inp, float dt)
        {
            var d = Def;
            float k = Mathf.Clamp01(inp.ActionT);
            float amp = 0.06f + 0.07f * runS + 0.02f * Mathf.Sin(t * 1.3f);
            float spacing = d.SegSpacing;
            float headPush = 0f;
            float lift = 0f;
            switch (inp.Action)
            {
                case RigAction.Windup:
                case RigAction.Cast:
                case RigAction.SlamWindup:
                    amp = Mathf.Lerp(amp, 0.16f, k); spacing *= Mathf.Lerp(1f, 0.7f, k); lift = 0.12f * k; break;
                case RigAction.Lunge:
                case RigAction.Strike:
                case RigAction.SlamStrike:
                    amp = 0.01f; spacing *= 1.15f; headPush = 0.2f; break;
            }
            float waveSpeed = 3f + 9f * runS;
            float ph = t * waveSpeed;
            int n = d.SerpentSegs;
            Vector2 prev = new Vector2(0.25f + headPush, Mathf.Sin(ph) * amp + lift);
            Off("head", prev.x, prev.y);
            float hy2 = Mathf.Sin(ph - 0.8f) * amp + lift * 0.6f;
            float slope = Mathf.Atan2(prev.y - hy2, spacing) * Mathf.Rad2Deg;
            Rot("head", slope + (inp.Action == RigAction.Windup ? 18f * k : 0f));
            for (int i = 0; i < n; i++)
            {
                float x = 0.18f - i * spacing;
                float y = Mathf.Sin(ph - (i + 1) * 0.8f) * amp * (1f - i / (float)(n + 2)) + lift * Mathf.Max(0f, 1f - i * 0.25f);
                if (inp.Dead) y -= Mathf.Clamp01(inp.DeadT / 0.45f) * 0.05f;
                Off("seg" + i, x, y);
            }
            if (inp.Hurt > 0f) Off("head", prev.x - 0.08f * inp.Hurt, prev.y + 0.05f * inp.Hurt);
        }

        void Ghost(in RigInput inp, float dt)
        {
            var d = Def;
            float bob = Mathf.Sin(t * 2.4f) * 0.07f;
            float lean = -runS * 10f + Mathf.Sin(t * 1.3f) * 3f;
            float armF = -15f + Mathf.Sin(t * 2f) * 12f, armN = 10f + Mathf.Sin(t * 2f + 1.5f) * 12f;
            if (inp.Action == RigAction.Windup || inp.Action == RigAction.Cast || inp.Action == RigAction.Shoot)
            {
                float k = Mathf.Clamp01(inp.ActionT);
                armF = Mathf.Lerp(armF, 150f, k); armN = Mathf.Lerp(armN, 130f, k); lean += 10f * k; bob += 0.08f * k;
            }
            if (inp.Hurt > 0f) lean += 18f * inp.Hurt;
            if (inp.Dead) { float e = inp.DeadT / 0.45f; bob -= 0.3f * e; lean += 40f * e; }
            Off("body", 0f, bob);
            Rot("body", lean);
            Rot("armF", armF); Rot("armN", armN);
            for (int i = 0; i < d.TatterCount; i++) Rot("tatter" + i, Mathf.Sin(t * 4f + i * 1.3f) * 16f + runS * 25f);
        }

        void Blob(in RigInput inp, float dt)
        {
            float pulse = 1f + Mathf.Sin(t * 3.2f) * 0.035f;
            float waddle = Mathf.Sin(phase) * 7f * runS;
            if (inp.Action == RigAction.Windup || inp.Action == RigAction.SlamWindup)
            {
                float k = Mathf.Clamp01(inp.ActionT);
                pulse = 1f + 0.35f * k + Mathf.Sin(t * 40f) * 0.04f * k;
            }
            var b = Bone("body");
            if (b != null) b.localScale = new Vector3(pulse, 1f + (pulse - 1f) * 0.7f, 1f);
            Rot("body", waddle);
            Rot("head", Mathf.Sin(t * 2.2f) * 6f - waddle);
            Rot("legF", Mathf.Sin(phase) * 25f * runS);
            Rot("legN", -Mathf.Sin(phase) * 25f * runS);
        }

        void Bell(in RigInput inp, float dt)
        {
            float a = Mathf.Sin(t * 1.8f) * 4f;
            if (inp.Action == RigAction.Windup || inp.Action == RigAction.Cast) a = Mathf.Sin(t * 22f) * 18f * Mathf.Clamp01(inp.ActionT);
            if (inp.Hurt > 0f) a += Mathf.Sin(t * 40f) * 12f * inp.Hurt;
            Rot("bell", a);
        }

        void Static(in RigInput inp, float dt)
        {
            float breath = 1f + Mathf.Sin(t * 2f) * 0.012f;
            var b = Bone("body");
            if (b != null) b.localScale = new Vector3(1f, breath, 1f);
            Rot("body", inp.Hurt * 8f + (inp.Dead ? 70f * Mathf.Clamp01(inp.DeadT / 0.45f) : 0f));
        }
    }
}
