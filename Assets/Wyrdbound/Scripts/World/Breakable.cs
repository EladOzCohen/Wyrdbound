using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Things in a chamber worth touching: barrels, crates and kegs that shatter under a blow and spill silver
    /// (sometimes healing or Soul-Embers), rune-stones that whisper lore when read, and offering altars that
    /// trade a little blood for a gift. Breakables block movement until they are smashed.
    /// </summary>
    public sealed class Breakable : MonoBehaviour
    {
        public static readonly List<Breakable> All = new List<Breakable>();
        /// <summary>Tests: always place a rune-stone and an altar.</summary>
        public static bool ForceExtras;
        static readonly string[] Kinds = { "barrel", "barrel", "crate", "keg" };

        Vector2 pos;
        float radius = 0.4f;
        bool broken;
        Color chip;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        // ------------------------------------------------------------------ placement

        /// <summary>Scatter breakables, and maybe a rune-stone and an altar, around a combat chamber.</summary>
        public static void Populate(RoomGrid g, Rng rng, Biome b, bool combat)
        {
            int count = combat ? rng.Range(3, 7) : rng.Range(1, 3);
            for (int i = 0; i < count; i++)
            {
                var p = Spot(g, rng, 0.55f);
                if (p == null) break;
                Spawn(g, Kinds[rng.Range(0, Kinds.Length)], p.Value, b, rng);
                // barrels come in little clusters
                if (rng.Chance(0.4f))
                {
                    var q = p.Value + rng.OnCircle() * 0.95f;
                    if (g.Free(q, 0.45f) && g.FreeOfObstacles(q, 0.45f)) Spawn(g, Kinds[rng.Range(0, Kinds.Length)], q, b, rng);
                }
            }
            if (combat && (ForceExtras || rng.Chance(0.3f))) { var p = Spot(g, rng, 1f); if (p != null) LoreStone(g, p.Value, b, rng); }
            if (combat && (ForceExtras || rng.Chance(0.2f))) { var p = Spot(g, rng, 1f); if (p != null) Altar(g, p.Value, b); }
        }

        static Vector2? Spot(RoomGrid g, Rng rng, float r)
        {
            for (int tries = 0; tries < 40; tries++)
            {
                var p = g.RandomFloor(rng, r, g.Entry, 3.5f, 20);
                if (Vector2.Distance(p, g.Entry) < 3f || !g.Free(p, r + 0.2f) || !g.FreeOfObstacles(p, r + 0.4f)) continue;
                bool nearDoor = false;
                foreach (var d in g.DoorSpots) if (Vector2.Distance(p, d) < 3f) nearDoor = true;
                if (!nearDoor) return p;
            }
            return null;
        }

        static void Spawn(RoomGrid g, string kind, Vector2 p, Biome b, Rng rng)
        {
            var model = Room3D.Prop(World.Root, kind, p, 0.4f, b, rng.Next());
            if (model == null) return;
            var br = model.AddComponent<Breakable>();
            br.pos = p;
            br.chip = kind == "crate" ? U.Hex("8a6a44") : U.Hex("7a5a3a");
            g.Obstacles.Add(new Obstacle(p, br.radius, false, "breakable"));
        }

        // ------------------------------------------------------------------ breaking

        /// <summary>Smash breakables caught by a melee strike (same shapes as Weapon.Strike). Returns how many broke.</summary>
        public static int StrikeArea(Vector2 origin, Vector2 dir, int shape, float range, float arc, float width)
        {
            int n = 0;
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var b = All[i];
                if (b == null || b.broken) continue;
                var d = b.pos - origin;
                float dist = d.magnitude;
                bool inside;
                if (shape == 2) inside = dist <= range + b.radius;
                else if (shape == 1)
                {
                    float along = Vector2.Dot(d, dir);
                    inside = along >= -0.4f && along <= range + b.radius && Mathf.Abs(Vector2.Dot(d, dir.Rot(90f))) <= width * 0.5f + b.radius;
                }
                else inside = dist <= range + b.radius && (dist < b.radius + 0.45f || Vector2.Angle(dir, d) <= arc * 0.5f + 20f);
                if (!inside) continue;
                b.Break(d.Norm(dir));
                n++;
            }
            return n;
        }

        /// <summary>Smash a breakable touched by a point (projectiles, thrown weapons).</summary>
        public static bool HitPoint(Vector2 p, float r)
        {
            foreach (var b in All)
                if (b != null && !b.broken && (b.pos - p).sqrMagnitude < (b.radius + r) * (b.radius + r)) { b.Break((b.pos - p).Norm(Vector2.up)); return true; }
            return false;
        }

        void Break(Vector2 dir)
        {
            broken = true;
            if (World.Grid != null) World.Grid.Obstacles.RemoveAll(o => o.Kind == "breakable" && (o.Pos - pos).sqrMagnitude < 0.01f);
            Game.I.Audio.Play("slam", 0.45f, 1.6f);
            Game.I.Audio.Play("armor", 0.35f, 1.4f);
            Vfx.Directional(pos + new Vector2(0f, 0.4f), dir, 10, chip, 60f, 7f, 0.4f);
            Vfx.Dust(pos, 6, World.Biome.Fog, 0.8f);
            Game.I.Cam.Shake(0.06f);

            var run = RunState.Current;
            var p = Player.I;
            float roll = Random.value;
            if (run != null && roll < 0.55f)
            {
                int n = run.AddSilver(Random.Range(3, 9));
                Game.I.Audio.Play("coin", 0.5f);
                Vfx.Text(pos + new Vector2(0f, 1f), "+" + n + " silver", Pal.Silver, 0.7f);
            }
            else if (run != null && roll < 0.67f && p != null)
            {
                p.HealAmount(p.MaxHP * 0.06f);
                Vfx.Text(pos + new Vector2(0f, 1f), "a sip of mead", Pal.Heal, 0.7f);
            }
            else if (run != null && roll < 0.72f)
            {
                run.Embers += 2;
                Vfx.Text(pos + new Vector2(0f, 1f), "+2 Soul-Embers", Pal.Ember, 0.7f);
            }
            gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ lore stones and altars

        static readonly string[] Lore =
        {
            "\"Here lies Ketill, who swore by Freyr and broke it. The serpent remembers every oath.\"",
            "\"Gjöll's bridge is roofed with gold. The living cross it loud; the dead, without a sound.\"",
            "\"Garmr howls before the end of all things. Listen: is he howling now?\"",
            "\"The Norns cut no thread in anger. They cut because the cloth is finished.\"",
            "\"Hel's plate is called Hunger, her knife Famine. She sets them for every guest.\"",
            "\"Even Odin hung nine nights to learn these runes. What will you give?\"",
            "\"Níðhöggr gnaws the root, and the tree groans, and the world does not notice. Yet.\"",
            "\"The son of the trickster walks here. The dead whisper: he does not stay dead.\"",
            "\"Baldr will rise when the world is new. Until then, Hel keeps him warm.\"",
            "\"A wolf's blood runs in the brother. A wolf's hunger, too.\"",
            "\"Carve your name where the frost cannot reach, or the mist will wear it away.\"",
            "\"Not all the dead want to leave. Ask yourself why you do.\"",
        };

        static void LoreStone(RoomGrid g, Vector2 p, Biome b, Rng rng)
        {
            var sr = Mats.SR(World.Root, "lorestone", PropArt.Get("runestone_big", b), Sort.Y(p.y));
            sr.transform.position = p;
            sr.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            View3D.Stand(sr.transform);
            var glow = Mats.SR(World.Root, "loreglow", SpriteLib.Light, Sort.FloorFx + 2, b.Accent.A(0.6f), true);
            glow.transform.position = p;
            glow.transform.localScale = new Vector3(3f, 3f, 1f);
            Lights.ToLayer(glow);
            g.Obstacles.Add(new Obstacle(p, 0.45f, true, "lorestone"));
            string line = Lore[rng.Range(0, Lore.Length)];
            bool read = false;
            var it = Interactable.Add(sr.gameObject, 1.9f, () => "Read the rune-stone", () =>
            {
                read = true;
                glow.enabled = false;
                var run = RunState.Current;
                if (run != null) run.Embers += 3;
                Game.I.Audio.Play("level", 0.5f, 0.9f);
                Hints.Show(U.Tint("The stone whispers:", Pal.Dim) + "  " + line + "\n" + U.Tint("+3 Soul-Embers", Pal.Ember));
            }, () => !read);
            it.Detail = () => "Old runes carved by the dead. Read them to learn a little of this place, and gain a few Soul-Embers.";
        }

        static void Altar(RoomGrid g, Vector2 p, Biome b)
        {
            var root = new GameObject("altar").transform;
            root.SetParent(World.Root, false);
            root.position = p;
            bool model = Room3D.Prop(root, "altar", p, 0.5f, b, 1) != null;
            if (model) Room3D.Prop(root, "candles", p + new Vector2(0f, 0.05f), 0.2f, b, 2)?.transform.Translate(0f, 0f, -0.86f, Space.World);
            var glow = Mats.SR(root, "altarglow", SpriteLib.Light, Sort.FloorFx + 2, Pal.Blood.A(0.7f), true);
            glow.transform.position = p;
            glow.transform.localScale = new Vector3(3.2f, 3.2f, 1f);
            Lights.ToLayer(glow);
            g.Obstacles.Add(new Obstacle(p, 0.55f, true, "altar"));
            bool used = false;
            var it = Interactable.Add(root.gameObject, 2f, () => "Make an offering — " + U.Tint("lose 10% of your max health", Pal.Danger), () =>
            {
                var pl = Player.I;
                var run = RunState.Current;
                if (pl == null || run == null) return;
                if (pl.HP <= pl.MaxHP * 0.1f + 1f) { Game.I.Audio.Play("ui_deny"); Game.I.UI.Toast("You have too little blood to give", Pal.Danger); return; }
                used = true;
                glow.enabled = false;
                pl.HP -= pl.MaxHP * 0.1f;
                pl.Flash(1f, Pal.Blood);
                Game.I.Audio.Play("hurt", 0.6f);
                Vfx.Burst(p + new Vector2(0f, 0.8f), 18, Pal.Blood, 6f, 0.5f, 0.35f);
                if (Random.value < 0.6f)
                {
                    int n = run.AddSilver(45);
                    Game.I.UI.Toast("The old gods accept. +" + n + " silver", Pal.Silver);
                }
                else
                {
                    run.Rerolls++;
                    Game.I.UI.Toast("The old gods accept. +1 Fate-Thread", Pal.Gold);
                }
                Game.I.Audio.Play("coin", 0.6f);
            }, () => !used);
            it.Detail = () => "An altar to forgotten gods. Give 10% of your maximum health, and they answer with a gift: 45 silver or a Fate-Thread.";
        }
    }
}
