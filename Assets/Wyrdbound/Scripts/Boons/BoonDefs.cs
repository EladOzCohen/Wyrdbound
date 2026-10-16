using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class BoonDef
    {
        public string Id, Name, Desc, Flavor;
        public God God, God2;
        public BoonSlot Slot;
        public float[] V;
        public float PerLevel;
        public string Fmt = "int";
        public string[] ReqA, ReqB;

        public bool IsDuo => God2 != God.None;

        public float Value(Rarity r, int level)
        {
            int ri = Mathf.Clamp((int)r, 0, V.Length - 1);
            return V[ri] + PerLevel * Mathf.Max(0, level - 1);
        }

        public string Format(float v)
        {
            switch (Fmt)
            {
                case "pct": return Mathf.RoundToInt(v * 100f) + "%";
                case "sec": return v.ToString("0.#") + "s";
                case "none": return "";
            }
            return Mathf.RoundToInt(v).ToString();
        }

        public string Describe(Rarity r, int level, bool showNext = false)
        {
            string val = U.Tint(Format(Value(r, level)), Pal.Gold);
            if (showNext && PerLevel > 0f) val += U.Tint(" → " + Format(Value(r, level + 1)), Pal.Heal);
            return Desc.Replace("{v}", val);
        }
    }

    /// <summary>Every blessing the gods can grant Narfi.</summary>
    public static class BoonDefs
    {
        public static readonly List<BoonDef> All = new List<BoonDef>();
        static readonly Dictionary<string, BoonDef> byId = new Dictionary<string, BoonDef>();

        static void D(string id, God g, BoonSlot slot, string name, string desc, float[] v, float per, string fmt = "int", string flavor = "")
        {
            var d = new BoonDef { Id = id, God = g, God2 = God.None, Slot = slot, Name = name, Desc = desc, V = v, PerLevel = per, Fmt = fmt, Flavor = flavor };
            All.Add(d);
            byId[id] = d;
        }

        static void Duo(string id, God a, God b, string name, string desc, string[] reqA, string[] reqB, string flavor)
        {
            var d = new BoonDef { Id = id, God = a, God2 = b, Slot = BoonSlot.Passive, Name = name, Desc = desc, V = new[] { 1f }, PerLevel = 0f, Fmt = "none", ReqA = reqA, ReqB = reqB, Flavor = flavor };
            All.Add(d);
            byId[id] = d;
        }

        public static BoonDef Get(string id) => byId.TryGetValue(id, out var d) ? d : null;

        static readonly string[] ThorCore = { "thor_attack", "thor_special", "thor_cast", "thor_dash" };
        static readonly string[] SkadiCore = { "skadi_attack", "skadi_special", "skadi_cast", "skadi_dash" };
        static readonly string[] TyrCore = { "tyr_attack", "tyr_special" };
        static readonly string[] OdinCore = { "odin_attack", "odin_special", "odin_cast", "odin_lore" };
        static readonly string[] FreyjaCore = { "freyja_attack", "freyja_special", "freyja_cast", "freyja_dash" };
        static readonly string[] NjordCore = { "njord_attack", "njord_special", "njord_dash", "njord_riptide" };

        static BoonDefs()
        {
            // ---------------------------------------------------------------- THOR
            D("thor_attack", God.Thor, BoonSlot.Attack, "Hammerfall Strike", "Your Attack calls down a thunderbolt on foes struck, dealing {v} damage.", new[] { 12f, 15f, 18f, 21f }, 3f, "int", "\"Strike hard, little wolf. The sky will strike harder.\"");
            D("thor_special", God.Thor, BoonSlot.Special, "Mjölnir's Echo", "Your Special releases chain-lightning that leaps between up to 4 foes, dealing {v} damage.", new[] { 16f, 20f, 24f, 28f }, 4f);
            D("thor_cast", God.Thor, BoonSlot.Cast, "Thunderstone", "Your Cast rune calls 3 thunderbolts where it lands, each dealing {v} damage.", new[] { 45f, 55f, 65f, 75f }, 8f);
            D("thor_dash", God.Thor, BoonSlot.Dash, "Storm-Step", "Your Dash strikes the nearest foe with lightning for {v} damage.", new[] { 18f, 22f, 26f, 30f }, 4f);
            D("thor_call", God.Thor, BoonSlot.Call, "Wrath of the Thunderer", "Call: hurl 12 thunderbolts among nearby foes, each dealing {v} damage.", new[] { 50f, 60f, 70f, 80f }, 10f);
            D("thor_megin", God.Thor, BoonSlot.Passive, "Megingjörð", "Your lightning deals +{v} damage.", new[] { 0.2f, 0.3f, 0.4f, 0.5f }, 0.1f, "pct");
            D("thor_resolve", God.Thor, BoonSlot.Passive, "Thunderous Resolve", "When struck, lightning falls on your attacker for {v} damage.", new[] { 50f, 60f, 70f, 80f }, 10f);
            D("thor_bane", God.Thor, BoonSlot.Passive, "Serpent's Bane", "Deal +{v} damage to Armored foes and Wardens.", new[] { 0.2f, 0.25f, 0.3f, 0.35f }, 0.05f, "pct");

            // ---------------------------------------------------------------- SKADI
            D("skadi_attack", God.Skadi, BoonSlot.Attack, "Winter's Kiss", "Your Attack deals +{v} damage and inflicts Frostbite.", new[] { 0.4f, 0.5f, 0.6f, 0.7f }, 0.1f, "pct", "\"The mountain does not hurry. It simply arrives.\"");
            D("skadi_special", God.Skadi, BoonSlot.Special, "Avalanche Edge", "Your Special deals +{v} damage and inflicts 2 Frostbite.", new[] { 0.6f, 0.7f, 0.8f, 0.9f }, 0.15f, "pct");
            D("skadi_cast", God.Skadi, BoonSlot.Cast, "Glacial Rune", "Your Cast rune bursts in a frost nova: {v} damage, 4 Frostbite, and foes freeze for 1s.", new[] { 40f, 50f, 60f, 70f }, 10f);
            D("skadi_dash", God.Skadi, BoonSlot.Dash, "Hunter's Trail", "Your Dash leaves a trail of rime that deals {v} damage and inflicts Frostbite.", new[] { 6f, 8f, 10f, 12f }, 2f);
            D("skadi_call", God.Skadi, BoonSlot.Call, "Fimbulwinter", "Call: freeze every foe for {v} and inflict 5 Frostbite.", new[] { 3f, 3.5f, 4f, 4.5f }, 0.5f, "sec");
            D("skadi_shatter", God.Skadi, BoonSlot.Passive, "Avalanche", "Shatter (10 Frostbite) deals +{v} damage.", new[] { 40f, 55f, 70f, 85f }, 15f);
            D("skadi_patience", God.Skadi, BoonSlot.Passive, "Huntress's Patience", "Deal +{v} damage to Frostbitten foes.", new[] { 0.15f, 0.2f, 0.25f, 0.3f }, 0.05f, "pct");
            D("skadi_snowblind", God.Skadi, BoonSlot.Passive, "Snowblind", "Frostbitten foes deal {v} less damage.", new[] { 0.15f, 0.2f, 0.25f, 0.3f }, 0.03f, "pct");

            // ---------------------------------------------------------------- TYR
            D("tyr_attack", God.Tyr, BoonSlot.Attack, "Oath of Blades", "Your Attack inflicts Verdict: after a moment, the foe suffers {v} damage.", new[] { 50f, 60f, 70f, 80f }, 10f, "int", "\"An oath is a blade you hold by the edge.\"");
            D("tyr_special", God.Tyr, BoonSlot.Special, "Judgment Edge", "Your Special inflicts Verdict for {v} damage.", new[] { 60f, 75f, 90f, 105f }, 12f);
            D("tyr_cast", God.Tyr, BoonSlot.Cast, "Blade Rift", "Your Cast rune tears open a blade rift, dealing {v} damage repeatedly for 3s.", new[] { 12f, 15f, 18f, 21f }, 3f);
            D("tyr_dash", God.Tyr, BoonSlot.Dash, "Einherjar's Step", "Your Dash leaves a blade vortex that deals {v} damage repeatedly.", new[] { 10f, 12f, 14f, 16f }, 2f);
            D("tyr_call", God.Tyr, BoonSlot.Call, "Ragnarök's Vanguard", "Call: become a storm of blades for 5s, dealing {v} damage repeatedly around you.", new[] { 18f, 22f, 26f, 30f }, 4f);
            D("tyr_hand", God.Tyr, BoonSlot.Passive, "Hand in the Wolf's Mouth", "Lose 20% Max Health. Deal +{v} damage.", new[] { 0.25f, 0.3f, 0.35f, 0.4f }, 0.05f, "pct");
            D("tyr_law", God.Tyr, BoonSlot.Passive, "Lawgiver's Retort", "When struck, inflict Verdict on all nearby foes for {v} damage.", new[] { 60f, 75f, 90f, 105f }, 15f);
            D("tyr_swift", God.Tyr, BoonSlot.Passive, "Swift Justice", "Verdict resolves 30% faster and deals +{v} damage.", new[] { 0.2f, 0.3f, 0.4f, 0.5f }, 0.1f, "pct");

            // ---------------------------------------------------------------- ODIN
            D("odin_attack", God.Odin, BoonSlot.Attack, "Raven's Eye", "Your Attack deals +{v} damage and has +15% Critical chance.", new[] { 0.2f, 0.25f, 0.3f, 0.35f }, 0.05f, "pct", "\"I gave an eye to see. You need only aim.\"");
            D("odin_special", God.Odin, BoonSlot.Special, "Gungnir's Mark", "Your Special deals +{v} damage and has +20% Critical chance.", new[] { 0.3f, 0.4f, 0.5f, 0.6f }, 0.1f, "pct");
            D("odin_cast", God.Odin, BoonSlot.Cast, "Huginn's Flight", "Your Cast becomes a raven that hunts foes, dealing {v} damage with +30% Critical chance.", new[] { 55f, 65f, 75f, 85f }, 10f);
            D("odin_dash", God.Odin, BoonSlot.Dash, "Muninn's Wings", "Your Dash brands foes near where you land with Raven's Mark. Marked foes suffer a Critical with +{v} damage.", new[] { 0.3f, 0.45f, 0.6f, 0.75f }, 0.15f, "pct");
            D("odin_call", God.Odin, BoonSlot.Call, "Hliðskjálf's Gaze", "Call: time slows for your foes for {v}, and all your hits are Critical.", new[] { 4f, 4.5f, 5f, 5.5f }, 0.5f, "sec");
            D("odin_eye", God.Odin, BoonSlot.Passive, "The Given Eye", "Critical hits deal +{v} damage.", new[] { 0.3f, 0.45f, 0.6f, 0.75f }, 0.15f, "pct");
            D("odin_lore", God.Odin, BoonSlot.Passive, "Runes of the Hanged", "+{v} Critical chance on all damage.", new[] { 0.05f, 0.07f, 0.09f, 0.11f }, 0.02f, "pct");
            D("odin_ravens", God.Odin, BoonSlot.Passive, "Huginn and Muninn", "Every 3s, a raven strikes a random foe for {v} damage and Marks it.", new[] { 20f, 25f, 30f, 35f }, 5f);

            // ---------------------------------------------------------------- FREYJA
            D("freyja_attack", God.Freyja, BoonSlot.Attack, "Seiðr Touch", "Your Attack deals +{v} damage and inflicts Enthralled (foes deal 30% less damage).", new[] { 0.4f, 0.5f, 0.6f, 0.7f }, 0.1f, "pct", "\"Half the slain are mine. I choose well.\"");
            D("freyja_special", God.Freyja, BoonSlot.Special, "Falcon's Dive", "Your Special deals +{v} damage and inflicts Enthralled.", new[] { 0.6f, 0.7f, 0.8f, 0.9f }, 0.15f, "pct");
            D("freyja_cast", God.Freyja, BoonSlot.Cast, "Heartfire Rune", "Your Cast rune bursts with longing, dealing {v} damage and Charming foes for 4s.", new[] { 40f, 50f, 60f, 70f }, 10f);
            D("freyja_dash", God.Freyja, BoonSlot.Dash, "Feathered Cloak", "Your Dash ends in a burst of passion, dealing {v} damage and inflicting Enthralled.", new[] { 20f, 25f, 30f, 35f }, 5f);
            D("freyja_call", God.Freyja, BoonSlot.Call, "Valkyrie's Choice", "Call: Charm every foe for {v} and restore 15 Health.", new[] { 5f, 6f, 7f, 8f }, 1f, "sec");
            D("freyja_neck", God.Freyja, BoonSlot.Passive, "Brísingamen", "Restore {v} Health after each cleared chamber.", new[] { 6f, 8f, 10f, 12f }, 2f);
            D("freyja_folk", God.Freyja, BoonSlot.Passive, "Fólkvangr's Claim", "Slain foes have a 25% chance to restore {v} Health.", new[] { 2f, 3f, 4f, 5f }, 1f);
            D("freyja_wiles", God.Freyja, BoonSlot.Passive, "Wiles of Vanaheim", "Hits against Enthralled foes have a {v} chance to Charm them.", new[] { 0.08f, 0.12f, 0.16f, 0.2f }, 0.03f, "pct");

            // ---------------------------------------------------------------- NJORD
            D("njord_attack", God.Njord, BoonSlot.Attack, "Tidal Blow", "Your Attack deals +{v} damage and knocks foes away.", new[] { 0.3f, 0.4f, 0.5f, 0.6f }, 0.1f, "pct", "\"Every shore was once a battle the sea won.\"");
            D("njord_special", God.Njord, BoonSlot.Special, "Breaking Wave", "Your Special deals +{v} damage and hurls foes far away.", new[] { 0.5f, 0.6f, 0.7f, 0.8f }, 0.1f, "pct");
            D("njord_cast", God.Njord, BoonSlot.Cast, "Maelstrom Rune", "Your Cast rune becomes a whirlpool that drags foes in, then bursts for {v} damage.", new[] { 50f, 60f, 70f, 80f }, 10f);
            D("njord_dash", God.Njord, BoonSlot.Dash, "Riptide Dash", "Your Dash creates a wave that pushes foes and deals {v} damage.", new[] { 20f, 25f, 30f, 35f }, 5f);
            D("njord_call", God.Njord, BoonSlot.Call, "Wrath of the Deep", "Call: a great wave crashes out from you, dealing {v} damage and hurling foes.", new[] { 100f, 120f, 140f, 160f }, 20f);
            D("njord_calm", God.Njord, BoonSlot.Passive, "Calm Seas", "+{v} Max Health.", new[] { 25f, 30f, 35f, 40f }, 5f);
            D("njord_riptide", God.Njord, BoonSlot.Passive, "Breakers", "Foes slammed into walls or stone take {v} damage.", new[] { 40f, 55f, 70f, 85f }, 15f);
            D("njord_wealth", God.Njord, BoonSlot.Passive, "Nóatún's Bounty", "Gain {v} Silver now, and +20% Silver thereafter.", new[] { 60f, 80f, 100f, 120f }, 20f);

            // ---------------------------------------------------------------- RATATOSKR
            D("rat_paws", God.Ratatoskr, BoonSlot.Passive, "Quick Paws", "+{v} movement speed.", new[] { 0.1f, 0.15f, 0.2f, 0.25f }, 0.05f, "pct", "\"Up the trunk, down the trunk, nobody ever thanks the squirrel!\"");
            D("rat_teeth", God.Ratatoskr, BoonSlot.Passive, "Gnashing Teeth", "+{v} Attack and Special speed.", new[] { 0.1f, 0.15f, 0.2f, 0.25f }, 0.05f, "pct");
            D("rat_climb", God.Ratatoskr, BoonSlot.Passive, "Tree-Runner", "+1 Dash charge, and Dashes recover {v} faster.", new[] { 0.1f, 0.2f, 0.3f, 0.4f }, 0.1f, "pct");
            D("rat_gossip", God.Ratatoskr, BoonSlot.Passive, "Idle Gossip", "Móðguðr's prices are {v} lower.", new[] { 0.15f, 0.2f, 0.25f, 0.3f }, 0.05f, "pct");
            D("rat_acorn", God.Ratatoskr, BoonSlot.Passive, "Acorn Cache", "+1 Cast rune, and runes fly to you from {v} paces away.", new[] { 3f, 4f, 5f, 6f }, 1f);
            D("rat_slip", God.Ratatoskr, BoonSlot.Passive, "Slippery Tail", "{v} chance to dodge any damage.", new[] { 0.08f, 0.12f, 0.16f, 0.2f }, 0.03f, "pct");

            // ---------------------------------------------------------------- DUOS
            Duo("duo_thor_skadi", God.Thor, God.Skadi, "Ice-Thunder", "Your lightning inflicts 2 Frostbite, and deals +50% damage to Frostbitten foes.", ThorCore, SkadiCore, "\"Thunder in the high snows. Even my father feared that sound.\"");
            Duo("duo_thor_odin", God.Thor, God.Odin, "Allfather's Storm", "Your lightning can be Critical, with +15% Critical chance.", ThorCore, OdinCore, "\"My son's anger, my aim.\"");
            Duo("duo_tyr_freyja", God.Tyr, God.Freyja, "Love and War", "Verdict inflicts Enthralled, and deals +40% damage to Enthralled foes.", TyrCore, FreyjaCore, "\"She chooses who falls. He decides why.\"");
            Duo("duo_skadi_njord", God.Skadi, God.Njord, "Frozen Tide", "Foes you slam gain 3 Frostbite, and Shatter strikes twice as wide.", SkadiCore, NjordCore, "\"A marriage of mountain and sea. It did not last. This did.\"");
            Duo("duo_odin_freyja", God.Odin, God.Freyja, "Völva's Vision", "+20% Critical chance against Enthralled or Charmed foes.", OdinCore, FreyjaCore, "\"She taught me seiðr. I taught her nothing she did not already know.\"");
            Duo("duo_njord_tyr", God.Njord, God.Tyr, "Drowning Verdict", "Foes you knock back suffer Verdict for 60 damage.", NjordCore, TyrCore, "\"The sea also keeps its oaths.\"");
            Duo("duo_thor_tyr", God.Thor, God.Tyr, "Thunder-Oath", "When Verdict resolves, lightning strikes the foe for 40 more damage.", ThorCore, TyrCore, "\"Swear it on the hammer, then.\"");
            Duo("duo_skadi_freyja", God.Skadi, God.Freyja, "Cold Embrace", "Charming a foe inflicts 4 Frostbite; Charm lasts 2s longer.", SkadiCore, FreyjaCore, "\"Love on the mountain is a cold and patient thing.\"");
        }
    }
}
