using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class RoomPlan
    {
        public RoomKind Kind;
        public Reward Reward;
        public God God;
        public int Seed;
        public string Npc;

        public string Label()
        {
            switch (Kind)
            {
                case RoomKind.Shop: return "Móðguðr's Waystation";
                case RoomKind.Rest: return "Spring of Hvergelmir";
                case RoomKind.Story: return "A Familiar Shade";
                case RoomKind.Boss: return "The Warden's Lair";
                case RoomKind.Passage: return "The Passage Upward";
                case RoomKind.Surface: return "The Surface";
                case RoomKind.MiniBoss: return "A Jarl of the Dead";
            }
            switch (Reward)
            {
                case Reward.Boon: return "Blessing of " + Pal.GodName(God);
                case Reward.Apple: return "Iðunn's Golden Apple";
                case Reward.Silver: return "Hel-Silver";
                case Reward.Embers: return "Soul-Embers";
                case Reward.Key: return "Grave-Key";
                case Reward.Mead: return "Mead of the Dead";
                case Reward.Salve: return "Eir's Salve";
                case Reward.Ratatoskr: return "Ratatoskr's Gossip";
            }
            return "Chamber";
        }
    }

    /// <summary>Everything about the current escape attempt.</summary>
    public sealed class RunState
    {
        public static RunState Current;

        public readonly int Seed;
        public readonly Rng Rng;
        public readonly BoonSystem Boons = new BoonSystem();
        public PlayerStats Stats = new PlayerStats();
        public string Weapon;
        public int Region, RoomInRegion, Depth;
        public int Silver;
        public int Embers, Keys, Mead;
        public int Kills;
        public float Time;
        public float RegionTime;
        public int Rerolls;
        public int Heat;
        public bool MaskUsed;
        public int DefianceLeft;
        public bool ThreadUsedThisRegion;
        public bool StoryThisRegion;
        public bool ShopThisRegion;
        public bool MiniThisRegion;

        public void EnterRegion()
        {
            ThreadUsedThisRegion = StoryThisRegion = ShopThisRegion = MiniThisRegion = false;
            RegionTime = 0f;
        }
        public int BoonsSinceStart;
        public RoomPlan Room;
        public string LastHitBy = "";
        public readonly List<string> GodsMet = new List<string>();
        public bool Won;

        public Biome Biome => Biomes.Run[Mathf.Clamp(Region, 0, Biomes.Run.Length - 1)];

        public RunState(int seed, string weapon)
        {
            Seed = seed;
            Rng = new Rng(seed);
            Weapon = weapon;
            Heat = Meta.Heat(SaveSystem.Data);
            Stats = Meta.Build(SaveSystem.Data, Boons);
            Rerolls = Stats.Rerolls;
            DefianceLeft = Stats.Defiance;
            Silver = 15 * SaveSystem.Data.Rune("fortune");
            if (SaveSystem.Data.keepsake == "tollstone") Silver += 20;
        }

        public void RecalcStats()
        {
            var old = Stats;
            Stats = Meta.Build(SaveSystem.Data, Boons);
            var p = Player.I;
            if (p == null) return;
            float delta = Stats.MaxHP - old.MaxHP;
            p.MaxHP = Stats.MaxHP;
            if (delta > 0f) p.HP += delta;
            p.HP = Mathf.Min(p.HP, p.MaxHP);
            p.OnStatsChanged();
        }

        public int AddSilver(int amount, bool applyMult = true)
        {
            int n = applyMult ? Mathf.RoundToInt(amount * Stats.SilverMult) : amount;
            Silver += n;
            return n;
        }

        public int Price(int basePrice)
        {
            float m = 1f + 0.4f * SaveSystem.Data.Weave("tithe") - Stats.ShopDiscount;
            return Mathf.Max(1, Mathf.RoundToInt(basePrice * Mathf.Max(0.3f, m)));
        }

        public float EnemyHpMult => (1f + 0.2f * SaveSystem.Data.Weave("hardened")) * (1f + 0.04f * Depth);
        public float EnemyDmgMult => (1f + 0.2f * SaveSystem.Data.Weave("fierce")) * (1f + 0.012f * Depth);
        public float EliteChance => 0.04f + Region * 0.06f + Depth * 0.004f + 0.15f * SaveSystem.Data.Weave("jarls");

        public float RegionLimit
        {
            get
            {
                int r = SaveSystem.Data.Weave("hound");
                return r <= 0 ? 0f : (r == 1 ? 480f : 360f);
            }
        }

        public int BoonChoices => SaveSystem.Data.Weave("thin") > 0 ? 2 : 3;

        public bool FirstBoonMinRare()
        {
            if (SaveSystem.Data.keepsake != "thread" || ThreadUsedThisRegion) return false;
            ThreadUsedThisRegion = true;
            return true;
        }
    }
}
