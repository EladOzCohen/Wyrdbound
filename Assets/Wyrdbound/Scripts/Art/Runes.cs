using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>The Elder Futhark, drawn as strokes. Used for god sigils, UI ornament and floor carvings.</summary>
    public static class Runes
    {
        public const int Fehu = 0, Uruz = 1, Thurisaz = 2, Ansuz = 3, Raido = 4, Kaunan = 5, Gebo = 6, Wunjo = 7,
            Hagalaz = 8, Naudiz = 9, Isa = 10, Jera = 11, Eihwaz = 12, Perthro = 13, Algiz = 14, Sowilo = 15,
            Tiwaz = 16, Berkano = 17, Ehwaz = 18, Mannaz = 19, Laguz = 20, Ingwaz = 21, Dagaz = 22, Othala = 23;

        public const int Count = 24;

        public static readonly string[] Names =
        {
            "Fehu", "Uruz", "Thurisaz", "Ansuz", "Raido", "Kaunan", "Gebo", "Wunjo",
            "Hagalaz", "Naudiz", "Isa", "Jera", "Eihwaz", "Perthro", "Algiz", "Sowilo",
            "Tiwaz", "Berkano", "Ehwaz", "Mannaz", "Laguz", "Ingwaz", "Dagaz", "Othala"
        };

        public static readonly string[] Glyphs =
        {
            "ᚠ", "ᚢ", "ᚦ", "ᚨ", "ᚱ", "ᚲ", "ᚷ", "ᚹ", "ᚺ", "ᚾ", "ᛁ", "ᛃ", "ᛇ", "ᛈ", "ᛉ", "ᛊ", "ᛏ", "ᛒ", "ᛖ", "ᛗ", "ᛚ", "ᛜ", "ᛞ", "ᛟ"
        };

        // Each rune: list of segments (x0, y0, x1, y1) in a unit box.
        static readonly float[][] S =
        {
            new[] { .3f, 0f, .3f, 1f, .3f, .5f, .75f, .8f, .3f, .75f, .7f, 1f },
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .75f, .7f, .75f, .7f, .75f, 0f },
            new[] { .3f, 0f, .3f, 1f, .3f, .75f, .7f, .5f, .7f, .5f, .3f, .25f },
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .7f, .75f, .3f, .75f, .7f, .5f },
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .7f, .78f, .7f, .78f, .3f, .55f, .3f, .55f, .72f, 0f },
            new[] { .65f, .85f, .3f, .5f, .3f, .5f, .65f, .15f },
            new[] { .2f, .1f, .8f, .9f, .2f, .9f, .8f, .1f },
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .7f, .78f, .7f, .78f, .3f, .55f },
            new[] { .25f, 0f, .25f, 1f, .75f, 0f, .75f, 1f, .25f, .65f, .75f, .35f },
            new[] { .5f, 0f, .5f, 1f, .3f, .65f, .7f, .4f },
            new[] { .5f, 0f, .5f, 1f },
            new[] { .45f, .85f, .25f, .6f, .25f, .6f, .45f, .35f, .55f, .65f, .75f, .4f, .75f, .4f, .55f, .15f },
            new[] { .5f, 0f, .5f, 1f, .5f, 1f, .72f, .82f, .5f, 0f, .28f, .18f },
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .65f, .8f, .65f, .8f, .7f, 1f, .3f, 0f, .65f, .2f, .65f, .2f, .7f, 0f },
            new[] { .5f, 0f, .5f, 1f, .5f, .5f, .2f, .85f, .5f, .5f, .8f, .85f },
            new[] { .65f, 1f, .35f, .65f, .35f, .65f, .65f, .35f, .65f, .35f, .35f, 0f },
            new[] { .5f, 0f, .5f, 1f, .5f, 1f, .2f, .7f, .5f, 1f, .8f, .7f },
            new[] { .3f, 0f, .3f, 1f, .3f, 1f, .68f, .75f, .68f, .75f, .3f, .5f, .3f, .5f, .68f, .25f, .68f, .25f, .3f, 0f },
            new[] { .2f, 0f, .2f, 1f, .8f, 0f, .8f, 1f, .2f, 1f, .5f, .65f, .5f, .65f, .8f, 1f },
            new[] { .2f, 0f, .2f, 1f, .8f, 0f, .8f, 1f, .2f, 1f, .8f, .55f, .8f, 1f, .2f, .55f },
            new[] { .35f, 0f, .35f, 1f, .35f, 1f, .7f, .7f },
            new[] { .5f, .9f, .8f, .5f, .8f, .5f, .5f, .1f, .5f, .1f, .2f, .5f, .2f, .5f, .5f, .9f },
            new[] { .2f, .1f, .2f, .9f, .8f, .1f, .8f, .9f, .2f, .9f, .8f, .1f, .2f, .1f, .8f, .9f },
            new[] { .5f, 1f, .8f, .6f, .8f, .6f, .25f, 0f, .5f, 1f, .2f, .6f, .2f, .6f, .75f, 0f },
        };

        /// <summary>Draw a rune into a canvas. (x, y) is the bottom-left of a size x size box.</summary>
        public static void Draw(Pix p, int rune, float x, float y, float size, float width, Color c)
        {
            var s = S[((rune % Count) + Count) % Count];
            for (int i = 0; i + 3 < s.Length; i += 4)
                p.Capsule(x + s[i] * size, y + s[i + 1] * size, x + s[i + 2] * size, y + s[i + 3] * size, width * 0.5f, c);
        }

        /// <summary>Draw a rune centred at (cx, cy).</summary>
        public static void DrawCentered(Pix p, int rune, float cx, float cy, float size, float width, Color c) =>
            Draw(p, rune, cx - size * 0.5f, cy - size * 0.5f, size, width, c);

        static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

        /// <summary>A white glowing rune sprite (tint it with SpriteRenderer.color). 1 world unit tall.</summary>
        public static Sprite Sprite(int rune, bool ui = false)
        {
            int key = rune * 2 + (ui ? 1 : 0);
            if (cache.TryGetValue(key, out var sp)) return sp;
            var p = new Pix(64, 64);
            Draw(p, rune, 10, 8, 48, 9, new Color(1, 1, 1, 0.25f));
            Draw(p, rune, 10, 8, 48, 5, Color.white);
            if (ui) p.Bleed();
            sp = p.ToSprite(new Vector2(0.5f, 0.5f), 64, !ui);
            cache[key] = sp;
            return sp;
        }
    }
}
