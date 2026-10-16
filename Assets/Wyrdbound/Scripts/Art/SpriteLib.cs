using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Generated, cached sprites: primitives, VFX shapes, projectiles, icons and UI frames.
    /// World sprites are premultiplied (for Wyrd shaders); UI sprites are straight alpha.
    /// </summary>
    public static class SpriteLib
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            cache[key] = s;
            return s;
        }

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        // ---------------------------------------------------------------- primitives

        /// <summary>White disc, 1 unit diameter.</summary>
        public static Sprite Disc => Cached("disc", () =>
        {
            var p = new Pix(64, 64);
            p.Circle(32, 32, 30.5f, Color.white, 1.5f);
            return p.ToSprite(Center, 64);
        });

        /// <summary>Soft radial glow, 1 unit diameter.</summary>
        public static Sprite Glow => Cached("glow", () =>
        {
            var p = new Pix(64, 64);
            p.Glow(32, 32, 32, Color.white, 2.2f);
            return p.ToSprite(Center, 64);
        });

        /// <summary>Very soft wide glow for light pools.</summary>
        public static Sprite Light => Cached("light", () =>
        {
            var p = new Pix(128, 128);
            p.Glow(64, 64, 64, Color.white, 1.4f);
            return p.ToSprite(Center, 128);
        });

        /// <summary>Thin white ring, 1 unit diameter.</summary>
        public static Sprite Ring => Cached("ring", () =>
        {
            var p = new Pix(128, 128);
            p.Ring(64, 64, 60, 4f, Color.white, 1.2f);
            p.Ring(64, 64, 58, 12f, new Color(1, 1, 1, 0.18f), 3f);
            return p.ToSprite(Center, 128);
        });

        /// <summary>White square, 1x1 unit.</summary>
        public static Sprite Square => Cached("square", () =>
        {
            var p = new Pix(8, 8);
            p.Fill(Color.white);
            return p.ToSprite(Center, 8);
        });

        /// <summary>White square with pivot on the left edge (for beams and lines).</summary>
        public static Sprite Beam => Cached("beam", () =>
        {
            var p = new Pix(16, 16);
            for (int y = 0; y < 16; y++)
            {
                float t = Mathf.Abs((y + 0.5f) / 16f - 0.5f) * 2f;
                float a = Mathf.Clamp01(1f - t * t);
                for (int x = 0; x < 16; x++) p.P[y * 16 + x] = new Color(1, 1, 1, a);
            }
            return p.ToSprite(new Vector2(0f, 0.5f), 16);
        });

        public static Sprite Shadow => Cached("shadow", () =>
        {
            var p = new Pix(64, 32);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x + 0.5f - 32f) / 32f, dy = (y + 0.5f - 16f) / 16f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < 1f) p.P[y * 64 + x] = new Color(0, 0, 0, Mathf.Pow(1f - d, 0.8f) * 0.75f);
                }
            return p.ToSprite(Center, 64);
        });

        /// <summary>Elongated spark (pointing +x).</summary>
        public static Sprite Spark => Cached("spark", () =>
        {
            var p = new Pix(64, 16);
            p.Poly(new[] { new Vector2(2, 8), new Vector2(40, 13), new Vector2(62, 8), new Vector2(40, 3) }, Color.white, 1.5f);
            return p.ToSprite(Center, 64);
        });

        public static Sprite Mote => Cached("mote", () =>
        {
            var p = new Pix(16, 16);
            p.Glow(8, 8, 8, Color.white, 1.6f);
            p.Circle(8, 8, 2f, Color.white);
            return p.ToSprite(Center, 16);
        });

        /// <summary>Large soft mist blob.</summary>
        public static Sprite Mist => Cached("mist", () =>
        {
            var p = new Pix(128, 128);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 64f) / 64f, dy = (y - 64f) / 64f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= 1f) continue;
                    float n = Noise.Fbm(x * 0.04f, y * 0.04f, 7, 4);
                    float a = Mathf.Pow(1f - d, 1.5f) * Mathf.Clamp01(n * 1.6f - 0.25f);
                    p.P[y * 128 + x] = new Color(1, 1, 1, a);
                }
            return p.ToSprite(Center, 128);
        });

        // ---------------------------------------------------------------- VFX shapes

        /// <summary>Crescent slash arc, pointing +x, outer radius = 1 unit.</summary>
        public static Sprite Slash(int arcDeg)
        {
            return Cached("slash" + arcDeg, () =>
            {
                const int S = 256;
                const float R = 124f;
                var p = new Pix(S, S);
                float half = arcDeg * 0.5f;
                float maxW = arcDeg >= 300 ? 34f : 44f;
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (r > R + 1.5f) continue;
                        float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (Mathf.Abs(ang) > half) continue;
                        float t = (ang + half) / arcDeg;
                        float w = maxW * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 0.65f);
                        if (w < 0.5f) continue;
                        float inner = R - w;
                        float edgeOut = Mathf.Clamp01(R + 0.5f - r);
                        float edgeIn = Mathf.Clamp01((r - inner) / 2.5f);
                        if (edgeIn <= 0f) continue;
                        float k = Mathf.Clamp01((r - inner) / w);
                        float a = edgeOut * edgeIn * (0.25f + 0.75f * k * k) * (0.45f + 0.55f * t);
                        p.P[y * S + x] = new Color(1, 1, 1, a);
                    }
                return p.ToSprite(Center, R);
            });
        }

        /// <summary>Telegraph sector (pointing +x), radius 1 unit.</summary>
        public static Sprite Cone(int arcDeg)
        {
            arcDeg = Mathf.Clamp((arcDeg + 7) / 15 * 15, 15, 360);
            return Cached("cone" + arcDeg, () =>
            {
                const int S = 256;
                const float R = 124f;
                var p = new Pix(S, S);
                float half = arcDeg * 0.5f;
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f;
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        if (r > R + 1f) continue;
                        float ang = Mathf.Abs(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                        float outA = Mathf.Clamp01(R + 0.5f - r);
                        float angDist = (half - ang) * Mathf.Deg2Rad * Mathf.Max(r, 1f);
                        if (half >= 180f) angDist = 999f;
                        float side = Mathf.Clamp01(angDist + 0.5f);
                        if (side <= 0f) continue;
                        float rim = Mathf.Max(Mathf.Clamp01(1f - (R - r) / 5f), Mathf.Clamp01(1f - angDist / 4f));
                        float a = outA * side * Mathf.Lerp(0.28f, 1f, rim);
                        p.P[y * S + x] = new Color(1, 1, 1, a);
                    }
                return p.ToSprite(Center, R);
            });
        }

        /// <summary>Telegraph disc with bright rim, 1 unit diameter.</summary>
        public static Sprite TeleCircle => Cached("telecircle", () =>
        {
            var p = new Pix(128, 128);
            p.Circle(64, 64, 62, new Color(1, 1, 1, 0.26f), 1.5f);
            p.Ring(64, 64, 60.5f, 3.5f, Color.white, 1.2f);
            return p.ToSprite(Center, 128);
        });

        /// <summary>Telegraph rectangle, pivot left-centre, 1x1 unit.</summary>
        public static Sprite TeleLine => Cached("teleline", () =>
        {
            var p = new Pix(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float ey = Mathf.Min(y + 0.5f, 63.5f - y);
                    float a = ey < 3f ? 1f : 0.26f;
                    p.P[y * 64 + x] = new Color(1, 1, 1, a);
                }
            return p.ToSprite(new Vector2(0f, 0.5f), 64);
        });

        // ---------------------------------------------------------------- projectiles (pointing +x)

        public static Sprite Arrow(Color shaft, Color head) => Cached("arrow" + U.Hex(shaft) + U.Hex(head), () =>
        {
            var p = new Pix(64, 16);
            p.Capsule(8, 8, 50, 8, 1.4f, shaft);
            p.Poly(new[] { new Vector2(48, 3), new Vector2(62, 8), new Vector2(48, 13) }, head);
            p.Poly(new[] { new Vector2(2, 3), new Vector2(14, 8), new Vector2(2, 13), new Vector2(8, 8) }, Pal.Bone.Lit(0.8f));
            p.Outline(Pal.Ink, 1);
            return p.ToSprite(new Vector2(0.75f, 0.5f), 64);
        });

        public static Sprite Orb => Cached("orb", () =>
        {
            var p = new Pix(48, 48);
            p.Glow(24, 24, 24, new Color(1, 1, 1, 0.8f), 2f);
            p.Circle(24, 24, 8, Color.white);
            return p.ToSprite(Center, 48);
        });

        public static Sprite Glob => Cached("glob", () =>
        {
            var p = new Pix(48, 48);
            p.Circle(24, 24, 13, Pal.Venom.Lit(0.6f));
            p.Circle(22, 26, 9, Pal.Venom);
            p.Circle(19, 29, 3, new Color(1, 1, 0.8f, 0.9f));
            p.Outline(Pal.Ink, 2);
            return p.ToSprite(Center, 48);
        });

        public static Sprite Shard => Cached("shard", () =>
        {
            var p = new Pix(48, 24);
            p.Poly(new[] { new Vector2(2, 12), new Vector2(18, 20), new Vector2(46, 12), new Vector2(18, 4) }, Pal.Frost);
            p.Poly(new[] { new Vector2(10, 12), new Vector2(20, 16), new Vector2(40, 12) }, Color.white);
            return p.ToSprite(Center, 48);
        });

        public static Sprite BoneShard => Cached("boneshard", () =>
        {
            var p = new Pix(48, 24);
            p.Capsule(8, 12, 40, 12, 3.5f, Pal.Bone);
            p.Circle(8, 15, 4, Pal.Bone); p.Circle(8, 9, 4, Pal.Bone);
            p.Circle(41, 15, 4, Pal.Bone); p.Circle(41, 9, 4, Pal.Bone);
            p.Outline(Pal.Ink, 2);
            return p.ToSprite(Center, 48);
        });

        public static Sprite Feather => Cached("feather", () =>
        {
            var p = new Pix(48, 20);
            p.Poly(new[] { new Vector2(2, 10), new Vector2(20, 17), new Vector2(46, 10), new Vector2(20, 3) }, U.Hex("2a2440"));
            p.Capsule(4, 10, 44, 10, 0.8f, U.Hex("a596ff"));
            p.Outline(Pal.Ink, 1);
            return p.ToSprite(Center, 48);
        });

        /// <summary>The thrown rune-stone (cast). White core, tinted per god.</summary>
        public static Sprite RuneStone => Cached("runestone", () =>
        {
            var p = new Pix(48, 48);
            p.Glow(24, 24, 24, new Color(1, 1, 1, 0.55f), 2f);
            p.Ellipse(24, 24, 11, 13, U.Hex("3b3f4a"));
            p.Ellipse(23, 26, 8, 9, U.Hex("5a606e"));
            Runes.DrawCentered(p, Runes.Naudiz, 24, 24, 16, 3.2f, Color.white);
            p.Outline(Pal.Ink, 1);
            return p.ToSprite(Center, 48);
        });

        public static Sprite Bolt => Cached("bolt", () =>
        {
            var p = new Pix(64, 24);
            p.Glow(44, 12, 12, new Color(1, 1, 1, 0.9f), 1.6f);
            p.Taper(4, 12, 46, 12, 1f, 6f, new Color(1, 1, 1, 0.5f));
            p.Circle(46, 12, 5, Color.white);
            return p.ToSprite(new Vector2(0.72f, 0.5f), 48);
        });

        // ---------------------------------------------------------------- icons (64px)

        public static Sprite Icon(string id, bool ui = true)
        {
            return Cached("icon:" + id + (ui ? ":ui" : ":w"), () =>
            {
                var p = new Pix(64, 64);
                DrawIcon(p, id);
                if (ui) p.Bleed(3);
                return p.ToSprite(Center, 64, !ui);
            });
        }

        public static Sprite GodIcon(God g, bool ui = true) => Icon("god:" + g, ui);

        public static Sprite RewardIcon(Reward r, God g, bool ui)
        {
            switch (r)
            {
                case Reward.Boon: return GodIcon(g, ui);
                case Reward.Apple: return Icon("apple", ui);
                case Reward.Silver: return Icon("silver", ui);
                case Reward.Embers: return Icon("ember", ui);
                case Reward.Key: return Icon("key", ui);
                case Reward.Mead: return Icon("mead", ui);
                case Reward.Salve: return Icon("salve", ui);
                case Reward.Ratatoskr: return Icon("god:Ratatoskr", ui);
                case Reward.Shop: return Icon("shop", ui);
                case Reward.Rest: return Icon("rest", ui);
                case Reward.Story: return Icon("story", ui);
                case Reward.Hoard: return Icon("hoard", ui);
                case Reward.Passage: return Icon("passage", ui);
                case Reward.Boss: return Icon("skull", ui);
                case Reward.Surface: return Icon("sun", ui);
            }
            return Icon("unknown", ui);
        }

        static void DrawIcon(Pix p, string id)
        {
            Color ink = Pal.Ink;
            if (id.StartsWith("god:"))
            {
                var g = (God)System.Enum.Parse(typeof(God), id.Substring(4));
                Color c = Pal.God(g);
                p.Glow(32, 32, 32, c.A(0.5f), 2f);
                p.Circle(32, 32, 24, c.Lit(0.35f));
                p.Circle(32, 32, 21, c.Lit(0.75f));
                p.Circle(32, 35, 14, c.Lit(0.95f).A(0.5f), 6f);
                p.Ring(32, 32, 24, 2.5f, c.Lit(1.3f));
                Runes.DrawCentered(p, Pal.GodRune(g), 32, 32, 26, 5f, ink.A(0.85f));
                p.Outline(ink, 2);
                return;
            }
            if (id.StartsWith("rune:"))
            {
                int r = int.Parse(id.Substring(5));
                p.Circle(32, 32, 26, U.Hex("2c2f38"));
                p.Circle(32, 34, 22, U.Hex("3d414d"));
                Runes.DrawCentered(p, r, 32, 32, 30, 5f, Pal.Narfi);
                p.LightGlow(32, 32, 30, Pal.Narfi.A(0.4f));
                p.Outline(ink, 2);
                return;
            }
            switch (id)
            {
                case "silver":
                    for (int i = 0; i < 4; i++)
                    {
                        float y = 16 + i * 7;
                        p.Ellipse(30 + (i % 2) * 3, y, 17, 7, U.Hex("8a93a3"));
                        p.Ellipse(30 + (i % 2) * 3, y + 1.5f, 15, 5.5f, Pal.Silver);
                    }
                    p.Ellipse(44, 20, 12, 6, U.Hex("8a93a3")); p.Ellipse(44, 21.5f, 10, 4.5f, Pal.Silver);
                    Runes.DrawCentered(p, Runes.Fehu, 33, 40, 8, 1.6f, U.Hex("5c6474"));
                    p.Outline(ink, 2);
                    break;
                case "ember":
                    p.Glow(32, 28, 30, Pal.Ember.A(0.6f), 1.8f);
                    p.Poly(new[] { new Vector2(32, 58), new Vector2(46, 30), new Vector2(42, 14), new Vector2(32, 8), new Vector2(22, 14), new Vector2(18, 30) }, Pal.Ember);
                    p.Poly(new[] { new Vector2(32, 44), new Vector2(39, 26), new Vector2(32, 13), new Vector2(25, 26) }, U.Hex("ffd27a"));
                    p.Circle(32, 22, 5, Color.white.A(0.9f));
                    p.Outline(ink, 2);
                    break;
                case "key":
                    p.Ring(20, 42, 10, 6, Pal.Key);
                    p.Capsule(26, 36, 50, 12, 4, Pal.Key);
                    p.Capsule(44, 18, 50, 24, 3, Pal.Key);
                    p.Capsule(38, 24, 44, 30, 3, Pal.Key);
                    p.Circle(20, 42, 3, U.Hex("8b7f66"));
                    p.Outline(ink, 2);
                    break;
                case "mead":
                    p.Taper(12, 50, 46, 16, 4f, 10f, U.Hex("8a5a2b"));
                    p.Taper(12, 50, 44, 18, 2f, 7f, U.Hex("c08a4a"));
                    p.Ellipse(47, 16, 10, 6, Pal.Gold);
                    p.Ellipse(47, 17, 8, 4, U.Hex("fff1c1"));
                    p.Capsule(18, 42, 40, 22, 1.2f, Pal.Gold);
                    p.Outline(ink, 2);
                    break;
                case "salve":
                    p.Glow(32, 30, 28, Pal.Heal.A(0.5f));
                    p.Rect(18, 10, 46, 38, U.Hex("3a6e57"), 7);
                    p.Rect(21, 13, 43, 34, Pal.Heal, 5);
                    p.Rect(24, 38, 40, 46, U.Hex("8b6a42"), 2);
                    p.Poly(new[] { new Vector2(32, 46), new Vector2(46, 58), new Vector2(36, 46) }, U.Hex("4ade80"));
                    p.Capsule(32, 18, 32, 30, 2, Color.white.A(0.9f));
                    p.Capsule(26, 24, 38, 24, 2, Color.white.A(0.9f));
                    p.Outline(ink, 2);
                    break;
                case "apple":
                    p.Glow(32, 30, 30, Pal.Gold.A(0.55f));
                    p.Circle(25, 28, 15, U.Hex("e0a526"));
                    p.Circle(39, 28, 15, U.Hex("e0a526"));
                    p.Circle(32, 24, 16, U.Hex("e0a526"));
                    p.Circle(27, 32, 9, U.Hex("ffe07a"));
                    p.Circle(24, 35, 3.5f, Color.white);
                    p.Capsule(32, 42, 35, 52, 1.8f, U.Hex("6b4423"));
                    p.Poly(new[] { new Vector2(35, 48), new Vector2(50, 56), new Vector2(42, 46) }, U.Hex("5fbf4a"));
                    p.Outline(ink, 2);
                    break;
                case "shop":
                    p.Ellipse(32, 26, 20, 18, U.Hex("6b4a2b"));
                    p.Ellipse(30, 29, 15, 13, U.Hex("8a6238"));
                    p.Rect(24, 40, 40, 48, U.Hex("6b4a2b"), 3);
                    p.Capsule(22, 44, 42, 44, 2, Pal.Gold);
                    Runes.DrawCentered(p, Runes.Fehu, 32, 24, 16, 3f, Pal.Gold);
                    p.Outline(ink, 2);
                    break;
                case "rest":
                    p.Glow(32, 28, 30, U.Hex("5ec8ff").A(0.5f));
                    p.Poly(new[] { new Vector2(32, 56), new Vector2(46, 28), new Vector2(32, 12), new Vector2(18, 28) }, U.Hex("3aa7e8"));
                    p.Circle(32, 25, 13, U.Hex("3aa7e8"));
                    p.Circle(28, 28, 6, U.Hex("b8ecff"));
                    p.Outline(ink, 2);
                    break;
                case "story":
                    p.Rect(14, 12, 50, 52, U.Hex("cdb98a"), 4);
                    p.Rect(17, 15, 47, 49, U.Hex("eadcb5"), 3);
                    for (int i = 0; i < 4; i++) p.Capsule(22, 22 + i * 7, 42 - (i % 2) * 6, 22 + i * 7, 1.2f, U.Hex("6b5a3a"));
                    Runes.DrawCentered(p, Runes.Ansuz, 42, 42, 10, 2f, Pal.Hel);
                    p.Outline(ink, 2);
                    break;
                case "hoard":
                    p.Rect(12, 12, 52, 36, U.Hex("5b3b22"), 3);
                    p.Rect(12, 34, 52, 48, U.Hex("6e4a2a"), 8);
                    p.Rect(12, 30, 52, 35, Pal.Gold, 1);
                    p.Rect(28, 26, 36, 38, Pal.Gold, 2);
                    p.LightGlow(32, 48, 24, Pal.Gold.A(0.5f));
                    p.Outline(ink, 2);
                    break;
                case "skull":
                    p.Circle(32, 36, 18, Pal.Bone);
                    p.Rect(22, 14, 42, 30, Pal.Bone, 5);
                    p.Circle(25, 34, 5.5f, ink); p.Circle(39, 34, 5.5f, ink);
                    p.Glow(25, 34, 6, Pal.Danger); p.Glow(39, 34, 6, Pal.Danger);
                    p.Poly(new[] { new Vector2(32, 27), new Vector2(35, 22), new Vector2(29, 22) }, ink);
                    for (int i = 0; i < 4; i++) p.Capsule(25 + i * 4.5f, 14, 25 + i * 4.5f, 19, 1f, ink);
                    p.Outline(ink, 2);
                    break;
                case "passage":
                    for (int i = 0; i < 5; i++) p.Rect(10 + i * 4, 10 + i * 8, 54 - i * 4, 16 + i * 8, U.Hex("6a6f7d").Lit(1f + i * 0.08f), 1);
                    p.Glow(32, 54, 18, Pal.Narfi.A(0.6f));
                    p.Outline(ink, 2);
                    break;
                case "sun":
                    p.Glow(32, 32, 32, Pal.Gold.A(0.7f), 1.5f);
                    for (int i = 0; i < 8; i++)
                    {
                        var d = U.Dir(i * 45f);
                        p.Capsule(32 + d.x * 16, 32 + d.y * 16, 32 + d.x * 27, 32 + d.y * 27, 2.5f, Pal.Gold);
                    }
                    p.Circle(32, 32, 12, U.Hex("fff1c1"));
                    p.Outline(ink, 2);
                    break;
                case "heart":
                    p.Circle(24, 38, 11, Pal.Blood);
                    p.Circle(40, 38, 11, Pal.Blood);
                    p.Poly(new[] { new Vector2(14, 34), new Vector2(50, 34), new Vector2(32, 10) }, Pal.Blood);
                    p.Circle(22, 41, 3.5f, Color.white.A(0.8f));
                    p.Outline(ink, 2);
                    break;
                case "heart_empty":
                    p.Circle(24, 38, 11, U.Hex("3a3440"));
                    p.Circle(40, 38, 11, U.Hex("3a3440"));
                    p.Poly(new[] { new Vector2(14, 34), new Vector2(50, 34), new Vector2(32, 10) }, U.Hex("3a3440"));
                    p.Outline(ink, 2);
                    break;
                case "keepsake":
                    p.Capsule(18, 54, 32, 38, 1.2f, Pal.Gold);
                    p.Capsule(46, 54, 32, 38, 1.2f, Pal.Gold);
                    p.Circle(32, 28, 13, Pal.Gold);
                    p.Circle(32, 28, 9, Pal.Narfi);
                    p.Circle(29, 31, 3, Color.white.A(0.9f));
                    p.Outline(ink, 2);
                    break;
                case "defiance":
                    p.Poly(new[] { new Vector2(12, 50), new Vector2(52, 50), new Vector2(48, 24), new Vector2(32, 8), new Vector2(16, 24) }, U.Hex("40362a"));
                    p.Poly(new[] { new Vector2(16, 47), new Vector2(48, 47), new Vector2(45, 25), new Vector2(32, 12), new Vector2(19, 25) }, Pal.Narfi.Lit(0.6f));
                    Runes.DrawCentered(p, Runes.Algiz, 32, 30, 24, 4f, Color.white);
                    p.Outline(ink, 2);
                    break;
                case "lock":
                    p.Ring(32, 40, 10, 5, U.Hex("8d8f99"));
                    p.Rect(18, 12, 46, 38, U.Hex("6e7280"), 4);
                    p.Circle(32, 27, 4, ink); p.Capsule(32, 18, 32, 26, 1.5f, ink);
                    p.Outline(ink, 2);
                    break;
                case "weave":
                    p.Ellipse(32, 32, 16, 22, U.Hex("5b4a7a"));
                    for (int i = 0; i < 6; i++) p.Capsule(16, 16 + i * 6, 48, 20 + i * 6, 1.2f, Pal.Hel);
                    p.Capsule(46, 30, 58, 10, 1.2f, Pal.Hel);
                    p.Outline(ink, 2);
                    break;
                case "w_axe": DrawAxe(p, 4, 32, 1f); p.Outline(ink, 2); break;
                case "w_spear": DrawSpear(p, 2, 32, 1f); p.Outline(ink, 2); break;
                case "w_bow": DrawBow(p, 32, 32, 1f); p.Outline(ink, 2); break;
                case "w_seax": DrawSeax(p, 8, 24, 1f, 20f); DrawSeax(p, 8, 40, 1f, -20f); p.Outline(ink, 2); break;
                case "cast":
                    p.Glow(32, 32, 30, Pal.Narfi.A(0.5f));
                    p.Ellipse(32, 32, 16, 19, U.Hex("3b3f4a"));
                    Runes.DrawCentered(p, Runes.Naudiz, 32, 32, 24, 4f, Pal.Narfi);
                    p.Outline(ink, 2);
                    break;
                default:
                    p.Circle(32, 32, 20, U.Hex("444"));
                    p.Outline(ink, 2);
                    break;
            }
        }

        // ---------------------------------------------------------------- weapons (pointing +x, pivot at grip)

        static void DrawAxe(Pix p, float x, float y, float s)
        {
            p.Capsule(x, y, x + 50 * s, y, 2.6f * s, U.Hex("5a3b22"));
            p.Capsule(x + 4 * s, y, x + 10 * s, y, 3f * s, U.Hex("2f2a26"));
            float hx = x + 44 * s;
            p.Poly(new[] { new Vector2(hx - 6 * s, y + 4 * s), new Vector2(hx + 2 * s, y + 20 * s), new Vector2(hx + 14 * s, y + 22 * s),
                new Vector2(hx + 12 * s, y - 4 * s), new Vector2(hx + 4 * s, y - 16 * s), new Vector2(hx - 6 * s, y - 4 * s) }, U.Hex("8d939e"));
            p.Poly(new[] { new Vector2(hx + 8 * s, y + 20 * s), new Vector2(hx + 14 * s, y + 22 * s), new Vector2(hx + 12 * s, y - 4 * s), new Vector2(hx + 4 * s, y - 16 * s), new Vector2(hx + 6 * s, y - 4 * s) }, U.Hex("d9dde4"));
            Runes.DrawCentered(p, Runes.Naudiz, hx + 1 * s, y + 3 * s, 10 * s, 1.6f * s, Pal.Narfi);
        }

        static void DrawSpear(Pix p, float x, float y, float s)
        {
            p.Capsule(x, y, x + 46 * s, y, 1.9f * s, U.Hex("6b4a2b"));
            p.Poly(new[] { new Vector2(x + 44 * s, y + 5 * s), new Vector2(x + 60 * s, y), new Vector2(x + 44 * s, y - 5 * s), new Vector2(x + 40 * s, y) }, U.Hex("cfd5de"));
            p.Capsule(x + 40 * s, y, x + 42 * s, y, 3f * s, Pal.Gold);
            p.Capsule(x + 36 * s, y + 2 * s, x + 32 * s, y + 7 * s, 1f * s, Pal.Narfi);
        }

        static void DrawBow(Pix p, float cx, float cy, float s)
        {
            for (int i = 0; i < 20; i++)
            {
                float a0 = Mathf.Lerp(-70, 70, i / 20f) * Mathf.Deg2Rad, a1 = Mathf.Lerp(-70, 70, (i + 1) / 20f) * Mathf.Deg2Rad;
                float r = 22 * s;
                p.Capsule(cx - 8 * s + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r, cx - 8 * s + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r, 2.6f * s, U.Hex("6b4a2b"));
            }
            float ex = cx - 8 * s + Mathf.Cos(70 * Mathf.Deg2Rad) * 22 * s;
            p.Capsule(ex, cy + 20.6f * s, ex, cy - 20.6f * s, 0.7f * s, Pal.Bone);
            p.Capsule(cx + 12 * s, cy - 4 * s, cx + 12 * s, cy + 4 * s, 2.4f * s, U.Hex("3a2a1c"));
        }

        static void DrawSeax(Pix p, float x, float y, float s, float tilt)
        {
            var d = U.Dir(tilt);
            var n = d.Rot(90f);
            Vector2 a = new Vector2(x, y);
            p.Capsule(a.x, a.y, a.x + d.x * 14 * s, a.y + d.y * 14 * s, 2.6f * s, U.Hex("3a2a1c"));
            Vector2 b0 = a + d * 14 * s, tip = a + d * 48 * s;
            p.Poly(new[] { b0 + n * 4 * s, tip, b0 - n * 3 * s, b0 - n * 3 * s + d * 2 }, U.Hex("cfd5de"));
            p.Capsule(b0.x + n.x * 5 * s, b0.y + n.y * 5 * s, b0.x - n.x * 5 * s, b0.y - n.y * 5 * s, 1.6f * s, Pal.Gold);
        }

        /// <summary>World weapon sprite: pointing +x, pivot at the grip.</summary>
        public static Sprite Weapon(string id) => Cached("weapon:" + id, () =>
        {
            var p = new Pix(80, 64);
            switch (id)
            {
                case "axe": DrawAxe(p, 8, 32, 1f); break;
                case "spear": DrawSpear(p, 4, 32, 1.2f); break;
                case "bow": DrawBow(p, 30, 32, 1f); break;
                case "seax": DrawSeax(p, 8, 32, 0.8f, 0f); break;
            }
            p.Outline(Pal.Ink, 2);
            float gx = id == "bow" ? 42f : (id == "spear" ? 4f : 10f);
            return p.ToSprite(new Vector2(gx / 80f, 0.5f), 64);
        });

        // ---------------------------------------------------------------- UI

        /// <summary>9-slice panel: dark fill, brass rim, notched corners.</summary>
        public static Sprite Panel => Cached("ui:panel", () =>
        {
            var p = new Pix(64, 64);
            p.Rect(1, 1, 63, 63, new Color(0.62f, 0.53f, 0.36f, 1f), 10);
            p.Rect(3, 3, 61, 61, new Color(0.055f, 0.06f, 0.085f, 0.97f), 8);
            p.Rect(6, 6, 58, 58, new Color(0.075f, 0.08f, 0.11f, 0.97f), 6);
            p.Bleed();
            return p.ToSprite(Center, 100, false, new Vector4(16, 16, 16, 16));
        });

        /// <summary>White rounded rectangle, 9-sliced (tint freely).</summary>
        public static Sprite Rounded => Cached("ui:rounded", () =>
        {
            var p = new Pix(32, 32);
            p.Rect(0.5f, 0.5f, 31.5f, 31.5f, Color.white, 8);
            p.Bleed();
            return p.ToSprite(Center, 100, false, new Vector4(10, 10, 10, 10));
        });

        /// <summary>White rounded outline, 9-sliced.</summary>
        public static Sprite RoundedFrame => Cached("ui:frame", () =>
        {
            var p = new Pix(32, 32);
            p.Rect(0.5f, 0.5f, 31.5f, 31.5f, Color.white, 8);
            var hole = new Pix(32, 32);
            hole.Rect(3f, 3f, 29f, 29f, Color.white, 6);
            for (int i = 0; i < p.P.Length; i++)
            {
                var c = p.P[i];
                c.a *= 1f - hole.P[i].a;
                p.P[i] = new Color(1, 1, 1, c.a);
            }
            return p.ToSprite(Center, 100, false, new Vector4(10, 10, 10, 10));
        });

        public static Sprite UiWhite => Cached("ui:white", () =>
        {
            var p = new Pix(4, 4);
            p.Fill(Color.white);
            return p.ToSprite(Center, 100, false);
        });

        public static Sprite UiCircle => Cached("ui:circle", () =>
        {
            var p = new Pix(64, 64);
            p.Circle(32, 32, 31, Color.white, 1.5f);
            p.Bleed();
            return p.ToSprite(Center, 100, false);
        });

        public static Sprite UiRing => Cached("ui:ring", () =>
        {
            var p = new Pix(64, 64);
            p.Ring(32, 32, 29, 3.5f, Color.white, 1.2f);
            return p.ToSprite(Center, 100, false);
        });

        public static Sprite UiGlow => Cached("ui:glow", () =>
        {
            var p = new Pix(64, 64);
            p.Glow(32, 32, 32, Color.white, 2f);
            for (int i = 0; i < p.P.Length; i++) p.P[i] = new Color(1, 1, 1, p.P[i].a);
            return p.ToSprite(Center, 100, false);
        });

        public static Sprite UiDiamond => Cached("ui:diamond", () =>
        {
            var p = new Pix(32, 32);
            p.Poly(new[] { new Vector2(16, 30), new Vector2(30, 16), new Vector2(16, 2), new Vector2(2, 16) }, Color.white, 1.2f);
            p.Bleed();
            return p.ToSprite(Center, 100, false);
        });

        public static Sprite Vignette => Cached("ui:vignette", () =>
        {
            const int S = 256;
            var p = new Pix(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S * 2f - 1f, dy = (y + 0.5f) / S * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.85f + dy * dy);
                    float a = Mathf.Clamp01((d - 0.55f) / 0.75f);
                    p.P[y * S + x] = new Color(0, 0, 0, a * a * 0.95f);
                }
            return p.ToSprite(Center, 100, false);
        });

        /// <summary>Horizontal ornament line with a centre diamond, for headers.</summary>
        public static Sprite Ornament => Cached("ui:ornament", () =>
        {
            var p = new Pix(256, 24);
            for (int x = 0; x < 256; x++)
            {
                float t = Mathf.Abs(x - 128f) / 128f;
                float a = Mathf.Clamp01(1f - t * t);
                p.Blend(x, 12, Color.white, a);
                p.Blend(x, 11, Color.white, a * 0.5f);
            }
            p.Poly(new[] { new Vector2(128, 22), new Vector2(138, 12), new Vector2(128, 2), new Vector2(118, 12) }, Color.white, 1.2f);
            p.Poly(new[] { new Vector2(128, 17), new Vector2(133, 12), new Vector2(128, 7), new Vector2(123, 12) }, new Color(0, 0, 0, 0.6f), 1f);
            p.Bleed();
            return p.ToSprite(Center, 100, false);
        });
    }
}
