using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Description of a humanoid figure; painted whole (portraits) or as rig parts (animation).</summary>
    public sealed class Hum
    {
        public int W = 96, H = 128;
        public float scale = 1f, width = 1f, torso = 1f, legs = 1f, hunch = 0f, headR = 12f, shoulders = 1f;
        public Color skin = U.Hex("cdbfae"), cloth = U.Hex("2b3440"), trim = U.Hex("b08d4a"), dark = U.Hex("1b1e25");
        public Color hair = U.Hex("e7dcc8"), eye = Pal.Narfi, metal = U.Hex("8d939e"), cape = U.Hex("20242c");
        public bool hood, helmet, horns, beard, longBeard, crown, boneCrown, skull, capeOn, longHair, robe, pads, tattered;
        public bool armsUp, oneHand, eyepatch, hat, hairFlame, kneel, shield, bow, sword, hammer, halo, flowers, necklace, fur, wings, staff;
        public bool wolfHead, deadHalf, iceShards, blindfold, bowl, ropes;
        public int chestRune = -1;
        public Color runeColor = Pal.Narfi;
        public bool eyesGlow = true;
    }

    public sealed class Quad
    {
        public int W = 144, H = 96;
        public float scale = 1f;
        public Color fur = U.Hex("3a3f4a"), belly = U.Hex("5a606e"), eye = Pal.Danger, chest = new Color(0, 0, 0, 0);
        public bool chains, spikes = true, flame;
    }

    public enum RigKind { Humanoid, Quad, Bird, Serpent, Ghost, Blob, Bell, Static }

    /// <summary>The look of every character, in one table.</summary>
    public static class Specs
    {
        public static RigKind Kind(string id)
        {
            if (Hum(id) != null) return RigKind.Humanoid;
            if (Quad(id) != null) return RigKind.Quad;
            switch (id)
            {
                case "raven": return RigKind.Bird;
                case "serpent":
                case "larva": return RigKind.Serpent;
                case "wraith":
                case "wight": return RigKind.Ghost;
                case "bloat": return RigKind.Blob;
                case "bell": return RigKind.Bell;
            }
            return RigKind.Static;
        }

        public static Quad Quad(string id)
        {
            switch (id)
            {
                case "helhound": return new Quad { W = 144, H = 96, scale = 0.85f, fur = U.Hex("2a2233"), belly = U.Hex("4a3a55"), eye = Pal.Hel, chest = Pal.Hel.A(0.6f) };
                case "garmr": return new Quad { W = 240, H = 176, scale = 1.55f, fur = U.Hex("2a1e1e"), belly = U.Hex("4a2a2a"), eye = U.Hex("ffb020"), chest = Pal.Blood, chains = true, flame = true };
            }
            return null;
        }

        public static Hum Hum(string id)
        {
            switch (id)
            {
                case "narfi":
                    return new Hum
                    {
                        hood = true, capeOn = true, tattered = true, cloth = U.Hex("1d2a26"), cape = U.Hex("121a18"), trim = U.Hex("3ee8a6").Lit(0.6f),
                        dark = U.Hex("15191c"), skin = U.Hex("d9d2c6"), chestRune = Runes.Naudiz, runeColor = Pal.Narfi, eye = Pal.Narfi, width = 0.95f
                    };
                case "hel":
                    return new Hum
                    {
                        W = 128, H = 176, scale = 1.3f, robe = true, capeOn = true, boneCrown = true, longHair = true, hair = U.Hex("17121f"), cloth = U.Hex("2a1d3a"), cape = U.Hex("160f20"),
                        trim = Pal.Bone, skin = U.Hex("e6dbe8"), eye = Pal.Hel, chestRune = Runes.Hagalaz, runeColor = Pal.Hel, width = 0.9f, torso = 1.1f, deadHalf = true
                    };
                case "hel_boss":
                    return new Hum
                    {
                        W = 176, H = 236, scale = 1.75f, robe = true, capeOn = true, boneCrown = true, longHair = true, hair = U.Hex("17121f"), cloth = U.Hex("2a1d3a"), cape = U.Hex("160f20"),
                        trim = Pal.Bone, skin = U.Hex("e6dbe8"), eye = Pal.Hel, chestRune = Runes.Hagalaz, runeColor = Pal.Hel, width = 0.95f, torso = 1.1f, staff = true, deadHalf = true
                    };
                case "baldr":
                    return new Hum { W = 112, H = 150, scale = 1.15f, robe = true, halo = true, beard = true, hair = U.Hex("f2d27a"), longHair = true, cloth = U.Hex("e8e0c8"), trim = Pal.Gold, skin = U.Hex("f0dcc4"), eyesGlow = false, chestRune = Runes.Sowilo, runeColor = Pal.Gold };
                case "nanna":
                    return new Hum { W = 96, H = 136, robe = true, longHair = true, flowers = true, hair = U.Hex("9b4a2a"), cloth = U.Hex("7c93b3"), trim = U.Hex("e8e0c8"), skin = U.Hex("ecd6c2"), eyesGlow = false, width = 0.85f };
                case "modgud":
                    return new Hum { W = 112, H = 150, scale = 1.15f, helmet = true, pads = true, capeOn = true, longHair = true, hair = U.Hex("dfe6ee"), cloth = U.Hex("3a4660"), cape = U.Hex("1c2333"), trim = U.Hex("9fb6d6"), skin = U.Hex("e8eef4"), eye = U.Hex("bfe4ff"), metal = U.Hex("aab3c2"), staff = true, runeColor = U.Hex("bfe4ff") };
                case "sigyn":
                    return new Hum { W = 96, H = 150, robe = true, armsUp = true, longHair = true, hair = U.Hex("c9a46a"), cloth = U.Hex("6b5640"), trim = Pal.Gold, skin = U.Hex("ead8c4"), eyesGlow = false, width = 0.9f, bowl = true };
                case "ganglot":
                    return new Hum { W = 96, H = 120, hunch = 0.7f, robe = true, hood = true, cloth = U.Hex("4a4238"), trim = U.Hex("8a7a5a"), skin = U.Hex("b8ac98"), eye = U.Hex("d8c8a0"), width = 0.9f, torso = 0.9f, staff = true, runeColor = U.Hex("d8c8a0") };
                case "hodr":
                    return new Hum { W = 104, H = 136, scale = 1.05f, capeOn = true, cape = U.Hex("3a3f48"), cloth = U.Hex("5a606e"), trim = U.Hex("a8b0bc"), skin = U.Hex("d8ccbc"), hair = U.Hex("8a8478"), beard = true, eyesGlow = false, bow = true, blindfold = true };
                case "hermod":
                    return new Hum { W = 112, H = 140, scale = 1.08f, helmet = true, pads = true, capeOn = true, cape = U.Hex("2e4a6a"), cloth = U.Hex("4a5468"), trim = Pal.Gold, metal = U.Hex("b8c0cc"), skin = U.Hex("e2c8ac"), beard = true, hair = U.Hex("a86a3a"), eyesGlow = false, staff = true, runeColor = Pal.Gold };
                case "loki":
                    return new Hum { W = 112, H = 128, kneel = true, hairFlame = true, cloth = U.Hex("2c3a2a"), trim = Pal.Gold, skin = U.Hex("d8cdb8"), eye = Pal.Narfi, beard = true, hair = U.Hex("6b8a3a"), ropes = true };
                case "thor":
                    return new Hum { W = 128, H = 160, scale = 1.25f, width = 1.25f, beard = true, hair = U.Hex("c4472a"), helmet = true, pads = true, capeOn = true, cape = U.Hex("8a2a1e"), cloth = U.Hex("4a3a2e"), trim = Pal.Gold, eye = Pal.God(God.Thor), hammer = true, skin = U.Hex("e2c3a4") };
                case "freyja":
                    return new Hum { W = 112, H = 150, scale = 1.12f, robe = true, longHair = true, hair = U.Hex("f2cf6e"), cloth = U.Hex("b04a7a"), capeOn = true, cape = U.Hex("7a5a4a"), trim = Pal.Gold, necklace = true, eye = Pal.God(God.Freyja), skin = U.Hex("f2dccc") };
                case "tyr":
                    return new Hum { W = 128, H = 160, scale = 1.2f, width = 1.15f, beard = true, hair = U.Hex("4a3428"), helmet = true, pads = true, capeOn = true, cape = U.Hex("8a1e22"), cloth = U.Hex("3a3a44"), oneHand = true, sword = true, eye = Pal.God(God.Tyr) };
                case "skadi":
                    return new Hum { W = 112, H = 150, scale = 1.12f, hood = true, fur = true, cloth = U.Hex("dfe8f0"), trim = U.Hex("8fe3ff"), skin = U.Hex("dbe7f2"), eye = Pal.God(God.Skadi), bow = true, capeOn = true, cape = U.Hex("b8c8d8") };
                case "odin":
                    return new Hum { W = 128, H = 168, scale = 1.25f, hat = true, beard = true, longBeard = true, hair = U.Hex("b8bcc4"), capeOn = true, cape = U.Hex("2a2f45"), cloth = U.Hex("3a3f5a"), trim = U.Hex("a596ff"), eyepatch = true, staff = true, eye = Pal.God(God.Odin), runeColor = Pal.God(God.Odin) };
                case "njord":
                    return new Hum { W = 112, H = 156, scale = 1.15f, robe = true, beard = true, longBeard = true, hair = U.Hex("e8f0f0"), cloth = U.Hex("1f5f66"), trim = U.Hex("39e0c4"), crown = true, staff = true, eye = Pal.God(God.Njord), runeColor = Pal.God(God.Njord) };

                case "draugr":
                    return new Hum { W = 96, H = 120, hunch = 0.6f, skin = U.Hex("7d8b99"), cloth = U.Hex("3a3328"), trim = U.Hex("6b5a3a"), helmet = true, metal = U.Hex("6e6258"), eye = U.Hex("6ff3ff"), tattered = true, capeOn = true, cape = U.Hex("2a251e"), sword = true };
                case "draugr_archer":
                    return new Hum { W = 96, H = 120, hunch = 0.35f, width = 0.82f, skin = U.Hex("7d8b99"), cloth = U.Hex("33362e"), hood = true, eye = U.Hex("6ff3ff"), bow = true, trim = U.Hex("5a4a32") };
                case "thrall":
                    return new Hum { W = 112, H = 132, scale = 1.12f, width = 1.3f, skin = U.Hex("6e7a88"), cloth = U.Hex("2e2a26"), helmet = true, metal = U.Hex("5e6670"), eye = U.Hex("6ff3ff"), shield = true, pads = true };
                case "grave_jarl":
                    return new Hum { W = 144, H = 176, scale = 1.55f, width = 1.3f, hunch = 0.3f, skin = U.Hex("6e7a88"), cloth = U.Hex("2b2a33"), helmet = true, horns = true, metal = U.Hex("7a6a52"), eye = U.Hex("9ff8ff"), pads = true, capeOn = true, tattered = true, cape = U.Hex("3a1e22"), sword = true, beard = true, hair = U.Hex("9aa1ad"), chestRune = Runes.Hagalaz, runeColor = U.Hex("6ff3ff") };
                case "spitter":
                    return new Hum { W = 104, H = 124, hunch = 0.5f, width = 1.35f, skin = U.Hex("8a9a6a"), cloth = U.Hex("3a4a2a"), hood = true, eye = Pal.Venom, robe = true, tattered = true, trim = Pal.Venom.Lit(0.6f), chestRune = Runes.Kaunan, runeColor = Pal.Venom };
                case "oathbreaker":
                    return new Hum { W = 112, H = 136, scale = 1.12f, width = 1.15f, skin = U.Hex("5a6a5a"), cloth = U.Hex("2a2e2a"), helmet = true, horns = true, metal = U.Hex("4e5a52"), eye = Pal.Venom, pads = true, capeOn = true, tattered = true, cape = U.Hex("1e261e"), sword = true };
                case "weaver":
                    return new Hum { W = 128, H = 160, scale = 1.3f, robe = true, longHair = true, hair = U.Hex("1e2a1a"), cloth = U.Hex("2a3a22"), trim = Pal.Venom, skin = U.Hex("9aa88a"), eye = Pal.Venom, crown = true, armsUp = true, chestRune = Runes.Kaunan, runeColor = Pal.Venom };
                case "revenant":
                    return new Hum { W = 104, H = 128, hunch = 0.2f, skin = U.Hex("8a96a8"), cloth = U.Hex("2a2f3a"), helmet = true, metal = U.Hex("a0a8b6"), eye = U.Hex("ff6060"), sword = true, capeOn = true, tattered = true, cape = U.Hex("4a1e22") };
                case "hrimthurs":
                    return new Hum { W = 176, H = 220, scale = 1.85f, width = 1.45f, hunch = 0.25f, skin = U.Hex("9cc6e0"), cloth = U.Hex("3a5670"), trim = U.Hex("dff6ff"), beard = true, longBeard = true, hair = U.Hex("eaf8ff"), eye = U.Hex("e0fbff"), fur = true, horns = true, iceShards = true };
                case "valkyrie":
                    return new Hum { W = 144, H = 150, scale = 1.12f, wings = true, helmet = true, longHair = true, hair = U.Hex("d8d0b8"), cloth = U.Hex("4a4f5e"), metal = U.Hex("c0c6d0"), skin = U.Hex("b8c0cc"), eye = U.Hex("ffd0a0"), pads = true, staff = true, runeColor = U.Hex("ffd0a0") };
                case "warden":
                    return new Hum { W = 144, H = 180, scale = 1.6f, width = 1.3f, helmet = true, pads = true, metal = U.Hex("9aa4b4"), cloth = U.Hex("2e3644"), cape = U.Hex("1a2030"), capeOn = true, skin = U.Hex("a8b4c4"), eye = U.Hex("bfe4ff"), staff = true, shield = true, runeColor = U.Hex("bfe4ff"), chestRune = Runes.Algiz };
                case "einherjar":
                    return new Hum { W = 104, H = 128, skin = U.Hex("b0a8c0"), cloth = U.Hex("3a2e4a"), helmet = true, metal = U.Hex("b8a878"), eye = Pal.Hel, pads = true, shield = true, capeOn = true, cape = U.Hex("2a1e3a") };
                case "vali":
                    return new Hum { W = 176, H = 208, scale = 1.6f, width = 1.35f, hunch = 0.55f, skin = U.Hex("4a4f5a"), cloth = U.Hex("2a2d36"), trim = U.Hex("8d939e"), eye = Pal.Danger, capeOn = true, tattered = true, cape = U.Hex("1e2028"), hair = U.Hex("2a2d36"), wolfHead = true };
            }
            return null;
        }
    }
}
