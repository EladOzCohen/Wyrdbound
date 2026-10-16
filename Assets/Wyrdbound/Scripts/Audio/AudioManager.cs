using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Pooled SFX playback and a two-layer music system that rises with the fighting.</summary>
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
            public AudioSource calm, fury;
            public float vol, target;
            public string track;
        }

        readonly AudioSource[] pool;
        int next;
        readonly Dictionary<string, AudioClip> sfx = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        readonly Dictionary<string, AudioClip[]> tracks = new Dictionary<string, AudioClip[]>();
        readonly Deck[] decks = new Deck[2];
        int active;
        float intensity, intensityTarget;
        public string CurrentTrack => decks[active].track;

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
            for (int d = 0; d < 2; d++)
            {
                var deck = new Deck();
                deck.calm = host.AddComponent<AudioSource>();
                deck.fury = host.AddComponent<AudioSource>();
                foreach (var s in new[] { deck.calm, deck.fury })
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

        public void Music(string track, float fade = 1.6f)
        {
            if (decks[active].track == track) return;
            if (!tracks.TryGetValue(track, out var clips))
            {
                MusicGen.Render(track, out var calm, out var fury);
                clips = new[] { calm, fury };
                tracks[track] = clips;
            }
            decks[active].target = 0f;
            active = 1 - active;
            var d = decks[active];
            d.track = track;
            d.calm.clip = clips[0];
            d.fury.clip = clips[1];
            d.calm.Play();
            d.fury.Play();
            d.fury.timeSamples = d.calm.timeSamples = 0;
            d.vol = 0f;
            d.target = 1f;
        }

        public void StopMusic()
        {
            decks[0].target = 0f;
            decks[1].target = 0f;
            decks[active].track = null;
        }

        /// <summary>0 = calm exploration, 1 = full battle.</summary>
        public void Intensity(float v) => intensityTarget = Mathf.Clamp01(v);

        public void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            intensity = U.Approach(intensity, intensityTarget, dt * 0.6f);
            for (int i = 0; i < 2; i++)
            {
                var d = decks[i];
                d.vol = U.Approach(d.vol, d.target, dt / 1.6f);
                if (d.vol <= 0f && d.target <= 0f)
                {
                    if (d.calm.isPlaying) { d.calm.Stop(); d.fury.Stop(); }
                    continue;
                }
                d.calm.volume = d.vol * MusicVol * Mathf.Lerp(1f, 0.7f, intensity);
                d.fury.volume = d.vol * MusicVol * intensity;
                if (d.calm.isPlaying && d.fury.isPlaying && Mathf.Abs(d.calm.timeSamples - d.fury.timeSamples) > 2048)
                    d.fury.timeSamples = d.calm.timeSamples;
            }
        }
    }
}
