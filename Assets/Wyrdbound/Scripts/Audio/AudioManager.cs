using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Pooled SFX playback and an adaptive, three-layer score. Every track has a calm, a fury and a danger
    /// layer playing in sync; their mix follows the fight live: Intensity (are we in combat) and Danger
    /// (how close and how many foes, how hurt Narfi is, whether he was just hit). Tracks are rendered on a
    /// worker thread ahead of time (Prepare) so changing the piece between chambers never stutters.
    /// </summary>
    public sealed class AudioManager
    {
        static readonly string[] Prewarm =
        {
            "swing", "swing_heavy", "hit", "hit_heavy", "crit", "armor", "hurt", "dash", "cast", "cast_land", "die", "spawn",
            "coin", "pickup", "boon", "door", "thunder", "zap", "frost", "verdict", "raven", "charm", "wave", "telegraph",
            "explode", "slam", "roar", "howl", "ui_move", "ui_ok", "ui_back", "ui_open", "ui_deny", "heartbeat", "horn",
            "level", "arrow", "bell", "splash", "chain", "defy"
        };

        sealed class Deck
        {
            public AudioSource calm, fury, danger;
            public float vol, target;
            public string track;
        }

        readonly AudioSource[] pool;
        int next;
        readonly Dictionary<string, AudioClip> sfx = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        readonly Dictionary<string, AudioClip[]> tracks = new Dictionary<string, AudioClip[]>();
        readonly List<string> lru = new List<string>();
        readonly Dictionary<string, Task<float[][]>> pending = new Dictionary<string, Task<float[][]>>();
        readonly Deck[] decks = new Deck[3];
        int active;
        string wanted;
        float wantedFade = 1.6f;
        float intensity, intensityTarget, danger, dangerTarget;
        const int MaxCached = 6;

        public string CurrentTrack => decks[active].track;
        public float DangerLevel => danger;

        public AudioManager(GameObject host)
        {
            pool = new AudioSource[24];
            for (int i = 0; i < pool.Length; i++)
            {
                var s = host.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool[i] = s;
            }
            for (int d = 0; d < decks.Length; d++)
            {
                var deck = new Deck();
                deck.calm = host.AddComponent<AudioSource>();
                deck.fury = host.AddComponent<AudioSource>();
                deck.danger = host.AddComponent<AudioSource>();
                foreach (var s in new[] { deck.calm, deck.fury, deck.danger })
                {
                    s.loop = true;
                    s.playOnAwake = false;
                    s.spatialBlend = 0f;
                    s.volume = 0f;
                }
                decks[d] = deck;
            }
            foreach (var id in Prewarm) sfx[id] = Synth.Make(id);
        }

        float SfxVol => SaveSystem.Prefs.master * SaveSystem.Prefs.sfx;
        float MusicVol => SaveSystem.Prefs.master * SaveSystem.Prefs.music;

        public void Play(string id, float vol = 1f, float pitch = 1f, float variance = 0.06f)
        {
            if (string.IsNullOrEmpty(id)) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < 0.035f) return;
            lastPlayed[id] = now;
            if (!sfx.TryGetValue(id, out var clip))
            {
                clip = Synth.Make(id);
                sfx[id] = clip;
            }
            var src = pool[next];
            next = (next + 1) % pool.Length;
            src.clip = clip;
            src.volume = Mathf.Clamp01(vol * SfxVol);
            src.pitch = pitch * (1f + Random.Range(-variance, variance));
            src.Play();
        }

        /// <summary>Start rendering a track in the background so a later Music() call switches instantly.</summary>
        public void Prepare(string track)
        {
            if (string.IsNullOrEmpty(track) || tracks.ContainsKey(track) || pending.ContainsKey(track)) return;
            pending[track] = Task.Run(() => MusicGen.RenderData(track));
        }

        /// <summary>Crossfade to a track. If it is still rendering, the current music plays on until it is ready.</summary>
        public void Music(string track, float fade = 1.6f)
        {
            if (decks[active].track == track) { wanted = null; return; }
            if (!tracks.ContainsKey(track))
            {
                if (pending.ContainsKey(track) || decks[active].track != null) { Prepare(track); wanted = track; wantedFade = fade; return; }
                MusicGen.Render(track, out var calm, out var fury, out var danger);
                Store(track, new[] { calm, fury, danger });
            }
            Switch(track, fade);
        }

        void Store(string track, AudioClip[] clips)
        {
            tracks[track] = clips;
            lru.Remove(track);
            lru.Add(track);
            // free old pieces (each is a few MB of samples), never the ones playing or about to play
            while (lru.Count > MaxCached)
            {
                string old = null;
                foreach (var t in lru)
                {
                    bool inUse = t == wanted;
                    foreach (var d in decks) if (d.track == t && (d.vol > 0f || d.target > 0f)) inUse = true;
                    if (!inUse) { old = t; break; }
                }
                if (old == null) break;
                lru.Remove(old);
                foreach (var c in tracks[old]) if (c != null) Object.Destroy(c);
                tracks.Remove(old);
            }
        }

        void Switch(string track, float fade)
        {
            wanted = null;
            var clips = tracks[track];
            lru.Remove(track);
            lru.Add(track);
            decks[active].target = 0f;
            // pick a deck that is silent (three decks, so a quick change never cuts a fading piece)
            int pick = (active + 1) % decks.Length;
            for (int i = 1; i < decks.Length; i++)
            {
                int k = (active + i) % decks.Length;
                if (decks[k].vol <= 0f) { pick = k; break; }
            }
            active = pick;
            var d = decks[active];
            d.track = track;
            d.calm.clip = clips[0];
            d.fury.clip = clips[1];
            d.danger.clip = clips[2];
            d.calm.Play();
            d.fury.Play();
            d.danger.Play();
            d.calm.timeSamples = d.fury.timeSamples = d.danger.timeSamples = 0;
            d.vol = 0f;
            d.target = 1f;
            fadeSpeed = 1f / Mathf.Max(0.2f, fade);
        }

        float fadeSpeed = 1f / 1.6f;

        public void StopMusic()
        {
            foreach (var d in decks) d.target = 0f;
            decks[active].track = null;
            wanted = null;
        }

        /// <summary>0 = calm exploration, 1 = full battle.</summary>
        public void Intensity(float v) => intensityTarget = Mathf.Clamp01(v);

        /// <summary>0 = in control, 1 = overwhelmed. Only heard while Intensity is up.</summary>
        public void Danger(float v) => dangerTarget = Mathf.Clamp01(v);

        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;

            // finished background renders become clips; a waiting track starts as soon as it is ready
            if (pending.Count > 0)
            {
                string done = null;
                foreach (var kv in pending) if (kv.Value.IsCompleted) { done = kv.Key; break; }
                if (done != null)
                {
                    var task = pending[done];
                    pending.Remove(done);
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        MusicGen.MakeClips(done, task.Result, out var calm, out var fury, out var dng);
                        Store(done, new[] { calm, fury, dng });
                    }
                    else Debug.LogWarning("[Wyrdbound] music render failed for " + done + ": " + task.Exception?.GetBaseException().Message);
                }
            }
            if (wanted != null && tracks.ContainsKey(wanted)) Switch(wanted, wantedFade);

            intensity = U.Approach(intensity, intensityTarget, dt * 0.6f);
            // danger rises fast (a threat should be heard at once) and settles slowly
            danger = U.Approach(danger, dangerTarget * intensityTarget, dt * (dangerTarget > danger ? 1.4f : 0.35f));
            for (int i = 0; i < decks.Length; i++)
            {
                var d = decks[i];
                d.vol = U.Approach(d.vol, d.target, dt * fadeSpeed);
                if (d.vol <= 0f && d.target <= 0f)
                {
                    if (d.calm.isPlaying) { d.calm.Stop(); d.fury.Stop(); d.danger.Stop(); }
                    continue;
                }
                float m = d.vol * MusicVol;
                d.calm.volume = m * Mathf.Lerp(1f, 0.6f, intensity) * Mathf.Lerp(1f, 0.7f, danger);
                d.fury.volume = m * intensity * Mathf.Lerp(1f, 0.85f, danger);
                d.danger.volume = m * Mathf.SmoothStep(0f, 1f, danger) * 0.95f;
                if (d.calm.isPlaying)
                {
                    int ts = d.calm.timeSamples;
                    if (d.fury.isPlaying && Mathf.Abs(ts - d.fury.timeSamples) > 2048) d.fury.timeSamples = ts;
                    if (d.danger.isPlaying && Mathf.Abs(ts - d.danger.timeSamples) > 2048) d.danger.timeSamples = ts;
                }
            }
        }
    }
}
