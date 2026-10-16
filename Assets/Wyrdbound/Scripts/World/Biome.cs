using UnityEngine;

namespace Wyrdbound
{
    public enum FloorStyle { Stones, Slabs, Obsidian, Hall, Cave }
    public enum AmbientKind { Mist, Snow, Spores, Embers, Ash, Motes }

    /// <summary>One realm of the escape. Look, enemy roster, boss and music.</summary>
    public sealed class Biome
    {
        public string Id, Name, Subtitle;
        public int Index;
        public Color Bg, Stone, StoneVar, Grout, Accent, Cliff, Fog, Decal;
        public FloorStyle Style;
        public AmbientKind Amb;
        public string[] Pool;
        public string[] Late;
        public string MiniBoss, Boss;
        public string Music;
        public int Rooms = 8;
        public string[] Props;
        public int DecalKind;
    }

    public static class Biomes
    {
        public static readonly Biome Niflhel = new Biome
        {
            Id = "niflhel", Index = 0, Name = "Niflhel", Subtitle = "The Mist-Depths, Where the Oathless Rot",
            Bg = U.Hex("06090d"), Stone = U.Hex("3a4552"), StoneVar = U.Hex("4b5866"), Grout = U.Hex("14181e"), Accent = U.Hex("6ff3ff"),
            Cliff = U.Hex("1c232c"), Fog = U.Hex("6fa8b8"), Decal = U.Hex("cfe8f0"), Style = FloorStyle.Stones, Amb = AmbientKind.Mist,
            Pool = new[] { "draugr", "draugr", "draugr_archer", "bone_rat", "wraith", "bloat" },
            Late = new[] { "thrall", "wraith", "draugr_archer", "bloat" },
            MiniBoss = "grave_jarl", Boss = "vali", Music = "niflhel", Rooms = 8,
            Props = new[] { "pillar", "stone", "bones", "brazier", "tree" }, DecalKind = 0
        };

        public static readonly Biome Nastrond = new Biome
        {
            Id = "nastrond", Index = 1, Name = "Náströnd", Subtitle = "The Corpse-Shore, Hall Woven of Serpents",
            Bg = U.Hex("070a06"), Stone = U.Hex("3a4232"), StoneVar = U.Hex("4a543c"), Grout = U.Hex("10140c"), Accent = Pal.Venom,
            Cliff = U.Hex("1a2014"), Fog = U.Hex("7a9a4a"), Decal = Pal.Venom, Style = FloorStyle.Cave, Amb = AmbientKind.Spores,
            Pool = new[] { "serpent", "serpent", "spitter", "raven", "larva", "draugr" },
            Late = new[] { "oathbreaker", "spitter", "raven", "larva" },
            MiniBoss = "weaver", Boss = "nidhogg", Music = "nastrond", Rooms = 8,
            Props = new[] { "root", "bones", "stone", "brazier" }, DecalKind = 1
        };

        public static readonly Biome Gjoll = new Biome
        {
            Id = "gjoll", Index = 2, Name = "Gjöll", Subtitle = "The Roaring River and the Echoing Bridge",
            Bg = U.Hex("05070c"), Stone = U.Hex("3e4658"), StoneVar = U.Hex("525c72"), Grout = U.Hex("101420"), Accent = U.Hex("9be7ff"),
            Cliff = U.Hex("161c2a"), Fog = U.Hex("8aa8d8"), Decal = U.Hex("e8f6ff"), Style = FloorStyle.Slabs, Amb = AmbientKind.Snow,
            Pool = new[] { "revenant", "wight", "valkyrie", "bell", "draugr_archer", "revenant" },
            Late = new[] { "hrimthurs", "valkyrie", "wight", "bell" },
            MiniBoss = "warden", Boss = "garmr", Music = "gjoll", Rooms = 8,
            Props = new[] { "crystal", "pillar", "stone", "brazier" }, DecalKind = 2
        };

        public static readonly Biome Helgrind = new Biome
        {
            Id = "helgrind", Index = 3, Name = "Helgrind", Subtitle = "The Gate of the Dead, Where Hel Keeps Her Vigil",
            Bg = U.Hex("08060c"), Stone = U.Hex("2a2433"), StoneVar = U.Hex("3a3046"), Grout = U.Hex("0c0a10"), Accent = Pal.Hel,
            Cliff = U.Hex("120e18"), Fog = U.Hex("8a6ab8"), Decal = Pal.Hel, Style = FloorStyle.Obsidian, Amb = AmbientKind.Embers,
            Pool = new[] { "helhound", "einherjar", "wraith", "spitter", "revenant", "valkyrie" },
            Late = new[] { "helhound", "einherjar", "hrimthurs", "oathbreaker" },
            MiniBoss = "grave_jarl", Boss = "hel", Music = "helgrind", Rooms = 7,
            Props = new[] { "statue", "pillar", "brazier", "bones" }, DecalKind = 3
        };

        public static readonly Biome Hall = new Biome
        {
            Id = "hall", Index = -1, Name = "Éljúðnir", Subtitle = "Hel's Hall",
            Bg = U.Hex("07050a"), Stone = U.Hex("2e2836"), StoneVar = U.Hex("3a3344"), Grout = U.Hex("0e0b12"), Accent = Pal.Gold,
            Cliff = U.Hex("16121c"), Fog = U.Hex("8a6ab8"), Decal = Pal.Hel, Style = FloorStyle.Hall, Amb = AmbientKind.Motes,
            Props = new[] { "brazier", "pillar" }, Music = "hall", DecalKind = 3
        };

        public static readonly Biome Surface = new Biome
        {
            Id = "surface", Index = 4, Name = "Midgard", Subtitle = "The Cave of Binding",
            Bg = U.Hex("050505"), Stone = U.Hex("3b352e"), StoneVar = U.Hex("4b443a"), Grout = U.Hex("12100c"), Accent = Pal.Venom,
            Cliff = U.Hex("1a1612"), Fog = U.Hex("5a5a4a"), Decal = Pal.Venom, Style = FloorStyle.Cave, Amb = AmbientKind.Ash,
            Props = new[] { "stone", "root" }, Music = "surface", DecalKind = 1
        };

        public static readonly Biome[] Run = { Niflhel, Nastrond, Gjoll, Helgrind };
    }
}
