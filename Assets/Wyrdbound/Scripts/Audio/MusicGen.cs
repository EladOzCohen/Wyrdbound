using System;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Generative Nordic score. Each track renders three synchronized loops that the AudioManager mixes live
    /// from the state of the fight:
    ///   calm   — drone, plucked lyre, wind, low horn (always playing)
    ///   fury   — frame drums and bowed tagelharpa (in combat)
    ///   danger — driving ostinato, war drums and horn blasts (when foes close in, Narfi is hurt or low)
    /// A track name may carry a variation, "niflhel:2": a different key, seed, progression and groove, so each
    /// chamber of a realm sounds related but new. Loops are 16 bars in two sections (A then B).
    /// Rendering is pure math with no Unity objects, so it can run on a worker thread (RenderData).
    /// </summary>
    public static class MusicGen
    {
        sealed class Spec
        {
            public float root = 146.83f, bpm = 84f;
            public int[] scale = { 0, 2, 3, 5, 7, 8, 10 };
            public int seed = 1, bars = 16;
            public float drumDensity = 0.6f, lyre = 1f, bowed = 1f, drone = 1f, horn = 0.3f, wind = 0.4f;
            public bool heavy;
            public int variant;
        }

        static readonly int[][] Progressions =
        {
            new[] { 0, 0, 5, 6, 0, 3, 5, 4 },
            new[] { 0, 5, 3, 6, 0, 5, 6, 0 },
            new[] { 0, 3, 4, 0, 5, 3, 6, 4 },
            new[] { 0, 6, 5, 6, 3, 4, 5, 0 },
            new[] { 0, 0, 3, 3, 5, 5, 6, 4 },
        };

        static Spec Get(string track)
        {
            int v = 0;
            int colon = track.IndexOf(':');
            if (colon >= 0) { int.TryParse(track.Substring(colon + 1), out v); track = track.Substring(0, colon); }
            var s = GetBase(track);
            s.variant = v;
            if (v > 0)
            {
                // same realm, new piece: shifted key, new seed (motifs, drum fills, bowed line), nudged tempo and groove
                int[] shift = { 0, -2, 3, -4, 2 };
                s.root *= Mathf.Pow(2f, shift[v % shift.Length] / 12f);
                s.seed += v * 101;
                s.bpm *= 1f + ((v % 3) - 1) * 0.05f;
                s.drumDensity = Mathf.Clamp01(s.drumDensity + ((v % 2 == 0) ? 0.1f : -0.1f));
                s.horn = Mathf.Clamp01(s.horn + (v % 3 == 2 ? 0.25f : 0f));
                s.lyre *= v % 3 == 1 ? 0.8f : 1f;
            }
            return s;
        }

        static Spec GetBase(string track)
        {
            switch (track)
            {
                case "title": return new Spec { root = 146.83f, bpm = 62, seed = 3, drumDensity = 0.3f, horn = 0.8f, wind = 0.6f };
                case "hall": return new Spec { root = 164.81f, bpm = 70, scale = new[] { 0, 2, 3, 5, 7, 9, 10 }, seed = 5, drumDensity = 0.25f, bowed = 0.6f, horn = 0.15f, wind = 0.25f };
                case "niflhel": return new Spec { root = 146.83f, bpm = 84, seed = 11, drumDensity = 0.55f, wind = 0.5f };
                case "nastrond": return new Spec { root = 130.81f, bpm = 90, scale = new[] { 0, 1, 3, 5, 7, 8, 10 }, seed = 13, drumDensity = 0.65f, horn = 0.4f };
                case "gjoll": return new Spec { root = 110f, bpm = 96, seed = 17, drumDensity = 0.7f, wind = 0.6f };
                case "helgrind": return new Spec { root = 185f, bpm = 100, scale = new[] { 0, 1, 3, 5, 7, 8, 10 }, seed = 19, drumDensity = 0.75f, horn = 0.5f };
                case "boss": return new Spec { root = 146.83f, bpm = 124, scale = new[] { 0, 1, 3, 5, 7, 8, 10 }, seed = 23, drumDensity = 1f, heavy = true, horn = 0.7f };
                case "boss_hel": return new Spec { root = 138.59f, bpm = 116, scale = new[] { 0, 1, 4, 5, 7, 8, 10 }, seed = 29, drumDensity = 1f, heavy = true, horn = 0.9f };
                case "surface": return new Spec { root = 196f, bpm = 56, seed = 31, drumDensity = 0.1f, bowed = 0.8f, horn = 0.2f, wind = 0.8f };
                case "ending": return new Spec { root = 174.61f, bpm = 60, scale = new[] { 0, 2, 4, 5, 7, 9, 11 }, seed = 37, drumDensity = 0.2f, horn = 0.6f, wind = 0.4f };
            }
            return new Spec();
        }

        /// <summary>Render a track's three layers to sample arrays. Thread-safe (no Unity objects).</summary>
        public static float[][] RenderData(string track)
        {
            var s = Get(track);
            float beat = 60f / s.bpm;
            float len = s.bars * 4 * beat;
            int n = (int)(len * Synth.SR);
            var a = new float[n];
            var b = new float[n];
            var c = new float[n];
            var rng = new System.Random(s.seed);

            // chord roots per bar (scale degrees): section A, then a contrasting section B
            int pa = (s.seed + s.variant) % Progressions.Length;
            int pb = (pa + 2 + s.variant) % Progressions.Length;
            var prog = new int[s.bars];
            for (int i = 0; i < s.bars; i++) prog[i] = i < 8 ? Progressions[pa][i % 8] : Progressions[pb][i % 8];

            if (s.drone > 0f) Drone(a, s, prog, beat);
            if (s.wind > 0f) Wind(a, s.wind, s.seed);
            if (s.lyre > 0f) Lyre(a, s, prog, beat, rng);
            if (s.horn > 0f) Horn(a, s, prog, beat);

            Drums(b, s, beat, rng);
            if (s.bowed > 0f) Bowed(b, s, prog, beat, rng);
            if (s.heavy) Drone(b, s, prog, beat, 0.5f);

            Ostinato(c, s, prog, beat);
            WarDrums(c, s, beat);
            HornBlasts(c, s, prog, beat);

            Reverb(a, 0.32f);
            Reverb(b, 0.22f);
            Reverb(c, 0.18f);
            return new[] { a, b, c };
        }

        public static void Render(string track, out AudioClip calm, out AudioClip fury, out AudioClip danger)
        {
            var d = RenderData(track);
            MakeClips(track, d, out calm, out fury, out danger);
        }

        /// <summary>Turn rendered layers into clips (main thread only).</summary>
        public static void MakeClips(string track, float[][] d, out AudioClip calm, out AudioClip fury, out AudioClip danger)
        {
            bool heavy = Get(track).heavy;
            calm = Synth.Clip(track + "_calm", d[0], 0.55f);
            fury = Synth.Clip(track + "_fury", d[1], heavy ? 0.75f : 0.6f);
            danger = Synth.Clip(track + "_danger", d[2], 0.62f);
        }

        /// <summary>Danger layer: a relentless 16th-note pluck on the chord root and fifth, accented on the beat.</summary>
        static void Ostinato(float[] buf, Spec s, int[] prog, float beat)
        {
            float step = beat * 0.25f;
            int steps = s.bars * 16;
            int[] pattern = { 0, 0, 4, 0, 0, 4, 0, 7, 0, 0, 4, 0, 7, 4, 0, 4 };
            for (int k = 0; k < steps; k++)
            {
                int bar = Mathf.Min(prog.Length - 1, k / 16);
                int deg = prog[bar] + (pattern[k % 16] == 7 ? 4 : pattern[k % 16] == 4 ? 2 : 0);
                float f = Freq(s, deg, -1);
                float amp = (k % 4 == 0 ? 0.55f : 0.32f) * (bar >= 8 ? 1.1f : 1f);
                Pluck(buf, k * step, f, amp, (uint)(k * 13 + s.seed));
            }
        }

        /// <summary>Danger layer: deep war drums on every beat, doubled at the end of each phrase.</summary>
        static void WarDrums(float[] buf, Spec s, float beat)
        {
            int beats = s.bars * 4;
            for (int k = 0; k < beats; k++)
            {
                bool phraseEnd = (k % 16) >= 14;
                Drum(buf, k * beat, 52f, 0.32f, k % 4 == 0 ? 1.15f : 0.85f, (uint)(k * 7 + 3), true);
                if (phraseEnd) Drum(buf, k * beat + beat * 0.5f, 58f, 0.25f, 0.8f, (uint)(k * 11 + 5), true);
                if (k % 2 == 1) Drum(buf, k * beat, 300f, 0.04f, 0.35f, (uint)(k * 17 + 9), false);
            }
        }

        /// <summary>Danger layer: short, rising horn calls every two bars.</summary>
        static void HornBlasts(float[] buf, Spec s, int[] prog, float beat)
        {
            float barLen = beat * 4f;
            for (int bar = 0; bar < prog.Length; bar += 2)
            {
                float start = bar * barLen + beat * 3f;
                float f = Freq(s, prog[bar] + 4, -1);
                int s0 = (int)(start * Synth.SR);
                int len = (int)(beat * 1.4f * Synth.SR);
                var lp = new Synth.Lp();
                float ph = 0, ph2 = 0;
                for (int i = 0; i < len && s0 + i < buf.Length; i++)
                {
                    float t = i / (float)Synth.SR;
                    float bend = 1f - 0.04f * Mathf.Exp(-t / 0.08f);
                    ph += f * bend / Synth.SR; ph2 += f * 2f * bend / Synth.SR;
                    float env = Mathf.Clamp01(t / 0.05f) * Mathf.Clamp01((len / (float)Synth.SR - t) / 0.25f);
                    float x = Synth.Saw(ph) + 0.35f * Synth.Saw(ph2);
                    buf[s0 + i] += lp.Run(x, 0.06f + 0.06f * env) * env * 0.38f;
                }
            }
        }

        static float Freq(Spec s, int degree, int octave = 0)
        {
            int len = s.scale.Length;
            int oct = Mathf.FloorToInt(degree / (float)len);
            int idx = ((degree % len) + len) % len;
            return s.root * Mathf.Pow(2f, (s.scale[idx] + 12 * (oct + octave)) / 12f);
        }

        static void Drone(float[] buf, Spec s, int[] prog, float beat, float amp = 1f)
        {
            float barLen = beat * 4f;
            var lp = new Synth.Lp();
            float p1 = 0, p2 = 0, p3 = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)Synth.SR;
                int bar = Mathf.Min(prog.Length - 1, (int)(t / barLen));
                float f = Freq(s, prog[bar], -2);
                float f5 = Freq(s, prog[bar] + 4, -2);
                p1 += f / Synth.SR; p2 += f * 1.003f / Synth.SR; p3 += f5 / Synth.SR;
                float x = Synth.Saw(p1) + Synth.Saw(p2) + 0.6f * Synth.Saw(p3);
                float k = 0.025f + 0.012f * Mathf.Sin(t * 0.4f);
                buf[i] += lp.Run(x, k) * 0.32f * s.drone * amp;
            }
        }

        static void Wind(float[] buf, float amp, int seed)
        {
            var n = new Synth.Noise((uint)(seed * 977 + 1));
            var lp = new Synth.Lp();
            var lp2 = new Synth.Lp();
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)Synth.SR;
                float k = 0.01f + 0.02f * (0.5f + 0.5f * Mathf.Sin(t * 0.37f + Mathf.Sin(t * 0.11f) * 2f));
                buf[i] += lp2.Run(lp.Run(n.Next(), k), k) * 1.4f * amp;
            }
        }

        /// <summary>Karplus-Strong plucked lyre playing a phrase built on AABA motifs.</summary>
        static void Lyre(float[] buf, Spec s, int[] prog, float beat, System.Random rng)
        {
            int steps = s.bars * 8;
            int[] motifA = Motif(rng, 8), motifB = Motif(rng, 8);
            float step = beat * 0.5f;
            for (int k = 0; k < steps; k++)
            {
                int bar = k / 8;
                int[] m = (bar % 4 == 2) ? motifB : motifA;
                int note = m[k % 8];
                if (note == int.MinValue) continue;
                float f = Freq(s, prog[bar] + note, 0);
                Pluck(buf, k * step, f, 0.55f * s.lyre * (k % 8 == 0 ? 1f : 0.75f), (uint)(k + 1));
                if (k % 8 == 0) Pluck(buf, k * step, Freq(s, prog[bar], -1), 0.35f * s.lyre, (uint)(k + 101));
            }
        }

        static int[] Motif(System.Random rng, int len)
        {
            var m = new int[len];
            int cur = 0;
            for (int i = 0; i < len; i++)
            {
                if (i > 0 && rng.NextDouble() < 0.3) { m[i] = int.MinValue; continue; }
                cur += rng.Next(-2, 3);
                cur = Mathf.Clamp(cur, -2, 6);
                m[i] = cur;
            }
            m[0] = 0;
            return m;
        }

        static void Pluck(float[] buf, float start, float f, float amp, uint seed)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(Synth.SR / f));
            var line = new float[period];
            var n = new Synth.Noise(seed * 7919u + 3u);
            for (int i = 0; i < period; i++) line[i] = n.Next();
            int s0 = (int)(start * Synth.SR);
            int len = Mathf.Min(buf.Length - s0, (int)(2.2f * Synth.SR));
            int idx = 0;
            float prev = 0f;
            for (int i = 0; i < len; i++)
            {
                float v = line[idx];
                float nv = 0.5f * (v + prev) * 0.996f;
                prev = v;
                line[idx] = nv;
                idx = (idx + 1) % period;
                int o = s0 + i;
                if (o >= buf.Length) break;
                buf[o] += v * amp;
            }
        }

        static void Horn(float[] buf, Spec s, int[] prog, float beat)
        {
            float barLen = beat * 4f;
            for (int bar = 0; bar < prog.Length; bar += 4)
            {
                float start = bar * barLen + beat * 0.5f;
                float f = Freq(s, prog[bar], -1);
                int s0 = (int)(start * Synth.SR);
                int len = (int)(barLen * 1.6f * Synth.SR);
                var lp = new Synth.Lp();
                float ph = 0, ph2 = 0;
                for (int i = 0; i < len && s0 + i < buf.Length; i++)
                {
                    float t = i / (float)Synth.SR;
                    float vib = 1f + 0.004f * Mathf.Sin(t * 5.2f * 6.283f);
                    ph += f * vib / Synth.SR; ph2 += f * 1.5f * vib / Synth.SR;
                    float env = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((len / (float)Synth.SR - t) / 0.8f);
                    float x = Synth.Saw(ph) + 0.45f * Synth.Saw(ph2);
                    buf[s0 + i] += lp.Run(x, 0.03f + 0.03f * env) * env * 0.45f * s.horn;
                }
            }
        }

        static void Drums(float[] buf, Spec s, float beat, System.Random rng)
        {
            int steps = s.bars * 16;
            float step = beat * 0.25f;
            // base frame-drum pattern (16ths): strong on 1, ghost notes, syncopation
            float[] pat = { 1f, 0f, 0f, 0.35f, 0f, 0f, 0.7f, 0f, 0.85f, 0f, 0.3f, 0f, 0f, 0.45f, 0f, 0.25f };
            for (int k = 0; k < steps; k++)
            {
                float v = pat[k % 16];
                if (v <= 0f) { if (rng.NextDouble() < 0.06 * s.drumDensity) v = 0.2f; else continue; }
                if (v < 0.5f && rng.NextDouble() > s.drumDensity) continue;
                float t0 = k * step;
                bool low = v > 0.6f;
                Drum(buf, t0, low ? 68f : 150f, low ? 0.22f : 0.08f, v * (s.heavy ? 1.15f : 0.9f), (uint)(k + 7), low);
                if (s.heavy && k % 4 == 2) Drum(buf, t0, 260f, 0.05f, 0.35f, (uint)(k + 999), false);
            }
            // bar-end fills
            for (int bar = 3; bar < s.bars; bar += 4)
                for (int j = 0; j < 4; j++)
                    Drum(buf, (bar * 16 + 12 + j) * step, 120f - j * 12f, 0.1f, 0.5f + j * 0.12f, (uint)(bar * 31 + j), true);
        }

        static void Drum(float[] buf, float start, float f, float decay, float amp, uint seed, bool body)
        {
            int s0 = (int)(start * Synth.SR);
            int len = (int)((decay * 6f + 0.02f) * Synth.SR);
            var n = new Synth.Noise(seed * 2654435761u + 1u);
            var lp = new Synth.Lp();
            float ph = 0;
            for (int i = 0; i < len && s0 + i < buf.Length; i++)
            {
                float t = i / (float)Synth.SR;
                float ff = f * (1f + 1.2f * Mathf.Exp(-t / 0.015f));
                ph += ff / Synth.SR;
                float tone = Synth.Sin(ph) * Mathf.Exp(-t / decay);
                float slap = lp.Run(n.Next(), body ? 0.25f : 0.6f) * Mathf.Exp(-t / 0.012f);
                buf[s0 + i] += (tone * (body ? 1f : 0.6f) + slap * 0.5f) * amp;
            }
        }

        /// <summary>A bowed tagelharpa line: rough saw with slow vibrato, long notes.</summary>
        static void Bowed(float[] buf, Spec s, int[] prog, float beat, System.Random rng)
        {
            float barLen = beat * 4f;
            var lp = new Synth.Lp();
            var lp2 = new Synth.Lp();
            float ph = 0;
            int[] line = new int[s.bars * 2];
            int cur = 4;
            for (int i = 0; i < line.Length; i++) { cur = Mathf.Clamp(cur + rng.Next(-2, 3), 0, 9); line[i] = cur; }
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)Synth.SR;
                int half = Mathf.Min(line.Length - 1, (int)(t / (barLen * 0.5f)));
                int bar = Mathf.Min(prog.Length - 1, (int)(t / barLen));
                float f = Freq(s, prog[bar] + line[half], -1);
                float local = t - half * barLen * 0.5f;
                float vib = 1f + 0.006f * Mathf.Sin(t * 5.5f * 6.283f) * Mathf.Clamp01(local / 0.3f);
                ph += f * vib / Synth.SR;
                float env = Mathf.Clamp01(local / 0.08f) * (0.7f + 0.3f * Mathf.Sin(local * 3f));
                float x = Synth.Saw(ph);
                x = lp2.Run(lp.Run(x, 0.12f), 0.2f);
                buf[i] += x * env * 0.5f * s.bowed;
            }
        }

        /// <summary>Schroeder reverb, run twice so the tail wraps seamlessly around the loop point.</summary>
        static void Reverb(float[] buf, float mix)
        {
            int[] combLen = { 1116, 1188, 1277, 1356 };
            int[] apLen = { 556, 441 };
            var combs = new float[combLen.Length][];
            var cIdx = new int[combLen.Length];
            var cLp = new float[combLen.Length];
            for (int i = 0; i < combLen.Length; i++) combs[i] = new float[combLen[i] / 2];
            var aps = new float[apLen.Length][];
            var aIdx = new int[apLen.Length];
            for (int i = 0; i < apLen.Length; i++) aps[i] = new float[apLen[i] / 2];
            var dry = (float[])buf.Clone();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < dry.Length; i++)
                {
                    float x = dry[i] * 0.2f;
                    float acc = 0f;
                    for (int c = 0; c < combs.Length; c++)
                    {
                        var line = combs[c];
                        float y = line[cIdx[c]];
                        cLp[c] = y * 0.7f + cLp[c] * 0.3f;
                        line[cIdx[c]] = x + cLp[c] * 0.84f;
                        cIdx[c] = (cIdx[c] + 1) % line.Length;
                        acc += y;
                    }
                    for (int a = 0; a < aps.Length; a++)
                    {
                        var line = aps[a];
                        float bv = line[aIdx[a]];
                        float o = -acc + bv;
                        line[aIdx[a]] = acc + bv * 0.5f;
                        aIdx[a] = (aIdx[a] + 1) % line.Length;
                        acc = o;
                    }
                    if (pass == 1) buf[i] = dry[i] * (1f - mix * 0.5f) + acc * mix;
                }
            }
        }
    }
}
