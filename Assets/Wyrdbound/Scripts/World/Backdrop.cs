using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Ginnungagap beneath the chambers: a nebula of mist and stars far away, and drifting ruins and rocks
    /// closer in, both moving with parallax so the floating platforms feel suspended over an abyss.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        sealed class Layer { public Transform T; public float Factor; public Vector2 Origin; public float Drift; }

        readonly List<Layer> layers = new List<Layer>();
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Backdrop Create(Transform root, Biome b, int seed)
        {
            var go = new GameObject("backdrop");
            go.transform.SetParent(root, false);
            var bd = go.AddComponent<Backdrop>();
            bd.Add(Nebula(b), 0.9f, Sort.Void - 10, 1.15f, 0f);
            var rng = new Rng(seed + 99);
            for (int i = 0; i < 9; i++)
            {
                var spr = Rock(b, i % 4);
                float factor = Mathf.Lerp(0.55f, 0.8f, rng.Value);
                var l = bd.Add(spr, factor, Sort.Void - 5 + i, rng.Range(0.7f, 1.6f), rng.Range(-0.08f, 0.08f));
                l.Origin = new Vector2(rng.Range(-26f, 26f), rng.Range(-18f, 14f));
            }
            return bd;
        }

        Layer Add(Sprite s, float factor, int order, float scale, float drift)
        {
            var sr = Mats.SR(transform, "layer", s, order);
            sr.sharedMaterial = Mats.Unlit;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            var l = new Layer { T = sr.transform, Factor = factor, Drift = drift };
            layers.Add(l);
            return l;
        }

        void LateUpdate()
        {
            var cam = Game.I.Cam.transform.position;
            float t = Time.time;
            foreach (var l in layers)
            {
                var p = new Vector2(cam.x, cam.y) * l.Factor + l.Origin * (1f - l.Factor) + new Vector2(l.Drift * t, Mathf.Sin(t * 0.2f + l.Origin.x) * 0.2f);
                l.T.position = new Vector3(p.x, p.y, 0f);
            }
        }

        static Sprite Nebula(Biome b)
        {
            string key = "neb:" + b.Id;
            if (cache.TryGetValue(key, out var s)) return s;
            const int W = 512, H = 288;
            var p = new Pix(W, H);
            int seed = b.Id.GetHashCode() & 0xffff;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W, v = y / (float)H;
                    float n = Noise.Fbm(u * 4f, v * 2.4f, seed, 5);
                    float n2 = Noise.Fbm(u * 9f + 3f, v * 5f, seed + 7, 3);
                    float clouds = Mathf.Clamp01((n - 0.42f) * 2.2f);
                    Color c = Color.Lerp(b.Bg, b.Fog.Lit(0.45f), clouds * 0.75f);
                    c = Color.Lerp(c, b.Accent.Lit(0.5f), Mathf.Clamp01((n2 - 0.62f) * 3f) * clouds * 0.6f);
                    float vign = 1f - Mathf.Pow(Mathf.Abs(v - 0.5f) * 2f, 2f) * 0.5f;
                    p.P[y * W + x] = c.Lit(vign).A(1f);
                }
            var rng = new Rng(seed);
            for (int i = 0; i < 260; i++)
            {
                int x = rng.Range(0, W), y = rng.Range(0, H);
                float br = rng.Range(0.3f, 1f);
                p.Blend(x, y, Color.white, br * 0.8f);
                if (br > 0.85f) p.Glow(x, y, 3f, b.Accent.A(0.5f));
            }
            s = p.ToSprite(new Vector2(0.5f, 0.5f), 12f);
            cache[key] = s;
            return s;
        }

        static Sprite Rock(Biome b, int variant)
        {
            string key = "rock:" + b.Id + variant;
            if (cache.TryGetValue(key, out var s)) return s;
            var p = new Pix(96, 96);
            var rng = new Rng(variant * 31 + b.Id.Length);
            Color dark = b.Cliff.Lit(0.55f), mid = b.Cliff.Lit(0.8f);
            float cx = 48f, top = 70f;
            var pts = new List<Vector2> { new Vector2(cx - 34f, top), new Vector2(cx - 18f, top + 6f), new Vector2(cx + 10f, top + 4f), new Vector2(cx + 36f, top - 2f) };
            pts.Add(new Vector2(cx + 20f, top - 30f));
            pts.Add(new Vector2(cx + 6f, top - 58f));
            pts.Add(new Vector2(cx - 8f, top - 40f));
            pts.Add(new Vector2(cx - 22f, top - 24f));
            p.Poly(pts.ToArray(), dark);
            p.Poly(new[] { new Vector2(cx - 34f, top), new Vector2(cx + 36f, top - 2f), new Vector2(cx + 30f, top - 8f), new Vector2(cx - 28f, top - 6f) }, mid);
            if (variant == 1)
            {
                p.Rect(cx - 10f, top, cx - 4f, top + 20f, mid, 1f);
                p.Rect(cx + 6f, top, cx + 12f, top + 14f, mid, 1f);
            }
            if (variant == 2) p.Taper(cx, top, cx + 4f, top + 22f, 3f, 1f, mid);
            Runes.DrawCentered(p, rng.Range(0, Runes.Count), cx, top - 22f, 12f, 2f, b.Accent.A(0.4f));
            for (int i = 0; i < p.P.Length; i++) p.P[i] = p.P[i].A(p.P[i].a * 0.85f);
            s = p.ToSprite(new Vector2(0.5f, 0.5f), 24f);
            cache[key] = s;
            return s;
        }
    }
}
