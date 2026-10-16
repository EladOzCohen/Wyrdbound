using System;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Generative Nordic score. Each track renders two synchronized loops:
    /// a "calm" layer (drone, plucked lyre, wind) and a "fury" layer (frame drums, bowed tagelharpa, low horn)
    /// that the AudioManager crossfades in combat.
    /// </summary>
    public static class MusicGen
    {
        sealed class Spec
        {
            public float root = 146.83f, bpm = 84f;
            public int[] scale = { 0, 2, 3, 5, 7, 8, 10 };
            public int seed = 1, bars = 8;
            public float drumDensity = 0.6f, lyre = 1f, bowed = 1f, drone = 1f, horn = 0.3f, wind = 0.4f;
            public bool heavy;
        }

        static Spec Get(string track)
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

        public static void Render(string track, out AudioClip calm, out AudioClip fury)
        {
            var s = Get(track);
            float beat = 60f / s.bpm;
            float len = s.bars * 4 * beat;
            int n = (int)(len * Synth.SR);
            var a = new float[n];
            var b = new float[n];
            var rng = new System.Random(s.seed);

            // chord roots per bar (scale degrees), a dark i - VI - VII - i style progression with variation
            int[] prog = { 0, 0, 5, 6, 0, 3, 5, 4 };
            if (s.seed % 3 == 0) prog = new[] { 0, 5, 3, 6, 0, 5, 6, 0 };

            if (s.drone > 0f) Drone(a, s, prog, beat);
            if (s.wind > 0f) Wind(a, s.wind, s.seed);
            if (s.lyre > 0f) Lyre(a, s, prog, beat, rng);
            if (s.horn > 0f) Horn(a, s, prog, beat);

            Drums(b, s, beat, rng);
            if (s.bowed > 0f) Bowed(b, s, prog, beat, rng);
            if (s.heavy) Drone(b, s, prog, beat, 0.5f);

            Reverb(a, 0.32f);
            Reverb(b, 0.22f);
            calm = Synth.Clip(track + "_calm", a, 0.55f);
            fury = Synth.Clip(track + "_fury", b, s.heavy ? 0.75f : 0.6f);
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
