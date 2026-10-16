using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Obstacles, decor, doors and hall stations. Pivot at the base, 64 px per unit.</summary>
    public static class PropArt
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string kind, Biome b)
        {
            string key = kind + ":" + (b != null ? b.Id : "none");
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = Paint(kind, b ?? Biomes.Niflhel, out float foot);
            cache[key] = s;
            return s;
        }

        static Sprite Paint(string kind, Biome b, out float foot)
        {
            foot = 8f;
            Pix p;
            Color stone = b.StoneVar.Lit(1.15f), stoneD = b.Stone.Lit(0.8f), acc = b.Accent;
            switch (kind)
            {
                case "pillar":
                    p = new Pix(80, 190);
                    p.Ellipse(40, 12, 34, 9, stoneD.Lit(0.7f));
                    p.Rect(10, 10, 70, 26, stoneD, 3);
                    p.Rect(16, 24, 64, 164, stone, 2);
                    for (int i = 0; i < 4; i++) p.Rect(20 + i * 11, 28, 24 + i * 11, 160, stoneD.A(0.7f), 2);
                    p.Rect(8, 160, 72, 178, stoneD, 3);
                    p.Rect(4, 174, 76, 184, stone.Lit(0.9f), 3);
                    Runes.DrawCentered(p, Runes.Algiz + (b.Index & 3), 40, 96, 22, 3.5f, acc);
                    p.LightGlow(40, 96, 22, acc.A(0.45f));
                    p.ShadeVertical(8, 184, 0.6f, 1.15f);
                    break;
                case "stone":
                    p = new Pix(72, 120);
                    p.Ellipse(36, 12, 28, 8, stoneD.Lit(0.6f));
                    p.Poly(new[] { new Vector2(12, 10), new Vector2(60, 10), new Vector2(56, 90), new Vector2(46, 108), new Vector2(26, 108), new Vector2(16, 88) }, stone);
                    p.Poly(new[] { new Vector2(40, 12), new Vector2(60, 10), new Vector2(56, 90), new Vector2(46, 108), new Vector2(40, 106) }, stoneD);
                    for (int i = 0; i < 3; i++) Runes.DrawCentered(p, (b.Index * 3 + i * 7) % Runes.Count, 32, 30 + i * 24, 16, 2.6f, acc);
                    p.LightGlow(32, 54, 30, acc.A(0.3f));
                    p.ShadeVertical(8, 108, 0.65f, 1.1f);
                    break;
                case "tree":
                    p = new Pix(128, 176);
                    {
                        Color bark = U.Hex("2a2622");
                        p.Ellipse(64, 12, 30, 8, bark.Lit(0.5f));
                        p.Taper(64, 10, 60, 110, 13, 7, bark);
                        p.Taper(60, 90, 24, 140, 6, 2, bark);
                        p.Taper(62, 104, 100, 150, 6, 2, bark);
                        p.Taper(60, 110, 58, 168, 5, 1.5f, bark);
                        p.Taper(36, 124, 14, 128, 3, 1, bark);
                        p.Taper(90, 138, 112, 132, 3, 1, bark);
                        p.Taper(52, 12, 30, 6, 6, 2, bark);
                        p.Taper(74, 12, 100, 8, 6, 2, bark);
                        p.ShadeVertical(8, 170, 0.7f, 1.2f);
                    }
                    break;
                case "brazier":
                    p = new Pix(64, 96);
                    p.Capsule(18, 8, 28, 50, 2.5f, U.Hex("3a3632"));
                    p.Capsule(46, 8, 36, 50, 2.5f, U.Hex("3a3632"));
                    p.Ellipse(32, 52, 22, 9, U.Hex("2a2622"));
                    p.Ellipse(32, 55, 19, 5, U.Hex("5a3a1a"));
                    p.Rect(10, 48, 54, 54, U.Hex("4a4440"), 2);
                    break;
                case "bones":
                    p = new Pix(64, 40);
                    p.Capsule(10, 12, 40, 18, 3, Pal.Bone.Lit(0.8f));
                    p.Capsule(24, 8, 50, 6, 2.5f, Pal.Bone.Lit(0.75f));
                    p.Circle(46, 18, 8, Pal.Bone.Lit(0.85f));
                    p.Circle(44, 18, 2.2f, Pal.Ink); p.Circle(50, 18, 2.2f, Pal.Ink);
                    p.Capsule(14, 22, 26, 26, 2, Pal.Bone.Lit(0.7f));
                    foot = 6f;
                    break;
                case "rubble":
                    p = new Pix(48, 32);
                    p.Ellipse(16, 10, 9, 6, stone.Lit(0.85f));
                    p.Ellipse(30, 8, 7, 5, stoneD);
                    p.Ellipse(36, 14, 5, 4, stone.Lit(0.7f));
                    foot = 5f;
                    break;
                case "root":
                    p = new Pix(112, 96);
                    {
                        Color r = U.Hex("3a2e1e");
                        p.Taper(8, 12, 40, 64, 7, 10, r);
                        p.Taper(40, 64, 76, 70, 10, 8, r.Lit(1.1f));
                        p.Taper(76, 70, 104, 14, 8, 5, r);
                        p.Taper(50, 60, 60, 10, 5, 3, r.Lit(0.85f));
                        p.LightGlow(60, 50, 20, Pal.Venom.A(0.25f));
                        p.ShadeVertical(8, 80, 0.7f, 1.2f);
                    }
                    break;
                case "crystal":
                    p = new Pix(80, 120);
                    p.Ellipse(40, 10, 30, 8, U.Hex("2a3a4a"));
                    p.Poly(new[] { new Vector2(30, 10), new Vector2(46, 10), new Vector2(44, 100), new Vector2(36, 112), new Vector2(30, 96) }, U.Hex("9be7ff").A(0.9f));
                    p.Poly(new[] { new Vector2(14, 10), new Vector2(28, 10), new Vector2(18, 70) }, U.Hex("7fc8e8").A(0.9f));
                    p.Poly(new[] { new Vector2(48, 10), new Vector2(66, 10), new Vector2(62, 64), new Vector2(54, 76) }, U.Hex("b8f0ff").A(0.9f));
                    p.Capsule(38, 20, 38, 96, 1.5f, Color.white.A(0.7f));
                    p.Glow(40, 50, 40, U.Hex("9be7ff").A(0.25f));
                    break;
                case "statue":
                    p = new Pix(96, 176);
                    p.Rect(16, 8, 80, 34, stoneD, 3);
                    p.Rect(20, 30, 76, 40, stone, 2);
                    p.Poly(new[] { new Vector2(30, 40), new Vector2(66, 40), new Vector2(62, 120), new Vector2(34, 120) }, stone);
                    p.Circle(48, 134, 15, stone);
                    p.Circle(48, 136, 18, stoneD);
                    p.Ellipse(52, 132, 9, 10, U.Hex("0b0d12"));
                    p.Glow(50, 133, 5, acc); p.Glow(56, 133, 5, acc);
                    Runes.DrawCentered(p, Runes.Hagalaz, 48, 22, 16, 2.4f, acc);
                    p.ShadeVertical(8, 160, 0.6f, 1.15f);
                    break;
                case "binding":
                    p = new Pix(176, 96);
                    p.Ellipse(88, 30, 82, 24, U.Hex("2a2520"));
                    p.Ellipse(88, 40, 76, 22, U.Hex("4a4238"));
                    p.Ellipse(80, 46, 56, 12, U.Hex("5a5246"));
                    for (int i = 0; i < 3; i++) p.Capsule(28 + i * 50, 22, 40 + i * 50, 60, 3, U.Hex("5e1f24"));
                    break;

                // ---- doors
                case "door":
                    p = new Pix(208, 224);
                    {
                        Color s1 = stone, s2 = stoneD;
                        p.Rect(28, 8, 180, 200, U.Hex("05060a"), 30);
                        p.Rect(10, 8, 40, 196, s2, 3);
                        p.Rect(168, 8, 198, 196, s2, 3);
                        p.Rect(14, 8, 36, 192, s1, 2);
                        p.Rect(172, 8, 194, 192, s1, 2);
                        p.Rect(4, 186, 204, 214, s2, 4);
                        p.Rect(8, 190, 200, 210, s1, 3);
                        p.Poly(new[] { new Vector2(92, 214), new Vector2(116, 214), new Vector2(122, 196), new Vector2(104, 184), new Vector2(86, 196) }, s2);
                        for (int i = 0; i < 5; i++)
                        {
                            Runes.DrawCentered(p, (i * 5 + b.Index * 2) % Runes.Count, 25, 40 + i * 32, 16, 2.4f, acc.A(0.7f));
                            Runes.DrawCentered(p, (i * 7 + 3 + b.Index) % Runes.Count, 183, 40 + i * 32, 16, 2.4f, acc.A(0.7f));
                        }
                        p.ShadeVertical(8, 214, 0.65f, 1.1f);
                    }
                    break;

                // ---- hall stations
                case "throne":
                    p = new Pix(192, 224);
                    {
                        Color bone = Pal.Bone.Lit(0.85f);
                        p.Rect(10, 8, 182, 40, U.Hex("2a2233"), 4);
                        p.Rect(20, 36, 172, 52, U.Hex("3a3046"), 3);
                        p.Rect(46, 50, 146, 190, U.Hex("2a1d3a"), 8);
                        for (int i = 0; i < 7; i++)
                        {
                            float x = 40 + i * 18.6f;
                            p.Taper(x, 160, x + (i - 3) * 4, 214 - Mathf.Abs(i - 3) * 10, 6, 2, bone);
                        }
                        p.Rect(30, 50, 56, 120, bone.Lit(0.85f), 6);
                        p.Rect(136, 50, 162, 120, bone.Lit(0.85f), 6);
                        p.Circle(43, 124, 12, bone); p.Circle(149, 124, 12, bone);
                        Runes.DrawCentered(p, Runes.Hagalaz, 96, 140, 40, 5f, Pal.Hel);
                        p.LightGlow(96, 140, 40, Pal.Hel.A(0.4f));
                        p.ShadeVertical(8, 214, 0.7f, 1.1f);
                    }
                    break;
                case "armory":
                    p = new Pix(176, 160);
                    {
                        Color wood = U.Hex("4a3424");
                        p.Rect(8, 8, 168, 22, wood.Lit(0.7f), 3);
                        p.Rect(14, 20, 24, 132, wood, 2);
                        p.Rect(152, 20, 162, 132, wood, 2);
                        p.Rect(8, 120, 168, 134, wood.Lit(1.1f), 3);
                        p.Rect(8, 70, 168, 78, wood, 2);
                    }
                    break;
                case "runestone_big":
                    p = new Pix(128, 200);
                    p.Ellipse(64, 14, 52, 12, U.Hex("1a1a20"));
                    p.Poly(new[] { new Vector2(22, 12), new Vector2(106, 12), new Vector2(100, 150), new Vector2(84, 186), new Vector2(46, 188), new Vector2(28, 150) }, U.Hex("4a4f5e"));
                    p.Poly(new[] { new Vector2(66, 12), new Vector2(106, 12), new Vector2(100, 150), new Vector2(84, 186), new Vector2(66, 188) }, U.Hex("3a3f4c"));
                    for (int i = 0; i < 5; i++) Runes.DrawCentered(p, (i * 5 + 1) % Runes.Count, 64, 40 + i * 28, 20, 3.4f, Pal.Narfi);
                    p.LightGlow(64, 100, 70, Pal.Narfi.A(0.35f));
                    p.ShadeVertical(8, 188, 0.7f, 1.15f);
                    break;
                case "well":
                    p = new Pix(160, 128);
                    p.Ellipse(80, 40, 64, 26, U.Hex("3a3f4a"));
                    p.Ellipse(80, 48, 58, 20, U.Hex("4a505e"));
                    p.Ellipse(80, 50, 48, 14, U.Hex("0e1830"));
                    p.Glow(80, 52, 46, U.Hex("9ab8ff").A(0.6f), 1.5f);
                    p.Capsule(24, 46, 30, 118, 4, U.Hex("5a4a3a"));
                    p.Capsule(136, 46, 130, 118, 4, U.Hex("5a4a3a"));
                    p.Capsule(24, 116, 136, 116, 4, U.Hex("5a4a3a"));
                    p.Capsule(80, 116, 80, 70, 1, Pal.Hel);
                    break;
                case "chest":
                    p = new Pix(96, 80);
                    p.Rect(8, 8, 88, 46, U.Hex("4a3020"), 3);
                    p.Rect(8, 42, 88, 66, U.Hex("5e3e26"), 10);
                    p.Rect(8, 38, 88, 44, Pal.Gold.Lit(0.8f), 1);
                    p.Rect(42, 30, 54, 50, Pal.Gold, 2);
                    for (int i = 0; i < 3; i++) p.Rect(14 + i * 30, 8, 18 + i * 30, 66, U.Hex("2a1a10"), 1);
                    break;
                case "pool":
                    p = new Pix(256, 128);
                    p.Ellipse(128, 64, 124, 58, U.Hex("2a2233"));
                    p.Ellipse(128, 64, 112, 50, U.Hex("3a0e16"));
                    p.Ellipse(128, 66, 100, 42, U.Hex("5a1420"));
                    p.Glow(128, 64, 110, Pal.Blood.A(0.35f), 1.4f);
                    foot = 64f;
                    break;
                case "gate":
                    p = new Pix(256, 256);
                    {
                        Color s1 = U.Hex("4a4256"), s2 = U.Hex("2e2836");
                        p.Rect(40, 8, 216, 236, U.Hex("05060a"), 60);
                        for (int i = 0; i < 6; i++) p.Rect(60 + i * 4, 10 + i * 18, 196 - i * 4, 26 + i * 18, U.Hex("2a2533").Lit(1f + i * 0.1f), 2);
                        p.Rect(14, 8, 52, 230, s2, 4);
                        p.Rect(204, 8, 242, 230, s2, 4);
                        p.Rect(18, 8, 48, 226, s1, 3);
                        p.Rect(208, 8, 238, 226, s1, 3);
                        p.Rect(6, 222, 250, 250, s2, 5);
                        p.Rect(10, 226, 246, 246, s1, 4);
                        for (int i = 0; i < 6; i++)
                        {
                            Runes.DrawCentered(p, (i * 3 + 2) % Runes.Count, 33, 40 + i * 32, 18, 2.8f, Pal.Narfi.A(0.85f));
                            Runes.DrawCentered(p, (i * 5 + 9) % Runes.Count, 223, 40 + i * 32, 18, 2.8f, Pal.Narfi.A(0.85f));
                        }
                        p.LightGlow(128, 236, 60, Pal.Narfi.A(0.3f));
                        p.ShadeVertical(8, 250, 0.7f, 1.1f);
                    }
                    break;
                case "hearth":
                    p = new Pix(128, 64);
                    p.Ellipse(64, 28, 56, 20, U.Hex("2a2622"));
                    for (int i = 0; i < 10; i++) { var d = U.Dir(i * 36f); p.Circle(64 + d.x * 46, 28 + d.y * 16, 8, U.Hex("4a4440")); }
                    p.Ellipse(64, 28, 34, 10, U.Hex("3a1a0a"));
                    foot = 28f;
                    break;
                case "table":
                    p = new Pix(160, 96);
                    p.Rect(16, 8, 26, 50, U.Hex("3a2a1c"), 2);
                    p.Rect(134, 8, 144, 50, U.Hex("3a2a1c"), 2);
                    p.Rect(4, 46, 156, 62, U.Hex("5a4028"), 4);
                    p.Rect(30, 62, 46, 80, U.Hex("8a6a3a"), 3);
                    p.Ellipse(110, 66, 14, 5, Pal.Gold.Lit(0.8f));
                    break;
                default:
                    p = new Pix(32, 32);
                    p.Circle(16, 16, 12, Color.magenta);
                    break;
            }
            p.Outline(Pal.Ink, 2);
            return p.ToSprite(new Vector2(0.5f, foot / p.H), 64);
        }
    }
}
