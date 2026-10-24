using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>
    /// The guide for players who have never played Hades or a roguelike: every key (keyboard and gamepad),
    /// what Attack / Special / Cast / Dash / Sidestep / Call and Fury actually are, and how dying makes you
    /// stronger. Opened from the title, the pause menu, with [H] / F1 anywhere, and once on the first visit
    /// to Hel's hall.
    /// </summary>
    public sealed class HowToPlayScreen : UIScreen
    {
        static readonly string[] TabNames = { "CONTROLS", "COMBAT", "BLESSINGS", "GROWING STRONGER" };
        readonly List<Text> tabs = new List<Text>();
        readonly List<RectTransform> tabRects = new List<RectTransform>();
        RectTransform page;
        Text left, right;
        int tab;

        static string K(string s) => U.Tint(s, Pal.Gold);
        static string H(string s) => "<size=34>" + U.Tint(s, Pal.Gold) + "</size>";
        static string D(string s) => U.Tint(s, Pal.Dim.Lit(1.35f));

        protected override void Create()
        {
            Dim(0.93f);
            UIK.Header(Root, "HOW TO PLAY", new Vector2(0.5f, 0.92f), Vector2.zero, 56, Pal.Gold);
            for (int i = 0; i < TabNames.Length; i++)
            {
                var rt = UIK.Rect(Root, "tab" + i).Place(new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.5f), new Vector2((i - 1.5f) * 340f, 0f), new Vector2(320, 56));
                var bg = UIK.Img(rt, SpriteLib.Panel, Color.white, "bg");
                bg.rectTransform.Stretch();
                var t = UIK.Txt(rt, TabNames[i], 26, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Bold, "t");
                t.rectTransform.Stretch(8, 0, 8, 0);
                tabs.Add(t);
                tabRects.Add(rt);
            }
            page = UIK.Rect(Root, "page").Place(new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1640, 680));
            var pbg = UIK.Panel(page, "pagebg");
            pbg.rectTransform.Stretch();
            left = UIK.Txt(page, "", 26, Pal.Text, TextAnchor.UpperLeft, FontStyle.Normal, "left");
            left.rectTransform.anchorMin = new Vector2(0f, 0f); left.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            left.rectTransform.offsetMin = new Vector2(48, 30); left.rectTransform.offsetMax = new Vector2(-24, -34);
            right = UIK.Txt(page, "", 26, Pal.Text, TextAnchor.UpperLeft, FontStyle.Normal, "right");
            right.rectTransform.anchorMin = new Vector2(0.5f, 0f); right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.offsetMin = new Vector2(24, 30); right.rectTransform.offsetMax = new Vector2(-48, -34);
            foreach (var t in new[] { left, right })
            {
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.lineSpacing = 1.12f;
            }
            var hint = UIK.Txt(Root, "", 24, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "hint");
            hint.rectTransform.Place(new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 40));
            var inp = Game.I.Input;
            hint.text = inp.UsingPad ? "◀ ▶  change page     (B)  close" : "◀ ▶ or click a tab  change page     Esc  close     Open this guide any time with  [H]";
            Show(0);
        }

        public void ShowTab(int i) => Show(i);

        void Show(int i)
        {
            tab = (i + TabNames.Length) % TabNames.Length;
            for (int k = 0; k < tabs.Count; k++)
            {
                tabs[k].color = k == tab ? Pal.Gold : Pal.Dim;
                tabRects[k].localScale = k == tab ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
            }
            switch (tab)
            {
                case 0: Controls(); break;
                case 1: Combat(); break;
                case 2: Blessings(); break;
                default: Growth(); break;
            }
        }

        void Controls()
        {
            left.text =
                H("Keyboard & mouse") + "\n\n" +
                K("W A S D") + "  move\n" +
                K("Mouse") + "  aim (the gold reticle)\n" +
                K("Left click") + " or " + K("J") + "  Attack\n" +
                K("Right click") + " or " + K("K") + "  Special attack\n" +
                K("Q") + " or " + K("middle click") + "  Cast a rune-stone\n" +
                K("Space") + " or " + K("Shift") + "  Dash\n" +
                K("C") + " or " + K("Ctrl") + " or " + K("mouse side button") + "  Sidestep\n" +
                K("F") + "  Call a god (when Fury is full)\n" +
                K("E") + "  Interact: talk, open, buy, use doors\n" +
                K("R") + "  Re-weave (re-roll) a blessing choice\n" +
                K("Esc") + "  Pause menu     " + K("H") + "  this guide";
            right.text =
                H("Gamepad") + "\n\n" +
                K("Left stick") + "  move\n" +
                K("Right stick") + "  aim (otherwise attacks lock on to nearby foes)\n" +
                K("X") + "  Attack\n" +
                K("Y") + "  Special attack\n" +
                K("RT") + "  Cast a rune-stone\n" +
                K("A") + "  Dash\n" +
                K("RB") + "  Sidestep\n" +
                K("LT") + " / " + K("LB") + "  Call a god\n" +
                K("B") + "  Interact\n" +
                K("Back") + "  Re-weave a blessing choice\n" +
                K("Start") + "  Pause menu (the guide is in there too)\n\n" +
                D("Menus: arrows / stick to move, Enter / A to choose, Esc / B to go back. The mouse works in every menu.");
        }

        void Combat()
        {
            left.text =
                H("Your moves") + "\n\n" +
                K("Attack") + " — your weapon's normal strike. Press it again quickly for a combo of up to three hits.\n\n" +
                K("Special") + " — a second, different move for each weapon (the axe spins, the spear throws itself and comes back, the bow fires a volley, the seax is thrown). Try it with every weapon.\n\n" +
                K("Cast") + " — throw a rune-stone that sticks in a foe and hurts it. You only carry a few: walk over a fallen rune (or kill the foe holding it) to pick it back up.\n\n" +
                K("Dash") + " — a fast dash. You cannot be hurt while dashing. You have a limited number of charges (the pips under your health) that refill on their own. Attack right after a dash for a stronger dash-strike.";
            right.text =
                K("Sidestep") + " — a short, very fast hop to the left or right of where you aim, without losing your position. Also untouchable, with its own quick cooldown, so it does not use dash charges.\n\n" +
                H("Fury and the Call") + "\n\n" +
                K("Fury") + " is the orange bar under your health. It fills when you deal damage and when you get hit.\n\n" +
                K("Call") + " — when the bar is full, press " + K("F") + " to summon a god's power: a huge burst that can turn a fight. You need a " + K("Call blessing") + " from a god first (they are offered like other blessings). Without one, nobody answers.\n\n" +
                D("Red shapes on the floor warn you where a foe is about to strike. Step out of them, or dash through.");
        }

        void Blessings()
        {
            left.text =
                H("Blessings (boons)") + "\n\n" +
                "Clearing a room can reward you with a " + K("blessing") + ": a god offers three powers and you keep one for the rest of the escape. The symbol above each door shows what that room will give.\n\n" +
                "Blessings attach to a " + K("slot") + ": your Attack, Special, Cast, Dash, Call, or a passive bonus. A new blessing for a filled slot replaces the old one.\n\n" +
                K("Rarity") + ": Common, Rare, Epic, Heroic — rarer means stronger. " + K("Duo") + " blessings combine two gods and need a blessing from each first.\n\n" +
                K("Golden Apples") + " upgrade a blessing you already own. " + K("Fate-Threads") + " let you re-roll a choice (" + K("R") + ").";
            right.text =
                H("The gods") + "\n\n" +
                K("Thor") + " — lightning that leaps between foes.\n" +
                K("Freyja") + " — Enthrall and Charm: weakened foes fight for you.\n" +
                K("Týr") + " — Verdict: marked foes take a delayed, heavy blow.\n" +
                K("Skaði") + " — Frostbite slows; Shatter breaks frozen foes.\n" +
                K("Odin") + " — critical hits and Raven's Mark.\n" +
                K("Njörðr") + " — tides that knock foes into walls.\n" +
                K("Ratatoskr") + " — the squirrel's tricks: speed, dashes, discounts.\n\n" +
                D("Mix gods that work together: freeze foes with Skaði, then break them with heavy hits.");
        }

        void Growth()
        {
            left.text =
                H("Dying is part of the story") + "\n\n" +
                "Every escape is a fresh climb through four realms of the dead. When you die, you wake up in " + K("Hel's hall") + ". The rooms, foes and rewards change every time, the story moves forward, and you keep part of what you found.\n\n" +
                H("What you keep, and what you lose") + "\n\n" +
                K("Lost at death") + ": blessings and " + K("silver") + " (spend silver in shops during the escape).\n" +
                K("Kept forever") + ": " + K("Soul-Embers") + ", " + K("Grave-Keys") + " and " + K("Mead") + ".";
            right.text =
                H("Spending it in Hel's hall") + "\n\n" +
                K("Rune-Stone of Narfi") + " — spend Soul-Embers on permanent upgrades: more health, an extra dash, more damage, and Death Defiance (a second life per escape).\n\n" +
                K("Hel's Armory") + " — spend Grave-Keys to unlock new weapons.\n\n" +
                K("Mead") + " — give it to the people of the hall to earn their " + K("Keepsakes") + " (wear one for a bonus on your next escape).\n\n" +
                K("Odin's Grace") + " (Settings) — damage reduction that grows each time you die, if you want an easier time.\n\n" +
                D("Talk to everyone after each death: the story continues there.");
        }

        public override void Tick(GameInput inp)
        {
            if (inp.CancelDown || inp.PauseDown || (inp.HelpDown && Age > 0.2f)) { Close(); return; }
            if (inp.NavRight) { Show(tab + 1); Game.I.Audio.Play("ui_move", 0.5f); }
            if (inp.NavLeft) { Show(tab - 1); Game.I.Audio.Play("ui_move", 0.5f); }
            if (inp.ClickDown)
                for (int i = 0; i < tabRects.Count; i++)
                    if (UIK.Hover(tabRects[i], inp.MouseScreen)) { Show(i); Game.I.Audio.Play("ui_move", 0.5f); }
        }
    }
}
