using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Pooled sprite particles, ground telegraphs and floating combat text.
    /// Lives for the whole session; Clear() wipes everything between chambers.
    /// </summary>
    public sealed class VfxSystem : MonoBehaviour
    {
        public static VfxSystem I;

        struct Part
        {
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float t, life, s0, s1, rot, rotSpd, drag, aspect, grav;
            public Color c0, c1;
            public bool active, unscaled;
        }

        public sealed class Tele
        {
            internal SpriteRenderer outline, fill;
            internal float t, dur;
            internal int kind;
            internal Vector2 pos, dir;
            internal float a, b;
            internal Color col;
            public bool Alive;
            public float Progress => dur > 0f ? Mathf.Clamp01(t / dur) : 1f;

            public void Move(Vector2 p, Vector2 d)
            {
                pos = p;
                if (d.sqrMagnitude > 1e-6f) dir = d.normalized;
            }

            public void Kill() { t = dur + 1f; }
        }

        sealed class FText
        {
            public TextMesh tm, shadow;
            public Vector2 pos, vel;
            public float t, life, size;
            public Color col;
            public bool active;
        }

        Part[] parts;
        readonly List<Tele> teles = new List<Tele>();
        readonly List<FText> texts = new List<FText>();
        int cursor;

        void Awake()
        {
            I = this;
            parts = new Part[1400];
            for (int i = 0; i < parts.Length; i++)
            {
                var sr = Mats.SR(transform, "p", null, Sort.Fx);
                sr.enabled = false;
                parts[i].sr = sr;
            }
            for (int i = 0; i < 48; i++)
            {
                var ft = new FText();
                ft.shadow = MakeText("ts", Sort.Text);
                ft.tm = MakeText("t", Sort.Text + 1);
                texts.Add(ft);
            }
        }

        TextMesh MakeText(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Fonts.Main;
            tm.fontSize = 64;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Fonts.Main.material;
            mr.sortingOrder = order;
            go.SetActive(false);
            return tm;
        }

        public void Clear()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].active = false;
                parts[i].sr.enabled = false;
            }
            foreach (var t in teles) { Destroy(t.outline.gameObject); Destroy(t.fill.gameObject); }
            teles.Clear();
            foreach (var f in texts) { f.active = false; f.tm.gameObject.SetActive(false); f.shadow.gameObject.SetActive(false); }
        }

        public void Spawn(Sprite s, Vector2 pos, Vector2 vel, float life, float s0, float s1, Color c0, Color c1, bool add,
            float rot = 0f, float rotSpd = 0f, float drag = 0f, int order = Sort.Fx, float aspect = 1f, float grav = 0f, bool flip = false, bool unscaled = false)
        {
            if (s == null || life <= 0f) return;
            int idx = -1;
            for (int k = 0; k < parts.Length; k++)
            {
                int i = (cursor + k) % parts.Length;
                if (!parts[i].active) { idx = i; break; }
            }
            if (idx < 0) idx = cursor;
            cursor = (idx + 1) % parts.Length;
            ref Part p = ref parts[idx];
            p.active = true;
            p.pos = pos; p.vel = vel; p.t = 0f; p.life = life; p.s0 = s0; p.s1 = s1; p.c0 = c0; p.c1 = c1;
            p.rot = rot; p.rotSpd = rotSpd; p.drag = drag; p.aspect = aspect; p.grav = grav; p.unscaled = unscaled;
            p.sr.sprite = s;
            p.sr.sharedMaterial = add ? Mats.Additive : Mats.Sprite;
            p.sr.sortingOrder = order;
            p.sr.flipX = flip;
            p.sr.enabled = true;
            p.sr.color = c0;
            p.sr.transform.position = pos;
            p.sr.transform.rotation = Quaternion.Euler(0, 0, rot);
            p.sr.transform.localScale = new Vector3(s0, s0 * aspect, 1f);
        }

        public Tele Telegraph(int kind, Vector2 pos, Vector2 dir, float a, float b, float dur, Color c)
        {
            var t = new Tele { kind = kind, pos = pos, dir = dir.Norm(Vector2.right), a = a, b = b, dur = dur, col = c, Alive = true };
            Sprite outline, fill;
            switch (kind)
            {
                case 0: outline = SpriteLib.TeleCircle; fill = SpriteLib.Disc; break;
                case 1: outline = SpriteLib.TeleLine; fill = SpriteLib.TeleLine; break;
                default: outline = SpriteLib.Cone((int)b); fill = SpriteLib.Cone((int)b); break;
            }
            t.outline = Mats.SR(transform, "tele", outline, Sort.Telegraph, true);
            t.fill = Mats.SR(transform, "telefill", fill, Sort.Telegraph + 1, true);
            teles.Add(t);
            UpdateTele(t);
            return t;
        }

        void UpdateTele(Tele t)
        {
            float pr = t.Progress;
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 18f);
            float ang = t.dir.Ang();
            var ot = t.outline.transform;
            var ft = t.fill.transform;
            ot.position = t.pos; ft.position = t.pos;
            ot.rotation = ft.rotation = Quaternion.Euler(0, 0, t.kind == 0 ? 0f : ang);
            t.outline.color = t.col.A(0.55f * pulse);
            t.fill.color = t.col.A(0.22f + 0.35f * pr);
            switch (t.kind)
            {
                case 0:
                    ot.localScale = new Vector3(t.a * 2f, t.a * 2f, 1f);
                    ft.localScale = new Vector3(t.a * 2f * pr, t.a * 2f * pr, 1f);
                    break;
                case 1:
                    ot.localScale = new Vector3(t.a, t.b, 1f);
                    ft.localScale = new Vector3(t.a * pr, t.b, 1f);
                    break;
                default:
                    ot.localScale = new Vector3(t.a, t.a, 1f);
                    ft.localScale = new Vector3(t.a * pr, t.a * pr, 1f);
                    break;
            }
        }

        public void Number(Vector2 pos, string text, Color c, float size)
        {
            FText f = null;
            float oldest = -1f;
            foreach (var x in texts)
            {
                if (!x.active) { f = x; break; }
                if (x.t > oldest) { oldest = x.t; f = x; }
            }
            if (f == null) return;
            f.active = true;
            f.t = 0f;
            f.life = 0.85f;
            f.pos = pos + new Vector2(Random.Range(-0.3f, 0.3f), 0f);
            f.vel = new Vector2(Random.Range(-0.6f, 0.6f), 3.2f);
            f.size = size;
            f.col = c;
            f.tm.text = text;
            f.shadow.text = text;
            f.tm.gameObject.SetActive(true);
            f.shadow.gameObject.SetActive(true);
        }

        void Update()
        {
            float dt = Time.deltaTime, udt = Time.unscaledDeltaTime;
            for (int i = 0; i < parts.Length; i++)
            {
                ref Part p = ref parts[i];
                if (!p.active) continue;
                float d = p.unscaled ? udt : dt;
                p.t += d;
                if (p.t >= p.life) { p.active = false; p.sr.enabled = false; continue; }
                float k = p.t / p.life;
                if (p.drag > 0f) p.vel *= Mathf.Exp(-p.drag * d);
                p.vel.y -= p.grav * d;
                p.pos += p.vel * d;
                p.rot += p.rotSpd * d;
                float s = Mathf.Lerp(p.s0, p.s1, U.OutCubic(k));
                var tr = p.sr.transform;
                tr.position = p.pos;
                tr.rotation = Quaternion.Euler(0, 0, p.rot);
                tr.localScale = new Vector3(s, s * p.aspect, 1f);
                p.sr.color = Color.Lerp(p.c0, p.c1, k);
            }

            for (int i = teles.Count - 1; i >= 0; i--)
            {
                var t = teles[i];
                t.t += dt;
                if (t.t > t.dur + 0.06f)
                {
                    t.Alive = false;
                    Destroy(t.outline.gameObject);
                    Destroy(t.fill.gameObject);
                    teles.RemoveAt(i);
                    continue;
                }
                UpdateTele(t);
            }

            foreach (var f in texts)
            {
                if (!f.active) continue;
                f.t += udt;
                if (f.t >= f.life) { f.active = false; f.tm.gameObject.SetActive(false); f.shadow.gameObject.SetActive(false); continue; }
                f.vel.y -= 7f * udt;
                f.pos += f.vel * udt;
                float k = f.t / f.life;
                float pop = k < 0.12f ? Mathf.Lerp(1.6f, 1f, k / 0.12f) : 1f;
                float s = f.size * pop;
                float a = k > 0.65f ? 1f - (k - 0.65f) / 0.35f : 1f;
                f.tm.transform.position = new Vector3(f.pos.x, f.pos.y, 0f);
                f.shadow.transform.position = new Vector3(f.pos.x + 0.05f, f.pos.y - 0.05f, 0f);
                f.tm.transform.localScale = f.shadow.transform.localScale = new Vector3(s, s, 1f);
                f.tm.color = f.col.A(a);
                f.shadow.color = new Color(0, 0, 0, a * 0.85f);
            }
        }
    }

    /// <summary>Convenience API over the VFX system.</summary>
    public static class Vfx
    {
        static VfxSystem S => VfxSystem.I;

        public static void Spawn(Sprite s, Vector2 pos, Vector2 vel, float life, float s0, float s1, Color c0, Color c1, bool add,
            float rot = 0f, float rotSpd = 0f, float drag = 0f, int order = Sort.Fx, float aspect = 1f, float grav = 0f)
        {
            if (S != null) S.Spawn(s, pos, vel, life, s0, s1, c0, c1, add, rot, rotSpd, drag, order, aspect, grav);
        }

        public static void Burst(Vector2 pos, int n, Color c, float speed = 7f, float life = 0.35f, float size = 0.35f)
        {
            if (S == null) return;
            for (int i = 0; i < n; i++)
            {
                var d = Rng.Fx.OnCircle();
                float sp = speed * Rng.Fx.Range(0.35f, 1f);
                S.Spawn(SpriteLib.Spark, pos, d * sp, life * Rng.Fx.Range(0.6f, 1.2f), size, size * 0.2f, c, c.A(0f), true, d.Ang(), 0f, 4f, Sort.Fx, 0.35f);
            }
        }

        public static void Directional(Vector2 pos, Vector2 dir, int n, Color c, float spread = 35f, float speed = 9f, float size = 0.4f)
        {
            if (S == null) return;
            for (int i = 0; i < n; i++)
            {
                var d = dir.Norm(Vector2.right).Rot(Rng.Fx.Range(-spread, spread));
                float sp = speed * Rng.Fx.Range(0.4f, 1f);
                S.Spawn(SpriteLib.Spark, pos, d * sp, Rng.Fx.Range(0.15f, 0.35f), size, size * 0.2f, c, c.A(0f), true, d.Ang(), 0f, 5f, Sort.Fx, 0.3f);
            }
        }

        public static void Motes(Vector2 pos, int n, Color c, float radius = 0.6f, float rise = 1.5f, float life = 0.9f)
        {
            if (S == null) return;
            for (int i = 0; i < n; i++)
            {
                var o = Rng.Fx.InCircle() * radius;
                S.Spawn(SpriteLib.Mote, pos + o, new Vector2(Rng.Fx.Range(-0.4f, 0.4f), rise * Rng.Fx.Range(0.5f, 1.2f)), life * Rng.Fx.Range(0.6f, 1.2f), 0.35f, 0.05f, c, c.A(0f), true);
            }
        }

        public static void Slash(Vector2 pos, float ang, float radius, int arc, Color c, float life = 0.16f, bool flip = false)
        {
            if (S == null) return;
            var sp = SpriteLib.Slash(arc);
            float aspect = flip ? -1f : 1f;
            S.Spawn(sp, pos, Vector2.zero, life, radius * 0.92f, radius * 1.08f, c, c.A(0f), true, ang, 0f, 0f, Sort.FxHigh, aspect);
            S.Spawn(sp, pos, Vector2.zero, life * 0.8f, radius * 0.88f, radius * 1.0f, Color.white.A(0.75f), Color.white.A(0f), true, ang, 0f, 0f, Sort.FxHigh + 1, aspect);
        }

        public static void Ring(Vector2 pos, float radius, Color c, float life = 0.35f, float from = 0.2f)
        {
            if (S == null) return;
            S.Spawn(SpriteLib.Ring, pos, Vector2.zero, life, radius * 2f * from, radius * 2f, c, c.A(0f), true, 0f, 0f, 0f, Sort.Fx);
        }

        public static void Flash(Vector2 pos, float radius, Color c, float life = 0.16f)
        {
            if (S == null) return;
            S.Spawn(SpriteLib.Glow, pos, Vector2.zero, life, radius * 2.4f, radius * 1.4f, c, c.A(0f), true);
        }

        public static void Light(Vector2 pos, float radius, Color c, float life)
        {
            if (S == null) return;
            S.Spawn(SpriteLib.Light, pos, Vector2.zero, life, radius * 2f, radius * 2f, c, c.A(0f), true, 0f, 0f, 0f, Sort.FloorFx);
        }

        public static void Line(Vector2 a, Vector2 b, float width, Color c, float life) => Seg(a, b, width, c, life);

        /// <summary>Jagged lightning between two points.</summary>
        public static void Bolt(Vector2 a, Vector2 b, Color c, float life = 0.22f, float width = 0.14f)
        {
            if (S == null) return;
            var d = b - a;
            float len = d.magnitude;
            if (len < 0.01f) return;
            var n = d.Rot(90f) / len;
            int segs = Mathf.Clamp(Mathf.CeilToInt(len / 0.6f), 3, 14);
            Vector2 prev = a;
            for (int i = 1; i <= segs; i++)
            {
                float t = i / (float)segs;
                Vector2 p = a + d * t + (i < segs ? n * Rng.Fx.Range(-0.45f, 0.45f) * Mathf.Min(1f, len * 0.2f) : Vector2.zero);
                Seg(prev, p, width, c, life);
                Seg(prev, p, width * 0.4f, Color.white, life * 0.8f);
                prev = p;
            }
            Flash(a, 0.4f, c, life);
            Flash(b, 0.7f, c, life * 1.4f);
        }

        public static void Seg(Vector2 a, Vector2 b, float width, Color c, float life)
        {
            if (S == null) return;
            var d = b - a;
            float len = d.magnitude;
            if (len < 0.001f) return;
            S.Spawn(SpriteLib.Beam, a, Vector2.zero, life, len, len, c, c.A(0f), true, d.Ang(), 0f, 0f, Sort.FxHigh, width / len);
        }

        public static void Dust(Vector2 pos, int n, Color c, float size = 0.6f)
        {
            if (S == null) return;
            for (int i = 0; i < n; i++)
            {
                var d = Rng.Fx.OnCircle();
                S.Spawn(SpriteLib.Glow, pos + d * 0.2f, d * Rng.Fx.Range(0.8f, 2.2f), Rng.Fx.Range(0.35f, 0.6f), size * 0.6f, size * 1.4f, c.A(0.45f), c.A(0f), false, 0f, 0f, 3f, Sort.Fx - 10, 0.7f);
            }
        }

        public static void Afterimage(SpriteRenderer src, Color c, float life = 0.25f)
        {
            if (S == null || src == null || src.sprite == null) return;
            var sc = src.transform.lossyScale;
            S.Spawn(src.sprite, src.transform.position, Vector2.zero, life, Mathf.Abs(sc.x), Mathf.Abs(sc.x) * 0.98f, c, c.A(0f), true,
                src.transform.eulerAngles.z, 0f, 0f, src.sortingOrder - 1, sc.y / Mathf.Max(0.001f, Mathf.Abs(sc.x)), 0f, sc.x < 0f ^ src.flipX);
        }

        public static void Damage(Vector2 pos, float dmg, bool crit, bool toPlayer)
        {
            if (S == null || !SaveSystem.Prefs.numbers) return;
            string txt = Mathf.Max(1, Mathf.RoundToInt(dmg)).ToString();
            Color c = toPlayer ? Pal.Danger : crit ? Pal.Gold : Color.white;
            S.Number(pos, crit ? txt + "!" : txt, c, crit ? 1.35f : 1f);
        }

        public static void Text(Vector2 pos, string text, Color c, float size = 0.9f)
        {
            if (S != null) S.Number(pos, text, c, size);
        }

        public static VfxSystem.Tele TeleCircle(Vector2 pos, float radius, float dur, Color c) => S != null ? S.Telegraph(0, pos, Vector2.right, radius, 0f, dur, c) : null;
        public static VfxSystem.Tele TeleLine(Vector2 pos, Vector2 dir, float len, float width, float dur, Color c) => S != null ? S.Telegraph(1, pos, dir, len, width, dur, c) : null;
        public static VfxSystem.Tele TeleCone(Vector2 pos, Vector2 dir, float radius, float arc, float dur, Color c) => S != null ? S.Telegraph(2, pos, dir, radius, arc, dur, c) : null;
    }
}
