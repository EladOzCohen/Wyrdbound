using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    // ======================================================================== TITLE

    public sealed class TitleScreen : UIScreen
    {
        readonly MenuList menu = new MenuList();
        readonly List<Text> items = new List<Text>();
        RectTransform ring, ring2;
        Text title;
        readonly List<Image> mists = new List<Image>();

        protected override void Create()
        {
            var bg = UIK.Img(Root, SpriteLib.UiWhite, U.Hex("040507"), "bg");
            bg.rectTransform.Stretch();
            for (int i = 0; i < 7; i++)
            {
                var m = UIK.Img(Root, SpriteLib.UiGlow, U.Hex("6fa8b8").A(0.05f), "mist");
                m.rectTransform.Place(new Vector2(UnityEngine.Random.value, UnityEngine.Random.Range(0.1f, 0.6f)), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 500));
                mists.Add(m);
            }
            ring = UIK.Rect(Root, "ring").Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            for (int i = 0; i < Runes.Count; i++)
            {
                var r = UIK.Img(ring, Runes.Sprite(i, true), Pal.Narfi.A(0.35f), "r" + i);
                var d = U.Dir(i * 15f) * 330f;
                r.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), d, new Vector2(54, 54));
                r.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 15f - 90f);
            }
            ring2 = UIK.Rect(Root, "ring2").Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
            var circle = UIK.Img(ring2, SpriteLib.UiRing, Pal.Gold.A(0.25f), "circle");
            circle.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(580, 580));
            var glow = UIK.Img(Root, SpriteLib.UiGlow, Pal.Narfi.A(0.12f), "glow");
            glow.rectTransform.Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 800));

            title = UIK.Txt(Root, "WYRDBOUND", 150, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, "title", true);
            title.rectTransform.Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1600, 200));
            var sub = UIK.Txt(Root, "SAGA I  ·  THE BOUND SON", 34, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Normal, "sub");
            sub.rectTransform.Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), new Vector2(0, -78), new Vector2(1200, 50));
            var orn = UIK.Img(Root, SpriteLib.Ornament, Pal.Gold.A(0.8f), "orn");
            orn.rectTransform.Place(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(620, 24));

            var opts = new List<(string, Action)>();
            if (SaveSystem.HasSave && SaveSystem.Data.Flag("intro_seen")) opts.Add(("Continue the Saga", () => Game.I.ContinueGame()));
            opts.Add(("Begin a New Saga", NewSaga));
            opts.Add(("Settings", () => Game.I.UI.Push(new SettingsScreen())));
            opts.Add(("Quit", () => Game.I.Quit()));
            for (int i = 0; i < opts.Count; i++)
            {
                var (label, act) = opts[i];
                var t = UIK.Txt(Root, label, 40, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "opt");
                t.rectTransform.Place(new Vector2(0.5f, 0.27f), new Vector2(0.5f, 0.5f), new Vector2(0, -i * 64f), new Vector2(700, 58));
                items.Add(t);
                int idx = i;
                menu.Add(t.rectTransform, act, on =>
                {
                    items[idx].color = on ? Pal.Gold : Pal.Dim;
                    items[idx].text = on ? "—  " + label + "  —" : label;
                });
            }
            menu.Refresh();

            var ver = UIK.Txt(Root, "Saga I · v0.1  —  every rune, every sound, woven from code", 18, Pal.Dim.A(0.6f), TextAnchor.LowerRight, FontStyle.Normal, "ver");
            ver.rectTransform.Place(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 16), new Vector2(900, 30));
        }

        void NewSaga()
        {
            if (SaveSystem.HasSave && SaveSystem.Data.runs > 0)
            {
                Game.I.UI.Push(new ChoiceScreen("Begin anew? Your current saga will be forgotten.", Pal.Danger, new[] { "Yes, unweave it", "No" }, i => { if (i == 0) Game.I.NewGame(); }));
                return;
            }
            Game.I.NewGame();
        }

        public override void Tick(GameInput inp)
        {
            float t = Time.unscaledTime;
            ring.localRotation = Quaternion.Euler(0, 0, t * 4f);
            ring2.localRotation = Quaternion.Euler(0, 0, -t * 2f);
            for (int i = 0; i < mists.Count; i++)
            {
                var rt = mists[i].rectTransform;
                var a = rt.anchorMin;
                a.x += Time.unscaledDeltaTime * 0.01f * (1f + i % 3);
                if (a.x > 1.3f) a.x = -0.3f;
                rt.anchorMin = rt.anchorMax = a;
            }
            title.color = Pal.Gold.Lit(0.9f + 0.1f * Mathf.Sin(t * 1.3f));
            menu.Tick(inp);
        }
    }

    // ======================================================================== PAUSE

    public sealed class PauseScreen : UIScreen
    {
        readonly MenuList menu = new MenuList();

        protected override void Create()
        {
            Dim(0.82f);
            bool inRun = Game.I.Mode == GameMode.Run && RunState.Current != null;
            UIK.Header(Root, "PAUSED", new Vector2(0.5f, 0.86f), Vector2.zero, 64, Pal.Gold);
            var opts = new List<(string, Action)>
            {
                ("Resume", Close),
                ("Mímir's Lore (Codex)", () => Game.I.UI.Push(new CodexScreen())),
                ("Settings", () => Game.I.UI.Push(new SettingsScreen())),
            };
            if (inRun) opts.Add(("Abandon this Escape", () => Confirm("Abandon the escape? You will wake in Hel's hall.", () => { Close(); RunDirector.I.Abandon(); })));
            opts.Add(("Quit to Title", () => Confirm(inRun ? "Quit to title? This escape will be lost." : "Return to the title screen?", () =>
            {
                if (inRun) RunDirector.I.EndRun(false);
                SaveSystem.Save();
                Game.I.GoTitle();
            })));
            opts.Add(("Quit Game", () => Confirm("Leave the Nine Worlds for now?", () =>
            {
                if (inRun) RunDirector.I.EndRun(false);
                Game.I.Quit();
            })));
            float x = inRun ? -520f : 0f;
            for (int i = 0; i < opts.Count; i++)
            {
                var (label, act) = opts[i];
                var t = UIK.Txt(Root, label, 38, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "opt");
                t.rectTransform.Place(new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), new Vector2(x, -i * 66f), new Vector2(640, 58));
                menu.Add(t.rectTransform, act, on => { t.color = on ? Pal.Gold : Pal.Dim; t.fontStyle = on ? FontStyle.Bold : FontStyle.Normal; });
            }
            menu.OnCancel = Close;
            menu.Refresh();

            if (inRun) BuildBoons(RunState.Current);
        }

        void BuildBoons(RunState run)
        {
            var frame = Screens.Frame(Root, new Vector2(1000, 760), new Vector2(380, -40));
            var head = UIK.Txt(frame, "Blessings of this Escape", 32, Pal.Gold, TextAnchor.UpperCenter, FontStyle.Bold, "head");
            head.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(900, 44));
            var sb = new System.Text.StringBuilder();
            if (run.Boons.Owned.Count == 0) sb.Append(U.Tint("No god has answered you yet.", Pal.Dim));
            foreach (var b in run.Boons.Owned)
            {
                Color c = b.Def.IsDuo ? Pal.Rarity(Rarity.Duo) : Pal.God(b.Def.God);
                sb.Append(U.Tint(b.Def.Name, c)).Append(U.Tint("  " + Pal.RarityName(b.Rarity) + (b.Level > 1 ? "  Lv " + b.Level : "") + "  ·  " + SlotName(b.Def.Slot), Pal.Dim)).Append('\n');
                sb.Append("<size=20>").Append(b.Def.Describe(b.Rarity, b.Level)).Append("</size>\n\n");
            }
            var t = UIK.Txt(frame, sb.ToString(), 24, Pal.Text, TextAnchor.UpperLeft, FontStyle.Normal, "boons");
            t.rectTransform.Stretch(36, 30, 36, 80);
            t.verticalOverflow = VerticalWrapMode.Truncate;
            var stats = UIK.Txt(frame, "Silver " + run.Silver + "    Kills " + run.Kills + "    Time " + U.Clock(run.Time) + "    Heat " + run.Heat, 20, Pal.Dim, TextAnchor.LowerCenter, FontStyle.Normal, "stats");
            stats.rectTransform.Stretch(20, 14, 20, 0);
        }

        public static string SlotName(BoonSlot s)
        {
            switch (s)
            {
                case BoonSlot.Attack: return "Attack";
                case BoonSlot.Special: return "Special";
                case BoonSlot.Cast: return "Cast";
                case BoonSlot.Dash: return "Dash";
                case BoonSlot.Call: return "Call";
            }
            return "Passive";
        }

        void Confirm(string q, Action yes) =>
            Game.I.UI.Push(new ChoiceScreen(q, Pal.Gold, new[] { "Yes", "No" }, i => { if (i == 0) yes(); }));

        public override void Tick(GameInput inp)
        {
            if (inp.PauseDown) { Close(); return; }
            menu.Tick(inp);
        }
    }

    // ======================================================================== SETTINGS

    public sealed class SettingsScreen : UIScreen
    {
        readonly MenuList menu = new MenuList();
        readonly List<Action<int>> adjust = new List<Action<int>>();
        readonly List<Func<string>> value = new List<Func<string>>();
        readonly List<Text> values = new List<Text>();

        protected override void Create()
        {
            Dim(0.9f);
            UIK.Header(Root, "SETTINGS", new Vector2(0.5f, 0.88f), Vector2.zero, 60, Pal.Gold);
            var p = SaveSystem.Prefs;
            AddSlider("Master Volume", () => p.master, v => p.master = v);
            AddSlider("Music", () => p.music, v => p.music = v);
            AddSlider("Sound Effects", () => p.sfx, v => p.sfx = v);
            AddSlider("Screen Shake", () => p.shake, v => p.shake = v);
            AddToggle("Damage Numbers", () => p.numbers, v => p.numbers = v);
            AddToggle("Hurt Flashes", () => p.flashes, v => p.flashes = v);
            AddToggle("Dynamic Lighting", () => p.lighting, v => p.lighting = v);
            AddToggle("Bloom & Colour Grading", () => p.bloom, v => p.bloom = v);
            AddToggle("Aim with Mouse", () => p.mouseAim, v => { p.mouseAim = v; Game.I.Input.MouseAim = v; });
            AddToggle("Fullscreen", () => p.fullscreen, v => { p.fullscreen = v; Game.I.ApplySettings(); });
            AddToggle("Odin's Grace (take far less damage; grows with each death)", () => p.grace, v =>
            {
                p.grace = v;
                RunState.Current?.RecalcStats();
            });
            AddButton("Back", Close);
            menu.OnCancel = Close;
            menu.Refresh();
            Screens.Hint(Root, "Left / Right to adjust  ·  " + Game.I.Input.Glyph("confirm") + " to toggle  ·  " + Game.I.Input.Glyph("cancel") + " to return");
        }

        RectTransform Line(string label, Func<string> val)
        {
            int i = values.Count;
            var rt = UIK.Rect(Root, "line").Place(new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.5f), new Vector2(0, -i * 62f), new Vector2(1200, 56));
            var bg = UIK.Img(rt, SpriteLib.Rounded, new Color(1, 1, 1, 0.04f), "bg");
            bg.rectTransform.Stretch();
            var l = UIK.Txt(rt, label, 30, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "label");
            l.rectTransform.Stretch(24, 0, 400, 0);
            var v = UIK.Txt(rt, val(), 30, Pal.Gold, TextAnchor.MiddleRight, FontStyle.Bold, "value");
            v.rectTransform.Stretch(700, 0, 24, 0);
            values.Add(v);
            value.Add(val);
            menu.Add(rt, () => adjust[i]?.Invoke(0), on => bg.color = on ? Pal.Gold.A(0.18f) : new Color(1, 1, 1, 0.04f));
            return rt;
        }

        void AddSlider(string label, Func<float> get, Action<float> set)
        {
            Line(label, () => Bar(get()));
            adjust.Add(d =>
            {
                if (d == 0) return;
                set(Mathf.Clamp01(Mathf.Round((get() + d * 0.1f) * 10f) / 10f));
                Game.I.Audio.Play("ui_move", 0.7f);
            });
        }

        void AddToggle(string label, Func<bool> get, Action<bool> set)
        {
            Line(label, () => get() ? "ON" : "OFF");
            adjust.Add(d => set(!get()));
        }

        void AddButton(string label, Action act)
        {
            Line(label, () => "");
            adjust.Add(d => { if (d == 0) act(); });
        }

        static string Bar(float v)
        {
            int n = Mathf.RoundToInt(v * 10f);
            return new string('|', n) + U.Tint(new string('|', 10 - n), Pal.Dim.A(0.4f)) + "  " + Mathf.RoundToInt(v * 100f) + "%";
        }

        public override void Tick(GameInput inp)
        {
            if (inp.NavLeft) adjust[menu.Sel]?.Invoke(-1);
            if (inp.NavRight) adjust[menu.Sel]?.Invoke(1);
            menu.Tick(inp);
            for (int i = 0; i < values.Count; i++) values[i].text = value[i]();
        }

        public override void OnClose() => SaveSystem.SavePrefs();
    }

    // ======================================================================== CHOICE

    /// <summary>A small menu: talk options, confirmations.</summary>
    public sealed class ChoiceScreen : UIScreen
    {
        readonly string title;
        readonly Color color;
        readonly string[] options;
        readonly Action<int> onPick;
        readonly string art;
        readonly MenuList menu = new MenuList();

        public ChoiceScreen(string title, Color color, string[] options, Action<int> onPick, string art = null)
        {
            this.title = title;
            this.color = color;
            this.options = options;
            this.onPick = onPick;
            this.art = art;
        }

        protected override void Create()
        {
            Dim(0.55f);
            float h = 130f + options.Length * 64f;
            var frame = Screens.Frame(Root, new Vector2(900, h), new Vector2(art != null ? 160f : 0f, -120f));
            if (art != null)
            {
                var sp = CharArt.Get(art, true);
                var port = UIK.Img(Root, sp, Color.white, "portrait");
                float ph = 520f;
                port.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-470f, -120f - h * 0.5f), new Vector2(ph * sp.rect.width / sp.rect.height, ph));
                port.preserveAspect = true;
            }
            var t = UIK.Txt(frame, title, 34, color, TextAnchor.MiddleCenter, FontStyle.Bold, "title");
            t.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(840, 70));
            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                var rt = UIK.Rect(frame, "opt").Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110 - i * 64f), new Vector2(760, 56));
                var bg = UIK.Img(rt, SpriteLib.Rounded, new Color(1, 1, 1, 0.04f), "bg");
                bg.rectTransform.Stretch();
                var l = UIK.Txt(rt, options[i], 30, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Normal, "label");
                l.rectTransform.Stretch();
                menu.Add(rt, () => { Close(); onPick?.Invoke(idx); }, on => { bg.color = on ? color.A(0.22f) : new Color(1, 1, 1, 0.04f); l.color = on ? Color.white : Pal.Dim; });
            }
            menu.OnCancel = () => { Close(); };
            menu.Refresh();
        }

        public override void Tick(GameInput inp) => menu.Tick(inp);
    }

    // ======================================================================== STORY CARDS

    /// <summary>Full-screen saga text, card by card.</summary>
    public sealed class CardsScreen : UIScreen
    {
        readonly string[] cards;
        readonly Action done;
        int idx = -1;
        float t, hold;
        Text text;
        Image rune;

        public CardsScreen(string[] cards, Action done)
        {
            this.cards = cards;
            this.done = done;
        }

        protected override void Create()
        {
            var bg = UIK.Img(Root, SpriteLib.UiWhite, Color.black, "bg");
            bg.rectTransform.Stretch();
            rune = UIK.Img(Root, Runes.Sprite(Runes.Naudiz, true), Pal.Narfi.A(0.1f), "rune");
            rune.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 700));
            text = UIK.Txt(Root, "", 44, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Italic, "text", true);
            text.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 600));
            Screens.Hint(Root, Game.I.Input.Glyph("confirm") + " continue    " + Game.I.Input.Glyph("cancel") + " skip");
            Next();
        }

        void Next()
        {
            idx++;
            if (idx >= cards.Length)
            {
                Close();
                done?.Invoke();
                return;
            }
            text.text = cards[idx];
            t = 0f;
            hold = 3f + cards[idx].Length * 0.045f;
        }

        public override void Tick(GameInput inp)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / 0.9f);
            if (t > hold) a = Mathf.Clamp01(1f - (t - hold) / 0.8f);
            text.SetAlpha(a);
            rune.color = Pal.Narfi.A(0.08f * a);
            rune.rectTransform.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 3f);
            if (t > hold + 0.8f) { Next(); return; }
            if (inp.CancelDown) { idx = cards.Length; Next(); return; }
            if ((inp.ConfirmDown || inp.ClickDown) && t > 0.4f)
            {
                if (t < hold) t = hold;
                else Next();
            }
        }
    }

    // ======================================================================== DEATH / VICTORY

    public sealed class DeathScreen : UIScreen
    {
        readonly RunSummary s;
        readonly Action next;

        public DeathScreen(RunSummary s, Action next)
        {
            this.s = s;
            this.next = next;
        }

        protected override void Create()
        {
            var bg = UIK.Img(Root, SpriteLib.UiWhite, new Color(0.05f, 0.0f, 0.01f, 0.88f), "bg");
            bg.rectTransform.Stretch();
            var vig = UIK.Img(Root, SpriteLib.Vignette, new Color(0.6f, 0f, 0.05f, 0.9f), "vig");
            vig.rectTransform.Stretch(-80, -80, -80, -80);
            var t = UIK.Txt(Root, "SLAIN", 150, Pal.Blood, TextAnchor.MiddleCenter, FontStyle.Bold, "title", true);
            t.rectTransform.Place(new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 200));
            string cause = string.IsNullOrEmpty(s.Cause) ? "the dark" : s.Cause;
            var sub = UIK.Txt(Root, "by " + cause + ", in " + s.Realm, 38, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Italic, "sub");
            sub.rectTransform.Place(new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 60));
            Body(Root, s);
            var cont = UIK.Txt(Root, Game.I.Input.Glyph("confirm") + "  Return to the Pool of Hel", 30, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "cont");
            cont.rectTransform.Place(new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 50));
        }

        public static void Body(RectTransform root, RunSummary s)
        {
            string stats = "Chamber " + s.Depth + "     Foes slain " + s.Kills + "     Time " + U.Clock(s.Time) + "     " + s.Weapon + (s.Heat > 0 ? "     Heat " + s.Heat : "");
            var st = UIK.Txt(root, stats, 28, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "stats");
            st.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 50));
            string carry = "You carry home:   " + U.Tint(s.Embers + " Soul-Embers", Pal.Ember) + "     " + U.Tint(s.Keys + " Grave-Keys", Pal.Key) + "     " + U.Tint(s.Mead + " Mead", Pal.Mead);
            var c = UIK.Txt(root, carry, 32, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "carry");
            c.rectTransform.Place(new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 50));
            if (!string.IsNullOrEmpty(s.Bounty))
            {
                var b = UIK.Txt(root, s.Bounty, 28, Pal.Hel, TextAnchor.MiddleCenter, FontStyle.Italic, "bounty");
                b.rectTransform.Place(new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 50));
            }
            var row = UIK.Rect(root, "boons").Place(new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 64));
            int n = s.Boons.Count;
            for (int i = 0; i < n; i++)
            {
                var img = UIK.Img(row, SpriteLib.GodIcon(s.Boons[i].Def.God), Color.white, "boon");
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - (n - 1) * 0.5f) * 62f, 0f), new Vector2(56, 56));
            }
        }

        public override void Tick(GameInput inp)
        {
            if (Age < 1.2f) return;
            if (inp.ConfirmDown || inp.ClickDown || inp.CancelDown)
            {
                Close();
                next?.Invoke();
            }
        }
    }

    public sealed class VictoryScreen : UIScreen
    {
        readonly RunSummary s;
        readonly Action next;

        public VictoryScreen(RunSummary s, Action next)
        {
            this.s = s;
            this.next = next;
        }

        protected override void Create()
        {
            var bg = UIK.Img(Root, SpriteLib.UiWhite, new Color(0.03f, 0.03f, 0.02f, 0.9f), "bg");
            bg.rectTransform.Stretch();
            var glow = UIK.Img(Root, SpriteLib.UiGlow, Pal.Gold.A(0.18f), "glow");
            glow.rectTransform.Place(new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 700));
            var t = UIK.Txt(Root, "ESCAPED", 150, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, "title", true);
            t.rectTransform.Place(new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 200));
            var sub = UIK.Txt(Root, "You stood beneath the sky of Midgard — for a moment. Escape " + SaveSystem.Data.clears + ".", 34, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Italic, "sub");
            sub.rectTransform.Place(new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 60));
            DeathScreen.Body(Root, s);
            var cont = UIK.Txt(Root, Game.I.Input.Glyph("confirm") + "  Return to Hel's hall", 30, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "cont");
            cont.rectTransform.Place(new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 50));
            Game.I.Audio.Music("ending");
        }

        public override void Tick(GameInput inp)
        {
            if (Age < 1.5f) return;
            if (inp.ConfirmDown || inp.ClickDown || inp.CancelDown)
            {
                Close();
                next?.Invoke();
            }
        }
    }
}
