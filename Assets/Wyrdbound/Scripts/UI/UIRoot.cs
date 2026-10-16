using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>Base for all full and partial screens.</summary>
    public abstract class UIScreen
    {
        public RectTransform Root;
        public bool Modal = true;
        public int OpenedFrame;
        public float Age;

        public void Build(RectTransform parent)
        {
            Root = UIK.Rect(parent, GetType().Name).Stretch();
            Create();
        }

        protected abstract void Create();
        public virtual void OnOpen() { }
        public virtual void OnClose() { }
        public abstract void Tick(GameInput inp);
        public void Close() => Game.I.UI.Pop(this);

        protected Image Dim(float a = 0.78f)
        {
            var img = UIK.Img(Root, SpriteLib.UiWhite, new Color(0.01f, 0.01f, 0.02f, a), "dim");
            img.rectTransform.Stretch();
            return img;
        }
    }

    /// <summary>Canvas, screen stack, HUD, fades, banners and toasts.</summary>
    public sealed class UIRoot : MonoBehaviour
    {
        public Canvas Canvas;
        public RectTransform Root, HudLayer, ScreenLayer, TopLayer;
        public HUD Hud;
        readonly List<UIScreen> stack = new List<UIScreen>();
        Image fade, hurt, vignette;
        Text bannerTitle, bannerSub;
        Image bannerOrn;
        float bannerT, bannerDur;
        float hurtA;
        readonly List<(Text t, float age)> toasts = new List<(Text, float)>();

        public bool Modal
        {
            get
            {
                foreach (var s in stack) if (s.Modal) return true;
                return false;
            }
        }

        public UIScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public bool Has<T>() where T : UIScreen { foreach (var s in stack) if (s is T) return true; return false; }

        public void Init()
        {
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            Root = (RectTransform)transform;

            vignette = UIK.Img(Root, SpriteLib.Vignette, new Color(1, 1, 1, 0.85f), "vignette");
            vignette.rectTransform.Stretch(-60, -60, -60, -60);
            hurt = UIK.Img(Root, SpriteLib.Vignette, new Color(0.8f, 0.05f, 0.05f, 0f), "hurt");
            hurt.rectTransform.Stretch(-40, -40, -40, -40);

            HudLayer = UIK.Rect(Root, "hud").Stretch();
            ScreenLayer = UIK.Rect(Root, "screens").Stretch();
            TopLayer = UIK.Rect(Root, "top").Stretch();

            bannerOrn = UIK.Img(TopLayer, SpriteLib.Ornament, new Color(1, 1, 1, 0), "bannerOrn");
            bannerOrn.rectTransform.Place(new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(0, -44), new Vector2(700, 26));
            bannerTitle = UIK.Txt(TopLayer, "", 72, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "bannerTitle", true);
            bannerTitle.rectTransform.Place(new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1600, 110));
            bannerSub = UIK.Txt(TopLayer, "", 30, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Italic, "bannerSub");
            bannerSub.rectTransform.Place(new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(0, -86), new Vector2(1500, 60));

            fade = UIK.Img(TopLayer, SpriteLib.UiWhite, new Color(0, 0, 0, 1f), "fade");
            fade.rectTransform.Stretch();

            Hud = new HUD();
            Hud.Build(HudLayer);
        }

        public void Push(UIScreen s)
        {
            s.Build(ScreenLayer);
            s.OpenedFrame = Time.frameCount;
            stack.Add(s);
            s.OnOpen();
            Game.I.Audio.Play("ui_open", 0.35f);
        }

        public void Pop(UIScreen s)
        {
            if (!stack.Contains(s)) return;
            s.OnClose();
            stack.Remove(s);
            if (s.Root != null) Destroy(s.Root.gameObject);
            if (!Modal && Player.I != null) Player.I.BlockInputsUntilRelease();
        }

        public void CloseAll()
        {
            for (int i = stack.Count - 1; i >= 0; i--) Pop(stack[i]);
        }

        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            var top = Top;
            if (top != null)
            {
                top.Age += dt;
                if (top.OpenedFrame != Time.frameCount) top.Tick(Game.I.Input);
            }
            Hud.Tick(dt);

            if (bannerDur > 0f)
            {
                bannerT += dt;
                float k = bannerT / bannerDur;
                float a = k < 0.12f ? k / 0.12f : k > 0.8f ? Mathf.Clamp01((1f - k) / 0.2f) : 1f;
                bannerTitle.SetAlpha(a);
                bannerSub.SetAlpha(a);
                bannerOrn.SetAlpha(a * 0.8f);
                float s = 1f + 0.06f * (1f - Mathf.Clamp01(k * 5f));
                bannerTitle.rectTransform.localScale = new Vector3(s, s, 1f);
                if (k >= 1f) bannerDur = 0f;
            }

            hurtA = Mathf.Max(0f, hurtA - dt * 1.8f);
            float low = 0f;
            var p = Player.I;
            if (p != null && Game.I.Mode == GameMode.Run && p.HP < p.MaxHP * 0.3f && !p.Dead) low = 0.25f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f);
            hurt.color = new Color(0.75f, 0.03f, 0.05f, Mathf.Max(hurtA, low));

            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                var (t, age) = toasts[i];
                age += dt;
                toasts[i] = (t, age);
                t.rectTransform.anchoredPosition = new Vector2(0, 240 + age * 30f + i * 40f);
                t.SetAlpha(age < 2f ? 1f : 1f - (age - 2f));
                if (age > 3f) { Destroy(t.gameObject); toasts.RemoveAt(i); }
            }
        }

        public IEnumerator Fade(float to, float dur)
        {
            float from = fade.color.a;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, U.Smooth(t / dur)));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, to);
        }

        public void SetFade(float a) => fade.color = new Color(0, 0, 0, a);

        public void Banner(string title, string sub, Color c, float dur)
        {
            bannerTitle.text = title;
            bannerTitle.color = c;
            bannerSub.text = sub ?? "";
            bannerOrn.color = c;
            bannerT = 0f;
            bannerDur = dur;
        }

        public void HurtFlash() { if (SaveSystem.Prefs.flashes) hurtA = 0.55f; }

        public void Toast(string text, Color c)
        {
            var t = UIK.Txt(HudLayer, text, 34, c, TextAnchor.MiddleCenter, FontStyle.Bold, "toast");
            t.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(1000, 50));
            toasts.Add((t, 0f));
        }
    }
}
