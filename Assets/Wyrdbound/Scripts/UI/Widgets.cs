using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>A selectable list row: icon, title, description and a right-hand note.</summary>
    public sealed class Row
    {
        public RectTransform Rt;
        public Image Bg, Icon, Marker;
        public Text Title, Desc, Right;
        public Color Accent = Pal.Gold;

        public static Row Make(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite icon, string title, string desc, string right = "", int titleSize = 30, int descSize = 22)
        {
            var r = new Row();
            r.Rt = UIK.Rect(parent, "row").Place(anchor, new Vector2(0.5f, 1f), pos, size);
            r.Bg = UIK.Img(r.Rt, SpriteLib.Rounded, new Color(1, 1, 1, 0.04f), "bg");
            r.Bg.rectTransform.Stretch();
            r.Marker = UIK.Img(r.Rt, SpriteLib.UiDiamond, Pal.Gold, "marker");
            r.Marker.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-6f, 0f), new Vector2(22, 22));
            r.Marker.enabled = false;
            float x = 18f;
            if (icon != null)
            {
                float s = Mathf.Min(size.y - 20f, 96f);
                r.Icon = UIK.Img(r.Rt, icon, Color.white, "icon");
                r.Icon.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(s, s));
                x = 32f + s;
            }
            r.Title = UIK.Txt(r.Rt, title, titleSize, Pal.Text, TextAnchor.UpperLeft, FontStyle.Bold, "title");
            r.Title.rectTransform.anchorMin = new Vector2(0f, 1f);
            r.Title.rectTransform.anchorMax = new Vector2(1f, 1f);
            r.Title.rectTransform.pivot = new Vector2(0f, 1f);
            r.Title.rectTransform.offsetMin = new Vector2(x, -titleSize * 1.5f - 10f);
            r.Title.rectTransform.offsetMax = new Vector2(-230f, -10f);
            r.Desc = UIK.Txt(r.Rt, desc, descSize, Pal.Dim, TextAnchor.UpperLeft, FontStyle.Normal, "desc");
            r.Desc.rectTransform.anchorMin = Vector2.zero;
            r.Desc.rectTransform.anchorMax = Vector2.one;
            r.Desc.rectTransform.offsetMin = new Vector2(x, 8f);
            r.Desc.rectTransform.offsetMax = new Vector2(-40f, -titleSize * 1.5f - 12f);
            r.Right = UIK.Txt(r.Rt, right, 24, Pal.Dim, TextAnchor.UpperRight, FontStyle.Bold, "right");
            r.Right.rectTransform.anchorMin = new Vector2(1f, 1f);
            r.Right.rectTransform.anchorMax = new Vector2(1f, 1f);
            r.Right.rectTransform.pivot = new Vector2(1f, 1f);
            r.Right.rectTransform.anchoredPosition = new Vector2(-18f, -12f);
            r.Right.rectTransform.sizeDelta = new Vector2(320f, 40f);
            return r;
        }

        public void Highlight(bool on)
        {
            Bg.color = on ? Accent.A(0.2f) : new Color(1, 1, 1, 0.04f);
            Marker.enabled = on;
            Marker.color = Accent;
            Desc.color = on ? Pal.Text : Pal.Dim;
            Rt.localScale = on ? new Vector3(1.015f, 1.015f, 1f) : Vector3.one;
        }
    }

    public static class Screens
    {
        /// <summary>A centred framed panel.</summary>
        public static RectTransform Frame(RectTransform root, Vector2 size, Vector2 offset = default)
        {
            var p = UIK.Panel(root, "frame");
            p.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, size);
            return p.rectTransform;
        }

        public static Text Hint(RectTransform root, string text)
        {
            var t = UIK.Txt(root, text, 22, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "hint");
            t.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(1600f, 40f));
            return t;
        }

        public static string Pips(int n, int max, Color on)
        {
            string s = "";
            for (int i = 0; i < max; i++) s += U.Tint(i < n ? "■" : "□", i < n ? on : Pal.Dim);
            return s;
        }
    }
}
