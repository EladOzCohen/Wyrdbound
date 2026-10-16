using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wyrdbound
{
    [Serializable]
    public class KV
    {
        public string k;
        public int v;
        public KV() { }
        public KV(string k, int v) { this.k = k; this.v = v; }
    }

    /// <summary>Everything that persists between runs. JSON via JsonUtility.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int embers, keys, mead;
        public int runs, clears, deaths, kills;
        public float playTime;
        public string weapon = "axe";
        public List<string> weapons = new List<string> { "axe" };
        public List<string> flags = new List<string>();
        public List<KV> counters = new List<KV>();
        public List<KV> runes = new List<KV>();
        public List<string> runeUnlocks = new List<string>();
        public List<KV> affinity = new List<KV>();
        public List<string> seen = new List<string>();
        public List<string> keepsakes = new List<string>();
        public string keepsake = "";
        public List<KV> weave = new List<KV>();
        public int bestHeat = -1;
        public string lastDeath = "";
        public string ending = "";
        public float bestTime;

        static KV Find(List<KV> l, string k)
        {
            for (int i = 0; i < l.Count; i++) if (l[i].k == k) return l[i];
            return null;
        }

        static int Get(List<KV> l, string k) { var e = Find(l, k); return e != null ? e.v : 0; }

        static void Set(List<KV> l, string k, int v)
        {
            var e = Find(l, k);
            if (e == null) l.Add(new KV(k, v)); else e.v = v;
        }

        public bool Flag(string f) => flags.Contains(f);

        public void SetFlag(string f, bool on = true)
        {
            if (on) { if (!flags.Contains(f)) flags.Add(f); }
            else flags.Remove(f);
        }

        public int Count(string k) => Get(counters, k);
        public void AddCount(string k, int n = 1) => Set(counters, k, Get(counters, k) + n);
        public int Rune(string id) => Get(runes, id);
        public void SetRune(string id, int r) => Set(runes, id, r);
        public int Aff(string npc) => Get(affinity, npc);
        public void AddAff(string npc, int n = 1) => Set(affinity, npc, Get(affinity, npc) + n);
        public int Weave(string id) => Get(weave, id);
        public void SetWeave(string id, int r) => Set(weave, id, r);
        public bool Seen(string id) => seen.Contains(id);
        public void MarkSeen(string id) { if (!seen.Contains(id)) seen.Add(id); }
        public bool HasWeapon(string id) => weapons.Contains(id);
    }

    [Serializable]
    public class Settings
    {
        public float master = 0.8f, music = 0.6f, sfx = 0.9f, shake = 1f;
        public bool numbers = true, fullscreen = true, mouseAim = true, grace = false, flashes = true;
    }

    public static class SaveSystem
    {
        public static SaveData Data = new SaveData();
        public static Settings Prefs = new Settings();

        /// <summary>Appended to file names (the automated play-test uses its own save).</summary>
        public static string Suffix = "";

        static string SavePath => Path.Combine(Application.persistentDataPath, "wyrdbound_save" + Suffix + ".json");
        static string PrefsPath => Path.Combine(Application.persistentDataPath, "wyrdbound_settings" + Suffix + ".json");

        public static void DeleteFiles()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(PrefsPath)) File.Delete(PrefsPath);
            }
            catch (Exception e) { Debug.LogWarning(e.Message); }
        }

        public static bool HasSave => File.Exists(SavePath);

        public static void Load()
        {
            Data = Read<SaveData>(SavePath) ?? new SaveData();
            Prefs = Read<Settings>(PrefsPath) ?? new Settings();
        }

        public static void Save() => Write(SavePath, Data);
        public static void SavePrefs() => Write(PrefsPath, Prefs);

        public static void NewGame()
        {
            Data = new SaveData();
            Save();
        }

        static T Read<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Wyrdbound] Could not read " + path + ": " + e.Message);
                return null;
            }
        }

        static void Write(string path, object o)
        {
            try
            {
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(o, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Wyrdbound] Could not write " + path + ": " + e.Message);
            }
        }
    }
}
