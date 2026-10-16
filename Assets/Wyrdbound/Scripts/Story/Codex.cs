using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class CodexEntry
    {
        public string Cat, Id, Name, Text, Flag;
    }

    /// <summary>Mímir's Lore: everything Narfi has seen, written down by a very old head.</summary>
    public static class Codex
    {
        public static readonly string[] Categories = { "People", "Gods", "Foes", "Realms", "Arms" };
        static Dictionary<string, List<CodexEntry>> byCat;

        static void Add(string cat, string id, string name, string flag, string text)
        {
            if (!byCat.TryGetValue(cat, out var l)) { l = new List<CodexEntry>(); byCat[cat] = l; }
            l.Add(new CodexEntry { Cat = cat, Id = id, Name = name, Flag = flag, Text = text });
        }

        static void Build()
        {
            if (byCat != null) return;
            byCat = new Dictionary<string, List<CodexEntry>>();

            Add("People", "narfi", "Narfi, Son of Loki", null,
                "Second son of Loki and Sigyn. Killed by his brother Váli, whom the gods had turned into a wolf; his entrails became the bonds that hold Loki beneath the earth.\n\nHe was, by every account, a quiet child who asked too many questions. He has not stopped.\n\n" + Note("He dies more than anyone I have ever recorded, and comes back angrier each time. I find this oddly encouraging."));
            Add("People", "hel", "Hel, Queen of the Dead", "codex_hel",
                "Daughter of Loki and the giantess Angrboða, cast down by Odin to rule the dead who do not die in glory. Half of her face is the most beautiful in the nine worlds. The other half has been dead since before the worlds were finished.\n\nShe built Éljúðnir for the forgotten: the old, the sick, the drowned. She will not let it burn.\n\n" + Note("She is harsher than she needs to be and kinder than she lets anyone see. A family trait, I suspect."));
            Add("People", "ganglot", "Ganglöt, the Slow Handmaid", "codex_ganglot",
                "One of Hel's two servants, both named for slowness. She keeps the Book of Deaths, in which every soul's ending is written, very carefully, very slowly.\n\n" + Note("Her brother Ganglati went to fetch salt four centuries ago. She expects him any day."));
            Add("People", "baldr", "Baldr the Bright", "codex_baldr",
                "Son of Odin and Frigg, beloved of all things. His mother made every object swear never to harm him, all but the young mistletoe, which seemed too small to matter. Loki made a dart of it and put it in the hand of blind Höðr.\n\nThe völva says Baldr will return after Ragnarök, to rule a green new world.\n\n" + Note("He is the only god I know who forgave his murderer's son on sight. He is also the only god who stands to gain from the end of the world. Both things are true."));
            Add("People", "nanna", "Nanna", "codex_nanna",
                "Baldr's wife, whose heart broke on his funeral pyre. She followed him to Hel and sent gifts back to Asgard with Hermóðr: linen for Frigg, a ring for Fulla.\n\n" + Note("She asks after everyone's mother. Including mine. I did not have the heart to tell her I cannot remember."));
            Add("People", "mimir", "Mímir", "codex_mimir",
                "Wisest of the Æsir, sent to the Vanir as a hostage and beheaded by them. Odin preserved the head with herbs and charms, and consults it still.\n\nIt sits, these days, on a pedestal in Hel's hall. A gift.\n\n" + Note("I would prefer not to write my own entry. Let us say I am very well informed, and leave it there."));
            Add("People", "modgud", "Móðguðr, Keeper of the Bridge", "codex_modgud",
                "The maiden who guards Gjallarbrú, the gold-roofed bridge over the river Gjöll. The bridge rings when the living cross it.\n\n" + Note("She runs a shop. Nobody asked her to. Nobody dares ask her to stop."));
            Add("People", "vali", "Váli, the Wolf-Brother", "codex_vali",
                "Elder son of Loki and Sigyn. When Loki was taken, the gods turned Váli into a wolf, and the wolf killed his brother Narfi. He was left in Niflhel, still a wolf, guarding the depths.\n\n" + Note("Odin visited him before the curse. I know because I was told to remember it."));
            Add("People", "hodr", "Höðr the Blind", "codex_hodr",
                "Baldr's blind brother, whose hand threw the mistletoe dart. He was killed in turn by Odin's son Váli, born for that purpose and grown in a single day.\n\n" + Note("He wanders the depths listening for his brother's curse. It has never come."));
            Add("People", "hermod", "Hermóðr the Bold", "codex_hermod",
                "Son of Odin, who rode Sleipnir nine nights down to Hel to beg for Baldr's return. Hel agreed, if all things wept. All things did, save one giantess in a cave.\n\n" + Note("The giantess was Loki. Hermóðr knows. He helps the trickster's son anyway. I do not understand him, and I have stopped trying."));
            Add("People", "loki", "Loki, the Bound God", "codex_loki",
                "Blood-brother to Odin, father of Hel, Fenrir, Jörmungandr, Sleipnir, Váli and Narfi. Bringer of gifts and disasters in roughly equal measure. Bound beneath the earth for Baldr's death, with the entrails of his son.\n\nWhen the venom strikes his face, the earth quakes.\n\n" + Note("He was the most interesting person in Asgard. That was always the problem."));
            Add("People", "sigyn", "Sigyn, the Faithful", "codex_sigyn",
                "Loki's wife, mother of Váli and Narfi. She stays beside her bound husband and holds a bowl to catch the serpent's venom. When she turns to empty it, the poison falls.\n\n" + Note("She has not slept in a very long time. Her arms shake. She tells everyone they are fine."));
            Add("People", "norns", "The Norns", "codex_norns",
                "Urðr, Verðandi and Skuld: What Was, What Is, What Shall Be. They tend the well beneath Yggdrasil and weave the wyrd of every living thing. Even the gods cannot unweave them.\n\n" + Note("They have taken up residence by Hel's well. They claim it has better acoustics."));

            Add("Gods", "thor", "Thor, the Thunderer", "codex_god_Thor", "Son of Odin, wielder of Mjölnir, warden of Midgard against the giants. Fated to slay Jörmungandr at the Twilight and to fall, nine steps later, from the serpent's poison.\n\nHis blessings bring lightning: bolts on struck foes, chains that leap between them, thunder from the sky.\n\n" + Note("Loud. Honest. Uncomplicated, which in Asgard counts as a kind of genius."));
            Add("Gods", "freyja", "Freyja of the Vanir", "codex_god_Freyja", "Goddess of love, war and seiðr, who takes half the battle-slain to her hall Fólkvangr. Owner of the necklace Brísingamen and a cloak of falcon feathers.\n\nHer blessings enthrall and charm: foes grow weak in her presence or turn their blades on each other.\n\n" + Note("She taught Odin magic. He has never quite forgiven her for it."));
            Add("Gods", "tyr", "Týr, the One-Handed", "codex_god_Tyr", "God of law, oaths and single combat. He placed his hand in Fenrir's mouth as a pledge of good faith while the gods bound the wolf with a lie, and lost it.\n\nHis blessings pass Verdict: a moment's pause, then judgement falls.\n\n" + Note("The bravest of the gods, and the only one who still feels the weight of what bravery cost."));
            Add("Gods", "skadi", "Skaði, the Huntress", "codex_god_Skadi", "Giantess of the mountains, goddess of winter and the hunt. She chose Njörðr for a husband by his feet alone, and left him for the snow. It was she who hung the serpent over Loki.\n\nHer blessings bring Frostbite, which slows foes and, at ten stacks, shatters them.\n\n" + Note("She is helping the son of the man she tortures. She says it is for the look on his face. I believe her."));
            Add("Gods", "odin", "Odin, the Allfather", "codex_god_Odin", "Chief of the Æsir, who hung nine nights on Yggdrasil to win the runes and gave an eye for a drink from Mímir's well. He knows how the world ends.\n\nHis blessings bring the ravens' sight: critical strikes, marks, and hunting birds.\n\n" + Note("I owe him my continued existence. I am writing this entry very carefully."));
            Add("Gods", "njord", "Njörðr of Nóatún", "codex_god_Njord", "God of the sea, the wind and wealth; a Vanir hostage given to the Æsir to seal a peace.\n\nHis blessings bring the tide: knockback, waves, and the sea's patient fury.\n\n" + Note("The calmest god in the nine worlds. He has had to be."));
            Add("Gods", "ratatoskr", "Ratatoskr, the Messenger", "codex_god_Ratatoskr", "A squirrel who runs up and down the World-Tree carrying insults between the eagle at the top and Níðhöggr at the roots.\n\nHis gossip makes you quicker: speed, dashes, dodges and bargains.\n\n" + Note("He knows everything. He tells everyone. Nobody believes him. It is, frankly, the perfect intelligence network."));

            foreach (var d in EnemyDefs.All)
                Add("Foes", d.Id, d.Name + (string.IsNullOrEmpty(d.Title) ? "" : ", " + d.Title), "codex_" + d.Id, d.Lore);

            foreach (var b in new[] { Biomes.Hall, Biomes.Niflhel, Biomes.Nastrond, Biomes.Gjoll, Biomes.Helgrind, Biomes.Surface })
                Add("Realms", b.Id, b.Name, b.Id == "hall" ? null : "codex_realm_" + b.Id, b.Subtitle + ".\n\n" + RealmText(b.Id));

            foreach (var w in Meta.Weapons)
                Add("Arms", w.Id, w.Name + ", " + w.Title, "weapon:" + w.Id, w.Desc + "\n\n" + WeaponText(w.Id));
        }

        static string Note(string s) => U.Tint("Mímir: \"" + s + "\"", U.Hex("7ec8ff"));

        static string RealmText(string id)
        {
            switch (id)
            {
                case "hall": return "Hel's hall in Helheim, whose name means Damp-With-Sleet. Its plate is Hunger, its knife Famine, its bed Sick-Bed. Hel has made it rather nicer than that, in practice.\n\n" + Note("The roof leaks. Ganglöt will fix it presently.");
                case "niflhel": return "The lowest mist-depths of Helheim, where the oathless and the lost wander as draugr. The cold here is older than the gods.\n\n" + Note("Every chamber floats over Ginnungagap, the yawning void. Do not look down. Or do, I suppose. You will only come back.");
                case "nastrond": return "The Corpse-Shore, far from the sun. A hall woven of serpents' backs, their heads turned inward, dripping venom. Oathbreakers and murderers wade its rivers, and Níðhöggr gnaws their bones.\n\n" + Note("Smells exactly as you would expect.");
                case "gjoll": return "The river nearest Hel's gates, thick with floating knives, spanned by the bridge Gjallarbrú that Móðguðr guards.\n\n" + Note("The knives are older than the water. I have never found out where they came from. Nobody has asked them.");
                case "helgrind": return "The Hel-gates, the last wall between the dead and the living. Hel herself keeps watch here, and her hound Garmr guards the cave of Gnipahellir below.\n\n" + Note("Beyond it: Midgard. A world full of people who do not know how lucky they are to be tired.");
                case "surface": return "A cave beneath Midgard where Loki lies bound on three stones, a serpent above his face, his wife beside him with a bowl.\n\n" + Note("The dead cannot linger there. Narfi's thread is woven into those chains, and the chains pull him home.");
            }
            return "";
        }

        static string WeaponText(string id)
        {
            switch (id)
            {
                case "axe": return Note("Forged in Hel's smithy from the iron of a sunken ship. It has bitten a great many bones. Narfi named it after the taste.");
                case "spear": return Note("Pulled from the river Gjöll. It always comes back to the hand that throws it. So did the man who owned it, for a while.");
                case "bow": return Note("Ullr's own yew, or a branch of the same tree. The god of winter archery has not noticed it is missing. Do not tell him.");
                case "seax": return Note("Two short blades, hungry as wolves. Forged, Hel says, from the chain that failed to hold Fenrir the first time. She may be joking. She rarely jokes.");
            }
            return "";
        }

        public static List<CodexEntry> Entries(string cat)
        {
            Build();
            return byCat.TryGetValue(cat, out var l) ? l : new List<CodexEntry>();
        }

        public static bool Unlocked(CodexEntry e)
        {
            if (string.IsNullOrEmpty(e.Flag)) return true;
            var d = SaveSystem.Data;
            if (e.Flag.StartsWith("weapon:")) return d.HasWeapon(e.Flag.Substring(7)) || d.keys > 0;
            return d.Flag(e.Flag);
        }

        public static string Text(CodexEntry e)
        {
            string t = e.Text;
            if (e.Cat == "Foes")
            {
                int k = SaveSystem.Data.Count("kill_" + e.Id);
                t += "\n\n" + U.Tint("Slain: " + k, Pal.Dim);
            }
            return t;
        }
    }
}
