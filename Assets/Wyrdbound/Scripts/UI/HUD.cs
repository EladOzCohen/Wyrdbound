using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wyrdbound
{
    /// <summary>Health, fury, runes, dashes, blessings, currency, boss bar and the interact prompt.</summary>
    public sealed class HUD
    {
        RectTransform root;
        Image hpFill, hpLag, furyFill, furyGlow, bossFill, bossLag, bossArmor;
        Text hpText, silverText, metaText, regionText, promptText, bossName, bossTitle, furyLabel;
        RectTransform bossRoot, promptRoot, runRoot, boonRow;
        readonly List<Image> runes = new List<Image>();
        readonly List<Image> dashes = new List<Image>();
        readonly List<Image> defy = new List<Image>();
        readonly List<Image> boonIcons = new List<Image>();
        float lag, bossLagV;
        int boonVersion = -1;

        public void Build(RectTransform parent)
        {
            root = UIK.Rect(parent, "HUD").Stretch();

            var hpPanel = UIK.Panel(root, "hpPanel");
            hpPanel.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -24), new Vector2(470, 60));
            var hpBg = UIK.Img(hpPanel.transform, SpriteLib.Rounded, new Color(0.12f, 0.03f, 0.04f, 1f), "hpBg");
            hpBg.rectTransform.Stretch(12, 14, 12, 14);
            hpLag = UIK.Img(hpBg.transform, SpriteLib.UiWhite, new Color(1f, 0.85f, 0.6f, 0.7f), "hpLag");
            hpFill = UIK.Img(hpBg.transform, SpriteLib.UiWhite, Pal.Blood, "hpFill");
            foreach (var img in new[] { hpLag, hpFill })
            {
                img.rectTransform.anchorMin = new Vector2(0, 0);
                img.rectTransform.anchorMax = new Vector2(1, 1);
                img.rectTransform.pivot = new Vector2(0, 0.5f);
                img.rectTransform.offsetMin = Vector2.zero;
                img.rectTransform.offsetMax = Vector2.zero;
            }
            hpText = UIK.Txt(hpPanel.transform, "50 / 50", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "hpText");
            hpText.rectTransform.Stretch();

            var furyBg = UIK.Img(root, SpriteLib.Rounded, new Color(0.08f, 0.06f, 0.03f, 0.9f), "furyBg");
            furyBg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -88), new Vector2(300, 14));
            furyFill = UIK.Img(furyBg.transform, SpriteLib.UiWhite, Pal.Fury, "furyFill");
            furyFill.rectTransform.anchorMin = Vector2.zero;
            furyFill.rectTransform.anchorMax = new Vector2(0, 1);
            furyFill.rectTransform.pivot = new Vector2(0, 0.5f);
            furyFill.rectTransform.offsetMin = new Vector2(2, 2);
            furyFill.rectTransform.offsetMax = new Vector2(0, -2);
            furyGlow = UIK.Img(furyBg.transform, SpriteLib.UiGlow, Pal.Fury.A(0f), "furyGlow");
            furyGlow.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
            furyLabel = UIK.Txt(root, "FURY", 18, Pal.Fury, TextAnchor.MiddleLeft, FontStyle.Bold, "furyLabel");
            furyLabel.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(350, -84), new Vector2(300, 22));

            for (int i = 0; i < 6; i++)
            {
                var r = UIK.Img(root, SpriteLib.Icon("cast"), Color.white, "rune" + i);
                r.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(36 + i * 40, -112), new Vector2(38, 38));
                runes.Add(r);
            }
            for (int i = 0; i < 4; i++)
            {
                var d = UIK.Img(root, SpriteLib.UiDiamond, Pal.Narfi, "dash" + i);
                d.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(290 + i * 26, -122), new Vector2(20, 20));
                dashes.Add(d);
            }
            for (int i = 0; i < 4; i++)
            {
                var d = UIK.Img(root, SpriteLib.Icon("defiance"), Color.white, "defy" + i);
                d.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(510 + i * 44, -30), new Vector2(42, 42));
                defy.Add(d);
            }

            runRoot = UIK.Rect(root, "runInfo").Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -24), new Vector2(520, 140));
            var silverIcon = UIK.Img(runRoot, SpriteLib.Icon("silver"), Color.white, "silverIcon");
            silverIcon.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(54, 54));
            silverText = UIK.Txt(runRoot, "0", 34, Pal.Silver, TextAnchor.MiddleRight, FontStyle.Bold, "silver");
            silverText.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -6), new Vector2(300, 44));
            metaText = UIK.Txt(runRoot, "", 22, Pal.Dim, TextAnchor.UpperRight, FontStyle.Normal, "meta");
            metaText.rectTransform.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(520, 80));

            regionText = UIK.Txt(root, "", 22, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Normal, "region");
            regionText.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(900, 34));

            boonRow = UIK.Rect(root, "boons").Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 28), new Vector2(900, 70));

            bossRoot = UIK.Rect(root, "boss").Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -52), new Vector2(980, 120));
            bossName = UIK.Txt(bossRoot, "", 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, "bossName", true);
            bossName.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(980, 48));
            bossTitle = UIK.Txt(bossRoot, "", 20, Pal.Dim, TextAnchor.MiddleCenter, FontStyle.Italic, "bossTitle");
            bossTitle.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(980, 28));
            var bbg = UIK.Img(bossRoot, SpriteLib.Rounded, new Color(0.08f, 0.02f, 0.03f, 0.95f), "bossBg");
            bbg.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -78), new Vector2(900, 24));
            bossLag = UIK.Img(bbg.transform, SpriteLib.UiWhite, new Color(1f, 0.85f, 0.6f, 0.7f), "bossLag");
            bossFill = UIK.Img(bbg.transform, SpriteLib.UiWhite, Pal.Blood, "bossFill");
            bossArmor = UIK.Img(bbg.transform, SpriteLib.UiWhite, Pal.Armor, "bossArmor");
            foreach (var img in new[] { bossLag, bossFill, bossArmor })
            {
                img.rectTransform.anchorMin = Vector2.zero;
                img.rectTransform.anchorMax = Vector2.one;
                img.rectTransform.pivot = new Vector2(0, 0.5f);
                img.rectTransform.offsetMin = new Vector2(3, 3);
                img.rectTransform.offsetMax = new Vector2(-3, -3);
            }
            bossArmor.rectTransform.offsetMin = new Vector2(3, 14);

            promptRoot = UIK.Rect(root, "prompt").Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(900, 56));
            var pbg = UIK.Img(promptRoot, SpriteLib.Rounded, new Color(0.02f, 0.02f, 0.03f, 0.75f), "promptBg");
            pbg.rectTransform.Stretch();
            promptText = UIK.Txt(promptRoot, "", 28, Pal.Text, TextAnchor.MiddleCenter, FontStyle.Normal, "promptText");
            promptText.rectTransform.Stretch(12, 0, 12, 0);
        }

        static void SetFill(Image img, float f)
        {
            var rt = img.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(f), rt.anchorMax.y);
        }

        public void Tick(float dt)
        {
            var g = Game.I;
            bool show = g.Mode == GameMode.Run || g.Mode == GameMode.Hub;
            root.gameObject.SetActive(show && !g.UI.Modal);
            if (!show) return;
            var p = Player.I;
            if (p == null) return;
            var run = RunState.Current;
            bool inRun = g.Mode == GameMode.Run && run != null;

            float f = p.MaxHP > 0f ? p.HP / p.MaxHP : 0f;
            lag = lag > f ? Mathf.MoveTowards(lag, f, dt * 0.6f) : f;
            SetFill(hpFill, f);
            SetFill(hpLag, lag);
            hpText.text = Mathf.CeilToInt(p.HP) + " / " + Mathf.RoundToInt(p.MaxHP);
            hpFill.color = p.St.Poison > 0f ? Pal.Venom.Lit(0.8f) : Pal.Blood;

            furyFill.transform.parent.gameObject.SetActive(inRun);
            furyLabel.gameObject.SetActive(inRun);
            if (inRun)
            {
                SetFill(furyFill, p.Fury / 100f);
                bool full = p.Fury >= 100f && run.Boons.HasCall;
                furyGlow.color = Pal.Fury.A(full ? 0.6f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f) : 0f);
                furyLabel.text = full ? "FURY  " + g.Input.Glyph("call") + " " + Pal.GodName(run.Boons.SlotGod(BoonSlot.Call)) : "FURY";
            }

            int castMax = p.S.CastMax;
            for (int i = 0; i < runes.Count; i++)
            {
                runes[i].gameObject.SetActive(inRun && i < castMax);
                runes[i].color = i < p.CastAmmo ? Color.white : new Color(1, 1, 1, 0.2f);
            }
            for (int i = 0; i < dashes.Count; i++)
            {
                dashes[i].gameObject.SetActive(i < p.S.DashCharges && p.S.DashCharges > 1);
                dashes[i].color = i < p.DashCharges ? Pal.Narfi : Pal.Narfi.A(0.2f);
            }
            int dl = inRun ? run.DefianceLeft : 0;
            for (int i = 0; i < defy.Count; i++) defy[i].gameObject.SetActive(i < dl);

            if (inRun)
            {
                silverText.text = run.Silver.ToString();
                silverText.transform.parent.gameObject.SetActive(true);
                string meta = "";
                if (run.Embers > 0) meta += U.Tint("Embers " + run.Embers, Pal.Ember) + "   ";
                if (run.Keys > 0) meta += U.Tint("Keys " + run.Keys, Pal.Key) + "   ";
                if (run.Mead > 0) meta += U.Tint("Mead " + run.Mead, Pal.Mead);
                if (run.Rerolls > 0) meta += "\n" + U.Tint("Fate-Threads " + run.Rerolls, Pal.Hel);
                metaText.text = meta;
                string reg = run.Room != null && run.Room.Kind == RoomKind.Surface ? "MIDGARD" : run.Biome.Name.ToUpper() + "  ·  CHAMBER " + (run.Depth + 1);
                reg += "   " + U.Clock(run.Time);
                if (run.RegionLimit > 0f)
                {
                    float left = run.RegionLimit - run.RegionTime;
                    reg += "   " + U.Tint(left > 0f ? "Garmr hunts in " + U.Clock(left) : "GARMR HUNTS", left > 60f ? Pal.Dim : Pal.Danger);
                }
                regionText.text = reg;
            }
            else
            {
                var d = SaveSystem.Data;
                silverText.text = "";
                metaText.text = U.Tint("Soul-Embers " + d.embers, Pal.Ember) + "    " + U.Tint("Grave-Keys " + d.keys, Pal.Key) + "    " + U.Tint("Mead " + d.mead, Pal.Mead);
                silverText.transform.parent.GetChild(0).gameObject.SetActive(false);
                regionText.text = "ÉLJÚÐNIR  ·  HEL'S HALL";
            }
            if (inRun) silverText.transform.parent.GetChild(0).gameObject.SetActive(true);

            int version = inRun ? run.Boons.Owned.Count * 100 + LevelSum(run) : -2;
            if (version != boonVersion)
            {
                boonVersion = version;
                foreach (var img in boonIcons) Object.Destroy(img.gameObject);
                boonIcons.Clear();
                if (inRun)
                {
                    int i = 0;
                    foreach (var b in run.Boons.Owned)
                    {
                        var img = UIK.Img(boonRow, SpriteLib.GodIcon(b.Def.God), Color.white, "boon");
                        float sz = b.Def.Slot == BoonSlot.Passive ? 40f : 56f;
                        img.rectTransform.Place(Vector2.zero, Vector2.zero, new Vector2(i * 60f, b.Def.Slot == BoonSlot.Passive ? 8f : 0f), new Vector2(sz, sz));
                        if (b.Level > 1)
                        {
                            var lv = UIK.Txt(img.transform, b.Level.ToString(), 18, Pal.Gold, TextAnchor.LowerRight, FontStyle.Bold, "lv");
                            lv.rectTransform.Stretch();
                        }
                        boonIcons.Add(img);
                        i++;
                    }
                }
            }

            var boss = Boss.Current;
            bool bossOn = inRun && boss != null && !boss.Dead && !boss.Spawning;
            bossRoot.gameObject.SetActive(bossOn);
            if (bossOn)
            {
                bossName.text = boss.Def.Name.ToUpper();
                bossTitle.text = boss.Title;
                float bf = boss.HP / boss.MaxHP;
                bossLagV = bossLagV > bf ? Mathf.MoveTowards(bossLagV, bf, dt * 0.3f) : bf;
                SetFill(bossFill, bf);
                SetFill(bossLag, bossLagV);
                SetFill(bossArmor, boss.MaxArmor > 0f ? boss.Armor / boss.MaxArmor : 0f);
                bossFill.color = boss.Phase >= 3 ? Pal.Hel : Pal.Blood;
            }

            var focus = p.Focus;
            bool prompt = focus != null && focus.Prompt != null && !p.Dead && p.Controllable;
            promptRoot.gameObject.SetActive(prompt);
            if (prompt) promptText.text = U.Tint(g.Input.Glyph("interact"), Pal.Gold) + "  " + focus.Prompt();
        }

        static int LevelSum(RunState run)
        {
            int s = 0;
            foreach (var b in run.Boons.Owned) s += b.Level + (int)b.Rarity * 7;
            return s;
        }
    }
}
