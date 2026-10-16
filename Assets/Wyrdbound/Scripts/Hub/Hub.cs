using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Éljúðnir, Hel's hall: where Narfi returns after every death, and where the story lives.</summary>
    public sealed class Hub
    {
        sealed class Resident
        {
            public string Id;
            public NpcView View;
        }

        readonly List<Resident> residents = new List<Resident>();

        public static readonly Vector2 Spawn = new Vector2(0f, -10.2f);

        public void Build(bool fromDeath)
        {
            residents.Clear();
            var d = SaveSystem.Data;
            var b = Biomes.Hall;
            var g = RoomGen.Hall();
            LevelBuilder.Build(g, b, 777, false, true);
            var root = World.Root;

            var pool = Mats.SR(root, "pool", PropArt.Get("pool", b), Sort.FloorFx + 10);
            pool.transform.position = new Vector3(0f, -12.2f, 0f);

            Station("throne", new Vector2(0f, 10.6f), 1.4f, null, null);
            Station("hearth", new Vector2(-11.5f, -10.6f), 0f, null, null);
            Station("table", new Vector2(-7.5f, -7.6f), 1.2f, null, null);
            var amb = root.GetComponent<Ambient>();
            amb?.AddFlame(new Vector2(-11.5f, -10.2f), Pal.Ember);

            Station("gate", new Vector2(-13.5f, 11.2f), 1.6f, () => "Begin the escape" + U.Tint("  (" + Meta.Weapon(d.weapon).Name + ")", Pal.Dim), () => Game.I.StartRun());
            var armory = Station("armory", new Vector2(-16.5f, 1.6f), 1.3f, () => "Hel's Armory — choose your arms", () => Game.I.UI.Push(new WeaponScreen()));
            for (int i = 0; i < Meta.Weapons.Length; i++)
            {
                var w = Meta.Weapons[i];
                var sr = Mats.SR(armory.transform, "w_" + w.Id, SpriteLib.Weapon(w.Id), Sort.Y(1.6f) + 1, d.HasWeapon(w.Id) ? Color.white : new Color(0.3f, 0.3f, 0.35f, 1f));
                sr.transform.localPosition = new Vector3(-1.75f + i * 1.15f, 1.2f, 0f);
                sr.transform.localRotation = Quaternion.Euler(0, 0, 90f);
                sr.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            }
            if (d.runs >= 1)
                Station("runestone_big", new Vector2(16.5f, 1.6f), 1.1f, () => "The Rune-Stone of Narfi — carve your strength", () => Game.I.UI.Push(new RuneStoneScreen()));
            Station("chest", new Vector2(-4.5f, -11.6f), 0.9f, () => "Hoard of Memories — keepsakes", () => Game.I.UI.Push(new KeepsakeScreen()));

            AddNpc("hel", new Vector2(0f, 8.9f), true, Pal.Hel.A(0.3f));
            AddNpc("ganglot", new Vector2(3.4f, -10.4f), true, new Color(0, 0, 0, 0));
            AddNpc("baldr", new Vector2(-12.8f, -6.4f), false, U.Hex("ffe7a3").A(0.3f));
            AddNpc("nanna", new Vector2(-10.2f, -7.6f), false, new Color(0, 0, 0, 0));
            AddNpc("mimir", new Vector2(14.8f, -8.2f), true, U.Hex("7ec8ff").A(0.25f));
            if (d.clears >= 1 || d.Flag("norns_open"))
            {
                Station("well", new Vector2(14.6f, 8.2f), 1.2f, null, null);
                AddNpc("norns", new Vector2(14.6f, 10f), true, U.Hex("f3e9ff").A(0.25f));
            }
            if (d.Flag("vali_hall")) AddNpc("vali", new Vector2(9f, -10.4f), true, Pal.Danger.A(0.2f));

            var p = Game.I.Player;
            p.Teleport(fromDeath ? new Vector2(0f, -12.2f) : Spawn);
            p.Facing = Vector2.up;
            Game.I.Cam.Snap(p.Pos);
            Game.I.Audio.Intensity(0f);
            Game.I.Audio.Music("hall");
            RefreshMarkers();
        }

        GameObject Station(string kind, Vector2 pos, float radius, System.Func<string> prompt, System.Action use)
        {
            var sr = Mats.SR(World.Root, kind, PropArt.Get(kind, Biomes.Hall), Sort.Y(pos.y));
            sr.transform.position = pos;
            if (radius > 0f) World.Grid.Obstacles.Add(new Obstacle(pos, radius, true, kind));
            if (prompt != null)
            {
                var it = Interactable.Add(sr.gameObject, radius + 1.6f, prompt, use);
                it.Offset = new Vector2(0f, -radius * 0.5f);
            }
            return sr.gameObject;
        }

        void AddNpc(string id, Vector2 pos, bool faceLeft, Color glow)
        {
            var who = Story.Who(id);
            var v = LevelBuilder.Npc(who.Art, pos, faceLeft, glow);
            if (id == "mimir" || id == "norns") v.TrackPlayer = false;
            residents.Add(new Resident { Id = id, View = v });
            SaveSystem.Data.SetFlag("codex_" + id);
            Interactable.Add(v.gameObject, 2.1f, () => "Speak with " + who.Name, () => Talk(id));
        }

        public void RefreshMarkers()
        {
            foreach (var r in residents) if (r.View != null) r.View.SetMarker(Story.HasNew(r.Id));
        }

        /// <summary>Talk: play anything new, then offer the resident's menu.</summary>
        public void Talk(string id)
        {
            if (Story.HasNew(id)) Story.Play(id, () => { RefreshMarkers(); Menu(id); });
            else Menu(id);
        }

        void Menu(string id)
        {
            var d = SaveSystem.Data;
            var who = Story.Who(id);
            var opts = new List<string>();
            var acts = new List<System.Action>();
            opts.Add("Talk");
            acts.Add(() => Story.Play(id, RefreshMarkers));
            if (id == "mimir") { opts.Add("Mímir's Lore (Codex)"); acts.Add(() => Game.I.UI.Push(new CodexScreen())); }
            if (id == "norns") { opts.Add("The Weave of Wyrd"); acts.Add(() => Game.I.UI.Push(new WeaveScreen())); }
            if (Giftable(id))
            {
                int aff = d.Aff(id);
                string label = aff >= 5 ? "Gift Mead (bond complete)" : "Gift Mead  " + U.Tint("(you have " + d.mead + ")", d.mead > 0 ? Pal.Mead : Pal.Dim);
                opts.Add(label);
                acts.Add(() => Gift(id));
            }
            opts.Add("Leave");
            acts.Add(null);
            string hearts = Giftable(id) ? "  " + Hearts(d.Aff(id)) : "";
            Game.I.UI.Push(new ChoiceScreen(who.Name + hearts, Story.Who(id).Color, opts.ToArray(), i => acts[i]?.Invoke(), who.Art));
        }

        static string Hearts(int n)
        {
            string s = "";
            for (int i = 0; i < 5; i++) s += i < n ? U.Tint("♥", Pal.Blood) : U.Tint("♥", Pal.Dim.A(0.4f));
            return s;
        }

        static bool Giftable(string id)
        {
            foreach (var k in Meta.Keepsakes) if (k.From == id) return true;
            return false;
        }

        /// <summary>Offer Mead to someone met during an escape (Móðguðr, Sigyn), then continue.</summary>
        public void OfferGift(string id, System.Action then)
        {
            var d = SaveSystem.Data;
            if (!Giftable(id) || d.mead <= 0 || d.Aff(id) >= 5) { then?.Invoke(); return; }
            var who = Story.Who(id);
            Game.I.UI.Push(new ChoiceScreen(who.Name + "  " + Hearts(d.Aff(id)), who.Color,
                new[] { "Gift Mead  " + U.Tint("(you have " + d.mead + ")", Pal.Mead), "Not now" },
                i => { if (i == 0) Gift(id, then); else then?.Invoke(); }, who.Art));
        }

        void Gift(string id) => Gift(id, null);

        void Gift(string id, System.Action then)
        {
            var d = SaveSystem.Data;
            if (d.Aff(id) >= 5)
            {
                Story.Say(then, "narfi: (There is nothing more mead can say between us.)");
                return;
            }
            if (d.mead <= 0)
            {
                Game.I.Audio.Play("ui_deny");
                Game.I.UI.Toast("You have no Mead of the Dead. Find it beyond the hall.", Pal.Dim);
                then?.Invoke();
                return;
            }
            d.mead--;
            d.AddAff(id);
            Game.I.Audio.Play("boon", 0.6f, 1.2f);
            string ks = null;
            foreach (var k in Meta.Keepsakes)
                if (k.From == id && !d.keepsakes.Contains(k.Id)) { d.keepsakes.Add(k.Id); ks = k.Name; }
            SaveSystem.Save();
            System.Action after = () =>
            {
                if (ks != null) Game.I.UI.Banner("KEEPSAKE: " + ks.ToUpper(), "Equip it from the Hoard of Memories", Pal.Gold, 2.4f);
                RefreshMarkers();
                then?.Invoke();
            };
            var c = Story.Pick("gift_" + id);
            if (c != null) Story.Play(c, after);
            else Story.Say(after, "narfi: (You pour a horn of the mead of the dead.)", id + ": " + StoryDB.GenericThanks(id));
        }

        public void OnArrive(bool fromDeath)
        {
            var d = SaveSystem.Data;
            if (!d.Flag("intro_seen"))
            {
                d.SetFlag("intro_seen");
                SaveSystem.Save();
                Story.Play("hel", () =>
                {
                    RefreshMarkers();
                    Game.I.UI.Toast("Speak with the dead of the hall, then take the Gate upward", Pal.Dim);
                });
                return;
            }
            if (fromDeath) Story.Play("return", RefreshMarkers);
            else RefreshMarkers();
        }
    }
}
