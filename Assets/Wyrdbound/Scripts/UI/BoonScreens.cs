using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>A god offers blessings. Choose one; re-weave with a Fate-Thread.</summary>
    public sealed class BoonScreen : UIScreen
    {
        readonly God god;
        List<BoonOffer> offers;
        readonly MenuList menu = new MenuList();
        RectTransform cards;
        Text hint;

        public BoonScreen(God g, List<BoonOffer> offers)
        {
            god = g;
            this.offers = offers;
        }

        protected override void Create()
        {
            Dim(0.72f);
            Color c = Pal.God(god);
            var glow = UIK.Img(Root, SpriteLib.UiGlow, c.A(0.16f), "glow");
            glow.rectTransform.Place(new Vector2(0.3f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 1400));
            string art = god.ToString().ToLower();
            var sp = CharArt.Get(art, true);
            var port = UIK.Img(Root, sp, Color.white, "god");
            port.preserveAspect = true;
            float ph = 760f;
            port.rectTransform.Place(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(60f, 60f), new Vector2(ph * sp.rect.width / sp.rect.height, ph));
            UIK.Header(Root, Pal.GodName(god).ToUpper(), new Vector2(0.62f, 0.9f), Vector2.zero, 64, c);
            var title = UIK.Txt(Root, Pal.GodTitle(god), 26, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic, "title");
            title.rectTransform.Place(new Vector2(0.62f, 0.9f), new Vector2(0.5f, 0.5f), new Vector2(0, -88), new Vector2(1000, 40));
            cards = UIK.Rect(Root, "cards").Place(new Vector2(0.62f, 0.76f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1100, 700));
            hint = Screens.Hint(Root, "");
            BuildCards();
        }

        void BuildCards()
        {
            UIK.Clear(cards);
            menu.Items.Clear();
            menu.Sel = 0;
            for (int i = 0; i < offers.Count; i++)
            {
                var o = offers[i];
                Color rc = Pal.Rarity(o.Rarity);
                string slot = PauseScreen.SlotName(o.Def.Slot);
                string desc = o.Def.Describe(o.Rarity, 1);
                if (o.Replaces != null) desc += "\n" + U.Tint("Replaces " + o.Replaces.Def.Name + " (" + Pal.GodName(o.Replaces.Def.God) + ")", Pal.Danger.Lit(1.2f));
                if (!string.IsNullOrEmpty(o.Def.Flavor)) desc += "\n" + U.Tint(o.Def.Flavor, Pal.Dim);
                var icon = o.Def.IsDuo ? SpriteLib.GodIcon(o.Def.God2) : SpriteLib.GodIcon(o.Def.God);
                var row = Row.Make(cards, new Vector2(0.5f, 1f), new Vector2(0, -i * 226f), new Vector2(1100, 210), icon, U.Tint(o.Def.Name, rc), desc, U.Tint(Pal.RarityName(o.Rarity).ToUpper(), rc) + "\n" + U.Tint(slot, Pal.Dim), 32, 23);
                row.Accent = Pal.God(god);
                int k = i;
                menu.Add(row.Rt, () => Pick(k), row.Highlight);
            }
            menu.Refresh();
            var run = RunState.Current;
            hint.text = Game.I.Input.Glyph("confirm") + " accept the blessing" + (run != null && run.Rerolls > 0 ? "    " + Game.I.Input.Glyph("reroll") + " re-weave (" + run.Rerolls + " Fate-Threads)" : "");
        }

        void Pick(int i)
        {
            var o = offers[i];
            var run = RunState.Current;
            run.Boons.Add(o.Def, o.Rarity);
            Game.I.Audio.Play("boon", 0.9f, 1.1f);
            if (Player.I != null) Vfx.Ring(Player.I.Pos, 2.5f, Pal.God(god), 0.5f);
            Game.I.UI.Toast(o.Def.Name + " — " + Pal.RarityName(o.Rarity), Pal.Rarity(o.Rarity));
            SaveSystem.Data.SetFlag("codex_boon_" + o.Def.Id);
            Close();
        }

        public override void Tick(GameInput inp)
        {
            var run = RunState.Current;
            if (inp.RerollDown && run != null && run.Rerolls > 0)
            {
                run.Rerolls--;
                offers = run.Boons.RollOffers(god, run.Rng, run.BoonChoices, run.Stats.RarityBonus, false);
                if (offers.Count == 0) { Close(); return; }
                Game.I.Audio.Play("chain", 0.6f);
                BuildCards();
                return;
            }
            menu.Tick(inp);
        }
    }

    /// <summary>Iðunn's golden apple: strengthen one blessing.</summary>
    public sealed class AppleScreen : UIScreen
    {
        readonly List<Boon> list;
        readonly MenuList menu = new MenuList();

        public AppleScreen(List<Boon> list) { this.list = list; }

        protected override void Create()
        {
            Dim(0.72f);
            UIK.Header(Root, "IÐUNN'S GOLDEN APPLE", new Vector2(0.5f, 0.88f), Vector2.zero, 60, Pal.Gold);
            var sub = UIK.Txt(Root, "One bite of the apple that keeps the gods young. Strengthen a blessing.", 26, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic, "sub");
            sub.rectTransform.Place(new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(0, -86), new Vector2(1400, 40));
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                var row = Row.Make(Root, new Vector2(0.5f, 0.74f), new Vector2(0, -i * 200f), new Vector2(1100, 186), SpriteLib.GodIcon(b.Def.God),
                    U.Tint(b.Def.Name, Pal.God(b.Def.God)) + U.Tint("   Lv " + b.Level + " → " + (b.Level + 1), Pal.Gold), b.Def.Describe(b.Rarity, b.Level, true), U.Tint(Pal.RarityName(b.Rarity), Pal.Rarity(b.Rarity)));
                row.Accent = Pal.Gold;
                int k = i;
                menu.Add(row.Rt, () => Pick(k), row.Highlight);
            }
            menu.Refresh();
            Screens.Hint(Root, Game.I.Input.Glyph("confirm") + " strengthen");
        }

        void Pick(int i)
        {
            var b = list[i];
            RunState.Current.Boons.LevelUp(b, 1);
            Game.I.Audio.Play("level", 0.9f);
            Game.I.UI.Toast(b.Def.Name + " grows stronger", Pal.Gold);
            Close();
        }

        public override void Tick(GameInput inp) => menu.Tick(inp);
    }
}
