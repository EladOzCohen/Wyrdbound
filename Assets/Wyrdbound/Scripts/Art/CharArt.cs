using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Paints whole characters: dialogue portraits (4x resolution) and static world sprites (2x).
    /// Animated characters are built from parts by RigArt using the same Specs and painters.
    /// Sprites face right; pivot is at the feet.
    /// </summary>
    public static class CharArt
    {
        public const float PPU = 64f;
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, float> heights = new Dictionary<string, float>();
        static float R = 2f;

        public static Sprite Get(string id, bool ui = false)
        {
            string key = id + (ui ? ":ui" : "");
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            R = ui ? 4f : 2f;
            var p = Paint(id, out float foot);
            if (ui) p.Bleed(3);
            s = p.ToSprite(new Vector2(0.5f, foot / p.BH), PPU, !ui);
            cache[key] = s;
            if (!ui) heights[id] = TopOf(p) / (PPU * p.S);
            return s;
        }

        /// <summary>Visual height in world units of a character (for damage numbers / health bars).</summary>
        public static float Height(string id)
        {
            if (!heights.ContainsKey(id)) Get(id);
            return heights.TryGetValue(id, out var h) ? h : 1.5f;
        }

        static float TopOf(Pix p)
        {
            for (int y = p.H - 1; y >= 0; y--)
                for (int x = 0; x < p.W; x++)
                    if (p.P[y * p.W + x].a > 0.3f) return y;
            return p.H;
        }

        static Pix NP(int w, int h) => new Pix(w, h, R);

        // ------------------------------------------------------------------ shared painters

        /// <summary>Head, hair, hood, helmet, crown, beard and eyes, centred at (hx, hy).</summary>
        public static void DrawHead(Pix p, Hum h, float hx, float hy, float drapeCx, float drapeSw, float drapeSho)
        {
            float s = h.scale;
            float hr = h.headR * s;
            if (h.wolfHead) { DrawWolfHead(p, h, hx, hy); return; }
            if (h.hood)
            {
                p.Poly(new[] { new Vector2(hx - hr * 1.35f, hy + hr * 0.2f), new Vector2(hx + hr * 1.1f, hy + hr * 0.3f), new Vector2(drapeCx + drapeSw * 0.9f, drapeSho - 4), new Vector2(drapeCx - drapeSw * 1.05f, drapeSho - 6) }, h.cloth.Lit(0.85f));
                p.Circle(hx - hr * 0.1f, hy + hr * 0.2f, hr * 1.28f, h.cloth.Lit(0.92f));
                p.Poly(new[] { new Vector2(hx - hr * 0.6f, hy + hr * 1.3f), new Vector2(hx - hr * 1.7f, hy + hr * 0.7f), new Vector2(hx - hr * 1.1f, hy + hr * 0.2f) }, h.cloth.Lit(0.92f));
                p.Ellipse(hx + hr * 0.3f, hy - hr * 0.05f, hr * 0.78f, hr * 0.86f, U.Hex("0b0d12"));
                p.Ellipse(hx + hr * 0.42f, hy - hr * 0.2f, hr * 0.5f, hr * 0.55f, h.skin.Lit(0.38f));
            }
            else
            {
                p.Circle(hx, hy, hr, h.skin);
                p.Ellipse(hx - hr * 0.35f, hy - hr * 0.1f, hr * 0.6f, hr * 0.85f, h.skin.Lit(0.85f));
                p.Ellipse(hx + hr * 0.95f, hy - hr * 0.15f, hr * 0.18f, hr * 0.22f, h.skin.Lit(0.92f));
                if (h.skull)
                {
                    p.Ellipse(hx + hr * 0.15f, hy - hr * 0.62f, hr * 0.6f, hr * 0.35f, h.skin.Lit(0.8f));
                    for (int i = 0; i < 4; i++) p.Capsule(hx - hr * 0.2f + i * hr * 0.22f, hy - hr * 0.85f, hx - hr * 0.2f + i * hr * 0.22f, hy - hr * 0.55f, 0.8f * s, Pal.Ink);
                }
                if (!h.helmet && !h.hat)
                {
                    if (h.hairFlame)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            float fx = hx - hr * 0.9f + i * hr * 0.45f;
                            p.Poly(new[] { new Vector2(fx - 5 * s, hy + hr * 0.4f), new Vector2(fx + 5 * s, hy + hr * 0.4f), new Vector2(fx - 3 * s + (i % 2) * 4, hy + hr * (1.6f + (i % 3) * 0.35f)) }, Pal.Narfi.Lit(0.8f + i * 0.05f));
                        }
                        p.Glow(hx, hy + hr * 1.2f, hr * 2f, Pal.Narfi.A(0.4f));
                    }
                    else
                    {
                        p.Poly(new[] { new Vector2(hx - hr * 1.05f, hy - hr * 0.1f), new Vector2(hx - hr * 0.9f, hy + hr * 0.85f), new Vector2(hx + hr * 0.2f, hy + hr * 1.12f),
                            new Vector2(hx + hr * 1.05f, hy + hr * 0.45f), new Vector2(hx + hr * 0.3f, hy + hr * 0.55f), new Vector2(hx - hr * 0.5f, hy + hr * 0.2f) }, h.hair);
                        for (int i = 0; i < 4; i++)
                            p.Capsule(hx - hr * 0.7f + i * hr * 0.35f, hy + hr * 0.95f - i * 0.6f, hx - hr * 0.4f + i * hr * 0.4f, hy + hr * 0.45f, 0.6f * s, h.hair.Lit(1.2f).A(0.6f));
                    }
                }
            }

            if (h.beard)
            {
                float len = h.longBeard ? 2.1f : 1.25f;
                p.Poly(new[] { new Vector2(hx - hr * 0.35f, hy - hr * 0.2f), new Vector2(hx + hr * 0.95f, hy - hr * 0.25f), new Vector2(hx + hr * 0.4f, hy - hr * len), new Vector2(hx - hr * 0.1f, hy - hr * len * 0.85f) }, h.hair);
                for (int i = 0; i < 3; i++)
                    p.Capsule(hx + hr * (0.05f + i * 0.25f), hy - hr * 0.35f, hx + hr * (0.0f + i * 0.2f), hy - hr * len * 0.8f, 0.6f * s, h.hair.Lit(0.8f));
            }

            if (h.helmet)
            {
                p.Draw(hx - hr * 1.2f, hy - hr * 0.1f, hx + hr * 1.2f, hy + hr * 1.3f, (x, y) =>
                {
                    float d = Mathf.Sqrt((x - hx) * (x - hx) + (y - hy - hr * 0.05f) * (y - hy - hr * 0.05f)) - hr * 1.08f;
                    return Mathf.Max(d, (hy + hr * 0.12f) - y);
                }, h.metal);
                p.Capsule(hx - hr * 0.6f, hy + hr * 0.95f, hx + hr * 0.2f, hy + hr * 1.05f, 1.1f * s, h.metal.Lit(1.35f).A(0.7f));
                p.Capsule(hx + hr * 0.55f, hy + hr * 0.25f, hx + hr * 0.6f, hy - hr * 0.45f, 1.6f * s, h.metal.Lit(0.8f));
                p.Capsule(hx - hr * 1.05f, hy + hr * 0.15f, hx + hr * 1.05f, hy + hr * 0.15f, 1.6f * s, h.metal.Lit(0.75f));
            }
            if (h.horns)
            {
                p.Taper(hx - hr * 0.8f, hy + hr * 0.7f, hx - hr * 1.9f, hy + hr * 1.9f, 3.5f * s, 1f * s, Pal.Bone.Lit(0.9f));
                p.Taper(hx + hr * 0.8f, hy + hr * 0.7f, hx + hr * 1.8f, hy + hr * 2.0f, 3.5f * s, 1f * s, Pal.Bone);
            }
            if (h.hat)
            {
                p.Ellipse(hx, hy + hr * 0.55f, hr * 1.9f, hr * 0.38f, h.cape.Lit(0.9f));
                p.Poly(new[] { new Vector2(hx - hr * 0.8f, hy + hr * 0.6f), new Vector2(hx + hr * 0.8f, hy + hr * 0.6f), new Vector2(hx + hr * 0.3f, hy + hr * 2.0f), new Vector2(hx - hr * 0.45f, hy + hr * 1.7f) }, h.cape);
            }
            if (h.crown || h.boneCrown)
            {
                Color cc = h.boneCrown ? Pal.Bone : Pal.Gold;
                for (int i = 0; i < 5; i++)
                {
                    float x = hx - hr * 0.8f + i * hr * 0.4f;
                    float tall = (i == 2 ? 1.9f : 1.45f) * hr;
                    p.Poly(new[] { new Vector2(x - 3 * s, hy + hr * 0.75f), new Vector2(x + 3 * s, hy + hr * 0.75f), new Vector2(x, hy + tall) }, cc);
                }
                p.Rect(hx - hr * 0.95f, hy + hr * 0.62f, hx + hr * 0.95f, hy + hr * 0.88f, cc, 1);
            }
            if (h.flowers)
                for (int i = 0; i < 5; i++)
                    p.Circle(hx - hr * 0.8f + i * hr * 0.4f, hy + hr * 0.85f, 2.4f * s, i % 2 == 0 ? U.Hex("f9d0e8") : U.Hex("fff3a0"));

            float ey = h.hood ? hy - hr * 0.1f : hy + hr * 0.05f;
            float e1 = hx + hr * (h.hood ? 0.25f : 0.12f), e2 = hx + hr * (h.hood ? 0.65f : 0.55f);
            if (h.blindfold)
            {
                p.Rect(hx - hr * 1.0f, ey - hr * 0.3f, hx + hr * 1.02f, ey + hr * 0.3f, U.Hex("2a2d36"), 2);
                p.Capsule(hx - hr * 1.0f, ey, hx - hr * 1.6f, ey - hr * 0.5f, 1.4f * s, U.Hex("2a2d36"));
            }
            else if (h.eyesGlow)
            {
                p.Glow(e1, ey, 5f * s, h.eye.A(0.85f));
                p.Glow(e2, ey, 5f * s, h.eye.A(0.85f));
                p.Circle(e1, ey, 1.5f * s, Color.white);
                if (!h.eyepatch) p.Circle(e2, ey, 1.5f * s, Color.white);
            }
            else
            {
                p.Circle(e1, ey, 1.4f * s, Pal.Ink);
                if (!h.eyepatch) p.Circle(e2, ey, 1.4f * s, Pal.Ink);
                p.Capsule(e1 - 1.5f * s, ey + 2.6f * s, e2 + 1.5f * s, ey + 2.9f * s, 0.6f * s, h.hair.Lit(0.6f));
            }
            if (h.eyepatch)
            {
                p.Circle(e2, ey, 2.6f * s, Pal.Ink);
                p.Capsule(hx - hr, ey + hr * 0.45f, hx + hr, ey - hr * 0.1f, 0.8f * s, Pal.Ink);
            }
        }

        /// <summary>Váli's wolf head (centred on where a human head would be).</summary>
        public static void DrawWolfHead(Pix p, Hum h, float hx, float hy)
        {
            float k = h.scale / 1.6f;
            p.Circle(hx, hy, 22 * k, U.Hex("3a3f4a"));
            p.Taper(hx + 6 * k, hy - 4 * k, hx + 40 * k, hy - 10 * k, 14 * k, 7 * k, U.Hex("4a4f5a"));
            p.Taper(hx + 8 * k, hy - 14 * k, hx + 34 * k, hy - 20 * k, 7 * k, 4 * k, U.Hex("5a606e"));
            for (int i = 0; i < 4; i++) p.Poly(new[] { new Vector2(hx + (14 + i * 5) * k, hy - 13 * k), new Vector2(hx + (18 + i * 5) * k, hy - 13 * k), new Vector2(hx + (16 + i * 5) * k, hy - 19 * k) }, Color.white);
            p.Circle(hx + 40 * k, hy - 9 * k, 4 * k, Pal.Ink);
            p.Poly(new[] { new Vector2(hx - 16 * k, hy + 12 * k), new Vector2(hx - 4 * k, hy + 16 * k), new Vector2(hx - 14 * k, hy + 36 * k) }, U.Hex("2a2d36"));
            p.Poly(new[] { new Vector2(hx - 2 * k, hy + 16 * k), new Vector2(hx + 10 * k, hy + 14 * k), new Vector2(hx + 2 * k, hy + 36 * k) }, U.Hex("3a3f4a"));
            for (int i = 0; i < 6; i++) p.Poly(new[] { new Vector2(hx - 20 * k + i * 3 * k, hy - 6 * k + i * 4 * k), new Vector2(hx - 28 * k + i * 3 * k, hy + i * 4 * k), new Vector2(hx - 16 * k + i * 3 * k, hy + 2 * k + i * 4 * k) }, U.Hex("2a2d36"));
            p.Glow(hx + 14 * k, hy + 4 * k, 10 * k, Pal.Danger);
            p.Circle(hx + 14 * k, hy + 4 * k, 3 * k, Color.white);
        }

        /// <summary>Held items, drawn relative to the grip (gx, gy), hanging down/forward.</summary>
        public static void DrawSword(Pix p, Hum h, float gx, float gy)
        {
            float s = h.scale;
            p.Capsule(gx, gy, gx + 18 * s, gy - 18 * s, 2.2f * s, h.metal.Lit(1.2f));
            p.Capsule(gx + 2 * s, gy - 2 * s, gx + 17 * s, gy - 17 * s, 0.6f * s, Color.white.A(0.6f));
            p.Capsule(gx - 3 * s, gy - 3 * s, gx + 3 * s, gy + 3 * s, 1.6f * s, h.trim);
        }

        public static void DrawHammer(Pix p, Hum h, float gx, float gy)
        {
            float s = h.scale;
            p.Capsule(gx, gy - 6 * s, gx, gy + 12 * s, 2f * s, U.Hex("5a3b22"));
            p.Rect(gx - 9 * s, gy - 14 * s, gx + 9 * s, gy - 4 * s, h.metal, 2);
            p.Rect(gx - 9 * s, gy - 6 * s, gx + 9 * s, gy - 4 * s, h.metal.Lit(1.3f), 1);
            Runes.DrawCentered(p, Runes.Thurisaz, gx, gy - 9 * s, 7 * s, 1.4f * s, Pal.God(God.Thor));
        }

        public static void DrawStaff(Pix p, Hum h, float gx, float gy, float down, float up)
        {
            float s = h.scale;
            p.Capsule(gx, gy - down, gx + 2 * s, gy + up, 1.8f * s, U.Hex("6b5a44"));
            p.Glow(gx + 2 * s, gy + up + 1 * s, 7 * s, h.runeColor.A(0.9f));
            p.Circle(gx + 2 * s, gy + up + 1 * s, 2.4f * s, Color.white.A(0.9f));
        }

        public static void DrawBow(Pix p, Hum h, float gx, float gy)
        {
            float s = h.scale;
            float bx = gx - 6 * s, by = gy + 8 * s;
            for (int i = 0; i < 12; i++)
            {
                float a0 = Mathf.Lerp(-60, 60, i / 12f) * Mathf.Deg2Rad, a1 = Mathf.Lerp(-60, 60, (i + 1) / 12f) * Mathf.Deg2Rad;
                p.Capsule(bx + Mathf.Cos(a0) * 18 * s, by + Mathf.Sin(a0) * 18 * s, bx + Mathf.Cos(a1) * 18 * s, by + Mathf.Sin(a1) * 18 * s, 1.8f * s, U.Hex("5a3b22"));
            }
            float ex = bx + Mathf.Cos(60 * Mathf.Deg2Rad) * 18 * s;
            p.Capsule(ex, by + 15.6f * s, ex, by - 15.6f * s, 0.5f * s, Pal.Bone);
        }

        public static void DrawShield(Pix p, Hum h, float sx, float sy)
        {
            float s = h.scale;
            p.Circle(sx, sy, 17 * s, U.Hex("4a3424"));
            p.Circle(sx, sy, 14.5f * s, U.Hex("6e4a2a"));
            for (int i = -2; i <= 2; i++) p.Capsule(sx + i * 5.5f * s, sy - 13 * s, sx + i * 5.5f * s, sy + 13 * s, 0.5f * s, U.Hex("4a3020").A(0.6f));
            p.Ring(sx, sy, 16 * s, 2.5f * s, h.metal);
            p.Circle(sx, sy, 4.5f * s, h.metal.Lit(1.2f));
            Runes.DrawCentered(p, Runes.Hagalaz, sx, sy, 16 * s, 2f * s, h.eye.A(0.8f));
        }

        // ------------------------------------------------------------------ whole-body humanoid

        static void Humanoid(Pix p, Hum h)
        {
            float s = h.scale;
            float cx = p.BW * 0.5f;
            float foot = 8f;
            float legH = (h.kneel ? 9f : 20f) * s * h.legs;
            float torH = 34f * s * h.torso;
            float hr = h.headR * s;
            float hip = foot + legH, sho = hip + torH;
            float hx = cx + h.hunch * 9f * s;
            float hy = sho + hr * 0.82f - h.hunch * 7f * s;
            float sw = 15f * s * h.width * h.shoulders, hw = 11f * s * h.width;
            Color clothDark = h.cloth.Lit(0.7f);

            if (h.halo) p.Glow(hx, hy, hr * 3.2f, U.Hex("ffe7a3").A(0.55f), 1.6f);
            if (h.wings)
                for (int side = -1; side <= 1; side += 2)
                {
                    float wx = cx - 4f * s;
                    p.Poly(new[] { new Vector2(wx, sho - 4 * s), new Vector2(wx - 40 * s, sho + 26 * s), new Vector2(wx - 34 * s + side * 3, sho + 8 * s),
                        new Vector2(wx - 44 * s, sho + 4 * s), new Vector2(wx - 30 * s, sho - 8 * s), new Vector2(wx - 36 * s, sho - 20 * s), new Vector2(wx - 8 * s, hip) }, U.Hex("d6dbe6").Lit(side < 0 ? 0.7f : 1f));
                }
            if (h.capeOn)
            {
                var pts = new List<Vector2> { new Vector2(cx - sw * 0.85f, sho + 1), new Vector2(cx + sw * 0.6f, sho + 1) };
                float bx0 = cx + sw * 0.9f, bx1 = cx - sw * 1.45f;
                if (h.tattered)
                    for (int i = 0; i <= 7; i++) pts.Add(new Vector2(Mathf.Lerp(bx0, bx1, i / 7f), foot + 2 + ((i % 2 == 0) ? 0f : 8f * s)));
                else { pts.Add(new Vector2(bx0, foot + 3)); pts.Add(new Vector2(bx1, foot + 3)); }
                p.Poly(pts.ToArray(), h.cape);
            }
            if (h.longHair && !h.hood)
                p.Poly(new[] { new Vector2(hx - hr * 1.1f, hy + hr * 0.4f), new Vector2(hx + hr * 0.5f, hy + hr * 0.6f), new Vector2(hx - hr * 0.2f, sho - torH * 0.45f), new Vector2(hx - hr * 1.5f, sho - torH * 0.5f) }, h.hair.Lit(0.85f));
            if (!h.robe)
            {
                float lw = 4.6f * s * h.width;
                p.Capsule(cx - hw * 0.45f, foot + 4, cx - hw * 0.5f, hip, lw, h.dark);
                p.Capsule(cx + hw * 0.45f, foot + 4, cx + hw * 0.5f, hip, lw, h.dark.Lit(1.15f));
                p.Ellipse(cx - hw * 0.45f + 2 * s, foot + 3, 6.5f * s, 4 * s, h.dark.Lit(0.6f));
                p.Ellipse(cx + hw * 0.45f + 2 * s, foot + 3, 6.5f * s, 4 * s, h.dark.Lit(0.7f));
            }
            if (h.robe)
            {
                var robe = new List<Vector2> { new Vector2(cx - sw, sho), new Vector2(cx + sw, sho) };
                if (h.tattered)
                    for (int i = 0; i <= 6; i++) robe.Add(new Vector2(Mathf.Lerp(cx + sw * 1.3f, cx - sw * 1.3f, i / 6f), foot + 1 + (i % 2 == 1 ? 7f * s : 0f)));
                else { robe.Add(new Vector2(cx + sw * 1.3f, foot + 1)); robe.Add(new Vector2(cx - sw * 1.3f, foot + 1)); }
                p.Poly(robe.ToArray(), h.cloth);
                p.Poly(new[] { new Vector2(cx - 2 * s, sho - 4), new Vector2(cx + 2 * s, sho - 4), new Vector2(cx + 5 * s, foot + 1), new Vector2(cx - 5 * s, foot + 1) }, h.trim.A(0.6f));
            }
            else
            {
                p.Poly(new[] { new Vector2(cx - sw, sho), new Vector2(cx + sw, sho), new Vector2(cx + hw, hip - 2), new Vector2(cx - hw, hip - 2) }, h.cloth);
                p.Poly(new[] { new Vector2(cx - hw, hip + 3), new Vector2(cx + hw, hip + 3), new Vector2(cx + hw * 1.3f, hip - 9 * s), new Vector2(cx - hw * 1.3f, hip - 9 * s) }, clothDark);
                p.Rect(cx - hw - 1, hip - 1, cx + hw + 1, hip + 3 * s, h.trim, 1);
            }
            if (h.fur)
                for (int i = 0; i < 6; i++)
                    p.Circle(cx - sw + i * (sw * 2f / 5f), sho - 1, 4.5f * s, U.Hex("e8e4dc").Lit(0.85f + (i % 2) * 0.15f));
            if (h.iceShards)
                for (int i = 0; i < 6; i++) p.Poly(new[] { new Vector2(cx - sw + i * sw * 0.4f, sho - 2), new Vector2(cx - sw + i * sw * 0.4f + 8 * s, sho - 2), new Vector2(cx - sw + i * sw * 0.4f + 4 * s, sho + 12 * s - (i % 3) * 3 * s) }, U.Hex("dff6ff"));
            if (h.chestRune >= 0)
            {
                float ry = (hip + sho) * 0.5f + 2;
                Runes.DrawCentered(p, h.chestRune, cx, ry, 12 * s, 2.2f * s, h.runeColor);
                p.LightGlow(cx, ry, 10 * s, h.runeColor.A(0.5f));
            }
            if (h.necklace)
            {
                p.Ring(hx - 1, sho - 2, 7 * s, 1.6f * s, Pal.Gold);
                p.Circle(hx - 1, sho - 9 * s, 2.2f * s, U.Hex("ff9ad5"));
            }

            float armR = 4.2f * s * Mathf.Max(0.8f, h.width * 0.9f);
            if (h.armsUp)
            {
                p.Capsule(cx - sw + 2, sho - 3, cx - sw * 0.55f, sho + 26 * s, armR, clothDark);
                p.Capsule(cx + sw - 2, sho - 3, cx + sw * 0.55f, sho + 26 * s, armR, h.cloth);
                p.Circle(cx - sw * 0.55f, sho + 27 * s, 3.6f * s, h.skin);
                p.Circle(cx + sw * 0.55f, sho + 27 * s, 3.6f * s, h.skin);
            }
            else
            {
                p.Capsule(cx - sw - 1, sho - 3, cx - sw - 2, hip + 2, armR, clothDark);
                p.Circle(cx - sw - 2, hip + 1, 3.6f * s, h.skin.Lit(0.85f));
                p.Capsule(cx + sw + 1, sho - 3, cx + sw + 4, hip + 3, armR, h.cloth);
                if (h.oneHand) p.Rect(cx + sw + 1, hip, cx + sw + 7, hip + 5 * s, h.trim, 1);
                else p.Circle(cx + sw + 4, hip + 2, 3.6f * s, h.skin);
            }
            if (h.pads)
            {
                p.Circle(cx - sw + 1, sho - 1, 6 * s, h.metal.Lit(0.8f));
                p.Circle(cx + sw - 1, sho - 1, 6 * s, h.metal);
            }
            if (h.hammer) DrawHammer(p, h, cx + sw + 5, hip + 2);
            if (h.sword) DrawSword(p, h, cx + sw + 5, hip + 2);
            if (h.staff) DrawStaff(p, h, cx + sw + 5, hip + 2, legH - 6, torH + 30 * s);
            if (h.bow) DrawBow(p, h, cx + sw + 6, hip + 2);
            if (h.shield) DrawShield(p, h, cx + sw * 0.7f, (hip + sho) * 0.5f);

            DrawHead(p, h, hx, hy, cx, sw, sho);

            if (h.bowl)
            {
                p.Ellipse(cx, sho + 31 * s, 19 * s, 6 * s, U.Hex("8a6a2a"));
                p.Ellipse(cx, sho + 34 * s, 17 * s, 4 * s, U.Hex("c8a04a"));
                p.Ellipse(cx, sho + 35 * s, 13 * s, 2.2f * s, Pal.Venom);
            }
            if (h.ropes)
            {
                for (int i = 0; i < 4; i++) p.Capsule(cx - 26, foot + 22 + i * 9, cx + 26, foot + 30 + i * 9, 2.2f, U.Hex("5e1f24"));
                p.Capsule(cx - 42, foot, cx - 22, foot + 42, 3f, U.Hex("5e1f24"));
                p.Capsule(cx + 42, foot, cx + 22, foot + 46, 3f, U.Hex("5e1f24"));
            }

            p.ShadeVertical(foot, sho + hr * 2f, 0.72f, 1.08f);
            if (h.deadHalf) p.SplitTint(cx + 2, new Color(0.32f, 0.36f, 0.52f), 4f);
        }

        // ------------------------------------------------------------------ beasts

        public static void Quadruped(Pix p, Quad q)
        {
            float s = q.scale;
            float cx = p.BW * 0.5f - 6 * s, foot = 8f;
            float by = foot + 30f * s;
            Color furD = q.fur.Lit(0.7f);
            p.Taper(cx - 40 * s, by + 4 * s, cx - 66 * s, by + 22 * s, 6.5f * s, 2.5f * s, furD);
            p.Capsule(cx - 28 * s, by, cx - 30 * s, foot + 4, 6 * s, furD);
            p.Capsule(cx + 24 * s, by, cx + 26 * s, foot + 4, 6 * s, furD);
            p.Ellipse(cx, by + 4 * s, 44 * s, 20 * s, q.fur);
            p.Ellipse(cx + 4 * s, by - 6 * s, 34 * s, 10 * s, q.belly);
            if (q.spikes)
                for (int i = 0; i < 8; i++)
                {
                    float x = cx - 34 * s + i * 9 * s;
                    float y = by + 20 * s - Mathf.Abs(i - 3.5f) * 1.5f * s;
                    p.Poly(new[] { new Vector2(x - 5 * s, y - 4 * s), new Vector2(x + 5 * s, y - 4 * s), new Vector2(x - 2 * s, y + 9 * s) }, q.fur.Lit(0.85f));
                }
            p.Capsule(cx - 20 * s, by - 2 * s, cx - 18 * s, foot + 4, 6.5f * s, q.fur);
            p.Capsule(cx + 32 * s, by - 2 * s, cx + 34 * s, foot + 4, 6.5f * s, q.fur);
            p.Ellipse(cx - 17 * s, foot + 3, 7 * s, 3.5f * s, furD);
            p.Ellipse(cx + 37 * s, foot + 3, 7 * s, 3.5f * s, furD);
            if (q.chest.a > 0f) p.Ellipse(cx + 30 * s, by - 2 * s, 14 * s, 14 * s, q.chest);
            DrawBeastHead(p, q, cx + 46 * s, by + 22 * s, cx + 26 * s, by + 8 * s, true);
            if (q.flame) p.Glow(cx + 30 * s, by, 26 * s, q.chest.A(0.5f), 1.6f);
            if (q.chains) p.Capsule(cx - 30 * s, by + 14 * s, cx + 10 * s, by - 12 * s, 2.2f * s, U.Hex("8d939e"));
            p.ShadeVertical(foot, by + 40 * s, 0.75f, 1.1f);
        }

        /// <summary>A hound's neck, head, ears and jaw. (hx, hy) is the skull centre; (nx, ny) the neck base.</summary>
        public static void DrawBeastHead(Pix p, Quad q, float hx, float hy, float nx, float ny, bool withJaw)
        {
            float s = q.scale;
            Color furD = q.fur.Lit(0.7f);
            p.Taper(nx, ny, hx, hy, 14 * s, 11 * s, q.fur);
            p.Circle(hx, hy, 14 * s, q.fur);
            p.Taper(hx + 4 * s, hy - 3 * s, hx + 28 * s, hy - 7 * s, 9 * s, 5 * s, q.fur.Lit(1.1f));
            if (withJaw)
            {
                p.Taper(hx + 6 * s, hy - 9 * s, hx + 24 * s, hy - 14 * s, 5 * s, 3 * s, q.belly.Lit(0.8f));
                for (int i = 0; i < 4; i++) p.Poly(new[] { new Vector2(hx + 10 * s + i * 4 * s, hy - 9 * s), new Vector2(hx + 13 * s + i * 4 * s, hy - 9 * s), new Vector2(hx + 11.5f * s + i * 4 * s, hy - 13 * s) }, Color.white);
            }
            p.Circle(hx + 28 * s, hy - 6 * s, 3.2f * s, Pal.Ink);
            p.Poly(new[] { new Vector2(hx - 10 * s, hy + 8 * s), new Vector2(hx - 2 * s, hy + 10 * s), new Vector2(hx - 8 * s, hy + 24 * s) }, furD);
            p.Poly(new[] { new Vector2(hx + 0 * s, hy + 10 * s), new Vector2(hx + 8 * s, hy + 9 * s), new Vector2(hx + 2 * s, hy + 24 * s) }, q.fur);
            p.Glow(hx + 8 * s, hy + 2 * s, 7 * s, q.eye.A(0.9f));
            p.Circle(hx + 8 * s, hy + 2 * s, 2 * s, Color.white);
            if (q.chains) p.Ring(hx - 8 * s, hy - 6 * s, 9 * s, 3 * s, U.Hex("8d939e"));
        }

        static void Serpent(Pix p, float scale, Color body, Color belly, Color eye, int segs, float amp, bool fangs)
        {
            float s = scale;
            float foot = 8f;
            float x0 = 14 * s, x1 = p.BW - 30 * s;
            for (int i = 0; i < segs; i++)
            {
                float t = i / (float)(segs - 1);
                float x = Mathf.Lerp(x0, x1, t);
                float y = foot + 12 * s + Mathf.Sin(t * Mathf.PI * 2.2f) * amp * s + t * 10 * s;
                float r = Mathf.Lerp(4f, 11f, Mathf.Sqrt(t)) * s;
                p.Circle(x, y, r, body.Lit(0.85f + 0.15f * (i % 2)));
                p.Circle(x, y - r * 0.4f, r * 0.55f, belly);
            }
            float hx = x1 + 10 * s, hy = foot + 22 * s + Mathf.Sin(2.2f * Mathf.PI) * amp * s + 10 * s;
            p.Ellipse(hx, hy, 16 * s, 11 * s, body);
            p.Ellipse(hx + 8 * s, hy - 3 * s, 11 * s, 6 * s, body.Lit(1.15f));
            if (fangs)
            {
                p.Poly(new[] { new Vector2(hx + 10 * s, hy - 6 * s), new Vector2(hx + 13 * s, hy - 6 * s), new Vector2(hx + 11.5f * s, hy - 13 * s) }, Color.white);
                p.Poly(new[] { new Vector2(hx + 15 * s, hy - 6 * s), new Vector2(hx + 18 * s, hy - 6 * s), new Vector2(hx + 16.5f * s, hy - 12 * s) }, Color.white);
            }
            p.Glow(hx + 4 * s, hy + 4 * s, 6 * s, eye.A(0.9f));
            p.Circle(hx + 4 * s, hy + 4 * s, 1.8f * s, Color.white);
        }

        // ------------------------------------------------------------------ the roster

        static Pix Paint(string id, out float foot)
        {
            foot = 8f;
            Pix p;
            var hum = Specs.Hum(id);
            if (hum != null)
            {
                p = NP(hum.W, hum.H);
                Humanoid(p, hum);
                if (id == "weaver")
                    for (int i = 0; i < 3; i++) p.Taper(30 + i * 34, 30, 40 + i * 24, 120, 4f, 1.5f, U.Hex("3f6b3a"));
                p.Volume(0.25f, 0.2f);
                p.Outline(Pal.Ink, 2);
                return p;
            }
            var quad = Specs.Quad(id);
            if (quad != null)
            {
                p = NP(quad.W, quad.H);
                Quadruped(p, quad);
                p.Grain(0.35f, 0.25f, id.Length);
                p.Volume(0.25f, 0.2f);
                p.Outline(Pal.Ink, 2);
                return p;
            }
            switch (id)
            {
                case "mimir":
                    p = NP(96, 112);
                    p.Rect(22, 6, 74, 44, U.Hex("3b3f4a"), 4);
                    p.Rect(26, 10, 70, 40, U.Hex("4b505e"), 3);
                    Runes.DrawCentered(p, Runes.Ansuz, 48, 25, 18, 3f, U.Hex("7ec8ff"));
                    p.LightGlow(48, 25, 16, U.Hex("7ec8ff").A(0.5f));
                    p.Ellipse(48, 46, 30, 6, U.Hex("2a2d36"));
                    p.Circle(48, 66, 17, U.Hex("b9ad9c"));
                    p.Poly(new[] { new Vector2(32, 62), new Vector2(64, 62), new Vector2(56, 34), new Vector2(48, 30), new Vector2(40, 34) }, U.Hex("9aa1ad"));
                    p.Poly(new[] { new Vector2(31, 72), new Vector2(48, 86), new Vector2(65, 72), new Vector2(60, 82), new Vector2(36, 82) }, U.Hex("9aa1ad"));
                    p.Capsule(39, 69, 45, 68, 1.2f, U.Hex("3a3a44"));
                    p.Capsule(51, 68, 57, 69, 1.2f, U.Hex("3a3a44"));
                    p.Glow(48, 68, 24, U.Hex("7ec8ff").A(0.25f));
                    foot = 6f;
                    break;
                case "norns":
                    {
                        p = NP(160, 128);
                        Color[] robes = { U.Hex("4a3b6b"), U.Hex("3b5a6b"), U.Hex("6b3b4a") };
                        for (int i = 0; i < 3; i++)
                        {
                            var sub = NP(96, 128);
                            Humanoid(sub, new Hum { hood = true, robe = true, cloth = robes[i], trim = Pal.Gold, eye = U.Hex("f3e9ff"), scale = i == 1 ? 1.05f : 0.9f });
                            int ox = Mathf.RoundToInt(i * 32 * p.S);
                            for (int y = 0; y < sub.H && y < p.H; y++)
                                for (int x = 0; x < sub.W; x++)
                                {
                                    var c = sub.P[y * sub.W + x];
                                    if (c.a > 0f) p.Blend(x + ox, y, c, 1f);
                                }
                        }
                        for (int i = 0; i < 3; i++) p.Capsule(30 + i * 40, 62, 60 + i * 30, 40 - i * 6, 0.9f, U.Hex("f3e9ff"));
                        break;
                    }
                case "ratatoskr":
                    {
                        p = NP(96, 96);
                        Color f = U.Hex("c8662a");
                        p.Ellipse(30, 46, 20, 30, f.Lit(0.8f));
                        p.Ellipse(26, 56, 14, 22, f.Lit(1.1f));
                        p.Ellipse(56, 30, 16, 20, f);
                        p.Ellipse(58, 26, 10, 12, U.Hex("f2d0a8"));
                        p.Circle(64, 54, 13, f);
                        p.Poly(new[] { new Vector2(58, 62), new Vector2(64, 64), new Vector2(57, 76) }, f.Lit(0.8f));
                        p.Poly(new[] { new Vector2(64, 64), new Vector2(70, 63), new Vector2(67, 77) }, f);
                        p.Ellipse(74, 50, 7, 5, f.Lit(1.15f));
                        p.Circle(80, 51, 2, Pal.Ink);
                        p.Circle(68, 57, 2.2f, Pal.Ink); p.Circle(68.6f, 57.6f, 0.8f, Color.white);
                        p.Ellipse(60, 10, 8, 4, f.Lit(0.6f)); p.Ellipse(46, 10, 8, 4, f.Lit(0.6f));
                        p.Grain(0.5f, 0.3f, 3);
                        break;
                    }
                case "wraith":
                case "wight":
                    {
                        bool w = id == "wight";
                        p = NP(96, 120);
                        Color body = w ? U.Hex("5a7ab0") : U.Hex("5fb3a8");
                        var pts = new List<Vector2> { new Vector2(48, 104), new Vector2(70, 84), new Vector2(72, 50) };
                        for (int i = 0; i <= 6; i++) pts.Add(new Vector2(Mathf.Lerp(72, 24, i / 6f), 12 + (i % 2) * 12));
                        pts.Add(new Vector2(26, 60)); pts.Add(new Vector2(30, 86));
                        p.Glow(48, 64, 50, body.A(0.35f), 1.5f);
                        p.Poly(pts.ToArray(), body.A(0.85f));
                        p.Ellipse(54, 82, 14, 16, U.Hex("0b0d12"));
                        p.Glow(50, 84, 6, Color.white); p.Glow(60, 84, 6, Color.white);
                        p.Circle(50, 84, 2, Color.white); p.Circle(60, 84, 2, Color.white);
                        p.Capsule(70, 70, 86, 56, 4, body.A(0.8f));
                        foot = 12f;
                        break;
                    }
                case "bone_rat":
                    p = NP(64, 48);
                    p.Taper(8, 14, 22, 14, 1f, 2.5f, Pal.Bone.Lit(0.7f));
                    p.Ellipse(30, 16, 13, 8, Pal.Bone.Lit(0.85f));
                    for (int i = 0; i < 4; i++) p.Capsule(22 + i * 5, 22, 22 + i * 5, 12, 0.9f, Pal.Ink.A(0.6f));
                    p.Circle(44, 18, 7, Pal.Bone);
                    p.Ellipse(50, 16, 5, 3.5f, Pal.Bone);
                    p.Circle(41, 26, 3.5f, Pal.Bone.Lit(0.8f));
                    p.Glow(46, 20, 4, Pal.Danger); p.Circle(46, 20, 1.3f, Color.white);
                    p.Capsule(26, 9, 25, 4, 1.4f, Pal.Bone.Lit(0.7f)); p.Capsule(36, 9, 37, 4, 1.4f, Pal.Bone.Lit(0.7f));
                    foot = 4f;
                    break;
                case "bloat":
                    p = NP(96, 100);
                    p.Ellipse(36, 10, 6, 4, U.Hex("3a4a2a")); p.Ellipse(58, 10, 6, 4, U.Hex("3a4a2a"));
                    p.Circle(48, 42, 30, U.Hex("6a7a4a"));
                    p.Circle(42, 48, 22, U.Hex("8a9a5a"));
                    for (int i = 0; i < 5; i++) p.Circle(30 + i * 9, 30 + (i % 3) * 10, 3.5f, Pal.Venom.A(0.8f));
                    p.Capsule(40, 60, 62, 36, 1.2f, Pal.Ink.A(0.7f));
                    for (int i = 0; i < 5; i++) p.Capsule(44 + i * 4, 52 - i * 4, 48 + i * 4, 56 - i * 4, 0.9f, Pal.Ink.A(0.7f));
                    p.Circle(58, 74, 11, U.Hex("7a8a5a"));
                    p.Glow(62, 76, 5, Pal.Venom); p.Circle(62, 76, 1.6f, Color.white);
                    p.LightGlow(48, 42, 26, Pal.Venom.A(0.25f));
                    foot = 7f;
                    break;
                case "serpent":
                    p = NP(128, 72);
                    Serpent(p, 1f, U.Hex("3f6b3a"), U.Hex("b8c87a"), Pal.Venom, 14, 5, true);
                    break;
                case "larva":
                    p = NP(96, 64);
                    for (int i = 0; i < 6; i++) p.Circle(20 + i * 10, 18 + Mathf.Sin(i) * 3, 9 - Mathf.Abs(i - 2.5f), U.Hex("c8b48a").Lit(0.85f + (i % 2) * 0.15f));
                    p.Circle(78, 22, 10, U.Hex("8a6a4a"));
                    for (int i = 0; i < 5; i++) { var d = U.Dir(-50 + i * 25); p.Capsule(78 + d.x * 8, 22 + d.y * 8, 78 + d.x * 15, 22 + d.y * 15, 1.4f, Pal.Bone); }
                    p.Glow(80, 24, 5, Pal.Venom);
                    break;
                case "raven":
                    {
                        p = NP(96, 72);
                        Color f = U.Hex("1e1a2a");
                        p.Poly(new[] { new Vector2(44, 34), new Vector2(10, 64), new Vector2(24, 40), new Vector2(4, 44), new Vector2(38, 26) }, f.Lit(1.3f));
                        p.Ellipse(48, 30, 18, 10, f);
                        p.Poly(new[] { new Vector2(30, 30), new Vector2(14, 22), new Vector2(18, 34) }, f);
                        p.Circle(64, 36, 8, f);
                        p.Poly(new[] { new Vector2(68, 39), new Vector2(84, 34), new Vector2(68, 31) }, U.Hex("3a3446"));
                        p.Poly(new[] { new Vector2(52, 34), new Vector2(84, 68), new Vector2(66, 40), new Vector2(90, 46), new Vector2(58, 28) }, f.Lit(1.6f));
                        p.Glow(66, 38, 4, Pal.Danger); p.Circle(66, 38, 1.4f, Color.white);
                        foot = 14f;
                        break;
                    }
                case "bell":
                    p = NP(96, 128);
                    p.Rect(44, 6, 52, 100, U.Hex("3a3f4a"), 2);
                    p.Rect(24, 96, 72, 104, U.Hex("3a3f4a"), 2);
                    p.Poly(new[] { new Vector2(36, 94), new Vector2(60, 94), new Vector2(70, 50), new Vector2(26, 50) }, U.Hex("6a5a3a"));
                    p.Ellipse(48, 50, 24, 6, U.Hex("8a7a4a"));
                    p.Circle(48, 44, 5, U.Hex("3a3020"));
                    Runes.DrawCentered(p, Runes.Hagalaz, 48, 72, 16, 2.5f, U.Hex("9be7ff"));
                    p.LightGlow(48, 72, 18, U.Hex("9be7ff").A(0.5f));
                    foot = 6f;
                    break;
                case "nidhogg_head":
                    {
                        p = NP(176, 144);
                        Color b = U.Hex("2f4a2a");
                        p.Ellipse(70, 60, 54, 40, b);
                        p.Ellipse(110, 46, 50, 26, b.Lit(1.1f));
                        p.Ellipse(118, 30, 40, 12, U.Hex("c8c08a"));
                        for (int i = 0; i < 7; i++) p.Poly(new[] { new Vector2(86 + i * 10, 34), new Vector2(92 + i * 10, 34), new Vector2(89 + i * 10, 20) }, Color.white);
                        for (int i = 0; i < 5; i++) p.Poly(new[] { new Vector2(30 + i * 16, 92 - i * 2), new Vector2(42 + i * 16, 92 - i * 2), new Vector2(30 + i * 16, 128 - i * 4) }, U.Hex("1e2e1a"));
                        p.Taper(64, 90, 30, 136, 6, 2, Pal.Bone); p.Taper(88, 88, 74, 140, 6, 2, Pal.Bone);
                        for (int i = 0; i < 9; i++) p.Circle(50 + (i % 3) * 18, 50 + (i / 3) * 14, 4, b.Lit(1.25f).A(0.5f));
                        p.Glow(110, 66, 14, Pal.Venom); p.Circle(110, 66, 4, Color.white);
                        p.Glow(150, 50, 18, Pal.Venom.A(0.6f));
                        foot = 10f;
                        break;
                    }
                case "nidhogg_seg":
                    p = NP(96, 88);
                    p.Ellipse(48, 40, 40, 30, U.Hex("2f4a2a"));
                    p.Ellipse(48, 30, 30, 14, U.Hex("8a9a5a"));
                    for (int i = 0; i < 6; i++) p.Circle(30 + (i % 3) * 16, 46 + (i / 3) * 10, 4, U.Hex("3f5a3a"));
                    for (int i = 0; i < 3; i++) p.Poly(new[] { new Vector2(30 + i * 14, 64), new Vector2(40 + i * 14, 64), new Vector2(32 + i * 14, 84) }, U.Hex("1e2e1a"));
                    foot = 10f;
                    break;
                default:
                    p = NP(64, 64);
                    p.Circle(32, 32, 20, Color.magenta);
                    break;
            }
            p.Volume(0.25f, 0.2f);
            p.Outline(Pal.Ink, 2);
            return p;
        }
    }
}
