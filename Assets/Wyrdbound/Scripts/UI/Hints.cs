using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>
    /// One-time tips for newcomers, shown at the moment they matter (first fight, first full Fury, first shop,
    /// out of runes, first death). Each is remembered in the save, so it appears only once per saga.
    /// </summary>
    public static class Hints
    {
        static RectTransform panel;
        static Text text;
        static float showT;
        const float Life = 8f;

        static bool Seen(string id) => SaveSystem.Data == null || SaveSystem.Data.Flag("hint_" + id);

        /// <summary>Show a tip once ever (by id).</summary>
        public static void Once(string id, string message)
        {
            if (Seen(id)) return;
            SaveSystem.Data.SetFlag("hint_" + id);
            Show(message);
        }

        public static void Show(string message)
        {
            if (Game.I == null || Game.I.UI == null) return;
            if (panel == null)
            {
                panel = UIK.Rect(Game.I.UI.HudLayer, "hint").Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(1100, 110));
                var bg = UIK.Img(panel, SpriteLib.Panel, Color.white, "bg");
                bg.rectTransform.Stretch();
                var icon = UIK.Txt(panel, "?", 46, Pal.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, "icon", true);
                icon.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22, 0), new Vector2(60, 80));
                text = UIK.Txt(panel, "", 26, Pal.Text, TextAnchor.MiddleLeft, FontStyle.Normal, "text");
                text.rectTransform.Stretch(90, 10, 28, 10);
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            text.text = message;
            panel.sizeDelta = new Vector2(1100, Mathf.Max(96f, text.preferredHeight + 34f));
            panel.gameObject.SetActive(true);
            showT = Life;
            Game.I.Audio.Play("ui_open", 0.35f, 1.2f);
        }

        static string Key(string action) => U.Tint(Game.I.Input.Glyph(action), Pal.Gold);

        public static void Tick()
        {
            var g = Game.I;
            if (panel != null && panel.gameObject.activeSelf)
            {
                if (!g.UI.Modal) showT -= Time.unscaledDeltaTime;
                var cg = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = Mathf.Clamp01(showT / 0.6f) * (g.UI.Modal ? 0f : 1f);
                if (showT <= 0f) panel.gameObject.SetActive(false);
            }
            if (g.UI.Modal || g.Transitioning || SaveSystem.Data == null || g.GetComponent<AutoTest>() != null) return;
            var p = g.Player;
            if (p == null || !p.gameObject.activeInHierarchy || p.Dead) return;
            var run = RunState.Current;

            if (g.Mode == GameMode.Hub && SaveSystem.Data.deaths >= 1)
                Once("after_death", "You died, and woke in Hel's hall. You keep " + U.Tint("Soul-Embers", Pal.Ember) + ", " + U.Tint("Grave-Keys", Pal.Gold) + " and " + U.Tint("Mead", Pal.Mead) +
                    ". Spend them here, talk to everyone, then take the gate to try again. " + Key("help") + " opens the guide.");

            if (run == null || g.Mode != GameMode.Run || run.Room == null) return;
            if (run.Room.Kind == RoomKind.Combat)
                Once("first_fight", "Aim with the mouse and " + Key("attack") + " to attack where the gold reticle points. " + Key("dash") + " dashes, " +
                    Key("side") + " sidesteps. Red floor shapes show where a foe will strike. " + Key("help") + " shows every control.");
            if (run.Room.Kind == RoomKind.Shop)
                Once("first_shop", "A shop. Walk up to an item (or point at it) to read what it does, then " + Key("interact") + " to buy it with silver.");
            if (p.Fury >= 100f)
            {
                if (run.Boons.HasCall) Once("fury_call", "Your Fury is full! Press " + Key("call") + " to Call your god's power.");
                else Once("fury_nocall", "Your Fury bar is full, but no god answers yet. Take a " + U.Tint("Call", Pal.Gold) + " blessing from a god, then press " + Key("call") + " when Fury is full.");
            }
            if (p.CastAmmo <= 0) Once("no_runes", "Out of rune-stones. Walk over the runes you threw (or kill the foe they are stuck in) to get them back.");
        }
    }
}
