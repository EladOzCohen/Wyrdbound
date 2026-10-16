using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public enum MoveKind { Chase, Kite, Teleport, Flyer, Stationary, Burrow, Strafe }
    public enum AtkKind { Swipe, Lunge, Charge, Slam, Leap, Shoot, Ring, Lob, Explode, Summon, Custom }

    public sealed class AttackDef
    {
        public AtkKind Kind;
        public string Tag = "";
        public float Range = 2f, MinRange, Windup = 0.6f, Active = 0.2f, Recover = 0.6f, Cooldown = 1.5f;
        public float Damage = 8f, Size = 1.5f, Arc = 90f, Dist = 4f, Speed = 10f, Spread, Knock = 4f, ZoneDps, Weight = 1f;
        public float TrackUntil = 0.65f, RepeatGap = 0.3f;
        public int Count = 1, Repeat = 1, MinPhase = 1;
        public string Proj = "orb", Summon;
        public Color ProjColor = U.Hex("ff6b6b");
    }

    public sealed class EnemyDef
    {
        public string Id, Name, Art, Lore = "";
        public float HP = 40f, Speed = 2.5f, Radius = 0.42f, Mass = 1f, Armor, PrefDist = 1.5f, Scale = 1f;
        public float Cost = 1f;
        public bool Flying, Boss, Shielded, ExplodeOnDeath;
        public MoveKind Move = MoveKind.Chase;
        public AttackDef[] Attacks = new AttackDef[0];
        public Color Blood = U.Hex("9fb4c8");
        public string Title = "";
    }

    /// <summary>The dead, the beasts and the Wardens of Helheim.</summary>
    public static class EnemyDefs
    {
        static readonly Dictionary<string, EnemyDef> defs = new Dictionary<string, EnemyDef>();

        /// <summary>Global balance knobs (tuned from the automated play-test).</summary>
        public const float HpScale = 1.6f, BossHpScale = 2.2f;

        public static EnemyDef Get(string id) => defs.TryGetValue(id, out var d) ? d : null;
        public static IEnumerable<EnemyDef> All => defs.Values;

        static void Add(EnemyDef d) => defs[d.Id] = d;

        static EnemyDefs()
        {
            Color teal = U.Hex("6ff3ff"), venom = Pal.Venom, hel = Pal.Hel, ice = Pal.Frost, blood = Pal.Blood;

            // ------------------------------------------------------------- Niflhel
            Add(new EnemyDef
            {
                Id = "draugr", Name = "Draugr", Art = "draugr", HP = 45f, Speed = 2.6f, PrefDist = 1.4f,
                Lore = "The oathless dead do not lie still in Niflhel. They remember the shape of a fight, if nothing else.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Swipe, Range = 2.1f, Size = 2.1f, Arc = 100f, Windup = 0.65f, Recover = 0.7f, Cooldown = 1.2f, Damage = 8f } }
            });
            Add(new EnemyDef
            {
                Id = "draugr_archer", Name = "Draugr Bowman", Art = "draugr_archer", HP = 32f, Speed = 2.4f, Move = MoveKind.Kite, PrefDist = 6.5f,
                Lore = "Even dead, a hunter keeps his eye. These ones keep little else.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Shoot, Range = 11f, Windup = 0.85f, Recover = 0.5f, Cooldown = 2.1f, Damage = 7f, Speed = 11f, Proj = "arrow" } }
            });
            Add(new EnemyDef
            {
                Id = "bone_rat", Name = "Bone-Rat", Art = "bone_rat", HP = 14f, Speed = 5.2f, Radius = 0.3f, PrefDist = 0.9f, Cost = 0.4f, Mass = 0.6f, Blood = Pal.Bone,
                Lore = "They gnaw the roots of the world in miniature, practising.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Lunge, Range = 2.8f, Dist = 2.6f, Size = 0.5f, Windup = 0.4f, Active = 0.22f, Recover = 0.5f, Cooldown = 1.0f, Damage = 4f, Knock = 2f } }
            });
            Add(new EnemyDef
            {
                Id = "wraith", Name = "Mist-Wraith", Art = "wraith", HP = 38f, Speed = 2f, Move = MoveKind.Teleport, PrefDist = 6f, Flying = true, Blood = teal,
                Lore = "Breath of the drowned, given spite and a direction.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Shoot, Range = 11f, Count = 3, Spread = 30f, Windup = 0.75f, Recover = 0.5f, Cooldown = 2.3f, Damage = 7f, Speed = 8f, Proj = "orb", ProjColor = teal } }
            });
            Add(new EnemyDef
            {
                Id = "thrall", Name = "Hel-Thrall", Art = "thrall", HP = 85f, Speed = 2.1f, Mass = 3f, Shielded = true, PrefDist = 1.6f, Radius = 0.5f, Cost = 2f,
                Lore = "Hel's household guard. Its shield turns aside anything that comes at it honestly. Come at it dishonestly.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Slam, Range = 1.9f, Size = 2.2f, Windup = 0.9f, Active = 0.15f, Recover = 0.9f, Cooldown = 2f, Damage = 13f, Knock = 8f } }
            });
            Add(new EnemyDef
            {
                Id = "bloat", Name = "Corpse-Bloat", Art = "bloat", HP = 30f, Speed = 1.7f, PrefDist = 1.2f, ExplodeOnDeath = true, Radius = 0.5f, Cost = 1.2f, Blood = venom,
                Lore = "What the serpents of Náströnd would not eat. It resents this.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Explode, Range = 1.7f, Size = 2.4f, Windup = 1.0f, Damage = 16f, Knock = 9f } }
            });
            Add(new EnemyDef
            {
                Id = "grave_jarl", Name = "Grave-Jarl", Title = "Hrafnkell the Unburied", Art = "grave_jarl", HP = 520f, Armor = 160f, Speed = 2.4f, Mass = 8f, Radius = 0.8f, PrefDist = 2f, Cost = 8f,
                Lore = "A chieftain buried without his ship. He has been looking for it, and for whoever took it, for three hundred years.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Slam, Range = 2.6f, Size = 3.2f, Windup = 0.9f, Active = 0.15f, Recover = 0.8f, Cooldown = 2.4f, Damage = 18f, Knock = 10f },
                    new AttackDef { Kind = AtkKind.Charge, Range = 9f, MinRange = 3f, Dist = 8f, Size = 1f, Windup = 0.8f, Active = 0.45f, Recover = 0.7f, Cooldown = 4f, Damage = 16f, Knock = 9f },
                    new AttackDef { Kind = AtkKind.Summon, Range = 99f, Windup = 1f, Recover = 0.6f, Cooldown = 11f, Summon = "draugr", Count = 2, Weight = 0.6f },
                    new AttackDef { Kind = AtkKind.Ring, Range = 99f, Windup = 0.8f, Recover = 0.6f, Cooldown = 6f, Count = 12, Speed = 7f, Damage = 8f, Proj = "orb", ProjColor = teal },
                }
            });

            // ------------------------------------------------------------- Náströnd
            Add(new EnemyDef
            {
                Id = "serpent", Name = "Serpent-Spawn", Art = "serpent", HP = 50f, Speed = 3f, PrefDist = 2.6f, Blood = venom,
                Lore = "Children of Níðhöggr, weaned on oathbreakers.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Lunge, Range = 4.6f, Dist = 4.5f, Size = 0.6f, Windup = 0.55f, Active = 0.28f, Recover = 0.6f, Cooldown = 1.6f, Damage = 10f } }
            });
            Add(new EnemyDef
            {
                Id = "spitter", Name = "Corpse-Spitter", Art = "spitter", HP = 48f, Speed = 1.8f, Move = MoveKind.Kite, PrefDist = 6f, Blood = venom, Cost = 1.3f,
                Lore = "A murderer of Náströnd. The venom that drips on the faithless has soaked all the way through him.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Lob, Range = 10f, Size = 1.6f, Windup = 0.8f, Recover = 0.6f, Cooldown = 2.6f, Damage = 9f, ZoneDps = 8f, Proj = "glob" } }
            });
            Add(new EnemyDef
            {
                Id = "raven", Name = "Corpse-Raven", Art = "raven", HP = 22f, Speed = 4.2f, Move = MoveKind.Flyer, PrefDist = 4f, Flying = true, Radius = 0.35f, Cost = 0.8f, Blood = U.Hex("2a2440"),
                Lore = "Not Odin's. Odin's ravens come back with news. These come back with eyes.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Charge, Range = 7f, Dist = 6f, Size = 0.6f, Windup = 0.6f, Active = 0.35f, Recover = 0.6f, Cooldown = 2.2f, Damage = 8f } }
            });
            Add(new EnemyDef
            {
                Id = "larva", Name = "Níð-Larva", Art = "larva", HP = 55f, Speed = 3.2f, Move = MoveKind.Burrow, PrefDist = 1.4f, Cost = 1.4f, Blood = U.Hex("c8b48a"),
                Lore = "It will grow into something that gnaws Yggdrasil. Kill it while it is merely gnawing you.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Ring, Range = 2.4f, Windup = 0.6f, Recover = 0.8f, Cooldown = 2f, Count = 8, Speed = 6f, Damage = 7f, Proj = "bone" },
                    new AttackDef { Kind = AtkKind.Slam, Range = 1.8f, Size = 1.8f, Windup = 0.5f, Recover = 0.7f, Cooldown = 2f, Damage = 10f },
                }
            });
            Add(new EnemyDef
            {
                Id = "oathbreaker", Name = "Oathbreaker", Art = "oathbreaker", HP = 110f, Speed = 2.5f, PrefDist = 1.6f, Mass = 2f, Radius = 0.5f, Cost = 2.2f, Blood = venom,
                Lore = "He swore on Týr's hand and broke it. Týr remembers. So does the shore.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Swipe, Range = 2.3f, Size = 2.3f, Arc = 120f, Windup = 0.55f, Recover = 0.6f, Cooldown = 1.6f, Damage = 12f, Repeat = 2 },
                    new AttackDef { Kind = AtkKind.Charge, Range = 8f, MinRange = 3f, Dist = 7f, Size = 0.8f, Windup = 0.9f, Active = 0.4f, Recover = 0.7f, Cooldown = 4f, Damage = 14f },
                }
            });
            Add(new EnemyDef
            {
                Id = "weaver", Name = "Serpent-Weaver", Title = "Gríðr of the Woven Hall", Art = "weaver", HP = 600f, Armor = 150f, Speed = 2.2f, Move = MoveKind.Kite, PrefDist = 5f, Mass = 8f, Radius = 0.75f, Cost = 8f, Blood = venom,
                Lore = "She wove the hall of Náströnd from living serpents, and she is not finished.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Lob, Range = 12f, Count = 3, Size = 1.7f, Windup = 0.8f, Recover = 0.5f, Cooldown = 3f, Damage = 10f, ZoneDps = 9f, Proj = "glob" },
                    new AttackDef { Kind = AtkKind.Summon, Range = 99f, Windup = 1f, Recover = 0.5f, Cooldown = 10f, Summon = "serpent", Count = 2, Weight = 0.6f },
                    new AttackDef { Kind = AtkKind.Ring, Range = 99f, Windup = 0.8f, Recover = 0.5f, Cooldown = 5f, Count = 16, Speed = 6.5f, Damage = 8f, Proj = "orb", ProjColor = venom },
                    new AttackDef { Kind = AtkKind.Shoot, Range = 12f, Count = 5, Spread = 50f, Windup = 0.7f, Recover = 0.5f, Cooldown = 3.5f, Damage = 9f, Speed = 10f, Proj = "orb", ProjColor = venom },
                }
            });

            // ------------------------------------------------------------- Gjöll
            Add(new EnemyDef
            {
                Id = "revenant", Name = "Blade-Revenant", Art = "revenant", HP = 70f, Speed = 2.8f, PrefDist = 3f, Cost = 1.5f, Blood = blood,
                Lore = "The river Gjöll runs with knives. Some of them were men.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Charge, Range = 6.5f, Dist = 6f, Size = 0.8f, Windup = 0.6f, Active = 0.25f, Recover = 0.6f, Cooldown = 1.9f, Damage = 13f },
                    new AttackDef { Kind = AtkKind.Swipe, Range = 2.2f, Size = 2.2f, Arc = 110f, Windup = 0.5f, Recover = 0.6f, Cooldown = 1.4f, Damage = 10f },
                }
            });
            Add(new EnemyDef
            {
                Id = "wight", Name = "River-Wight", Art = "wight", HP = 60f, Speed = 2f, Move = MoveKind.Teleport, PrefDist = 6f, Flying = true, Cost = 1.4f, Blood = ice,
                Lore = "A drowned thing that never learned it was dead, only that it was cold.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Ring, Range = 9f, Windup = 0.7f, Recover = 0.6f, Cooldown = 2.4f, Count = 10, Speed = 7f, Damage = 8f, Proj = "orb", ProjColor = ice },
                    new AttackDef { Kind = AtkKind.Shoot, Range = 11f, Count = 2, Spread = 14f, Windup = 0.6f, Recover = 0.5f, Cooldown = 2f, Damage = 8f, Speed = 10f, Proj = "shard" },
                }
            });
            Add(new EnemyDef
            {
                Id = "valkyrie", Name = "Fallen Valkyrie", Art = "valkyrie", HP = 90f, Speed = 3.6f, Move = MoveKind.Strafe, PrefDist = 3.5f, Cost = 2f, Blood = U.Hex("ffd0a0"),
                Lore = "She chose the wrong man once. Freyja does not forgive that kind of mistake.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Leap, Range = 7f, MinRange = 2f, Size = 2f, Windup = 0.7f, Active = 0.45f, Recover = 0.6f, Cooldown = 3f, Damage = 14f, Knock = 7f },
                    new AttackDef { Kind = AtkKind.Shoot, Range = 10f, Count = 3, Spread = 24f, Windup = 0.6f, Recover = 0.5f, Cooldown = 2.4f, Damage = 9f, Speed = 12f, Proj = "bone" },
                }
            });
            Add(new EnemyDef
            {
                Id = "bell", Name = "Wailing Bell", Art = "bell", HP = 75f, Speed = 0f, Move = MoveKind.Stationary, Mass = 99f, Radius = 0.55f, Cost = 1.5f, Blood = ice,
                Lore = "The bridge Gjallarbrú rings when the living cross. These bells ring for no one, constantly.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Ring, Range = 99f, Windup = 1f, Recover = 0.4f, Cooldown = 3f, Count = 14, Speed = 5.5f, Damage = 8f, Proj = "orb", ProjColor = ice } }
            });
            Add(new EnemyDef
            {
                Id = "hrimthurs", Name = "Hrímþurs", Art = "hrimthurs", HP = 260f, Armor = 80f, Speed = 1.6f, Mass = 9f, Radius = 0.9f, PrefDist = 2.4f, Cost = 4f, Blood = ice,
                Lore = "A frost-giant, older than the gods' grudges. He has wandered down here to feel warm.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Slam, Range = 2.8f, Size = 3.5f, Windup = 1.2f, Active = 0.15f, Recover = 1f, Cooldown = 3f, Damage = 20f, Knock = 12f },
                    new AttackDef { Kind = AtkKind.Lob, Range = 10f, Count = 3, Size = 1.4f, Windup = 0.9f, Recover = 0.7f, Cooldown = 4f, Damage = 12f, Proj = "shard" },
                }
            });
            Add(new EnemyDef
            {
                Id = "warden", Name = "Warden of Gjallarbrú", Title = "The Bridge-Keeper's Shadow", Art = "warden", HP = 800f, Armor = 220f, Speed = 2.3f, Mass = 9f, Radius = 0.85f, PrefDist = 2.2f, Cost = 9f, Blood = ice,
                Lore = "Móðguðr keeps the bridge. This is what keeps it when she is not looking.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Swipe, Range = 3.2f, Size = 3.4f, Arc = 150f, Windup = 0.7f, Recover = 0.7f, Cooldown = 1.8f, Damage = 16f, Repeat = 2 },
                    new AttackDef { Kind = AtkKind.Charge, Range = 10f, MinRange = 3f, Dist = 9f, Size = 1.1f, Windup = 0.8f, Active = 0.45f, Recover = 0.8f, Cooldown = 4f, Damage = 18f },
                    new AttackDef { Kind = AtkKind.Shoot, Range = 12f, Count = 7, Spread = 70f, Windup = 0.7f, Recover = 0.5f, Cooldown = 4f, Damage = 9f, Speed = 9f, Proj = "orb", ProjColor = ice },
                    new AttackDef { Kind = AtkKind.Slam, Range = 2.4f, Size = 3f, Windup = 0.9f, Recover = 0.8f, Cooldown = 3.5f, Damage = 18f, Knock = 10f },
                }
            });

            // ------------------------------------------------------------- Helgrind
            Add(new EnemyDef
            {
                Id = "helhound", Name = "Hel-Hound", Art = "helhound", HP = 70f, Speed = 4.6f, PrefDist = 1.3f, Radius = 0.55f, Cost = 1.5f, Blood = hel,
                Lore = "Garmr's litter. They have his hunger and none of his patience.",
                Attacks = new[] { new AttackDef { Kind = AtkKind.Lunge, Range = 4.2f, Dist = 4f, Size = 0.8f, Windup = 0.45f, Active = 0.25f, Recover = 0.55f, Cooldown = 1.3f, Damage = 12f } }
            });
            Add(new EnemyDef
            {
                Id = "einherjar", Name = "Fallen Einherjar", Art = "einherjar", HP = 120f, Speed = 2.6f, Shielded = true, Mass = 3f, Radius = 0.5f, PrefDist = 1.7f, Cost = 2.4f, Blood = hel,
                Lore = "Chosen for Valhalla, then un-chosen. There is a story there, and Hel will not tell it.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Swipe, Range = 2.3f, Size = 2.4f, Arc = 120f, Windup = 0.6f, Recover = 0.6f, Cooldown = 1.6f, Damage = 13f },
                    new AttackDef { Kind = AtkKind.Charge, Range = 8f, MinRange = 3f, Dist = 7f, Size = 0.9f, Windup = 0.8f, Active = 0.4f, Recover = 0.8f, Cooldown = 4f, Damage = 15f },
                }
            });

            // ------------------------------------------------------------- Bosses
            Add(new EnemyDef
            {
                Id = "vali", Name = "Váli", Title = "The Wolf-Brother", Art = "vali", Boss = true, HP = 1600f, Speed = 3.4f, Mass = 99f, Radius = 0.9f, PrefDist = 2.2f, Blood = blood,
                Lore = "Your brother. The gods made him a wolf so he would kill you. He did. Then they left him like this.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Lunge, Tag = "pounce", Range = 10f, MinRange = 2.5f, Dist = 7f, Size = 0.9f, Windup = 0.55f, Active = 0.3f, Recover = 0.55f, Cooldown = 3f, Damage = 14f, Repeat = 3, Knock = 8f },
                    new AttackDef { Kind = AtkKind.Swipe, Range = 2.8f, Size = 2.8f, Arc = 140f, Windup = 0.45f, Recover = 0.45f, Cooldown = 1.3f, Damage = 12f, Repeat = 2 },
                    new AttackDef { Kind = AtkKind.Leap, Range = 12f, MinRange = 4f, Size = 2.6f, Windup = 0.7f, Active = 0.55f, Recover = 0.7f, Cooldown = 5f, Damage = 18f, Knock = 10f },
                    new AttackDef { Kind = AtkKind.Custom, Tag = "rush", Range = 99f, Windup = 1.1f, Active = 0.5f, Recover = 0.6f, Cooldown = 9f, Damage = 15f, Weight = 0.8f },
                    new AttackDef { Kind = AtkKind.Custom, Tag = "howl", Range = 99f, Windup = 1f, Active = 0.6f, Recover = 0.8f, Cooldown = 13f, Damage = 9f, MinPhase = 2 },
                }
            });
            Add(new EnemyDef
            {
                Id = "nidhogg", Name = "Níðhöggr", Title = "Gnawer of the World-Root", Art = "nidhogg_head", Boss = true, HP = 2600f, Speed = 2.3f, Mass = 99f, Radius = 1.2f, PrefDist = 4f, Blood = venom,
                Lore = "He gnaws the root of Yggdrasil and the corpses of the faithless. He has never once been full.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Swipe, Tag = "breath", Range = 7f, Size = 6.5f, Arc = 60f, Windup = 0.9f, Recover = 0.8f, Cooldown = 4.5f, Damage = 16f, ZoneDps = 10f },
                    new AttackDef { Kind = AtkKind.Lunge, Range = 6f, Dist = 5f, Size = 1.4f, Windup = 0.55f, Active = 0.35f, Recover = 0.7f, Cooldown = 3f, Damage = 18f, Knock = 10f },
                    new AttackDef { Kind = AtkKind.Lob, Range = 14f, Count = 5, Size = 1.6f, Windup = 0.8f, Recover = 0.6f, Cooldown = 5f, Damage = 10f, ZoneDps = 10f, Proj = "glob" },
                    new AttackDef { Kind = AtkKind.Custom, Tag = "burrow", Range = 99f, Windup = 0.6f, Active = 3.6f, Recover = 0.6f, Cooldown = 11f, Damage = 20f },
                    new AttackDef { Kind = AtkKind.Summon, Range = 99f, Windup = 1f, Recover = 0.6f, Cooldown = 14f, Summon = "larva", Count = 2, MinPhase = 2 },
                }
            });
            Add(new EnemyDef
            {
                Id = "garmr", Name = "Garmr", Title = "Blood-Chested Hound of Gnipahellir", Art = "garmr", Boss = true, HP = 3200f, Speed = 3.4f, Mass = 99f, Radius = 1.3f, PrefDist = 2.6f, Blood = blood,
                Lore = "He will howl when the end comes, and break his chain. Until then he guards the gate, and he is very, very good at it.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Lunge, Range = 5f, Dist = 4f, Size = 1.2f, Windup = 0.5f, Active = 0.25f, Recover = 0.5f, Cooldown = 2.2f, Damage = 16f, Repeat = 2, Knock = 9f },
                    new AttackDef { Kind = AtkKind.Leap, Range = 14f, MinRange = 4f, Size = 3f, Windup = 0.75f, Active = 0.5f, Recover = 0.7f, Cooldown = 4.5f, Damage = 22f, Knock = 12f },
                    new AttackDef { Kind = AtkKind.Ring, Range = 99f, Windup = 0.9f, Recover = 0.6f, Cooldown = 6f, Count = 24, Speed = 7f, Damage = 10f, Proj = "orb", ProjColor = blood },
                    new AttackDef { Kind = AtkKind.Swipe, Tag = "blaze", Range = 6f, Size = 5f, Arc = 70f, Windup = 0.8f, Recover = 0.7f, Cooldown = 6f, Damage = 14f, ZoneDps = 10f },
                    new AttackDef { Kind = AtkKind.Custom, Tag = "chains", Range = 99f, Windup = 0.95f, Active = 0.3f, Recover = 0.6f, Cooldown = 8f, Damage = 16f },
                }
            });
            Add(new EnemyDef
            {
                Id = "hel", Name = "Hel", Title = "Queen of the Dead, Your Sister", Art = "hel_boss", Boss = true, HP = 2400f, Speed = 2.4f, Mass = 99f, Radius = 0.9f, Move = MoveKind.Kite, PrefDist = 6f, Blood = hel,
                Lore = "Half of her is the most beautiful woman in the nine worlds. The other half has been dead since before you were born. Both halves love you.",
                Attacks = new[]
                {
                    new AttackDef { Kind = AtkKind.Shoot, Range = 16f, Count = 5, Spread = 40f, Windup = 0.7f, Recover = 0.6f, Cooldown = 4f, Damage = 12f, Speed = 13f, Proj = "bone", Repeat = 3, RepeatGap = 0.35f },
                    new AttackDef { Kind = AtkKind.Custom, Tag = "hands", Range = 99f, Windup = 0.5f, Active = 1.3f, Recover = 0.6f, Cooldown = 6f, Damage = 16f },
                    new AttackDef { Kind = AtkKind.Custom, Tag = "beams", Range = 99f, Windup = 0.6f, Active = 3.2f, Recover = 0.8f, Cooldown = 9f, Damage = 20f },
                    new AttackDef { Kind = AtkKind.Ring, Range = 99f, Windup = 0.8f, Recover = 0.6f, Cooldown = 5f, Count = 20, Speed = 6f, Damage = 10f, Proj = "orb", ProjColor = hel },
                    new AttackDef { Kind = AtkKind.Summon, Range = 99f, Windup = 1f, Recover = 0.6f, Cooldown = 14f, Summon = "einherjar", Count = 2, Weight = 0.7f },
                }
            });
        }
    }
}
