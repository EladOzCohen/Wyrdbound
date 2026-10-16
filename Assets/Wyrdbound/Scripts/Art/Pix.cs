using System;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// A CPU pixel canvas with anti-aliased signed-distance shape drawing.
    /// Every sprite in the game (characters, floors, icons, UI frames) is painted with this.
    /// Coordinates are in pixels, origin bottom-left, y up.
    /// </summary>
    public sealed class Pix
    {
        public readonly int W, H;
        public readonly Color[] P;

        /// <summary>
        /// Resolution scale. Shape calls take coordinates in "base" units (w x h); the canvas holds
        /// w*S x h*S real pixels, so the same drawing code renders at any resolution.
        /// Per-pixel calls (Blend, Light, Tint, P[]) always use real pixels.
        /// </summary>
        public readonly float S;

        public delegate float Sdf(float x, float y);

        public Pix(int w, int h, float s = 1f)
        {
            S = Mathf.Max(0.25f, s);
            W = Mathf.CeilToInt(w * S);
            H = Mathf.CeilToInt(h * S);
            P = new Color[W * H];
        }

        /// <summary>Base-unit width / height.</summary>
        public float BW => W / S;
        public float BH => H / S;

        public void Fill(Color c)
        {
            for (int i = 0; i < P.Length; i++) P[i] = c;
        }

        public Color Get(int x, int y) => ((uint)x < (uint)W && (uint)y < (uint)H) ? P[y * W + x] : new Color(0, 0, 0, 0);

        /// <summary>When set, drawing only lands where pixels already exist (decals on a floor).</summary>
        public bool Mask;

        public void Blend(int x, int y, Color c, float a)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
            a *= c.a;
            if (a <= 0f) return;
            if (a > 1f) a = 1f;
            int i = y * W + x;
            Color d = P[i];
            if (Mask)
            {
                if (d.a < 0.98f) return;
                P[i] = new Color(Mathf.Lerp(d.r, c.r, a), Mathf.Lerp(d.g, c.g, a), Mathf.Lerp(d.b, c.b, a), d.a);
                return;
            }
            float ia = 1f - a;
            float oa = a + d.a * ia;
            if (oa <= 1e-6f) { P[i] = new Color(0, 0, 0, 0); return; }
            float k = d.a * ia;
            P[i] = new Color((c.r * a + d.r * k) / oa, (c.g * a + d.g * k) / oa, (c.b * a + d.b * k) / oa, oa);
        }

        /// <summary>Brighten existing pixels (only where something was already painted).</summary>
        public void Light(int x, int y, Color c, float a)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
            int i = y * W + x;
            Color d = P[i];
            if (d.a <= 0f) return;
            a *= c.a;
            P[i] = new Color(Mathf.Min(1f, d.r + c.r * a), Mathf.Min(1f, d.g + c.g * a), Mathf.Min(1f, d.b + c.b * a), d.a);
        }

        /// <summary>Multiply-tint existing pixels.</summary>
        public void Tint(int x, int y, Color c, float a)
        {
            if ((uint)x >= (uint)W || (uint)y >= (uint)H) return;
            int i = y * W + x;
            Color d = P[i];
            P[i] = new Color(Mathf.Lerp(d.r, d.r * c.r, a), Mathf.Lerp(d.g, d.g * c.g, a), Mathf.Lerp(d.b, d.b * c.b, a), d.a);
        }

        public void Draw(float x0, float y0, float x1, float y1, Sdf sdf, Color c, float soft = 1f)
        {
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(x0 * S) - 2), iy0 = Mathf.Max(0, Mathf.FloorToInt(y0 * S) - 2);
            int ix1 = Mathf.Min(W - 1, Mathf.CeilToInt(x1 * S) + 2), iy1 = Mathf.Min(H - 1, Mathf.CeilToInt(y1 * S) + 2);
            float inv = 1f / S;
            for (int y = iy0; y <= iy1; y++)
                for (int x = ix0; x <= ix1; x++)
                {
                    float d = sdf((x + 0.5f) * inv, (y + 0.5f) * inv) * S;
                    float a = Mathf.Clamp01(0.5f - d / soft);
                    if (a > 0f) Blend(x, y, c, a);
                }
        }

        public void Circle(float cx, float cy, float r, Color c, float soft = 1f) =>
            Draw(cx - r, cy - r, cx + r, cy + r, (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c, soft);

        public void Ellipse(float cx, float cy, float rx, float ry, Color c, float soft = 1f) =>
            Draw(cx - rx, cy - ry, cx + rx, cy + ry, (x, y) =>
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
            }, c, soft);

        public void Capsule(float ax, float ay, float bx, float by, float r, Color c, float soft = 1f)
        {
            var a = new Vector2(ax, ay);
            var b = new Vector2(bx, by);
            Draw(Mathf.Min(ax, bx) - r, Mathf.Min(ay, by) - r, Mathf.Max(ax, bx) + r, Mathf.Max(ay, by) + r,
                (x, y) => U.SegDist(new Vector2(x, y), a, b) - r, c, soft);
        }

        /// <summary>Tapered stroke: radius goes from ra at a to rb at b.</summary>
        public void Taper(float ax, float ay, float bx, float by, float ra, float rb, Color c, float soft = 1f)
        {
            var a = new Vector2(ax, ay);
            var b = new Vector2(bx, by);
            Vector2 ab = b - a;
            float l2 = Mathf.Max(1e-4f, ab.sqrMagnitude);
            float rm = Mathf.Max(ra, rb);
            Draw(Mathf.Min(ax, bx) - rm, Mathf.Min(ay, by) - rm, Mathf.Max(ax, bx) + rm, Mathf.Max(ay, by) + rm, (x, y) =>
            {
                var p = new Vector2(x, y);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
                return Vector2.Distance(p, a + ab * t) - Mathf.Lerp(ra, rb, t);
            }, c, soft);
        }

        public void Ring(float cx, float cy, float r, float w, Color c, float soft = 1f) =>
            Draw(cx - r - w, cy - r - w, cx + r + w, cy + r + w,
                (x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - w * 0.5f, c, soft);

        public void Rect(float x0, float y0, float x1, float y1, Color c, float round = 0f, float soft = 1f)
        {
            float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f, hx = (x1 - x0) * 0.5f, hy = (y1 - y0) * 0.5f;
            Draw(x0, y0, x1, y1, (x, y) =>
            {
                float qx = Mathf.Abs(x - cx) - hx + round, qy = Mathf.Abs(y - cy) - hy + round;
                float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
                return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - round;
            }, c, soft);
        }

        public void Poly(Color c, params Vector2[] v) => Poly(v, c, 1f);

        public void Poly(Vector2[] v, Color c, float soft = 1f)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in v) { x0 = Mathf.Min(x0, p.x); y0 = Mathf.Min(y0, p.y); x1 = Mathf.Max(x1, p.x); y1 = Mathf.Max(y1, p.y); }
            Draw(x0, y0, x1, y1, (x, y) => SdPoly(new Vector2(x, y), v), c, soft);
        }

        static float SdPoly(Vector2 p, Vector2[] v)
        {
            int n = v.Length;
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(1e-6f, Vector2.Dot(e, e)));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        public void Glow(float cx, float cy, float r, Color c, float power = 2f) => GlowImpl(cx, cy, r, c, power, false);

        /// <summary>Additive glow onto existing pixels only (rim light, glowing runes on armor).</summary>
        public void LightGlow(float cx, float cy, float r, Color c, float power = 2f) => GlowImpl(cx, cy, r, c, power, true);

        void GlowImpl(float cx, float cy, float r, Color c, float power, bool onlyExisting)
        {
            cx *= S; cy *= S; r *= S;
            int ix0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), iy0 = Mathf.Max(0, Mathf.FloorToInt(cy - r));
            int ix1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + r)), iy1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + r));
            for (int y = iy0; y <= iy1; y++)
                for (int x = ix0; x <= ix1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (d >= 1f) continue;
                    float a = Mathf.Pow(1f - d, power);
                    if (onlyExisting) Light(x, y, c, a); else Blend(x, y, c, a);
                }
        }

        /// <summary>Darken toward the bottom of painted pixels between y0 and y1 (cheap form shading).</summary>
        public void ShadeVertical(float y0, float y1, float darkBottom, float lightTop)
        {
            for (int y = 0; y < H; y++)
            {
                float t = Mathf.InverseLerp(y0, y1, y / S);
                float k = Mathf.Lerp(darkBottom, lightTop, t);
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    var c = P[i];
                    if (c.a <= 0f) continue;
                    P[i] = new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
                }
            }
        }

        public void Outline(Color c, int baseRadius)
        {
            int r = Mathf.Max(1, Mathf.RoundToInt(baseRadius * S));
            var src = (Color[])P.Clone();
            int r2 = r * r;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    float own = src[i].a;
                    if (own >= 0.99f) continue;
                    float m = 0f;
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int yy = y + dy;
                        if ((uint)yy >= (uint)H) continue;
                        for (int dx = -r; dx <= r; dx++)
                        {
                            if (dx * dx + dy * dy > r2 + 1) continue;
                            int xx = x + dx;
                            if ((uint)xx >= (uint)W) continue;
                            float a = src[yy * W + xx].a;
                            if (a > m) m = a;
                        }
                    }
                    if (m <= own) continue;
                    float oa = m * c.a;
                    Color s = src[i];
                    float outA = s.a + oa * (1f - s.a);
                    if (outA <= 1e-6f) continue;
                    float k = oa * (1f - s.a);
                    P[i] = new Color((s.r * s.a + c.r * k) / outA, (s.g * s.a + c.g * k) / outA, (s.b * s.a + c.b * k) / outA, outA);
                }
        }

        /// <summary>
        /// Edge lighting: painted pixels whose neighbour in direction (dx, dy) (base units) is empty get lit
        /// (rim light) or darkened (core shadow). Gives cut-out parts a sense of volume.
        /// </summary>
        public void Edge(float dx, float dy, Color c, float amount, bool darken)
        {
            int ox = Mathf.RoundToInt(dx * S), oy = Mathf.RoundToInt(dy * S);
            var src = (Color[])P.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    var s = src[i];
                    if (s.a < 0.5f) continue;
                    int xx = x + ox, yy = y + oy;
                    float na = ((uint)xx < (uint)W && (uint)yy < (uint)H) ? src[yy * W + xx].a : 0f;
                    if (na > 0.5f) continue;
                    float k = amount * (1f - na);
                    P[i] = darken
                        ? new Color(s.r * (1f - k), s.g * (1f - k), s.b * (1f - k), s.a)
                        : new Color(Mathf.Min(1f, s.r + c.r * k), Mathf.Min(1f, s.g + c.g * k), Mathf.Min(1f, s.b + c.b * k), s.a);
                }
        }

        /// <summary>Standard volume pass: top-left rim light, bottom-right core shadow.</summary>
        public void Volume(float rim = 0.35f, float core = 0.3f)
        {
            Edge(-1.6f, 1.6f, new Color(1f, 0.97f, 0.9f), rim, false);
            Edge(2.2f, -2.2f, Color.black, core, true);
        }

        /// <summary>Speckle painted pixels with value noise (cloth weave, fur, stone grain).</summary>
        public void Grain(float scale, float amount, int seed)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    var c = P[i];
                    if (c.a <= 0f) continue;
                    float n = Noise.Fbm(x / S * scale, y / S * scale, seed, 2) - 0.5f;
                    float k = 1f + n * amount;
                    P[i] = new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
                }
        }

        /// <summary>Recolor every painted pixel right of x (used for Hel's half-living, half-dead body).</summary>
        public void SplitTint(float splitX, Color mul, float feather)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    var c = P[i];
                    if (c.a <= 0f) continue;
                    float t = Mathf.Clamp01((x / S - splitX) / Mathf.Max(0.5f, feather) + 0.5f);
                    if (t <= 0f) continue;
                    P[i] = new Color(Mathf.Lerp(c.r, c.r * mul.r, t), Mathf.Lerp(c.g, c.g * mul.g, t), Mathf.Lerp(c.b, c.b * mul.b, t), c.a);
                }
        }

        /// <summary>Spread colour into fully transparent pixels so bilinear filtering of straight alpha has no dark fringes.</summary>
        public void Bleed(int passes = 2)
        {
            for (int pass = 0; pass < passes; pass++)
            {
                var src = (Color[])P.Clone();
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        int i = y * W + x;
                        if (src[i].a > 0f) continue;
                        float r = 0, g = 0, b = 0; int n = 0;
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int xx = x + dx, yy = y + dy;
                                if ((uint)xx >= (uint)W || (uint)yy >= (uint)H) continue;
                                var s = src[yy * W + xx];
                                if (s.a <= 0f && !(s.r > 0f || s.g > 0f || s.b > 0f)) continue;
                                r += s.r; g += s.g; b += s.b; n++;
                            }
                        if (n > 0) P[i] = new Color(r / n, g / n, b / n, 0f);
                    }
            }
        }

        public Texture2D ToTex(bool premultiply = true, FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var cols = new Color[P.Length];
            for (int i = 0; i < P.Length; i++)
            {
                var c = P[i];
                cols[i] = premultiply ? new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a) : c;
            }
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            t.filterMode = filter;
            t.wrapMode = wrap;
            t.SetPixels(cols);
            t.Apply(false, true);
            return t;
        }

        public Sprite ToSprite(Vector2 pivot, float ppu, bool premultiply = true, Vector4 border = default)
        {
            var t = ToTex(premultiply);
            return Sprite.Create(t, new Rect(0, 0, W, H), pivot, ppu * S, 0, SpriteMeshType.FullRect, border * S);
        }
    }

    public static class Noise
    {
        public static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        public static float Value(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            float u = fx * fx * (3f - 2f * fx), v = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed), c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        public static float Fbm(float x, float y, int seed, int oct = 4)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < oct; i++)
            {
                sum += Value(x, y, seed + i * 131) * amp;
                norm += amp;
                x *= 2.03f; y *= 2.03f; amp *= 0.5f;
            }
            return sum / norm;
        }

        /// <summary>Voronoi: returns (F1, F2, cell hash).</summary>
        public static Vector3 Cell(float x, float y, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float f1 = 9f, f2 = 9f, id = 0f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int cx = xi + i, cy = yi + j;
                    float px = cx + 0.15f + 0.7f * Hash(cx, cy, seed);
                    float py = cy + 0.15f + 0.7f * Hash(cx, cy, seed + 77);
                    float dx = px - x, dy = py - y;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < f1) { f2 = f1; f1 = d; id = Hash(cx, cy, seed + 991); }
                    else if (d < f2) f2 = d;
                }
            return new Vector3(f1, f2, id);
        }
    }
}
