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

        void Awake()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-wyrdout");
            outDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "autotest");
            Directory.CreateDirectory(outDir);
            log = new StreamWriter(Path.Combine(outDir, "autotest_log.txt")) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            Note("AutoTest started. Unity " + Application.unityVersion + ", screen " + Screen.width + "x" + Screen.height);
        }

        void Start()
        {
            Game.I.Input.Pad = pad;
            StartCoroutine(Run());
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

            g.TestSpeed = 1.7f;
            g.StartRun();
            yield return Until(() => g.Mode == GameMode.Run && !g.Transitioning, 20f, "run start");
            float runStart = Time.realtimeSinceStartup;
            while (g.Mode == GameMode.Run && Time.realtimeSinceStartup - runStart < 1500f && rooms < 40)
            {
                if (Player.I == null || Player.I.Dead) break;
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
                pad.Call = p.Fury >= 100f && Time.frameCount % 20 == 0;
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
