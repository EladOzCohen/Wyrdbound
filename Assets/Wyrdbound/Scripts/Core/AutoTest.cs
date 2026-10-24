using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Automated play-test, enabled with the command-line flag -wyrdtest (optionally -wyrdout DIR).
    /// Plays title → intro → hall → menus → a full escape (fighting, blessings, doors, shop) → death → return,
    /// capturing screenshots and logging every error and exception. Uses its own save file.
    /// </summary>
    public sealed class AutoTest : MonoBehaviour
    {
        string outDir;
        StreamWriter log;
        int errors, exceptions, shots, rooms;
        readonly GameInput.TestPad pad = new GameInput.TestPad();
        bool god = true;
        RoomPlan lastRoom;
        float roomT, stuckT, lastDoorT;
        Vector2 lastPos;
        bool shotRoom, shotCombat, shotBoon, usedRoom;
        readonly HashSet<string> realms = new HashSet<string>();
        readonly HashSet<string> errorSet = new HashSet<string>();
        int doorPick;
        bool closeA, closeB;
        int maxRooms = 40;

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-wyrdout");
            outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "autotest");
            Directory.CreateDirectory(outDir);
            log = new StreamWriter(Path.Combine(outDir, "autotest_log.txt")) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            int ri = Array.IndexOf(args, "-wyrdrooms");
            if (ri >= 0 && ri + 1 < args.Length) int.TryParse(args[ri + 1], out maxRooms);
            Note("AutoTest started. Unity " + Application.unityVersion + ", screen " + Screen.width + "x" + Screen.height);
        }

        void Start()
        {
            Game.I.Input.Pad = pad;
            var args = Environment.GetCommandLineArgs();
            bool gallery = Array.IndexOf(args, "-wyrdgallery") >= 0;
            bool features = Array.IndexOf(args, "-wyrdfeatures") >= 0;
            StartCoroutine(features ? Features() : gallery ? Gallery() : Run());
        }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            if (type == LogType.Exception) exceptions++;
            else if (type == LogType.Error || type == LogType.Assert) errors++;
            string key = type + msg;
            if (type != LogType.Warning && errorSet.Add(key)) Note(type.ToString().ToUpper() + ": " + msg + "\n" + stack);
            else if (type == LogType.Warning && errorSet.Add(key)) Note("warning: " + msg);
        }

        void Note(string s) => log?.WriteLine("[" + Time.realtimeSinceStartup.ToString("0.0") + "] " + s);

        void Shot(string name)
        {
            string file = Path.Combine(outDir, (shots++).ToString("00") + "_" + name + ".png");
            ScreenCapture.CaptureScreenshot(file);
            Note("screenshot " + file);
        }

        IEnumerator Snap(string name)
        {
            Shot(name);
            yield return null;
            yield return null;
        }

        /// <summary>A zoomed-in burst of frames around Narfi, to inspect animation.</summary>
        IEnumerator Closeup(string name, int frames, float gap)
        {
            var cam = Game.I.Cam;
            float baseSize = cam.BaseSize;
            cam.BaseSize = 3.2f;
            for (int i = 0; i < frames; i++)
            {
                Shot(name + "_" + i);
                float t = Time.realtimeSinceStartup + gap;
                while (Time.realtimeSinceStartup < t) { Drive(); yield return null; }
            }
            cam.BaseSize = baseSize;
        }

        static IEnumerator Real(float s)
        {
            float t = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < t) yield return null;
        }

        IEnumerator Until(Func<bool> cond, float timeout, string what)
        {
            float t = Time.realtimeSinceStartup + timeout;
            while (!cond() && Time.realtimeSinceStartup < t)
            {
                PulseUI();
                yield return null;
            }
            if (!cond()) Note("TIMEOUT waiting for " + what);
        }

        void PulseUI()
        {
            pad.Move = Vector2.zero;
            pad.Attack = pad.Special = pad.Cast = pad.Dash = pad.Call = false;
            pad.Confirm = Game.I.UI.Top != null && Time.frameCount % 8 == 0;
        }

        /// <summary>
        /// -wyrdfeatures: exercise the newcomer and quality-of-life features one by one and photograph them:
        /// the How-to-Play guide, a hint, shop tooltips / descriptions / a purchase, breakables, rune-stone and
        /// altar, mouse aiming at chest height, the sidestep, and the adaptive music's track and danger level.
        /// </summary>
        IEnumerator Features()
        {
            var g = Game.I;
            yield return Real(2.5f);
            var guide = new HowToPlayScreen();
            g.UI.Push(guide);
            yield return Real(0.4f);
            for (int i = 0; i < 4; i++) { guide.ShowTab(i); yield return Real(0.25f); yield return Snap("guide_" + i); }
            g.UI.CloseAll();
            yield return Real(0.3f);

            g.NewGame();
            yield return Real(2f);
            pad.Cancel = true; yield return null; yield return null; pad.Cancel = false;
            yield return Until(() => g.Mode == GameMode.Hub && !g.Transitioning, 20f, "hub");
            yield return Real(1f);
            g.UI.CloseAll();
            yield return Real(0.5f);
            Hints.Show("Aim with the mouse and " + U.Tint("[LMB/J]", Pal.Gold) + " to attack where the gold reticle points. " + U.Tint("[Space]", Pal.Gold) + " dashes, " + U.Tint("[C]", Pal.Gold) + " sidesteps.");
            yield return Real(0.4f);
            yield return Snap("hint");

            g.StartRun();
            yield return Until(() => g.Mode == GameMode.Run && !g.Transitioning, 20f, "run");
            yield return Real(1f);
            var run = RunState.Current;
            run.Silver = 999;

            // shop: tooltip on hover, description in the prompt, then a purchase leaving a SOLD pedestal
            RunDirector.I.DebugRoom(RoomKind.Shop, 4242);
            yield return Real(1.5f);
            g.UI.CloseAll();
            var wares = new List<Interactable>();
            foreach (var it in Interactable.All) if (it != null && it.name == "ware") wares.Add(it);
            Note("shop wares: " + wares.Count);
            if (wares.Count > 0)
            {
                var w0 = (Vector2)wares[0].transform.position;
                pad.Mouse = g.Cam.WorldToScreen(w0 + new Vector2(0f, 0.6f));
                yield return Real(0.4f);
                yield return Snap("shop_tooltip");
                pad.Mouse = null;
                g.Player.Teleport(w0 + new Vector2(0f, -1.1f));
                yield return Real(0.4f);
                yield return Snap("shop_prompt");
                wares[0].Use();
                yield return Real(0.8f);
                g.UI.CloseAll();
                yield return Real(0.3f);
                yield return Snap("shop_sold");
            }

            // a combat chamber with things to smash and use
            Breakable.ForceExtras = true;
            RunDirector.I.DebugRoom(RoomKind.Combat, 777);
            Breakable.ForceExtras = false;
            yield return Real(1.2f);
            Note("breakables: " + Breakable.All.Count + ", music track: " + g.Audio.CurrentTrack);
            yield return Snap("breakables");

            // aim: cursor just right of Narfi's chest must aim right, not into the floor behind him
            var pp = g.Player.Pos;
            pad.Mouse = g.Cam.WorldToScreen(pp + new Vector2(1.2f, 0.75f));
            yield return Real(0.2f);
            Note("aim with cursor right of chest: " + g.Player.Aim.ToString("F2") + " (expect about (1, 0))");
            pad.Mouse = g.Cam.WorldToScreen(pp + new Vector2(0f, 2.5f));
            yield return Real(0.2f);
            Note("aim with cursor above head: " + g.Player.Aim.ToString("F2") + " (expect about (0, 1))");
            pad.Mouse = g.Cam.WorldToScreen(pp + new Vector2(1.2f, 0.75f));

            // sidestep: a quick hop to the side of the aim
            var before = g.Player.Pos;
            pad.Sidestep = true; yield return null; yield return null; pad.Sidestep = false;
            yield return Real(0.07f);
            yield return Snap("sidestep");
            yield return Real(0.3f);
            Note("sidestep moved " + (g.Player.Pos - before).ToString("F2") + ", cooldown " + g.Player.SidestepCooldown.ToString("F2"));
            pad.Mouse = null;

            // smash the nearest breakable
            if (Breakable.All.Count > 0)
            {
                var b = Breakable.All[0];
                var bp = (Vector2)b.transform.position;
                int silver = run.Silver;
                Breakable.HitPoint(bp, 0.2f);
                yield return Real(0.3f);
                Note("smashed a breakable: silver " + silver + " -> " + run.Silver + ", left " + Breakable.All.Count);
                yield return Snap("smashed");
            }

            // the rune-stone and the altar
            foreach (var it in Interactable.All.ToArray())
            {
                if (it == null || it.Prompt == null) continue;
                string pr = it.Prompt();
                if (pr.StartsWith("Read the rune-stone") || pr.StartsWith("Make an offering"))
                {
                    g.Player.Teleport((Vector2)it.transform.position + new Vector2(0f, -1.2f));
                    yield return Real(0.4f);
                    yield return Snap(pr.StartsWith("Read") ? "lorestone" : "altar");
                    it.Use();
                    yield return Real(0.6f);
                    yield return Snap(pr.StartsWith("Read") ? "lorestone_read" : "altar_used");
                }
            }

            // adaptive music: watch the danger level as foes close in
            for (int i = 0; i < 8; i++)
            {
                yield return Real(0.75f);
                Note("music " + g.Audio.CurrentTrack + " danger " + g.Audio.DangerLevel.ToString("F2") + " foes " + Combat.AllOf(Team.Enemy).Count + " hp " + g.Player.HP.ToString("F0"));
            }
            yield return Snap("fight_music");
            Note("SUMMARY features errors=" + errors + " exceptions=" + exceptions + " shots=" + shots);
            yield return Real(0.5f);
            Application.Quit();
        }

        /// <summary>-wyrdgallery: line up every 3D model, pose them (idle, run, attack, death) and photograph each group.</summary>
        IEnumerator Gallery()
        {
            var g = Game.I;
            yield return Real(2f);
            g.UI.CloseAll();
            g.ClearLevel();
            g.Mode = GameMode.Hub;
            g.Player.gameObject.SetActive(false);
            var grid = RoomGen.Arena(new Rng(3), Biomes.Niflhel, 9, false);
            LevelBuilder.Build(grid, Biomes.Niflhel, 5, false, false);
            string[][] groups =
            {
                new[] { "narfi", "draugr", "draugr_archer", "thrall", "grave_jarl" },
                new[] { "helhound", "garmr", "raven", "wraith", "wight", "bloat" },
                new[] { "serpent", "larva", "bell", "nidhogg_head", "spitter", "oathbreaker" },
                new[] { "hel", "baldr", "thor", "odin", "valkyrie", "hrimthurs" },
            };
            var cam = g.Cam;
            cam.BaseSize = 5.8f;
            for (int gi = 0; gi < groups.Length; gi++)
            {
                var root = new GameObject("gallery").transform;
                root.SetParent(World.Root, false);
                var ms = new List<Model3D>();
                int n = groups[gi].Length;
                for (int i = 0; i < n; i++)
                {
                    var go = new GameObject(groups[gi][i]);
                    go.transform.SetParent(root, false);
                    go.transform.position = new Vector3((i - (n - 1) * 0.5f) * 2.6f, (i % 2) * 0.6f, 0f);
                    var m = Model3D.Build(go.transform, groups[gi][i]);
                    if (m == null) Note("gallery: no model for " + groups[gi][i]); else ms.Add(m);
                }
                cam.Snap(new Vector2(0f, 0.8f));
                string[] poses = { "idle", "run", "windup", "strike", "dead" };
                foreach (var pose in poses)
                {
                    float t0 = Time.realtimeSinceStartup;
                    while (Time.realtimeSinceStartup - t0 < (pose == "dead" ? 2f : 0.9f))
                    {
                        float k = Mathf.Clamp01((Time.realtimeSinceStartup - t0) / 0.8f);
                        var ri = new RigInput { MaxSpeed = 4f, Spawn = 1f };
                        if (pose == "run") ri.Speed = 4f;
                        if (pose == "windup") { ri.Action = RigAction.Windup; ri.ActionT = k; }
                        if (pose == "strike") { ri.Action = RigAction.Strike; ri.ActionT = k * 0.4f; }
                        if (pose == "dead") { ri.Dead = true; ri.DeadT = k * 1.2f; }
                        foreach (var m in ms) m.Animate(ri, pose == "run" ? new Vector2(1f, -0.4f) : new Vector2(0.2f, -1f), Time.deltaTime);
                        yield return null;
                    }
                    yield return Snap("gallery" + gi + "_" + pose);
                }
                Destroy(root.gameObject);
                yield return null;
            }
            Note("SUMMARY gallery errors=" + errors + " exceptions=" + exceptions + " shots=" + shots);
            yield return Real(0.5f);
            Application.Quit();
        }

        IEnumerator Run()
        {
            var g = Game.I;
            yield return Real(3f);
            yield return Snap("title");
            g.NewGame();
            yield return Real(2f);
            yield return Snap("intro_cards");
            pad.Cancel = true;
            yield return null;
            yield return null;
            pad.Cancel = false;
            yield return Until(() => g.Mode == GameMode.Hub && !g.Transitioning, 20f, "hub");
            yield return Real(1.5f);
            yield return Snap("hall_first_dialogue");
            yield return Until(() => g.UI.Top == null, 30f, "dialogue end");
            yield return Real(1f);
            yield return Snap("hall");

            foreach (var s in new UIScreen[] { new WeaponScreen(), new RuneStoneScreen(), new KeepsakeScreen(), new WeaveScreen(), new CodexScreen(), new SettingsScreen(), new PauseScreen() })
            {
                g.UI.Push(s);
                yield return Real(0.6f);
                yield return Snap("screen_" + s.GetType().Name);
                g.UI.CloseAll();
                yield return Real(0.2f);
            }
            g.Hub.Talk("baldr");
            yield return Real(1.2f);
            yield return Snap("talk_baldr");
            yield return Until(() => g.UI.Top == null || g.UI.Top is ChoiceScreen, 20f, "baldr talk");
            g.UI.CloseAll();
            Player.I.Teleport(new Vector2(-11f, -8.4f));
            yield return Closeup("close_hall", 4, 0.35f);

            g.TestSpeed = 1.7f;
            g.StartRun();
            yield return Until(() => g.Mode == GameMode.Run && !g.Transitioning, 20f, "run start");
            float runStart = Time.realtimeSinceStartup;
            while (g.Mode == GameMode.Run && Time.realtimeSinceStartup - runStart < 1500f && rooms < maxRooms)
            {
                if (Player.I == null || Player.I.Dead) break;
                if (!closeA && rooms == 1 && roomT > 2.2f && Combat.Nearest(Player.I.Pos, Team.Enemy, 99f) != null) { closeA = true; yield return Closeup("close_combat", 10, 0.1f); }
                if (!closeB && Boss.Current != null && !Boss.Current.Spawning && roomT > 5f) { closeB = true; yield return Closeup("close_boss", 6, 0.15f); }
                Drive();
                yield return null;
            }
            Note("run loop ended: rooms=" + rooms + ", mode=" + g.Mode + ", realms=" + string.Join(",", realms));

            if (g.Mode == GameMode.Run)
            {
                god = false;
                float t = Time.realtimeSinceStartup + 20f;
                while (Player.I != null && !Player.I.Dead && Time.realtimeSinceStartup < t)
                {
                    PulseUI();
                    var h = new HitInfo { Damage = 99999f, Kind = HitKind.Hazard, Cause = "the AutoTest" };
                    Combat.Deal(Player.I, h);
                    yield return Real(0.3f);
                }
                yield return Until(() => g.UI.Top is DeathScreen, 15f, "death screen");
                yield return Real(1.5f);
                yield return Snap("death_screen");
                yield return Until(() => g.Mode == GameMode.Hub && !g.Transitioning, 30f, "hub after death");
                yield return Real(1.5f);
                yield return Snap("return_dialogue");
                yield return Until(() => g.UI.Top == null, 30f, "return dialogue");
                yield return Real(1f);
                yield return Snap("hall_after_death");
            }
            else
            {
                yield return Until(() => g.Mode == GameMode.Hub && !g.Transitioning, 30f, "hub after victory");
                yield return Real(1.5f);
                yield return Snap("hall_after_victory");
            }

            var d = SaveSystem.Data;
            Note("SUMMARY rooms=" + rooms + " realms=" + string.Join(",", realms) + " kills=" + d.kills + " runs=" + d.runs + " deaths=" + d.deaths + " clears=" + d.clears +
                 " embers=" + d.embers + " errors=" + errors + " exceptions=" + exceptions + " shots=" + shots);
            log.Flush();
            yield return Real(1f);
            Application.Quit();
        }

        void Drive()
        {
            var g = Game.I;
            var p = Player.I;
            var run = RunState.Current;
            if (god && p != null && !p.Dead) p.HP = p.MaxHP;
            float dt = Time.unscaledDeltaTime;

            var room = run?.Room;
            if (room != lastRoom && room != null)
            {
                lastRoom = room;
                rooms++;
                roomT = 0f;
                shotRoom = shotCombat = usedRoom = false;
                realms.Add(run.Biome.Name);
                Note("room " + rooms + ": " + room.Kind + " / " + room.Reward + " / " + room.God + " in " + run.Biome.Name + "  hp " + Mathf.RoundToInt(p.HP) + "/" + p.MaxHP + "  boons " + run.Boons.Owned.Count);
            }
            roomT += dt;
            if (!shotRoom && roomT > 1.8f && room != null)
            {
                shotRoom = true;
                Note("room " + rooms + " " + room.Kind + " music " + Game.I.Audio.CurrentTrack);
                if (rooms <= 5 || room.Kind != RoomKind.Combat) Shot("room" + rooms.ToString("00") + "_" + room.Kind + "_" + run.Biome.Id);
            }

            var top = g.UI.Top;
            if (top != null)
            {
                if (top is BoonScreen && !shotBoon) { shotBoon = true; Shot("boon_choice"); }
                PulseUI();
                return;
            }
            pad.Confirm = false;
            if (g.Transitioning || p == null) { PulseUI(); return; }

            var target = Combat.Nearest(p.Pos, Team.Enemy, 99f);
            if (target != null)
            {
                if (!shotCombat && roomT > 3.5f && (rooms % 3 == 1 || Boss.Current != null))
                {
                    shotCombat = true;
                    Shot("combat" + rooms.ToString("00") + "_" + run.Biome.Id);
                }
                var to = target.Pos - p.Pos;
                float dist = to.magnitude;
                pad.Move = dist > 1.5f ? to.normalized : to.normalized * 0.12f;
                pad.Attack = Time.frameCount % 10 < 5;
                pad.Special = Time.frameCount % 97 == 0;
                pad.Cast = Time.frameCount % 173 == 0;
                pad.Dash = Time.frameCount % 131 == 0;
                pad.Call = p.Fury >= 100f && run.Boons.HasCall && Time.frameCount % 20 == 0;
                if (pad.Call) Note("call: " + run.Boons.SlotGod(BoonSlot.Call));
                if ((p.Pos - lastPos).sqrMagnitude < 0.0004f && dist > 2.5f) stuckT += dt; else stuckT = 0f;
                lastPos = p.Pos;
                if (stuckT > 2.5f && World.Grid != null)
                {
                    stuckT = 0f;
                    p.Teleport(World.Grid.NearestFloor(target.Pos - to.normalized * 1.4f, p.Radius));
                    Note("unstuck teleport");
                }
                return;
            }
            pad.Attack = pad.Special = pad.Cast = pad.Dash = pad.Call = false;
            pad.Move = Vector2.zero;

            foreach (var pk in UnityEngine.Object.FindObjectsByType<Pickup>())
            {
                p.Teleport(pk.transform.position);
                return;
            }

            if (!usedRoom && room != null && roomT > 1.5f)
            {
                usedRoom = true;
                foreach (var prefix in new[] { "Buy", "Drink", "Approach", "Speak" })
                {
                    bool done = false;
                    foreach (var it in Interactable.All.ToArray())
                    {
                        if (it == null || !it.Usable || it.Prompt == null) continue;
                        string pr = it.Prompt();
                        if (!pr.StartsWith(prefix)) continue;
                        if (prefix == "Buy" && run.Silver < 60) continue;
                        Note("interact: " + pr + "  (silver " + run.Silver + ")");
                        it.Use();
                        done = true;
                        break;
                    }
                    if (done) break;
                }
                return;
            }

            var dir = RunDirector.I;
            if (dir.Cleared && dir.Doors.Count > 0 && roomT > 3f && Time.realtimeSinceStartup - lastDoorT > 2f)
            {
                lastDoorT = Time.realtimeSinceStartup;
                var door = dir.Doors[doorPick++ % dir.Doors.Count];
                Note("take door → " + door.Plan.Kind + " / " + door.Plan.Reward);
                dir.TakeDoor(door);
            }
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            log?.Dispose();
        }
    }
}
