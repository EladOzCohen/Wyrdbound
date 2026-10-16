using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>Base for list screens in the hall: header, currency line, rows rebuilt on every change.</summary>
    public abstract class ListScreen : UIScreen
    {
        protected readonly MenuList menu = new MenuList();
        protected RectTransform list;
        protected Text wallet, hint;
        protected float rowH = 84f;

        protected abstract string Title { get; }
        protected virtual Color Accent => Pal.Gold;
        protected abstract void Fill();

        protected override void Create()
        {
            Dim(0.88f);
            UIK.Header(Root, Title, new Vector2(0.5f, 0.92f), Vector2.zero, 56, Accent);
            wallet = UIK.Txt(Root, "", 26, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Normal, "wallet");
            wallet.rectTransform.Place(new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(1500, 40));
            list = UIK.Rect(Root, "list").Place(new Vector2(0.5f, 0.82f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1400, 860));
            hint = Screens.Hint(Root, "");
            menu.OnCancel = Close;
            Rebuild();
        }

        protected void Rebuild()
        {
            int sel = menu.Sel;
            UIK.Clear(list);
            menu.Items.Clear();
            Fill();
            menu.Sel = Mathf.Clamp(sel, 0, Mathf.Max(0, menu.Items.Count - 1));
            menu.Refresh();
            var d = SaveSystem.Data;
            wallet.text = U.Tint("Soul-Embers " + d.embers, Pal.Ember) + "      " + U.Tint("Grave-Keys " + d.keys, Pal.Key) + "      " + U.Tint("Mead " + d.mead, Pal.Mead);
        }

        protected Row AddRow(Sprite icon, string title, string desc, string right, System.Action confirm)
        {
            int i = menu.Items.Count;
            var row = Row.Make(list, new Vector2(0.5f, 1f), new Vector2(0, -i * (rowH + 6f)), new Vector2(1400, rowH), icon, title, desc, right, 28, 21);
            row.Accent = Accent;
            menu.Add(row.Rt, confirm, row.Highlight);
            return row;
        }

        protected void Deny(string msg)
        {
            Game.I.Audio.Play("ui_deny");
            Game.I.UI.Toast(msg, Pal.Danger);
        }

        public override void Tick(GameInput inp) => menu.Tick(inp);
        public override void OnClose() => SaveSystem.Save();
    }

    // ------------------------------------------------------------------ Rune-Stone

    public sealed class RuneStoneScreen : ListScreen
    {
        protected override string Title => "THE RUNE-STONE OF NARFI";
        protected override Color Accent => Pal.Narfi;

        protected override void Fill()
        {
            rowH = 78f;
            var d = SaveSystem.Data;
            foreach (var r in Meta.Runes)
            {
                var def = r;
                int rank = d.Rune(r.Id);
                bool unlocked = Meta.RuneUnlocked(d, r);
                string value = r.Desc.Replace("{v}", U.Tint(r.Format(r.Per * Mathf.Max(1, rank)), Pal.Gold));
                string right;
                if (!unlocked) right = U.Tint("Unlock: " + r.KeyCost + " Grave-Key" + (r.KeyCost > 1 ? "s" : ""), d.keys >= r.KeyCost ? Pal.Key : Pal.Danger);
                else if (rank >= r.MaxRank) right = U.Tint("MASTERED", Pal.Narfi);
                else right = U.Tint("Carve: " + r.Cost[rank] + " Embers", d.embers >= r.Cost[rank] ? Pal.Ember : Pal.Danger);
                string title = (unlocked ? r.Name : U.Tint(r.Name, Pal.Dim)) + "   " + Screens.Pips(rank, r.MaxRank, Pal.Narfi);
                AddRow(SpriteLib.Icon("rune:" + r.Rune), title, unlocked ? value : U.Tint("Sealed. " + value, Pal.Dim), right, () => Buy(def));
            }
            hint.text = Game.I.Input.Glyph("confirm") + " carve / unseal    " + Game.I.Input.Glyph("cancel") + " leave";
        }

        void Buy(RuneDef r)
        {
            var d = SaveSystem.Data;
            if (!Meta.RuneUnlocked(d, r))
            {
                if (d.keys < r.KeyCost) { Deny("Not enough Grave-Keys"); return; }
                d.keys -= r.KeyCost;
                d.runeUnlocks.Add(r.Id);
                Game.I.Audio.Play("chain");
                Rebuild();
                return;
            }
            int rank = d.Rune(r.Id);
            if (rank >= r.MaxRank) { Deny("This rune is fully carved"); return; }
            int cost = r.Cost[rank];
            if (d.embers < cost) { Deny("Not enough Soul-Embers"); return; }
            d.embers -= cost;
            d.SetRune(r.Id, rank + 1);
            Game.I.Audio.Play("level", 0.8f);
            Rebuild();
        }
    }

    // ------------------------------------------------------------------ Armory

    public sealed class WeaponScreen : ListScreen
    {
        protected override string Title => "HEL'S ARMORY";

        protected override void Fill()
        {
            rowH = 150f;
            var d = SaveSystem.Data;
            foreach (var w in Meta.Weapons)
            {
                var def = w;
                bool owned = d.HasWeapon(w.Id);
                string right = d.weapon == w.Id ? U.Tint("EQUIPPED", Pal.Narfi) : owned ? U.Tint("Take up", Pal.Text) : U.Tint("Unseal: " + w.KeyCost + " Keys", d.keys >= w.KeyCost ? Pal.Key : Pal.Danger);
                string clears = d.Count("clear_" + w.Id) > 0 ? U.Tint("   Escapes: " + d.Count("clear_" + w.Id), Pal.Dim) : "";
                AddRow(SpriteLib.Icon("w_" + w.Id), (owned ? w.Name : U.Tint(w.Name, Pal.Dim)) + U.Tint("   " + w.Title, Pal.Dim) + clears, w.Desc, right, () => Pick(def));
            }
            hint.text = Game.I.Input.Glyph("confirm") + " take up / unseal    " + Game.I.Input.Glyph("cancel") + " leave";
        }

        void Pick(WeaponDef w)
        {
            var d = SaveSystem.Data;
            if (!d.HasWeapon(w.Id))
            {
                if (d.keys < w.KeyCost) { Deny("Not enough Grave-Keys"); return; }
                d.keys -= w.KeyCost;
                d.weapons.Add(w.Id);
                Game.I.Audio.Play("chain");
            }
            d.weapon = w.Id;
            Player.I?.Equip(w.Id);
            Game.I.Audio.Play("swing_heavy", 0.6f);
            Rebuild();
        }
    }

    // ------------------------------------------------------------------ Weave of Wyrd

    public sealed class WeaveScreen : ListScreen
    {
        protected override string Title => "THE WEAVE OF WYRD";
        protected override Color Accent => Pal.Hel;

        protected override void Fill()
        {
            rowH = 74f;
            var d = SaveSystem.Data;
            foreach (var w in Meta.Weave)
            {
                int r = d.Weave(w.Id);
                AddRow(SpriteLib.Icon("weave"), w.Name + "   " + Screens.Pips(r, w.MaxRank, Pal.Hel), w.Desc, U.Tint("+" + w.Heat + " heat / rank", Pal.Dim), null);
            }
            int heat = Meta.Heat(d);
            hint.text = "Left / Right to tighten or loosen a thread      " + U.Tint("HEAT " + heat, heat > 0 ? Pal.Hel : Pal.Dim) +
                        "      Highest woven and escaped: " + Mathf.Max(0, d.bestHeat) + "      " + Game.I.Input.Glyph("cancel") + " leave";
        }

        public override void Tick(GameInput inp)
        {
            int dir = inp.NavRight ? 1 : inp.NavLeft ? -1 : 0;
            if (dir != 0 && menu.Sel < Meta.Weave.Length)
            {
                var w = Meta.Weave[menu.Sel];
                var d = SaveSystem.Data;
                int r = Mathf.Clamp(d.Weave(w.Id) + dir, 0, w.MaxRank);
                if (r != d.Weave(w.Id))
                {
                    d.SetWeave(w.Id, r);
                    Game.I.Audio.Play(dir > 0 ? "chain" : "ui_back", 0.6f);
                    Rebuild();
                }
            }
            base.Tick(inp);
        }
    }

    // ------------------------------------------------------------------ Keepsakes

    public sealed class KeepsakeScreen : ListScreen
    {
        protected override string Title => "HOARD OF MEMORIES";

        protected override void Fill()
        {
            rowH = 92f;
            var d = SaveSystem.Data;
            foreach (var k in Meta.Keepsakes)
            {
                var def = k;
                bool owned = d.keepsakes.Contains(k.Id);
                string from = Story.Who(k.From).Name;
                if (owned)
                    AddRow(SpriteLib.Icon("keepsake"), k.Name + U.Tint("   from " + from, Pal.Dim), k.Desc + "\n" + U.Tint(k.Lore, Pal.Dim), d.keepsake == k.Id ? U.Tint("WORN", Pal.Narfi) : "", () => Toggle(def));
                else
                    AddRow(SpriteLib.Icon("lock"), U.Tint("???", Pal.Dim), U.Tint("Share Mead of the Dead with " + from + ".", Pal.Dim), "", () => Deny("Not yet earned"));
            }
            hint.text = Game.I.Input.Glyph("confirm") + " wear / remove (one at a time)    " + Game.I.Input.Glyph("cancel") + " leave";
        }

        void Toggle(KeepsakeDef k)
        {
            var d = SaveSystem.Data;
            d.keepsake = d.keepsake == k.Id ? "" : k.Id;
            Game.I.Audio.Play("pickup", 0.6f);
            Rebuild();
        }
    }

    // ------------------------------------------------------------------ Codex

    public sealed class CodexScreen : UIScreen
    {
        int cat;
        int sel;
        Text catText, body, entryName;
        RectTransform listRoot;
        readonly List<Text> lines = new List<Text>();
        List<CodexEntry> entries = new List<CodexEntry>();

        protected override void Create()
        {
            Dim(0.92f);
            UIK.Header(Root, "MÍMIR'S LORE", new Vector2(0.5f, 0.93f), Vector2.zero, 56, U.Hex("7ec8ff"));
            catText = UIK.Txt(Root, "", 30, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Bold, "cats");
            catText.rectTransform.Place(new Vector2(0.5f, 0.93f), new Vector2(0.5f, 0.5f), new Vector2(0, -84), new Vector2(1600, 44));
            var left = Screens.Frame(Root, new Vector2(560, 780), new Vector2(-560, -60));
            listRoot = UIK.Rect(left, "list").Stretch(20, 20, 20, 20);
            for (int i = 0; i < 15; i++)
            {
                var t = UIK.Txt(listRoot, "", 26, Pal.Dim, TextAnchor.MiddleLeft, FontStyle.Normal, "e" + i);
                t.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16, -i * 49f), new Vector2(500, 46));
                lines.Add(t);
            }
            var right = Screens.Frame(Root, new Vector2(1020, 780), new Vector2(250, -60));
            entryName = UIK.Txt(right, "", 40, U.Hex("7ec8ff"), TextAnchor.UpperLeft, FontStyle.Bold, "name", true);
            entryName.rectTransform.Stretch(40, 0, 40, 30);
            body = UIK.Txt(right, "", 27, Pal.Text, TextAnchor.UpperLeft, FontStyle.Normal, "body");
            body.rectTransform.Stretch(40, 30, 40, 100);
            body.lineSpacing = 1.2f;
            Screens.Hint(Root, "Left / Right: category    Up / Down: entry    " + Game.I.Input.Glyph("cancel") + " close");
            Refresh();
        }

        void Refresh()
        {
            entries = Codex.Entries(Codex.Categories[cat]);
            sel = Mathf.Clamp(sel, 0, Mathf.Max(0, entries.Count - 1));
            string cs = "";
            for (int i = 0; i < Codex.Categories.Length; i++)
                cs += (i == cat ? U.Tint("[ " + Codex.Categories[i] + " ]", U.Hex("7ec8ff")) : U.Tint(Codex.Categories[i], Pal.Dim)) + "     ";
            catText.text = cs;
            int start = Mathf.Clamp(sel - 7, 0, Mathf.Max(0, entries.Count - lines.Count));
            for (int i = 0; i < lines.Count; i++)
            {
                int k = start + i;
                if (k >= entries.Count) { lines[i].text = ""; continue; }
                var e = entries[k];
                bool open = Codex.Unlocked(e);
                lines[i].text = (k == sel ? "> " : "   ") + (open ? e.Name : "???");
                lines[i].color = k == sel ? Pal.Gold : open ? Pal.Text : Pal.Dim.A(0.6f);
            }
            if (entries.Count == 0) { entryName.text = ""; body.text = ""; return; }
            var cur = entries[sel];
            if (Codex.Unlocked(cur))
            {
                entryName.text = cur.Name;
                body.text = Codex.Text(cur);
            }
            else
            {
                entryName.text = "???";
                body.text = U.Tint("Mímir says nothing yet. \"Live a little more. Or die a little more. Either way, come back.\"", Pal.Dim);
            }
        }

        public override void Tick(GameInput inp)
        {
            bool ch = false;
            if (inp.NavLeft) { cat = (cat + Codex.Categories.Length - 1) % Codex.Categories.Length; sel = 0; ch = true; }
            if (inp.NavRight) { cat = (cat + 1) % Codex.Categories.Length; sel = 0; ch = true; }
            if (inp.NavUp && entries.Count > 0) { sel = (sel + entries.Count - 1) % entries.Count; ch = true; }
            if (inp.NavDown && entries.Count > 0) { sel = (sel + 1) % entries.Count; ch = true; }
            if (ch) { Game.I.Audio.Play("ui_move", 0.5f); Refresh(); }
            if (inp.CancelDown || inp.PauseDown) Close();
        }
    }
}
