using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>Terse helpers for building uGUI from code.</summary>
    public static class UIK
    {
        public static RectTransform Rect(Transform parent, string name = "rect")
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchor at a single point (0..1), with pivot, offset position and size in reference pixels.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Img(Transform parent, Sprite s, Color c, string name = "img")
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.color = c;
            img.raycastTarget = false;
            if (s != null && s.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform parent, string name = "panel") => Img(parent, SpriteLib.Panel, Color.white, name);

        public static Text Txt(Transform parent, string text, int size, Color c, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, string name = "txt", bool title = false)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = title ? Fonts.Title : Fonts.Main;
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = anchor;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.85f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static void SetAlpha(this Graphic g, float a)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        public static bool Hover(RectTransform rt, Vector2 screen) =>
            rt != null && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>A header line: title text with an ornament underneath.</summary>
        public static Text Header(Transform parent, string text, Vector2 anchor, Vector2 pos, int size, Color c)
        {
            var t = Txt(parent, text, size, c, TextAnchor.MiddleCenter, FontStyle.Bold, "header", true);
            t.rectTransform.Place(anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(1200, size * 1.6f));
            var orn = Img(parent, SpriteLib.Ornament, c.A(0.8f), "ornament");
            orn.rectTransform.Place(anchor, new Vector2(0.5f, 0.5f), pos + new Vector2(0, -size * 0.85f), new Vector2(520, 24));
            return t;
        }
    }

    /// <summary>Keyboard / gamepad / mouse navigable list of items.</summary>
    public sealed class MenuList
    {
        public sealed class Item
        {
            public RectTransform Rt;
            public Action OnConfirm;
            public Action<bool> OnHighlight;
            public Func<bool> Enabled;
        }

        public readonly List<Item> Items = new List<Item>();
        public int Sel;
        public bool Horizontal;
        public int Columns = 1;
        public Action OnCancel;
        public Action<int> OnSelChanged;

        public Item Add(RectTransform rt, Action confirm, Action<bool> highlight, Func<bool> enabled = null)
        {
            var it = new Item { Rt = rt, OnConfirm = confirm, OnHighlight = highlight, Enabled = enabled };
            Items.Add(it);
            return it;
        }

        public void Refresh()
        {
            for (int i = 0; i < Items.Count; i++) Items[i].OnHighlight?.Invoke(i == Sel);
        }

        void Move(int d)
        {
            if (Items.Count == 0) return;
            Sel = (Sel + d + Items.Count) % Items.Count;
            Game.I.Audio.Play("ui_move", 0.5f);
            Refresh();
            OnSelChanged?.Invoke(Sel);
        }

        public void Tick(GameInput inp)
        {
            if (Items.Count == 0) { if (inp.CancelDown) OnCancel?.Invoke(); return; }
            Sel = Mathf.Clamp(Sel, 0, Items.Count - 1);
            int step = Columns > 1 ? Columns : 1;
            if (Columns > 1)
            {
                if (inp.NavLeft) Move(-1);
                if (inp.NavRight) Move(1);
                if (inp.NavUp) Move(-step);
                if (inp.NavDown) Move(step);
            }
            else if (Horizontal)
            {
                if (inp.NavLeft) Move(-1);
                if (inp.NavRight) Move(1);
            }
            else
            {
                if (inp.NavUp) Move(-1);
                if (inp.NavDown) Move(1);
            }
            for (int i = 0; i < Items.Count; i++)
            {
                if (i != Sel && UIK.Hover(Items[i].Rt, inp.MouseScreen) && !inp.UsingPad)
                {
                    if (inp.ClickDown || (inp.MouseScreen - lastMouse).sqrMagnitude > 4f)
                    {
                        Sel = i;
                        Refresh();
                        OnSelChanged?.Invoke(Sel);
                    }
                }
            }
            lastMouse = inp.MouseScreen;
            bool clicked = inp.ClickDown && UIK.Hover(Items[Sel].Rt, inp.MouseScreen);
            if (inp.ConfirmDown && !inp.ClickDown || clicked)
            {
                var it = Items[Sel];
                if (it.Enabled == null || it.Enabled())
                {
                    Game.I.Audio.Play("ui_ok", 0.6f);
                    it.OnConfirm?.Invoke();
                }
                else Game.I.Audio.Play("ui_deny", 0.6f);
            }
            else if (inp.CancelDown) OnCancel?.Invoke();
        }

        Vector2 lastMouse;
    }
}
