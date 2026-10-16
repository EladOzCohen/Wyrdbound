using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class PlayerStats
    {
        public float MaxHP = 50f, MoveSpeed = 6.8f, DmgMult = 1f, AttackSpeed = 1f, CritChance = 0f, CritMult = 3f;
        public float DamageReduction, Dodge, SilverMult = 1f, HealMult = 1f, FuryMult = 1f, ShopDiscount, RarityBonus, BossDmg, FirstStrike;
        public float Magnet = 1.4f, DashRecharge = 0.55f, BossHpMult = 1f;
        public int DashCharges = 1, CastMax = 3, Rerolls, Defiance;
    }

    /// <summary>Permanent upgrades carved into the Rune-Stone of Narfi with Soul-Embers.</summary>
    public sealed class RuneDef
    {
        public string Id, Name, Desc;
        public int Rune, MaxRank, KeyCost;
        public int[] Cost;
        public float Per;
        public string Fmt = "int";
        public string Format(float v) => Fmt == "pct" ? Mathf.RoundToInt(v * 100f) + "%" : Mathf.RoundToInt(v).ToString();
    }

    /// <summary>Optional hardships woven by the Norns: the heat system.</summary>
    public sealed class WeaveDef
    {
        public string Id, Name, Desc;
        public int MaxRank, Heat;
    }

    public sealed class KeepsakeDef
    {
        public string Id, Name, From, Desc, Lore;
    }

    public sealed class WeaponDef
    {
        public string Id, Name, Title, Desc;
        public int KeyCost;
    }

    public static class Meta
    {
        public static readonly RuneDef[] Runes =
        {
            new RuneDef { Id = "vigor", Name = "Rune of Vigor", Rune = Wyrdbound.Runes.Uruz, MaxRank = 5, Cost = new[] { 10, 20, 30, 45, 60 }, Per = 5f, Desc = "+{v} Max Health." },
            new RuneDef { Id = "defiance", Name = "Rune of Defiance", Rune = Wyrdbound.Runes.Algiz, MaxRank = 3, Cost = new[] { 30, 80, 160 }, Per = 1f, Desc = "Refuse death {v} time(s) per escape, rising with 40% Health." },
            new RuneDef { Id = "ferocity", Name = "Rune of First Blood", Rune = Wyrdbound.Runes.Tiwaz, MaxRank = 5, Cost = new[] { 15, 25, 35, 50, 70 }, Per = 0.12f, Fmt = "pct", Desc = "Deal +{v} damage to foes at full Health." },
            new RuneDef { Id = "fortune", Name = "Rune of Fortune", Rune = Wyrdbound.Runes.Fehu, MaxRank = 5, Cost = new[] { 10, 15, 25, 35, 50 }, Per = 0.1f, Fmt = "pct", KeyCost = 1, Desc = "+{v} Silver, and begin each escape with more." },
            new RuneDef { Id = "hide", Name = "Rune of Hardened Hide", Rune = Wyrdbound.Runes.Eihwaz, MaxRank = 5, Cost = new[] { 20, 30, 45, 60, 80 }, Per = 0.04f, Fmt = "pct", KeyCost = 1, Desc = "Take {v} less damage." },
            new RuneDef { Id = "swift", Name = "Rune of the Wind", Rune = Wyrdbound.Runes.Raido, MaxRank = 2, Cost = new[] { 40, 120 }, Per = 1f, KeyCost = 2, Desc = "+{v} Dash charge(s)." },
            new RuneDef { Id = "stones", Name = "Rune of Stones", Rune = Wyrdbound.Runes.Naudiz, MaxRank = 2, Cost = new[] { 40, 120 }, Per = 1f, KeyCost = 2, Desc = "+{v} Cast rune(s)." },
            new RuneDef { Id = "favor", Name = "Rune of Favor", Rune = Wyrdbound.Runes.Gebo, MaxRank = 4, Cost = new[] { 30, 50, 80, 110 }, Per = 0.04f, Fmt = "pct", KeyCost = 3, Desc = "+{v} chance for Rare or better blessings." },
            new RuneDef { Id = "norns", Name = "Rune of the Norns", Rune = Wyrdbound.Runes.Jera, MaxRank = 3, Cost = new[] { 50, 90, 150 }, Per = 1f, KeyCost = 3, Desc = "{v} Fate-Thread(s): re-weave a choice of blessings." },
            new RuneDef { Id = "fury", Name = "Rune of Fury", Rune = Wyrdbound.Runes.Sowilo, MaxRank = 3, Cost = new[] { 30, 60, 100 }, Per = 0.15f, Fmt = "pct", KeyCost = 4, Desc = "Build Fury {v} faster." },
        };

        public static readonly WeaveDef[] Weave =
        {
            new WeaveDef { Id = "hardened", Name = "Hardened Dead", MaxRank = 3, Heat = 1, Desc = "Foes have +20% Health per rank." },
            new WeaveDef { Id = "fierce", Name = "Fierce Dead", MaxRank = 3, Heat = 1, Desc = "Foes deal +20% damage per rank." },
            new WeaveDef { Id = "swarm", Name = "Swarming Dead", MaxRank = 2, Heat = 1, Desc = "+20% more foes per rank." },
            new WeaveDef { Id = "jarls", Name = "Rising Jarls", MaxRank = 2, Heat = 1, Desc = "Foes are far more often Armored." },
            new WeaveDef { Id = "wardens", Name = "Wrathful Wardens", MaxRank = 1, Heat = 2, Desc = "Bosses have +25% Health and attack faster." },
            new WeaveDef { Id = "tithe", Name = "Hel's Tithe", MaxRank = 2, Heat = 1, Desc = "Móðguðr's prices rise 40% per rank." },
            new WeaveDef { Id = "thin", Name = "Thinned Favor", MaxRank = 1, Heat = 2, Desc = "Blessings offer one fewer choice." },
            new WeaveDef { Id = "unhealing", Name = "Unhealing Wounds", MaxRank = 2, Heat = 1, Desc = "All healing is 30% weaker per rank." },
            new WeaveDef { Id = "hound", Name = "The Hound's Pursuit", MaxRank = 2, Heat = 2, Desc = "Cross each realm in 8 minutes (6 at rank 2), or Garmr's hunger bleeds you." },
            new WeaveDef { Id = "dark", Name = "Darkened Path", MaxRank = 1, Heat = 1, Desc = "Doors no longer reveal what lies beyond them." },
        };

        public static readonly KeepsakeDef[] Keepsakes =
        {
            new KeepsakeDef { Id = "hel_mask", Name = "Hel's Half-Mask", From = "hel", Desc = "Once per escape, survive a killing blow with 30% Health.", Lore = "She wore it for a thousand years. Now she lends it to the brother who keeps leaving." },
            new KeepsakeDef { Id = "draupnir", Name = "Draupnir's Echo", From = "baldr", Desc = "+25% Silver gained.", Lore = "Every ninth night, the true ring drips eight more. This is a ring the true one dripped." },
            new KeepsakeDef { Id = "linen", Name = "Nanna's Linen", From = "nanna", Desc = "+15% Max Health.", Lore = "She once sent linen from Hel to Frigg. This scrap she kept for someone who still bleeds." },
            new KeepsakeDef { Id = "draught", Name = "Mímir's Draught", From = "mimir", Desc = "+10% chance for Rare or better blessings, and +1 Fate-Thread.", Lore = "A sip from the well that cost Odin an eye. Mímir insists this one is free. Mímir lies." },
            new KeepsakeDef { Id = "tollstone", Name = "Gjallarbrú Toll-Stone", From = "modgud", Desc = "Móðguðr's prices are 20% lower.", Lore = "The bridge only rings under the living. This stone makes it ring for you anyway." },
            new KeepsakeDef { Id = "fang", Name = "Wolf-Brother's Fang", From = "vali", Desc = "+20% damage to Wardens and Armored foes.", Lore = "It was the tooth that killed you. He wants you to have it." },
            new KeepsakeDef { Id = "bowl", Name = "Sigyn's Bowl", From = "sigyn", Desc = "Restore 25% Health on entering each new realm.", Lore = "A chip from the rim of the bowl that catches the serpent's venom. It is still warm." },
            new KeepsakeDef { Id = "thread", Name = "Norn-Thread", From = "norns", Desc = "Your first blessing in each realm is at least Rare.", Lore = "A single strand of your own wyrd. Do not pull it." },
        };

        public static readonly WeaponDef[] Weapons =
        {
            new WeaponDef { Id = "axe", Name = "Bitter Edge", Title = "The Bone-Bitten Axe", KeyCost = 0, Desc = "A grave-axe of Hel's forges. Wide cleaves and a crushing third blow. Special: Cleaving Whirl." },
            new WeaponDef { Id = "spear", Name = "Gjallr", Title = "The Spear of the Drowned", KeyCost = 1, Desc = "Long thrusts; hold Attack to spin. Special: hurl the spear, which returns to your hand." },
            new WeaponDef { Id = "bow", Name = "Ullr's Yew", Title = "The Winter Bow", KeyCost = 3, Desc = "Hold to draw; release at the peak for a Power Shot. Special: Volley." },
            new WeaponDef { Id = "seax", Name = "Fang and Claw", Title = "The Twin Seaxes", KeyCost = 4, Desc = "Relentless strikes, deadlier from behind. Special: a bounding seax that leaps between foes." },
        };

        public static RuneDef Rune(string id) { foreach (var r in Runes) if (r.Id == id) return r; return null; }
        public static WeaveDef WeaveOf(string id) { foreach (var w in Weave) if (w.Id == id) return w; return null; }
        public static KeepsakeDef Keepsake(string id) { foreach (var k in Keepsakes) if (k.Id == id) return k; return null; }
        public static WeaponDef Weapon(string id) { foreach (var w in Weapons) if (w.Id == id) return w; return Weapons[0]; }

        public static int Heat(SaveData d)
        {
            int h = 0;
            foreach (var w in Weave) h += d.Weave(w.Id) * w.Heat;
            return h;
        }

        public static bool RuneUnlocked(SaveData d, RuneDef r) => r.KeyCost == 0 || d.runeUnlocks.Contains(r.Id);

        public static PlayerStats Build(SaveData d, BoonSystem boons)
        {
            var s = new PlayerStats();
            s.MaxHP += 5f * d.Rune("vigor");
            s.Defiance = d.Rune("defiance");
            s.FirstStrike = 0.12f * d.Rune("ferocity");
            s.SilverMult += 0.1f * d.Rune("fortune");
            s.DamageReduction += 0.04f * d.Rune("hide");
            s.DashCharges += d.Rune("swift");
            s.CastMax += d.Rune("stones");
            s.RarityBonus += 0.04f * d.Rune("favor");
            s.Rerolls += d.Rune("norns");
            s.FuryMult += 0.15f * d.Rune("fury");

            switch (d.keepsake)
            {
                case "linen": s.MaxHP *= 1.15f; break;
                case "draupnir": s.SilverMult += 0.25f; break;
                case "draught": s.RarityBonus += 0.1f; s.Rerolls += 1; break;
                case "tollstone": s.ShopDiscount += 0.2f; break;
                case "fang": s.BossDmg += 0.2f; break;
            }

            s.HealMult -= 0.3f * d.Weave("unhealing");
            if (d.Weave("wardens") > 0) s.BossHpMult = 1.25f;

            if (SaveSystem.Prefs.grace) s.DamageReduction += Mathf.Min(0.8f, 0.2f + 0.02f * d.deaths);

            boons?.ApplyStats(s);
            s.MaxHP = Mathf.Round(s.MaxHP);
            s.DamageReduction = Mathf.Min(0.85f, s.DamageReduction);
            s.HealMult = Mathf.Max(0.2f, s.HealMult);
            return s;
        }
    }
}
