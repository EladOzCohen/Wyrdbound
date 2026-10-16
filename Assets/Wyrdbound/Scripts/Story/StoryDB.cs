using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// The words of the saga. Every conversation, its conditions and the flags it sets.
    /// Lines are "speaker: text"; a line with no speaker is narration.
    /// </summary>
    public static class StoryDB
    {
        public static readonly string[] IntroCards =
        {
            "In the age before the Twilight, the trickster Loki brought death to Baldr the Bright.",
            "The gods' vengeance fell not on Loki alone.",
            "They turned his son Váli into a wolf — and the wolf tore apart his brother, Narfi.",
            "With Narfi's entrails, they bound Loki beneath the earth, where venom drips upon his face.",
            "Narfi died.",
            "But the dead do not rest in Helheim. Not when their father screams.",
        };

        public static string[] EndingCards(string ending)
        {
            if (ending == "cut")
                return new[]
                {
                    "Narfi lays his hand on the iron that was his body, and he pulls.",
                    "The chains part like breath. Loki rises, venom-scarred, laughing and weeping at once.",
                    "In the east, a wolf howls, and a chain of dwarf-craft snaps like thread. In the sea, a serpent opens its eyes.",
                    "In Helheim's harbor, a ship of dead men's nails slips its moorings.",
                    "At its prow stands a figure who is mostly mist now — a boy with green eyes, unravelling, smiling.",
                    "The Twilight has begun.",
                    "WYRDBOUND\nSaga II — TWILIGHT\n\nThe thread continues.",
                };
            return new[]
            {
                "Narfi closes his fist around the chain, and does not pull.",
                "\"I'm sorry, Father.\"\n\"...Don't be. It's the first honest thing anyone has done in this cave.\"",
                "Sigyn sets down the bowl for a single heartbeat to touch her son's face. The venom falls. Loki does not scream.",
                "Narfi walks back into the dark of Helheim, and this time he does not climb. He sits by Hel's fire, beside his brother, and the hall is a little less cold.",
                "Far above, the Allfather's ravens circle a cave that should have opened, and find it closed.",
                "But the Norns only smile. Threads, they say, can be cut by other hands.",
                "WYRDBOUND\nSaga II — THE OTHER HANDS\n\nThe thread continues.",
            };
        }

        public static bool EndingReady(SaveData d) =>
            d.clears >= 4 && d.Flag("norns_truth") && d.Flag("mimir_confess") && d.Flag("vali_forgiven");

        public static string GenericThanks(string id)
        {
            switch (id)
            {
                case "hel": return "Again? You will make me sentimental, brother. That would be bad for discipline.";
                case "baldr": return "You are generous for a dead man. So was I. It did not save me, but it made the hall warmer.";
                case "nanna": return "Thank you, dear. I'll pour one for Baldr too, and pretend we are at a wedding.";
                case "mimir": return "Pour slowly. I have to savour things now; I have no stomach to rush them into.";
                case "modgud": return "Another toll paid in mead. The bridge will gossip about this.";
                case "vali": return "...Good. ...Warm. ...Thank you, brother.";
                case "norns": return "We will weave a sweeter strand for you. Probably.";
                case "sigyn": return "I'll keep it beside the bowl. Something of yours, close to him.";
            }
            return "Thank you.";
        }

        // ------------------------------------------------------------------ builders

        static Convo C(string id, string ctx, int pri, Cond when, params string[] lines)
        {
            var c = new Convo { Id = id, Ctx = ctx, Pri = pri, When = when, Lines = Story.Parse(lines) };
            Story.Add(c);
            return c;
        }

        static Convo R(string id, string ctx, Cond when, params string[] lines)
        {
            var c = C(id, ctx, 0, when, lines);
            c.Repeat = true;
            return c;
        }

        static Convo F(this Convo c, params string[] flags)
        {
            c.SetFlags = flags;
            return c;
        }

        static bool K(SaveData d, string boss) => d.Count("bosskill_" + boss) > 0;
        static bool Died(SaveData d, string boss) => d.Count("deathto_" + boss) > 0;

        public static void Register()
        {
            Story.AddSpeaker("narfi", "Narfi", Pal.Narfi, "narfi");
            Story.AddSpeaker("hel", "Hel", Pal.Hel, "hel");
            Story.AddSpeaker("ganglot", "Ganglöt", U.Hex("b8a888"), "ganglot");
            Story.AddSpeaker("baldr", "Baldr", Pal.Gold, "baldr");
            Story.AddSpeaker("nanna", "Nanna", U.Hex("9ab8d8"), "nanna");
            Story.AddSpeaker("mimir", "Mímir", U.Hex("7ec8ff"), "mimir");
            Story.AddSpeaker("modgud", "Móðguðr", U.Hex("bfe4ff"), "modgud");
            Story.AddSpeaker("norns", "The Norns", U.Hex("f3e9ff"), "norns");
            Story.AddSpeaker("vali", "Váli", Pal.Danger, "vali");
            Story.AddSpeaker("hodr", "Höðr", U.Hex("a8b0bc"), "hodr");
            Story.AddSpeaker("hermod", "Hermóðr", U.Hex("c8d4e4"), "hermod");
            Story.AddSpeaker("loki", "Loki", U.Hex("7dffb0"), "loki");
            Story.AddSpeaker("sigyn", "Sigyn", U.Hex("e0c27a"), "sigyn");
            Story.AddSpeaker("thor", "Thor", Pal.God(God.Thor), "thor");
            Story.AddSpeaker("freyja", "Freyja", Pal.God(God.Freyja), "freyja");
            Story.AddSpeaker("tyr", "Týr", Pal.God(God.Tyr), "tyr");
            Story.AddSpeaker("skadi", "Skaði", Pal.God(God.Skadi), "skadi");
            Story.AddSpeaker("odin", "Odin", Pal.God(God.Odin), "odin");
            Story.AddSpeaker("njord", "Njörðr", Pal.God(God.Njord), "njord");
            Story.AddSpeaker("ratatoskr", "Ratatoskr", Pal.God(God.Ratatoskr), "ratatoskr");
            Story.AddSpeaker("nidhogg", "Níðhöggr", Pal.Venom, "nidhogg_head");
            Story.AddSpeaker("garmr", "Garmr", Pal.Blood, "garmr");

            Hel();
            Ganglot();
            Baldr();
            Nanna();
            Mimir();
            Modgud();
            Norns();
            Vali();
            Wanderers();
            Gods();
            Bosses();
            Surface();
            Gifts();
        }

        // ------------------------------------------------------------------ Hel

        static void Hel()
        {
            C("hel_first", "hel", 1000, d => true,
                "The hall is cold. It has always been cold. Somewhere, a bell tolls for no one.",
                "hel: So. You have woken.",
                "narfi: Where... I was — the wolf. Váli. His teeth were in my—",
                "hel: In your belly. Yes. And now you are in mine. My hall, I mean. Éljúðnir. Welcome home, little brother.",
                "narfi: Hel. Father's daughter. I know the stories.",
                "hel: Then you know the dead do not leave. That gate climbs to nothing but teeth and frost.",
                "narfi: They bound him with me, sister. With my own insides. I can hear him screaming through the stone.",
                "hel: Everyone in this hall can hear him. We have learned not to listen. You will learn too.",
                "narfi: I'm going up.",
                "hel: Then go. Die. Come back to my pool, and we will have this conversation again, as many times as you need.").F("met_hel");

            C("hel_return1", "hel", 200, d => d.deaths >= 1,
                "hel: Back already. The Pool of Return gives you up faster than most.",
                "narfi: I got farther than you said I would.",
                "hel: You got exactly as far as the dead always get. A few chambers of mist, and then the cold remembers you.",
                "narfi: Then I'll go a few more.",
                "hel: You are your father's son. He never knew when to stop either. Look where it got him.");

            C("hel_vali", "hel", 180, d => K(d, "vali") || Died(d, "vali"),
                "narfi: I saw Váli. In Niflhel. He's... not a man anymore.",
                "hel: No. The gods made him a wolf to do their work, and when the work was done they did not bother to unmake him.",
                "narfi: You let him rot down there?",
                "hel: I let him guard the depths. It is the only thing he can still do. Would you rather I chained him beside my throne, where he could see what he did?",
                "narfi: ...Maybe I would.",
                "hel: Careful, brother. That sounds like forgiveness. Down here, it is heavier than any chain.");

            C("hel_why", "hel", 170, d => d.runs >= 3,
                "narfi: Why do you care if I free him? You're his daughter. You should want him loose more than anyone.",
                "hel: Do you know what happens when Father is freed, Narfi?",
                "narfi: He gets up. He stops screaming.",
                "hel: He gets up. The wolf breaks his chain. The serpent leaves the sea. And a ship of dead men's nails sails out of my harbor, crewed by my people, to die a second time on a field of fire.",
                "hel: I have spent an age making this a home for the ones the gods forgot. The old, the sick, the drowned, the ones without glory. Ragnarök takes them all.",
                "narfi: So I'm supposed to leave him under the venom forever.",
                "hel: I am not telling you what to do. I am telling you what it costs.").F("hel_ragnarok");

            C("hel_nastrond", "hel", 150, d => d.Flag("reached_nastrond"),
                "hel: You smell of the Corpse-Shore. Serpent-rot. Did Níðhöggr try to eat you?",
                "narfi: He tried. Repeatedly.",
                "hel: He eats oathbreakers. Do not take it personally. He has never once been told no, and it has made him rude.");

            C("hel_garmr", "hel", 140, d => K(d, "garmr") || Died(d, "garmr"),
                "hel: Garmr likes you. He does not usually play with his food that long.",
                "narfi: That was playing?",
                "hel: He is mine, Narfi. Bound at my gate as Father is bound under the earth. Every creature in this family ends up chained to something.");

            C("hel_surface1", "hel", 300, d => d.clears >= 1,
                "hel: You struck me down. In my own house.",
                "narfi: You got back up.",
                "hel: Of course I did. I am Death. I always get back up. But you... you reached the sky. Did you see him?",
                "narfi: I saw him. And my mother.",
                "hel: Sigyn. Still holding that bowl.",
                "hel: She was kind to me once, when no one else in Asgard would look at my face. Tell her... no. Tell her nothing. She has enough to carry.");

            C("hel_surface2", "hel", 290, d => d.clears >= 2,
                "hel: Twice now you have reached the surface, and twice you have come back to my pool like a stone thrown up a well.",
                "narfi: The dead can't stay up there. I can feel myself coming apart.",
                "hel: That is not the surface, brother. That is the binding. Your thread is woven into his chains. The higher you climb, the harder it pulls you back.",
                "narfi: How do you know that?",
                "hel: Because I asked the Norns, the day they brought you to me in pieces.");

            C("hel_vali_home", "hel", 260, d => d.Flag("vali_hall"),
                "narfi: You brought Váli to the hall.",
                "hel: He asked. In words. Do you know how long it has been since that wolf used words?",
                "hel: Be gentle with him. He is still more teeth than brother. But he is trying, and down here, trying is a kind of miracle.");

            C("hel_ending_cut", "hel", 500, d => d.ending == "cut",
                "hel: You let him go.",
                "hel: The ship is not sailing yet. The Norns say the Twilight is slow to wake, like an old dog. We have a little while.",
                "hel: Stay. Fight your way up as often as you like. I will be here. Until I am not.");

            C("hel_ending_hold", "hel", 500, d => d.ending == "hold",
                "hel: You stayed.",
                "hel: I have had a thousand years of the dead coming to me because they had no choice. You are the first who chose.",
                "hel: Thank you, brother. Now go and fight something. You are making my hall sentimental.");

            R("hel_r1", "hel", null, "hel: Go on, then. The gate is where it always is.");
            R("hel_r2", "hel", null, "hel: You will die again. I will be here when you do.");
            R("hel_r3", "hel", null, "hel: Do not track mist through my hall.");
            R("hel_r4", "hel", d => d.runs >= 2, "hel: Ganglöt is still sweeping up after your last return. Be patient with her. She is slower than the grave.");
        }

        // ------------------------------------------------------------------ Ganglöt, the slow handmaid

        static void Ganglot()
        {
            C("ret_first", "return", 1000, d => d.deaths == 1,
                "ganglot: Oh. Oh! A... returner. We... do not get... many.",
                "ganglot: I am Ganglöt. Hel's... handmaid. I am... very slow. You will... get used to it. Most... take a century.",
                "ganglot: I keep... the book. Of deaths. Yours says... {cause}. I will... underline it.");

            void Cause(string id, string cause, string line) =>
                C(id, "return", 100, d => d.lastDeath == cause, "ganglot: " + line);

            Cause("ret_draugr", "Draugr", "A... draugr. The slowest... of the dead. Slower... than me. Almost. ...How?");
            Cause("ret_bowman", "Draugr Bowman", "An arrow. ...From a man... who has no eyes. I will... not ask.");
            Cause("ret_rat", "Bone-Rat", "Rats. ...I will not... tell Lady Hel. She... would laugh. She has... a terrible laugh.");
            Cause("ret_bloat", "Corpse-Bloat", "You... exploded? No... it exploded. You... were nearby. The book... is unclear.");
            Cause("ret_wraith", "Mist-Wraith", "A wraith. ...They are... only mist... and spite. ...Like... my brother.");
            Cause("ret_thrall", "Hel-Thrall", "One of... the Lady's own guards. ...They hit... very honestly.");
            Cause("ret_jarl", "Grave-Jarl", "The Jarl. ...He is still... looking for... his ship. ...You are... not his ship.");
            Cause("ret_vali", "Váli", "Your... brother. ...I am sorry. Some deaths... the book... does not want... to hold.");
            Cause("ret_serpent", "Serpent-Spawn", "Bitten. ...The book... smells... of venom now.");
            Cause("ret_spitter", "Corpse-Spitter", "Spat... upon. ...To death. ...That is... a new one.");
            Cause("ret_raven", "Corpse-Raven", "A raven. ...Not... the Allfather's. ...Probably.");
            Cause("ret_oath", "Oathbreaker", "An oathbreaker. ...He swore... he would not... hurt you. ...Didn't he?");
            Cause("ret_weaver", "Serpent-Weaver", "The Weaver. ...She asked... after you. ...I said... you were busy... dying.");
            Cause("ret_nidhogg", "Níðhöggr", "Eaten... by the dragon. He... will spit you... out. He... always... does. Eventually.");
            Cause("ret_revenant", "Blade-Revenant", "The river... of knives. ...Some knives... walk.");
            Cause("ret_wight", "River-Wight", "So cold. ...You are... very cold. I... will fetch... a blanket. ...Tomorrow.");
            Cause("ret_bell", "Wailing Bell", "Killed... by a bell. ...I will not... ring... for dinner... anymore.");
            Cause("ret_giant", "Hrímþurs", "A frost-giant. ...He was... only trying... to get warm. ...On you.");
            Cause("ret_valk", "Fallen Valkyrie", "A Valkyrie. ...She chose you. ...Badly.");
            Cause("ret_warden", "Warden of Gjallarbrú", "The bridge... said no. ...Móðguðr... will be... smug.");
            Cause("ret_garmr", "Garmr", "The hound... got you. He... is a good boy. ...To Lady Hel.");
            Cause("ret_hound", "Hel-Hound", "One of... Garmr's pups. ...They... grow up... so fast.");
            Cause("ret_einherjar", "Fallen Einherjar", "A fallen... warrior. ...Valhalla... did not want him. ...He took it... out... on you.");
            Cause("ret_hel", "Hel", "The Lady... herself. She came down... to the pool... after. She... watched you... sleep. ...Do not... tell her... I said.");
            Cause("ret_hunger", "Garmr's Hunger", "You... were too slow. ...I understand. ...Truly.");
            Cause("ret_surrender", "his own surrender", "You... came back... on your own? ...Rest, then. Even... the dead... are allowed... to rest.");
            Cause("ret_venom", "Venom", "Poison. ...Sigyn's bowl... does not... catch everything... it seems.");

            R("ret_r1", "return", null, "ganglot: Welcome... back... Master Narfi. You... died. ...I... wrote it down.");
            R("ret_r2", "return", null, "ganglot: The book... says... {cause}. ...Again? ...I will... underline it.");
            R("ret_r3", "return", d => d.deaths >= 5, "ganglot: That is... {deaths}... deaths. ...I need... a new... quill.");
            R("ret_r4", "return", d => d.clears >= 1, "ganglot: Not... the sky... this time. ...The sky... will wait. ...I... am waiting... too.");

            C("gang_1", "ganglot", 100, d => true,
                "narfi: Ganglöt, how long have you served here?",
                "ganglot: Since... the hall... was built. I... carried... the first stone. It took... nine years. ...I was not... in a hurry.",
                "narfi: And Ganglati? The other servant?",
                "ganglot: My brother. He... went to fetch... salt. ...Four hundred years ago. ...He will be back... soon.");
            C("gang_2", "ganglot", 90, d => d.runs >= 4,
                "ganglot: Master Narfi... the book... I keep. Every death... in Helheim.",
                "ganglot: Yours... are the only ones... I have to write... more than once. ...My hand... is tired.",
                "ganglot: ...Keep going.");
            C("gang_3", "ganglot", 80, d => d.clears >= 1,
                "ganglot: You... reached the sky. ...What... is it like?",
                "narfi: Grey. Wet. Loud. Everything is alive, and nobody notices.",
                "ganglot: ...That... sounds... exhausting. ...I am... glad... I am dead.");
            R("gang_r1", "ganglot", null, "ganglot: ...Yes?");
            R("gang_r2", "ganglot", null, "ganglot: I am... sweeping. ...Do not... rush me.");
        }

        // ------------------------------------------------------------------ Baldr

        static void Baldr()
        {
            C("baldr_first", "baldr", 100, d => true,
                "baldr: So you are Loki's boy. You have his eyes. That green that never quite settles.",
                "narfi: You're Baldr. The Bright One. My father killed you.",
                "baldr: Your father found the one thing in the nine worlds that had not sworn to spare me — a sprig of mistletoe — and put it in my blind brother's hand. Yes. He killed me.",
                "narfi: Then why are you smiling at me?",
                "baldr: Because you did not do it. And because I have been dead a long time, and anger is a fire. Down here, fuel is scarce.");
            C("baldr_dash", "baldr", 90, d => d.runs >= 1,
                "baldr: I watched you leave through the gate. You fight like someone who expects to lose.",
                "narfi: I usually do.",
                "baldr: Then lose differently. Strike just as your dash ends — the motion carries into the blow. Even Thor fights that way, though he will never admit he learned it from me.");
            C("baldr_cast", "baldr", 85, d => d.runs >= 2,
                "baldr: Your rune-stones. Throw them, then go and take them back from the fallen.",
                "baldr: A warrior who keeps his stones in his pocket is only carrying rocks.");
            C("baldr_nanna", "baldr", 70, d => d.runs >= 4,
                "baldr: Nanna followed me here, you know. Her heart broke on my pyre.",
                "baldr: She chose this. Every morning I wake in a dead god's hall and she is beside me, and I do not know how to deserve that.");
            C("baldr_hodr", "baldr", 120, d => d.Flag("met_hodr"),
                "narfi: I met Höðr, in the mists. Your brother. The one who threw the mistletoe.",
                "baldr: Höðr. How is he?",
                "narfi: Blind. Sorry. He asked if you hate him.",
                "baldr: Tell him the truth. That I never once did. Tell him I would have caught the dart myself, if I had seen it coming, just so it would not have been his hand.").F("baldr_hodr");
            C("baldr_hope", "baldr", 110, d => d.runs >= 6 && d.Flag("hel_ragnarok"),
                "narfi: Hel says if I free my father, the world ends.",
                "baldr: Yes. The world ends. And then, the völva says, it begins again — green, out of the sea. And Höðr and I walk out of Hel together into the new sun.",
                "narfi: ...You want me to do it.",
                "baldr: I want nothing, Narfi. I am a dead god in a cold hall.",
                "baldr: But I will not pretend I have not counted the years.").F("baldr_hope");
            C("baldr_odin", "baldr", 130, d => d.Flag("mimir_confess"),
                "baldr: So Mímir told you. Father's eye in Hel's hall.",
                "baldr: I should be angry. But Odin has been playing this game since before the first tree. Every one of us is a piece on his board — even the ones he loves. Especially those.");
            C("baldr_sky", "baldr", 140, d => d.clears >= 1,
                "baldr: You reached the surface! I can see it on you. The light clings. Tell me, is the sun still the same colour?",
                "narfi: I was in a cave.",
                "baldr: Ah. Well. Next time.");
            R("baldr_r1", "baldr", null, "baldr: Dash, then strike. Always.");
            R("baldr_r2", "baldr", null, "baldr: The hall is brighter when you are in it. Do not tell Hel I said so; she prefers it dim.");
            R("baldr_r3", "baldr", null, "baldr: Go gently, Narfi. Or at least go.");
        }

        // ------------------------------------------------------------------ Nanna

        static void Nanna()
        {
            C("nanna_first", "nanna", 100, d => true,
                "nanna: You must be Narfi. Baldr told me. Sit, if the dead can sit.",
                "nanna: ...You look like your mother.",
                "narfi: You knew Sigyn?",
                "nanna: Everyone in Asgard knew Sigyn. The only one in that hall who was kind to everyone, even when it cost her. She married your father anyway.",
                "nanna: Kindness is not the same as wisdom.");
            C("nanna_linen", "nanna", 80, d => d.runs >= 2,
                "nanna: I wove linen once, for Frigg, and sent it up with Hermóðr when he came to beg for Baldr.",
                "nanna: ...Do you think it reached her?",
                "narfi: I don't know.",
                "nanna: It does not matter. Some gifts are for the giver.");
            C("nanna_sigyn", "nanna", 120, d => d.clears >= 1,
                "nanna: You saw her. Sigyn. How does she look?",
                "narfi: Tired. Her arms shake. She holds the bowl over him and she never stops.",
                "nanna: Then she is still herself.",
                "nanna: Narfi. When you see her again, tell her someone down here remembers her name.").F("nanna_message");
            C("nanna_grief", "nanna", 70, d => d.runs >= 5,
                "nanna: People say I died of grief. That is not quite right.",
                "nanna: I died of choosing. I chose to follow him. Grief was only the road.");
            R("nanna_r1", "nanna", null, "nanna: Eat something. ...Oh. Habit.");
            R("nanna_r2", "nanna", null, "nanna: The linen is fraying. Everything does, down here. Even us.");
        }

        // ------------------------------------------------------------------ Mímir

        static void Mimir()
        {
            C("mimir_first", "mimir", 100, d => true,
                "mimir: Ah! A new face. Well — a returning face. Forgive me, I do not get up much.",
                "narfi: You're a head.",
                "mimir: Mímir. Once the wisest of the Æsir — and still, if we are honest — but now considerably more portable.",
                "mimir: Ask, and I will tell you what I know of the dead and their makers. I keep a record. Choose 'Mímir's Lore' and read.",
                "narfi: Why are you in Hel's hall?",
                "mimir: A gift. From Odin, to Hel. A diplomatic present. One does not ask a gift why it is on the shelf.");
            C("mimir_lore", "mimir", 90, d => d.runs >= 1,
                "mimir: Each foe you fell, each god who blesses you — I write it down.",
                "mimir: Knowledge is the only thing in this hall that does not decay. Well. That and Ganglöt.");
            C("mimir_suspect", "mimir", 120, d => d.runs >= 5,
                "narfi: Mímir. When I was at the gate today, I saw a raven on the stones. Odin's raven. Down here.",
                "mimir: Ravens go where they like. That is rather the point of them.",
                "narfi: It flew to you.",
                "mimir: ...Did it? How remarkable. My eyes are not what they were.").F("mimir_suspect");
            C("mimir_confess", "mimir", 200, d => d.Flag("mimir_suspect") && d.runs >= 8 && (K(d, "garmr") || d.clears >= 1),
                "narfi: Tell me the truth. You talk to Odin.",
                "mimir: ...Every night, the ravens come. Huginn and Muninn. Thought and Memory. And I tell them everything I have seen in Hel's hall. Yes.",
                "narfi: Why?",
                "mimir: Because he made me, in the way that matters. Because a head cannot run. And because — listen to me, boy — because Odin needs your father bound. Not forever. Until the right moment.",
                "narfi: What moment?",
                "mimir: The one the völva foretold. The Twilight. Odin knows how it ends, and he means for it to end exactly that way, not a day early.",
                "mimir: When the gods made Váli a wolf, it was not cruelty. It was carpentry. They needed a binding no god could untie. They needed you.",
                "narfi: My death was... a joint in a beam.",
                "mimir: A load-bearing one. I am sorry. I did not choose it. But I have watched you climb, and I have started to wonder whether a beam can choose to fall.").F("mimir_confess");
            C("mimir_after", "mimir", 110, d => d.Flag("mimir_confess"),
                "mimir: I have stopped telling the ravens everything. Only most things.",
                "mimir: It is a start. One does not unlearn a thousand years of loyalty in an afternoon. Even with no body to slow one down.");
            R("mimir_r1", "mimir", null, "mimir: Knowledge is the only treasure that grows heavier the more you give away.");
            R("mimir_r2", "mimir", null, "mimir: Hm? Oh, I was not asleep. I was thinking with my eyes closed. An entirely different thing.");
        }

        // ------------------------------------------------------------------ Móðguðr

        static void Modgud()
        {
            C("modgud_first", "modgud", 100, d => true,
                "modgud: Halt. Name and business.",
                "narfi: Narfi. Leaving.",
                "modgud: Ha. The bridge Gjallarbrú rings when the living cross it. It rings like thunder for five troops of dead men. For you it makes no sound at all. And yet you walk toward the sky.",
                "modgud: I am Móðguðr. I keep the bridge, and I keep a little shop, because even the dead want things. Silver buys what you need. Hel's silver, mind. It spends nowhere else.");
            C("modgud_2", "modgud", 80, d => d.runs >= 2,
                "modgud: Back. Your silver jingles more each time. Either you are getting better at dying or worse at it.");
            C("modgud_hermod", "modgud", 90, d => d.Flag("met_hermod"),
                "modgud: Hermóðr crossed my bridge once, on Sleipnir, riding for Baldr. Five troops of dead men made less noise.",
                "modgud: A living man, coming down into death, on purpose, for love. You are the opposite of that.",
                "modgud: Still. It is a rare kind of foolishness, and I find I respect it.");
            C("modgud_bridge", "modgud", 120, d => d.clears >= 1,
                "modgud: The bridge rang, you know. Last time. Just once, very faintly, as you passed.",
                "modgud: I have kept this bridge since before your father was born. It has never rung for a dead man.",
                "narfi: What does it mean?",
                "modgud: It means buy something. I have a feeling you will need it.");
            R("modgud_r1", "modgud", null, "modgud: Silver first. Questions never.");
            R("modgud_r2", "modgud", null, "modgud: Mind the knives in the river. They are older than the water.");
            R("modgud_r3", "modgud", null, "modgud: Browse. Quickly.");
        }

        // ------------------------------------------------------------------ The Norns

        static void Norns()
        {
            C("norns_first", "norns", 100, d => true,
                "norns: Urðr, who weaves what was. Verðandi, who weaves what is. Skuld, who weaves what shall be.",
                "norns: Little knot. Little tangle. We have been waiting for you to look up from the loom of your own death.",
                "narfi: The Norns. Here?",
                "norns: Where else is the thread of Hel spun? Every death passes through our fingers. Yours passes through them again, and again, and again.",
                "norns: We will weave you harder paths, if you ask. The Weave of Wyrd. Pull a thread and the dead grow fiercer. Escape anyway, and we will reward you.").F("norns_met");
            C("norns_truth", "norns", 200, d => d.clears >= 2 && d.Flag("norns_met"),
                "narfi: You called me a knot.",
                "norns: Because you are one. Your father is bound with chains that were once your body. The chains are your thread, turned to iron at the gods' command.",
                "norns: Cut the chains, and you unravel. Hold them, and he stays. You are not the key to your father's prison, Narfi.",
                "norns: You are the lock.",
                "narfi: Then there's no way to free him and live.",
                "norns: Live? Sweet child. You have not been alive for a very long time. The question was never whether you live.",
                "norns: The question is what your thread holds when it is finally pulled.").F("norns_truth");
            C("norns_odin", "norns", 150, d => d.Flag("norns_truth") && d.Flag("mimir_confess"),
                "norns: The Allfather came to our well, once, and drank, and gave an eye. He saw the end.",
                "norns: He has been trying to arrive at it on time ever since. He thinks he is the weaver.",
                "norns: He is only very, very good at pulling threads.");
            C("norns_heat", "norns", 90, d => d.bestHeat >= 3,
                "norns: You wear the weave well. Tighter. Tighter.",
                "norns: We love a thread that does not snap.");
            R("norns_r1", "norns", null, "norns: Pull a thread, little knot.");
            R("norns_r2", "norns", null, "norns: What was, what is, what shall be. You are all three. It is very confusing for us.");
        }

        // ------------------------------------------------------------------ Váli

        static void Vali()
        {
            C("bv_first", "boss_vali", 100, d => true,
                "The mist thickens into the shape of a wolf the size of a longhouse. It has a man's eyes.",
                "vali: ...Rrrhh... ...brother.",
                "narfi: Váli? Váli, it's me — it's Narfi!",
                "vali: ...I know. ...I remember... the taste.",
                "vali: The gods... said... guard. So I guard. ...Run, little brother. Please. ...Run.").F("met_vali");
            C("bv_again", "boss_vali", 90, d => !K(d, "vali") && Died(d, "vali"),
                "vali: ...Again. ...Why do you... come back? ...I do not want... to do it again.",
                "narfi: Then don't.",
                "vali: ...I cannot... stop. ...The curse... is in my teeth.");
            C("bv_won", "boss_vali", 80, d => K(d, "vali") && !d.Flag("vali_hall"),
                "vali: ...You... beat me. ...It felt... like waking.",
                "vali: ...Do it... again.");
            C("bv_hall", "boss_vali", 120, d => d.Flag("vali_hall"),
                "vali: Brother. Hel lets me sleep in her hall now. ...I still have to guard. But I dream... in words, now.",
                "vali: ...Come. Let us... fight. It is the only way... I know how to hold you.");
            R("bv_r1", "boss_vali", null, "vali: ...Rrrhh... ...run.");
            R("bv_r2", "boss_vali", null, "Váli lowers his head. The mist howls with him.");

            C("bdv_1", "bossdown_vali", 100, d => d.Count("bosskill_vali") == 1,
                "vali: ...Hhh... You are... strong, now. ...Go up. Find Father.",
                "vali: ...Tell him... I did not... want to.");
            C("bdv_3", "bossdown_vali", 200, d => d.Count("bosskill_vali") >= 3 && !d.Flag("vali_hall"),
                "vali: ...Three times... you have put me down. Each time... I remember more. ...My name. Our mother's... hands.",
                "Hel's voice rolls up out of the mist, from very far below.",
                "hel: Enough, Váli. Come home. Come and sleep by my fire. Narfi will find you there.").F("vali_hall");
            R("bdv_r", "bossdown_vali", null, "vali: ...Go.");

            C("vali_hub1", "vali", 100, d => true,
                "vali: ...Brother. The fire is... warm. I did not... remember warm.",
                "narfi: Váli. I don't know what to say to you.",
                "vali: ...Neither do I. ...Sit with me, then. Wolves... are good at silence.");
            C("vali_hub2", "vali", 90, d => d.Aff("vali") >= 1,
                "vali: The mead... helps. The words... come easier.",
                "vali: ...Narfi. I killed you.",
                "narfi: The gods killed me. They used your mouth.",
                "vali: ...It was still... my mouth.");
            C("vali_hub3", "vali", 80, d => d.Aff("vali") >= 2,
                "vali: I dreamed... of Odin. He came to me... before. Before the curse. He... smiled.",
                "vali: He said: 'This will hurt, boy, but it will matter.' ...I hate... that it mattered.",
                "narfi: So do I. ...But I don't hate you.",
                "vali: ...Say it again.",
                "narfi: I don't hate you, brother.",
                "The wolf closes his eyes. For the first time since you have known him, his breathing slows.").F("vali_forgiven");
            R("vali_r1", "vali", null, "vali: ...Brother.");
            R("vali_r2", "vali", null, "vali: The chain... does not hurt. Hel made it... soft.");
        }

        // ------------------------------------------------------------------ wanderers in the depths

        static void Wanderers()
        {
            C("hodr_1", "hodr", 100, d => true,
                "hodr: Who walks? I cannot see you. I cannot see anything. I never could.",
                "narfi: Höðr. Baldr's brother.",
                "hodr: Then you know what I did. Loki put the dart in my hand and turned me toward the light, and said, 'Honour him, as the others do.' I threw. Everyone was laughing, and then no one was.",
                "narfi: My father did that to you.",
                "hodr: Your father? ...Then you are Narfi. The gods' revenge reached even you.",
                "hodr: Here. Take my blessing. It is not much. Blind men cannot see where it lands.").F("met_hodr");
            C("hodr_2", "hodr", 90, d => d.Flag("baldr_hodr"),
                "narfi: Baldr says he never hated you. He says he would have caught the dart himself, so it wouldn't have been your hand.",
                "hodr: ...He said that?",
                "narfi: He did.",
                "hodr: ...Then I can stop listening for him to come and curse me. I have been listening for so long.");
            C("hodr_3", "hodr", 80, d => d.Flag("met_hodr"),
                "hodr: The other Váli killed me, you know. Odin's son, born to avenge Baldr. A day old, and he walked into the world and killed me.",
                "hodr: Two Vális, two killings. The gods are not imaginative with names.");
            R("hodr_r", "hodr", null, "hodr: I hear your footsteps more clearly each time. You are becoming real to me.");

            C("hermod_1", "hermod", 100, d => true,
                "hermod: Hold — a living man? No. You're dead. I can tell. I'm Hermóðr. I rode Sleipnir nine nights down to Hel, to beg her to send Baldr home.",
                "narfi: And she said no.",
                "hermod: She said yes. If every thing in the worlds wept for him. And everything did. Stones, trees, the very frost. All but one old giantess in a cave, who said she'd keep Hel's dead and dry her eyes.",
                "narfi: ...Let me guess who the giantess really was.",
                "hermod: Your father, wearing a hag's face. Here — silver, and bread. I've stopped expecting things to go right, but I still like to help them along.").F("met_hermod");
            C("hermod_2", "hermod", 90, d => d.Flag("met_hermod"),
                "hermod: I keep riding back. Odin sends me with messages for Hel. She never answers.",
                "hermod: I think I'm the only messenger in the nine worlds whose whole job is to be ignored.");
            C("hermod_3", "hermod", 120, d => d.Flag("mimir_confess"),
                "hermod: There's a thing I never told anyone. When I rode out of Hel, Odin met me on the road and asked me one question.",
                "hermod: 'Does the trickster's son still breathe?' This was before you died, Narfi. I didn't understand it then.");
            R("hermod_r", "hermod", null, "hermod: Ride safe. Well. Walk.");
        }

        // ------------------------------------------------------------------ the gods

        static void Gods()
        {
            C("thor_1", "god_thor", 100, d => true,
                "thor: HA! Loki's whelp, crawling up out of Hel! I should squash you on principle.",
                "narfi: Thor.",
                "thor: Your father shaved my wife bald, you know. Got me dressed as a bride. Nearly got us all killed a dozen times. And yet.",
                "thor: The way the gods paid him back, through you... it sat wrong in my belly. Take my lightning. Hit things. Hard. That part, at least, I understand.");
            C("thor_2", "god_thor", 80, d => d.runs >= 3,
                "thor: The serpent stirs in the sea, boy. Jörmungandr. Your brother, I suppose. One day he and I will kill each other.",
                "thor: I think about it every morning. It's restful, knowing.");
            C("thor_3", "god_thor", 90, d => d.Flag("mimir_confess"),
                "thor: Odin? Scheming? Father's always scheming. I just hit what he points at.",
                "thor: ...Is that so wrong? ...Don't answer that.");
            R("thor_r1", "god_thor", null, "thor: Strike like you mean it!");
            R("thor_r2", "god_thor", null, "thor: Thunder for the son of mischief. Don't tell Sif.");
            R("thor_r3", "god_thor", null, "thor: More lightning! Always more lightning!");

            C("freyja_1", "god_freyja", 100, d => true,
                "freyja: Oh, look at you. A lost little ghost with such angry eyes.",
                "narfi: Freyja.",
                "freyja: Half the battle-dead are mine, you know. Odin takes his share to Valhalla; I take mine to Fólkvangr. You died in no battle... but I could make an exception.",
                "narfi: I'm not looking for a new hall.",
                "freyja: No. You're looking for your father. How sweet. Take my blessing, and let us see how far love carries a dead boy.");
            C("freyja_2", "god_freyja", 80, d => d.runs >= 4,
                "freyja: Odin thinks I don't know about his little ravens in Hel's hall.",
                "freyja: Darling, I taught him seiðr. I know everything he knows, and several things he's forgotten.");
            R("freyja_r1", "god_freyja", null, "freyja: Charm them, darling. It's kinder than killing. Usually.");
            R("freyja_r2", "god_freyja", null, "freyja: For you, little ghost. Don't waste it.");

            C("tyr_1", "god_tyr", 100, d => true,
                "tyr: I know you, Narfi. I knew your brother Fenrir. I put my hand in his mouth as a pledge, so that he would let us bind him.",
                "narfi: And he bit it off.",
                "tyr: He did. Because we lied. The binding was a lie, and my hand was the price of the lie. I have paid for every chain the gods have forged since.",
                "tyr: Yours too. Take my blade. If you must cut a binding, cut it honestly.");
            C("tyr_2", "god_tyr", 120, d => d.Flag("norns_truth"),
                "tyr: So you have learned what the chains are made of.",
                "tyr: I am sorry, boy. An oath kept is not always an oath worth keeping.");
            R("tyr_r1", "god_tyr", null, "tyr: Judge them. Swiftly.");
            R("tyr_r2", "god_tyr", null, "tyr: My hand is gone. My sword arm is not.");

            C("skadi_1", "god_skadi", 100, d => true,
                "skadi: I hung the serpent over your father's face.",
                "narfi: ...What?",
                "skadi: The venom that drips on him. My serpent. He mocked my father's death and laughed at my grief, and when the gods bound him, I asked for the privilege.",
                "narfi: Then why help me?",
                "skadi: Because a hunter respects prey that keeps running. And because I want to see his face when his son walks up out of Hel with my frost in his hands.");
            C("skadi_2", "god_skadi", 120, d => d.clears >= 1,
                "skadi: You saw him. Under my serpent. Did he scream?",
                "skadi: ...You don't have to answer. I can see it in how you hold the frost.");
            R("skadi_r1", "god_skadi", null, "skadi: Patience. The mountain always wins.");
            R("skadi_r2", "god_skadi", null, "skadi: Cold hands. Steady aim.");

            C("odin_1", "god_odin", 100, d => true,
                "odin: Narfi. Son of Loki. You climb well.",
                "narfi: Allfather. You, of all gods — why bless me?",
                "odin: Because I have seen how this ends, and you are in it. Take my ravens' sight. You will need to see further than you think.",
                "narfi: That's not an answer.",
                "odin: It is the only kind of answer I give.");
            C("odin_2", "god_odin", 150, d => d.Flag("mimir_confess"),
                "narfi: Mímir told me. About Váli. About the binding. About why.",
                "odin: Mímir talks too much for a man without lungs.",
                "narfi: Was any of it a lie?",
                "odin: None of it. I made your brother a wolf. I made you a chain. I would do it again. The worlds need an ending, Narfi, or they rot. I chose the shape of the ending. Someone had to.",
                "narfi: Then why help me now?",
                "odin: Because the ending needs the chain to break, when it is time. I am only making certain you are strong enough to be the one who breaks it.").F("odin_admit");
            R("odin_r1", "god_odin", null, "odin: See further.");
            R("odin_r2", "god_odin", null, "odin: Every blow, a rune. Every rune, a choice.");

            C("njord_1", "god_njord", 100, d => true,
                "njord: Peace, son of Loki. I am Njörðr, of the Vanir. I came to Asgard as a hostage, to end a war.",
                "njord: I have lived my whole life as a guarantee for other men's promises.",
                "narfi: Then we have something in common.",
                "njord: We do. Take the tide's blessing. The sea does not care whose son you are.");
            C("njord_2", "god_njord", 80, d => d.runs >= 5,
                "njord: Skaði was my wife, once. She chose me by my feet — feet were all she was allowed to see.",
                "njord: We could not live together. Her mountains, my sea. Some bindings are made by choosing wrong.");
            R("njord_r1", "god_njord", null, "njord: Let the tide carry them away.");
            R("njord_r2", "god_njord", null, "njord: Calm, boy. Calm wins long wars.");

            C("rat_1", "god_ratatoskr", 100, d => true,
                "ratatoskr: Oho! Who's this, who's this? A dead boy going UP? Nobody goes up! I go up! I go up and down! Up and down the World-Tree, carrying insults between the eagle and the dragon!",
                "narfi: You're Ratatoskr.",
                "ratatoskr: Famous! I'm famous! Here, have some gossip, it makes you fast.",
                "ratatoskr: Did you know Níðhöggr is sensitive about his breath? Don't tell him I told you. Actually — do tell him, it's funnier.");
            C("rat_2", "god_ratatoskr", 80, d => d.runs >= 3,
                "ratatoskr: Psst. Psst! The eagle says Odin's ravens fly to Hel every night. Every night! What for, what for? Somebody's telling secrets!",
                "ratatoskr: Not me. Well. Also me.");
            C("rat_3", "god_ratatoskr", 120, d => d.Flag("mimir_confess"),
                "ratatoskr: You found out about the head! Ha! I KNEW you'd find out! I've known for six hundred years!",
                "ratatoskr: Nobody ever asks the squirrel!");
            R("rat_r1", "god_ratatoskr", null, "ratatoskr: Up and down, up and down!");
            R("rat_r2", "god_ratatoskr", null, "ratatoskr: Speed's free! Gossip's cheap! Take both!");
        }

        // ------------------------------------------------------------------ bosses

        static void Bosses()
        {
            C("bn_1", "boss_nidhogg", 100, d => true,
                "The shore heaves. Something enormous lifts out of a sea of corpses, chewing.",
                "nidhogg: Mmmh. A thread of Loki's. I can smell the binding on you. Delicious.",
                "nidhogg: Oathbreakers taste of ash. But you... you taste of a promise.");
            C("bn_rat", "boss_nidhogg", 90, d => d.Flag("met_Ratatoskr"),
                "nidhogg: The squirrel told you something about me. I can tell.",
                "nidhogg: I will find that squirrel.");
            R("bn_r1", "boss_nidhogg", d => K(d, "nidhogg") || Died(d, "nidhogg"), "nidhogg: Back on my shore? I am always hungry, little knot. Always.");
            R("bn_r2", "boss_nidhogg", null, "nidhogg: Mmmh. Food.");
            R("bdn_r", "bossdown_nidhogg", null, "nidhogg: ...Hhh. Go, then. The root will still be here. So will I.");

            C("bg_1", "boss_garmr", 100, d => true,
                "Before the cave of Gnipahellir, chained to the living rock, a hound with a chest soaked red raises its head.",
                "garmr: GRRRRR.",
                "narfi: ...Good dog?",
                "garmr: GRRRRRRRRR.");
            C("bg_2", "boss_garmr", 90, d => K(d, "garmr"),
                "garmr: Grrr... *sniff*... ...*thump, thump*.",
                "narfi: Did you just wag your tail at me?",
                "garmr: GRRRR.");
            R("bg_r1", "boss_garmr", null, "garmr: GRRRRR.");
            R("bg_r2", "boss_garmr", null, "Garmr's chain rattles. He is grinning, in the way of dogs.");
            R("bdg_r", "bossdown_garmr", null, "Garmr lies down, panting, and lets you pass. He looks almost pleased.");

            C("bh_1", "boss_hel", 100, d => true,
                "hel: You came all this way. Through the mist, the shore, the river of knives. To my gate.",
                "narfi: Let me through, sister.",
                "hel: I cannot. Not because I do not love you. Because I do.",
                "hel: If you want the sky, Narfi, you will have to kill Death in her own house. Come. Let us see if it can be done.");
            C("bh_2", "boss_hel", 90, d => K(d, "hel"),
                "hel: Again. You know I will rise.",
                "narfi: I know.",
                "hel: Then why do you keep doing this?",
                "narfi: Because he's my father, and he's screaming.",
                "hel: ...He is mine too. Come, then.");
            C("bh_3", "boss_hel", 120, d => d.Flag("norns_truth"),
                "hel: The Norns told you what you are. And still you climb.",
                "narfi: I want to see him one more time. Before I decide.",
                "hel: Decide? ...Oh, little brother.");
            C("bh_4", "boss_hel", 130, d => d.ending != "",
                "hel: After everything, you still come up here to fight me.",
                "narfi: It's the only way out.",
                "hel: It's the only way out you've ever let yourself see.");
            R("bh_r1", "boss_hel", null, "hel: To the gate, then.");
            R("bh_r2", "boss_hel", null, "hel: Raise your weapon. I will not hold back.");
            C("bdh_1", "bossdown_hel", 100, d => d.Count("bosskill_hel") == 1,
                "hel: ...Go. The sky is right there. Go and see what you climbed for.");
            R("bdh_r1", "bossdown_hel", null, "hel: Go. Before I change my mind.");
            R("bdh_r2", "bossdown_hel", null, "hel: ...I will be at the pool.");
        }

        // ------------------------------------------------------------------ the surface

        static void Surface()
        {
            C("surf_1", "surface", 500, d => d.clears == 0,
                "A cave, wet and low. Three stones, and on them a man bound with iron that was once flesh. Above him a serpent drips venom. Beside him a woman holds up a bowl.",
                "sigyn: ...Narfi?",
                "narfi: Mother.",
                "sigyn: No. No, no — you're dead, I saw — oh, my boy, what have they done to you now?",
                "loki: Sigyn, my love, mind the bowl, it's — ah. Ah. Hello, son.",
                "narfi: Father.",
                "loki: You've come up out of Hel. Of course you have. You always were the stubborn one. Váli was the angry one; you were the one who would never stop asking why.",
                "narfi: I'm going to get you out.",
                "loki: Are you? Look at the chains, Narfi. Look closely.",
                "The iron is warm. It has a pulse. It is your pulse.",
                "narfi: ...They're me.",
                "loki: They're you. Clever gods. Cruel, but clever. I will give them that.",
                "The air of Midgard pulls at you. You are coming unthreaded.",
                "sigyn: You're fading. Go, sweetheart. Go before it takes you. Please.").F("met_loki");
            C("surf_2", "surface", 400, d => d.clears == 1,
                "loki: Back again. You'll wear a path in the world, coming and going like this.",
                "narfi: Tell me how to break them.",
                "loki: You know how. You just don't like it. Cut the chain, and the chain is you. Ah, but what's a son for, if not to save his father?",
                "sigyn: Loki.",
                "loki: I'm joking, my love. Mostly.");
            C("surf_nanna", "surface", 350, d => d.Flag("nanna_message"),
                "narfi: Mother. Nanna sent word. She says someone down there still remembers your name.",
                "sigyn: Nanna...",
                "sigyn: Tell her I remember hers. Tell her I'm sorry about Baldr. Tell her — tell her the bowl is not so heavy, when someone remembers.");
            C("surf_3", "surface", 300, d => d.clears >= 2,
                "sigyn: Narfi. Come here, where he can't hear.",
                "sigyn: Odin came. Years ago. He stood where you stand and he looked at your father and he said, 'Not yet.' Just that. 'Not yet.'",
                "narfi: He wants him freed. Just — later. At the end.",
                "sigyn: Then don't give him his ending, my love. Find your own.").F("sigyn_odin");
            C("surf_4", "surface", 200, d => d.clears >= 3 && !d.Flag("norns_truth"),
                "loki: You look like you're carrying a question too big for your mouth.",
                "loki: Ask the Norns. They love a question. They hate an answer.");
            C("surf_5", "surface", 250, d => d.clears >= 3 && d.Flag("norns_truth"),
                "loki: So you know. You're the lock.",
                "loki: I'm sorry, son. I truly am. It's the first thing I've been sorry for in a thousand years, and it's this.",
                "sigyn: There must be another way.",
                "loki: There's always another way, my love. That's the one thing I've ever known for certain.");
            R("surf_r1", "surface", null, "loki: Son.", "sigyn: Go before it takes you, sweetheart.");
            R("surf_r2", "surface", null, "sigyn: My arms are fine. They are always fine.", "loki: She lies beautifully. I taught her nothing; she was born to it.");
            R("surf_r3", "surface", d => d.ending != "", "loki: You made your choice, son. The worlds will spin on it a while yet.");

            var end = C("ending", "ending", 1000, d => true,
                "The cave is silent but for the drip of venom. Sigyn's bowl is nearly full.",
                "norns: Little knot. You have climbed enough. The thread is in your hands.",
                "loki: Well, son? The whole of the nine worlds is waiting on you. No pressure.",
                "narfi: If I cut it, you go free. And the wolf breaks loose, and the serpent rises, and Hel's dead sail out to die again.",
                "loki: And the gods who did this to you fall. All of them. Odin, too. Don't forget that part. It's the best part.",
                "sigyn: And if you hold it?",
                "narfi: Then he stays. And I stay. Forever. I become the chain.",
                "norns: Choose.");
            end.Choices = new[] { "Cut the bonds. Let the Twilight come.", "Hold the knot. Let the world go on." };
            end.OnChoice = i =>
            {
                var d = SaveSystem.Data;
                d.ending = i == 0 ? "cut" : "hold";
                d.SetFlag("ending_seen");
                SaveSystem.Save();
            };
        }

        // ------------------------------------------------------------------ gifts

        static void Gifts()
        {
            C("gift_hel_1", "gift_hel", 100, d => true,
                "narfi: For you. From the deep cellars.",
                "hel: Mead of the dead. You brought me my own mead.",
                "hel: ...Very well. Take this. My half-mask. I wore it the day I was given this realm, so the gods would not see which half of my face was weeping.");
            C("gift_baldr_1", "gift_baldr", 100, d => true,
                "baldr: Mead! You did not have to. Here — take this, I have been waiting to give it to someone.",
                "baldr: A ring. Or the echo of one. Draupnir. Father laid the true ring on my pyre, and every ninth night it drips eight more. This is one of the drips.");
            C("gift_nanna_1", "gift_nanna", 100, d => true,
                "nanna: Oh, you dear thing.",
                "nanna: Here — linen. I kept a strip back, from the bolt I sent to Frigg. Wrap it round your wounds. It holds.");
            C("gift_mimir_1", "gift_mimir", 100, d => true,
                "mimir: You will have to pour it — yes, into my mouth, thank you — ah. That is wonderful.",
                "mimir: In return, a draught from my well. Well, the memory of one. It will sharpen your luck.");
            C("gift_modgud_1", "gift_modgud", 100, d => true,
                "modgud: Mead? For the bridge-keeper?",
                "modgud: ...Nobody has ever paid me a toll. Here. A stone from the bridge. Hold it, and the bridge will remember you.");
            C("gift_vali_1", "gift_vali", 100, d => true,
                "vali: ...Mead. Like at home.",
                "vali: ...Take this. My tooth. The one that... found you. I do not want it... in my mouth... anymore.");
            C("gift_norns_1", "gift_norns", 100, d => true,
                "norns: Mead for the weavers. How quaint.",
                "norns: Here: a single strand. Your own. Do not pull it.");
            C("gift_sigyn_1", "gift_sigyn", 100, d => true,
                "sigyn: Oh, sweetheart. I can't drink it — my hands, the bowl — but I'll keep it.",
                "sigyn: Here. A chip from the rim. It's warm. Keep it close when you climb.");
        }
    }
}
