using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// The heart of the saga. Owns every service, the game mode, time (pause, hitstop, slow-motion),
    /// scheduled callbacks and the fade transitions between title, hall and escape.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class Game : MonoBehaviour
    {
        public static Game I;

        public readonly GameInput Input = new GameInput();
        public AudioManager Audio;
        public CameraRig Cam;
        public UIRoot UI;
        public RunDirector Director;
        public Hub Hub;
        public Player Player;
        public GameMode Mode = GameMode.Boot;

        public bool Paused => UI != null && UI.Modal;
        public bool Transitioning => transitioning;

        sealed class Timer
        {
            public float T;
            public Action A;
            public bool Unscaled;
        }

        readonly List<Timer> timers = new List<Timer>();
        float hitstopT, slowT, slowScale = 1f;
        bool transitioning;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 144;
            QualitySettings.vSyncCount = 1;
            Application.runInBackground = true;

            bool test = Array.IndexOf(Environment.GetCommandLineArgs(), "-wyrdtest") >= 0;
            if (test)
            {
                SaveSystem.Suffix = "_autotest";
                SaveSystem.DeleteFiles();
            }
            SaveSystem.Load();
            ApplySettings();

            foreach (var c in Camera.allCameras) c.gameObject.SetActive(false);
            foreach (var l in UnityEngine.Object.FindObjectsByType<AudioListener>()) l.enabled = false;

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(transform, false);
            Cam = camGo.AddComponent<CameraRig>();
            camGo.AddComponent<LightingRig>();
            camGo.AddComponent<PostFx>();

            Audio = new AudioManager(gameObject);

            var vfx = new GameObject("Vfx");
            vfx.transform.SetParent(transform, false);
            vfx.AddComponent<VfxSystem>();

            var ui = new GameObject("UI");
            ui.transform.SetParent(transform, false);
            UI = ui.AddComponent<UIRoot>();
            UI.Init();

            Director = gameObject.AddComponent<RunDirector>();
            Hub = new Hub();

            World.Root = new GameObject("Level").transform;
            Player = Player.Create(transform);
            Player.gameObject.SetActive(false);
            if (test) gameObject.AddComponent<AutoTest>();
        }

        /// <summary>Time multiplier used by the automated play-test.</summary>
        public float TestSpeed = 1f;

        void Start() => GoTitle();

        public void ApplySettings()
        {
            var p = SaveSystem.Prefs;
            if (!Application.isEditor)
                Screen.fullScreenMode = p.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (!p.mouseAim) Input.MouseAim = false;
        }

        // ------------------------------------------------------------------ time

        public void Hitstop(float secs) => hitstopT = Mathf.Max(hitstopT, secs);

        public void SlowMo(float scale, float secs)
        {
            slowScale = scale;
            slowT = Mathf.Max(slowT, secs);
        }

        /// <summary>Run an action after a delay in game time (pauses with the game). Cleared between chambers.</summary>
        public void After(float delay, Action a) => timers.Add(new Timer { T = delay, A = a });

        /// <summary>Like After, but in real time (keeps running through slow-motion).</summary>
        public void AfterReal(float delay, Action a) => timers.Add(new Timer { T = delay, A = a, Unscaled = true });

        public void ClearTimers() => timers.Clear();

        void Update()
        {
            Input.Update();

            if (Input.PauseDown && !transitioning && (Mode == GameMode.Hub || Mode == GameMode.Run) && !UI.Modal && Player != null && !Player.Dead)
                UI.Push(new PauseScreen());

            UI.Tick();
            Audio.Tick();

            float dt = Time.deltaTime, udt = Time.unscaledDeltaTime;
            if (timers.Count > 0)
            {
                var due = new List<Timer>();
                for (int i = timers.Count - 1; i >= 0; i--)
                {
                    var t = timers[i];
                    t.T -= t.Unscaled ? (Paused ? 0f : udt) : dt;
                    if (t.T <= 0f) { due.Add(t); timers.RemoveAt(i); }
                }
                for (int i = due.Count - 1; i >= 0; i--)
                {
                    try { due[i].A?.Invoke(); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            }

            hitstopT -= udt;
            slowT -= udt;
            float ts = 1f;
            if (Paused) ts = 0f;
            else if (hitstopT > 0f) ts = 0.03f;
            else if (slowT > 0f) ts = slowScale;
            Time.timeScale = ts * TestSpeed;

            if ((Mode == GameMode.Hub || Mode == GameMode.Run) && !Paused) SaveSystem.Data.playTime += udt;

            if (Player != null && Player.gameObject.activeSelf)
            {
                Cam.Target = Player.Pos + new Vector2(0f, 0.4f);
                Vector2 look = Input.MouseAim ? Player.Aim * 1.4f : Player.Facing * 0.8f;
                Cam.LookAhead = U.Damp(Cam.LookAhead, Mode == GameMode.Run ? look : look * 0.4f, 4f, udt);
            }
        }

        // ------------------------------------------------------------------ flow

        public void Transition(Action mid, Action after = null, float outT = 0.3f, float inT = 0.45f)
        {
            if (transitioning) return;
            StartCoroutine(TransitionCo(mid, after, outT, inT));
        }

        IEnumerator TransitionCo(Action mid, Action after, float outT, float inT)
        {
            transitioning = true;
            if (Player != null) Player.Controllable = false;
            yield return UI.Fade(1f, outT);
            try { mid?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
            yield return null;
            yield return UI.Fade(0f, inT);
            transitioning = false;
            if (Player != null) Player.Controllable = true;
            try { after?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>Destroy the current chamber and everything spawned in it.</summary>
        public void ClearLevel()
        {
            ClearTimers();
            if (VfxSystem.I != null) VfxSystem.I.Clear();
            if (World.Root != null)
            {
                World.Root.gameObject.SetActive(false);
                Destroy(World.Root.gameObject);
            }
            World.Root = new GameObject("Level").transform;
            World.Grid = null;
            hitstopT = slowT = 0f;
        }

        public void GoTitle()
        {
            UI.CloseAll();
            Mode = GameMode.Title;
            RunState.Current = null;
            ClearLevel();
            Player.gameObject.SetActive(false);
            Cam.Cam.backgroundColor = U.Hex("040507");
            Audio.Intensity(0f);
            Audio.Music("title");
            UI.Push(new TitleScreen());
            StartCoroutine(UI.Fade(0f, 1.2f));
        }

        public void NewGame()
        {
            SaveSystem.NewGame();
            UI.CloseAll();
            Audio.Music("title");
            UI.Push(new CardsScreen(StoryDB.IntroCards, () => GoHub(false)));
        }

        public void ContinueGame()
        {
            UI.CloseAll();
            if (!SaveSystem.Data.Flag("intro_seen")) { NewGame(); return; }
            GoHub(false);
        }

        public void GoHub(bool fromDeath)
        {
            Transition(() =>
            {
                UI.CloseAll();
                Mode = GameMode.Hub;
                RunState.Current = null;
                ClearLevel();
                Player.gameObject.SetActive(true);
                Player.ResetForRun();
                Player.Equip(SaveSystem.Data.weapon);
                Hub.Build(fromDeath);
                SaveSystem.Save();
            }, () => Hub.OnArrive(fromDeath), 0.4f, 0.8f);
        }

        public void StartRun()
        {
            Transition(() =>
            {
                UI.CloseAll();
                Mode = GameMode.Run;
                Director.StartRun(SaveSystem.Data.weapon);
            }, () => Director.OnRunStarted(), 0.5f, 0.6f);
        }

        public void OnPlayerDied()
        {
            Audio.Intensity(0f);
            SlowMo(0.2f, 1.4f);
            Cam.Punch(1.4f);
            Cam.Shake(0.6f);
            Audio.Play("die", 1f, 0.6f);
            Audio.Play("heartbeat", 1f);
            Vfx.Burst(Player.Center, 30, Pal.Blood, 10f, 0.8f, 0.5f);
            var summary = Director.EndRun(false);
            AfterReal(1.6f, () => UI.Push(new DeathScreen(summary, () => GoHub(true))));
        }

        public void OnRunWon()
        {
            var summary = Director.EndRun(true);
            UI.Push(new VictoryScreen(summary, () => GoHub(false)));
        }

        public void Quit()
        {
            SaveSystem.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnApplicationQuit()
        {
            if (Mode == GameMode.Hub || Mode == GameMode.Run) SaveSystem.Save();
        }
    }
}
