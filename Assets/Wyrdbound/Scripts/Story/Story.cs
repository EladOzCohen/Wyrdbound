using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class Speaker
    {
        public string Id, Name, Art;
        public Color Color;
        public Speaker(string id, string name, Color c, string art) { Id = id; Name = name; Color = c; Art = art; }
    }

    public sealed class Line
    {
        public string Who, Text;
        public Line(string who, string text) { Who = who; Text = text; }
    }

    public delegate bool Cond(SaveData d);

    public sealed class Convo
    {
        public string Id, Ctx;
        public int Pri;
        public Cond When;
        public bool Repeat;
        public Line[] Lines;
        public string[] SetFlags;
        public string[] Choices;
        public Action<int> OnChoice;
        public Action OnDone;
    }

    /// <summary>Picks and plays conversations. Content lives in StoryDB.</summary>
    public static class Story
    {
        static readonly Dictionary<string, Speaker> speakers = new Dictionary<string, Speaker>();
        static readonly List<Convo> convos = new List<Convo>();
        static bool built;

        public static Speaker Who(string id)
        {
            Build();
            return speakers.TryGetValue(id, out var s) ? s : new Speaker(id, id, Color.white, "");
        }

        public static void AddSpeaker(string id, string name, Color c, string art) => speakers[id] = new Speaker(id, name, c, art);
        public static void Add(Convo c) => convos.Add(c);
        public static IEnumerable<Speaker> Speakers { get { Build(); return speakers.Values; } }

        static void Build()
        {
            if (built) return;
            built = true;
            StoryDB.Register();
        }

        /// <summary>Highest-priority unseen convo for a context; otherwise a random repeatable one.</summary>
        public static Convo Pick(string ctx)
        {
            Build();
            var d = SaveSystem.Data;
            Convo best = null;
            var repeats = new List<Convo>();
            foreach (var c in convos)
            {
                if (c.Ctx != ctx) continue;
                if (c.When != null && !c.When(d)) continue;
                if (c.Repeat) { repeats.Add(c); continue; }
                if (d.Seen(c.Id)) continue;
                if (best == null || c.Pri > best.Pri) best = c;
            }
            if (best != null) return best;
            if (repeats.Count > 0) return repeats[UnityEngine.Random.Range(0, repeats.Count)];
            return null;
        }

        public static bool HasNew(string ctx)
        {
            Build();
            var d = SaveSystem.Data;
            foreach (var c in convos)
                if (c.Ctx == ctx && !c.Repeat && !d.Seen(c.Id) && (c.When == null || c.When(d))) return true;
            return false;
        }

        public static void Play(string ctx, Action onDone)
        {
            var c = Pick(ctx);
            if (c == null) { onDone?.Invoke(); return; }
            Play(c, onDone);
        }

        public static void Play(Convo c, Action onDone)
        {
            var d = SaveSystem.Data;
            if (!c.Repeat) d.MarkSeen(c.Id);
            if (c.SetFlags != null) foreach (var f in c.SetFlags) d.SetFlag(f);
            SaveSystem.Save();
            Game.I.UI.Push(new DialogueScreen(c, () =>
            {
                c.OnDone?.Invoke();
                onDone?.Invoke();
            }));
        }

        /// <summary>Play a list of lines directly (no bookkeeping).</summary>
        public static void Say(Action onDone, params string[] lines)
        {
            var c = new Convo { Id = "_say", Ctx = "_", Repeat = true, Lines = Parse(lines) };
            Game.I.UI.Push(new DialogueScreen(c, onDone));
        }

        public static Line[] Parse(string[] raw)
        {
            var list = new Line[raw.Length];
            for (int i = 0; i < raw.Length; i++)
            {
                string s = raw[i];
                int k = s.IndexOf(':');
                if (k > 0 && k < 12 && !s.Substring(0, k).Contains(" ")) list[i] = new Line(s.Substring(0, k).Trim(), s.Substring(k + 1).Trim());
                else list[i] = new Line("", s);
            }
            return list;
        }

        public static string Expand(string text)
        {
            var d = SaveSystem.Data;
            string cause = string.IsNullOrEmpty(d.lastDeath) ? "the dark" : d.lastDeath;
            return text.Replace("{cause}", cause).Replace("{runs}", d.runs.ToString()).Replace("{clears}", d.clears.ToString()).Replace("{deaths}", d.deaths.ToString());
        }
    }
}
