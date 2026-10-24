using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Gold mouse cursors drawn in code, outlined in near-black so they stay visible on mist, snow and fire:
    /// an arrow for menus, and an aiming reticle while playing with the mouse.
    /// </summary>
    public static class CursorArt
    {
        const int S = 32;
        static Texture2D arrow, reticle;
        static int current = -1;

        static readonly Color Gold = new Color(1f, 0.78f, 0.25f, 1f);
        static readonly Color GoldLight = new Color(1f, 0.93f, 0.6f, 1f);
        static readonly Color Ink = new Color(0.03f, 0.02f, 0.02f, 1f);

        /// <summary>Show the reticle (true) or the menu arrow (false). Cheap to call every frame.</summary>
        public static void Use(bool aim)
        {
            int want = aim ? 1 : 0;
            if (want == current) return;
            current = want;
            if (aim) Cursor.SetCursor(Reticle, new Vector2(S * 0.5f, S * 0.5f), CursorMode.Auto);
            else Cursor.SetCursor(Arrow, new Vector2(1f, 1f), CursorMode.Auto);
        }

        static Texture2D Arrow
        {
            get
            {
                if (arrow != null) return arrow;
                var p = new Pix(S, S);
                // Pix rows run bottom-up; the arrow tip sits at the top-left pixel (the hotspot)
                Vector2 V(float x, float y) => new Vector2(x, S - y);
                var outline = new[] { V(0, 0), V(0, 25), V(6.5f, 19.5f), V(11, 30), V(16, 28), V(11.5f, 18), V(20, 18) };
                var fill = new[] { V(2, 4), V(2, 20.5f), V(7, 16), V(11.8f, 26.5f), V(13.6f, 25.7f), V(9, 15.8f), V(15.2f, 15.8f) };
                p.Poly(outline, Ink, 1f);
                p.Poly(fill, Gold, 1f);
                p.Poly(new[] { V(2, 4), V(2, 12), V(8.5f, 10.5f) }, GoldLight, 1f);
                arrow = Make(p, "cursor_arrow");
                return arrow;
            }
        }

        static Texture2D Reticle
        {
            get
            {
                if (reticle != null) return reticle;
                var p = new Pix(S, S);
                float c = S * 0.5f;
                p.Ring(c, c, 11f, 5f, Ink);
                p.Ring(c, c, 11f, 2.4f, Gold);
                for (int i = 0; i < 4; i++)
                {
                    var d = U.Dir(i * 90f);
                    p.Capsule(c + d.x * 6f, c + d.y * 6f, c + d.x * 15f, c + d.y * 15f, 2.4f, Ink);
                    p.Capsule(c + d.x * 7f, c + d.y * 7f, c + d.x * 14f, c + d.y * 14f, 1.1f, GoldLight);
                }
                p.Circle(c, c, 3f, Ink);
                p.Circle(c, c, 1.7f, GoldLight);
                reticle = Make(p, "cursor_reticle");
                return reticle;
            }
        }

        static Texture2D Make(Pix p, string name)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear };
            t.SetPixels(p.P);
            t.Apply(false, false);
            return t;
        }
    }
}
