using UnityEngine;

namespace Wyrdbound
{
    /// <summary>The colour language of the saga. Gods, rarities, currencies and UI.</summary>
    public static class Pal
    {
        public static readonly Color Ink = U.Hex("05060a");
        public static readonly Color Bone = U.Hex("e7dcc8");
        public static readonly Color Gold = U.Hex("e8b84a");
        public static readonly Color Blood = U.Hex("c0262f");
        public static readonly Color Narfi = U.Hex("3ee8a6");
        public static readonly Color Hel = U.Hex("b48cff");
        public static readonly Color Text = U.Hex("ece4d2");
        public static readonly Color Dim = U.Hex("968c79");
        public static readonly Color UiBg = new Color(0.035f, 0.04f, 0.06f, 0.94f);
        public static readonly Color UiLine = U.Hex("a08a5c");
        public static readonly Color Heal = U.Hex("6ee7a0");
        public static readonly Color Silver = U.Hex("d8dee9");
        public static readonly Color Ember = U.Hex("ff8a3d");
        public static readonly Color Key = U.Hex("d9cfb6");
        public static readonly Color Mead = U.Hex("f2b134");
        public static readonly Color Danger = U.Hex("ff3b3b");
        public static readonly Color Fury = U.Hex("ffb347");
        public static readonly Color Armor = U.Hex("b9c4d6");
        public static readonly Color Venom = U.Hex("a6e22e");
        public static readonly Color Frost = U.Hex("9be7ff");

        public static Color God(God g)
        {
            switch (g)
            {
                case Wyrdbound.God.Thor: return U.Hex("ffd84a");
                case Wyrdbound.God.Freyja: return U.Hex("ff7ac8");
                case Wyrdbound.God.Tyr: return U.Hex("ff5148");
                case Wyrdbound.God.Skadi: return U.Hex("8fe3ff");
                case Wyrdbound.God.Odin: return U.Hex("a596ff");
                case Wyrdbound.God.Njord: return U.Hex("39e0c4");
                case Wyrdbound.God.Ratatoskr: return U.Hex("ff9b4a");
            }
            return Color.white;
        }

        public static string GodName(God g)
        {
            switch (g)
            {
                case Wyrdbound.God.Thor: return "Thor";
                case Wyrdbound.God.Freyja: return "Freyja";
                case Wyrdbound.God.Tyr: return "Týr";
                case Wyrdbound.God.Skadi: return "Skaði";
                case Wyrdbound.God.Odin: return "Odin";
                case Wyrdbound.God.Njord: return "Njörðr";
                case Wyrdbound.God.Ratatoskr: return "Ratatoskr";
            }
            return "";
        }

        public static string GodTitle(God g)
        {
            switch (g)
            {
                case Wyrdbound.God.Thor: return "Thunderer, Warden of Midgard";
                case Wyrdbound.God.Freyja: return "Lady of the Vanir, Chooser of the Slain";
                case Wyrdbound.God.Tyr: return "One-Handed, God of Oaths";
                case Wyrdbound.God.Skadi: return "Huntress of the Mountains";
                case Wyrdbound.God.Odin: return "The Allfather, the Hanged One";
                case Wyrdbound.God.Njord: return "Lord of Nóatún, Calmer of Seas";
                case Wyrdbound.God.Ratatoskr: return "Messenger of Yggdrasil";
            }
            return "";
        }

        public static int GodRune(God g)
        {
            switch (g)
            {
                case Wyrdbound.God.Thor: return Runes.Thurisaz;
                case Wyrdbound.God.Freyja: return Runes.Fehu;
                case Wyrdbound.God.Tyr: return Runes.Tiwaz;
                case Wyrdbound.God.Skadi: return Runes.Isa;
                case Wyrdbound.God.Odin: return Runes.Ansuz;
                case Wyrdbound.God.Njord: return Runes.Laguz;
                case Wyrdbound.God.Ratatoskr: return Runes.Raido;
            }
            return Runes.Naudiz;
        }

        public static Color Rarity(Rarity r)
        {
            switch (r)
            {
                case Wyrdbound.Rarity.Rare: return U.Hex("4aa8ff");
                case Wyrdbound.Rarity.Epic: return U.Hex("c06bff");
                case Wyrdbound.Rarity.Heroic: return U.Hex("ff9d2e");
                case Wyrdbound.Rarity.Duo: return U.Hex("7dffb0");
            }
            return U.Hex("c8c2b4");
        }

        public static string RarityName(Rarity r)
        {
            switch (r)
            {
                case Wyrdbound.Rarity.Rare: return "Rare";
                case Wyrdbound.Rarity.Epic: return "Epic";
                case Wyrdbound.Rarity.Heroic: return "Heroic";
                case Wyrdbound.Rarity.Duo: return "Duo";
            }
            return "Common";
        }
    }
}
