using System;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Procedural sound design. Every sound effect is synthesised at startup from oscillators,
    /// filtered noise and envelopes, so the game ships with no audio files.
    /// </summary>
    public static class Synth
    {
        public const int SR = 22050;

        // ------------------------------------------------------------- DSP helpers

        public struct Lp
        {
            float y;
            public float Run(float x, float k) { y += (x - y) * Mathf.Clamp01(k); return y; }
        }

        public struct Hp
        {
            float lp;
            public float Run(float x, float k) { lp += (x - lp) * Mathf.Clamp01(k); return x - lp; }
        }

        public sealed class Noise
        {
            uint s;
            public Noise(uint seed) { s = seed == 0 ? 0x9E3779B9u : seed; }
            public float Next()
            {
                s ^= s << 13; s ^= s >> 17; s ^= s << 5;
                return (s / 4294967295f) * 2f - 1f;
            }
        }

        public static float Sin(float ph) => Mathf.Sin(ph * 2f * Mathf.PI);
        public static float Saw(float ph) => 2f * (ph - Mathf.Floor(ph + 0.5f));
        public static float Sqr(float ph) => (ph - Mathf.Floor(ph)) < 0.5f ? 1f : -1f;
        public static float Tri(float ph) { float f = ph - Mathf.Floor(ph); return f < 0.5f ? 4f * f - 1f : 3f - 4f * f; }

        public static float Env(float t, float a, float d) => t < a ? t / Mathf.Max(1e-4f, a) : Mathf.Exp(-(t - a) / Mathf.Max(1e-4f, d));

        static float[] Buf(float secs) => new float[Mathf.Max(1, (int)(secs * SR))];

        public static AudioClip Clip(string name, float[] d, float peak = 0.9f)
        {
            float m = 1e-6f;
            for (int i = 0; i < d.Length; i++) m = Mathf.Max(m, Mathf.Abs(d[i]));
            float k = peak / m;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
            int fade = Mathf.Min(64, d.Length / 4);
            for (int i = 0; i < fade; i++) d[d.Length - 1 - i] *= i / (float)fade;
            var c = AudioClip.Create(name, d.Length, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        /// <summary>Kick-like thump: sine with falling pitch.</summary>
        static void Thump(float[] b, float start, float f0, float f1, float decay, float amp)
        {
            int s0 = (int)(start * SR);
            float ph = 0f;
            for (int i = s0; i < b.Length; i++)
            {
                float t = (i - s0) / (float)SR;
                float e = Mathf.Exp(-t / decay);
                if (e < 0.001f) break;
                float f = f1 + (f0 - f1) * Mathf.Exp(-t / (decay * 0.35f));
                ph += f / SR;
                b[i] += Sin(ph) * e * amp;
            }
        }

        static void NoiseBurst(float[] b, float start, float attack, float decay, float amp, float lpK, float hpK, uint seed)
        {
            var n = new Noise(seed);
            var lp = new Lp();
            var hp = new Hp();
            int s0 = (int)(start * SR);
            for (int i = s0; i < b.Length; i++)
            {
                float t = (i - s0) / (float)SR;
                float e = Env(t, attack, decay);
                if (t > attack && e < 0.001f) break;
                float x = lp.Run(n.Next(), lpK);
                if (hpK > 0f) x = hp.Run(x, hpK);
                b[i] += x * e * amp;
            }
        }

        static void Tone(float[] b, float start, float f0, float f1, float attack, float decay, float amp, int wave = 0, float vib = 0f, float vibRate = 5f)
        {
            int s0 = (int)(start * SR);
            float ph = 0f;
            for (int i = s0; i < b.Length; i++)
            {
                float t = (i - s0) / (float)SR;
                float e = Env(t, attack, decay);
                if (t > attack && e < 0.001f) break;
                float k = Mathf.Clamp01(t / Mathf.Max(0.001f, attack + decay * 2f));
                float f = Mathf.Lerp(f0, f1, k) * (1f + vib * Mathf.Sin(t * vibRate * 2f * Mathf.PI));
                ph += f / SR;
                float w = wave == 0 ? Sin(ph) : wave == 1 ? Saw(ph) : wave == 2 ? Sqr(ph) : Tri(ph);
                b[i] += w * e * amp;
            }
        }

        static void Sweep(float[] b, float k0, float k1, float amp, float attack, float decay, uint seed, float hpK = 0.02f)
        {
            var n = new Noise(seed);
            var lp = new Lp();
            var lp2 = new Lp();
            var hp = new Hp();
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float u = i / (float)b.Length;
                float k = Mathf.Lerp(k0, k1, u);
                float x = lp2.Run(lp.Run(n.Next(), k), k);
                x = hp.Run(x, hpK);
                b[i] += x * Env(t, attack, decay) * amp;
            }
        }

        static void Bell(float[] b, float start, float f, float decay, float amp)
        {
            float[] ratios = { 1f, 2.0f, 2.42f, 3.0f, 4.17f, 5.43f };
            float[] amps = { 1f, 0.6f, 0.45f, 0.3f, 0.2f, 0.12f };
            for (int k = 0; k < ratios.Length; k++)
                Tone(b, start, f * ratios[k], f * ratios[k], 0.002f, decay / (1f + k * 0.6f), amp * amps[k]);
        }

        static void Drive(float[] b, float gain)
        {
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * gain);
        }

        // ------------------------------------------------------------- the effect library

        public static AudioClip Make(string id)
        {
            float[] b;
            switch (id)
            {
                case "swing":
                    b = Buf(0.2f); Sweep(b, 0.55f, 0.08f, 1f, 0.02f, 0.05f, 11, 0.08f); break;
                case "swing_heavy":
                    b = Buf(0.32f); Sweep(b, 0.35f, 0.04f, 1f, 0.05f, 0.08f, 12, 0.03f); Tone(b, 0, 180, 90, 0.04f, 0.08f, 0.2f); break;
                case "hit":
                    b = Buf(0.22f); Thump(b, 0, 160, 55, 0.07f, 1f); NoiseBurst(b, 0, 0.001f, 0.025f, 0.8f, 0.5f, 0.05f, 21); Tone(b, 0, 320, 200, 0.001f, 0.02f, 0.3f, 2); Drive(b, 1.6f); break;
                case "hit_heavy":
                    b = Buf(0.4f); Thump(b, 0, 120, 38, 0.13f, 1f); NoiseBurst(b, 0, 0.001f, 0.06f, 0.9f, 0.35f, 0.02f, 22); Drive(b, 2f); break;
                case "crit":
                    b = Buf(0.5f); Thump(b, 0, 150, 50, 0.08f, 0.9f); NoiseBurst(b, 0, 0.001f, 0.03f, 0.6f, 0.6f, 0.08f, 23); Tone(b, 0, 1900, 1900, 0.001f, 0.18f, 0.35f); Tone(b, 0, 2870, 2870, 0.001f, 0.12f, 0.25f); Drive(b, 1.4f); break;
                case "armor":
                    b = Buf(0.35f); Bell(b, 0, 620, 0.25f, 0.6f); NoiseBurst(b, 0, 0.001f, 0.02f, 0.5f, 0.7f, 0.2f, 24); break;
                case "hurt":
                    b = Buf(0.45f); Thump(b, 0, 110, 40, 0.15f, 1f); NoiseBurst(b, 0, 0.002f, 0.08f, 0.7f, 0.25f, 0.01f, 25); Tone(b, 0.01f, 330, 140, 0.01f, 0.12f, 0.35f, 1); Drive(b, 2.2f); break;
                case "dash":
                    b = Buf(0.26f); Sweep(b, 0.08f, 0.45f, 1f, 0.08f, 0.06f, 31, 0.05f); break;
                case "cast":
                    b = Buf(0.35f); Tone(b, 0, 380, 980, 0.02f, 0.12f, 0.5f, 3); Sweep(b, 0.2f, 0.6f, 0.4f, 0.05f, 0.1f, 32, 0.1f); break;
                case "cast_land":
                    b = Buf(0.25f); NoiseBurst(b, 0, 0.001f, 0.02f, 0.7f, 0.8f, 0.2f, 33); Tone(b, 0, 920, 860, 0.001f, 0.05f, 0.5f); Thump(b, 0, 140, 70, 0.05f, 0.5f); break;
                case "die":
                    b = Buf(0.9f); Thump(b, 0, 100, 35, 0.12f, 0.8f); for (int k = 0; k < 5; k++) Tone(b, 0.02f + k * 0.03f, 900 + k * 230, 600 + k * 180, 0.02f, 0.35f, 0.12f); Sweep(b, 0.3f, 0.05f, 0.4f, 0.01f, 0.25f, 34, 0.05f); break;
                case "spawn":
                    b = Buf(0.6f); Sweep(b, 0.02f, 0.4f, 1f, 0.5f, 0.04f, 35, 0.01f); Thump(b, 0.48f, 140, 60, 0.06f, 0.6f); break;
                case "coin":
                    b = Buf(0.35f); Tone(b, 0, 1320, 1320, 0.001f, 0.12f, 0.6f); Tone(b, 0.05f, 1980, 1980, 0.001f, 0.15f, 0.5f); break;
                case "pickup":
                    b = Buf(0.45f); Tone(b, 0, 660, 660, 0.002f, 0.1f, 0.5f, 3); Tone(b, 0.07f, 880, 880, 0.002f, 0.1f, 0.5f, 3); Tone(b, 0.14f, 1320, 1320, 0.002f, 0.18f, 0.5f, 3); break;
                case "boon":
                    b = Buf(1.6f);
                    foreach (var f in new[] { 220f, 330f, 440f, 523.25f, 659.25f, 880f }) Tone(b, 0, f, f, 0.06f, 0.6f, 0.22f, 0, 0.004f, 5.5f);
                    Sweep(b, 0.1f, 0.5f, 0.2f, 0.4f, 0.4f, 36, 0.2f);
                    break;
                case "door":
                    b = Buf(0.9f); Sweep(b, 0.03f, 0.01f, 1f, 0.1f, 0.3f, 37, 0.002f); Tone(b, 0, 55, 50, 0.05f, 0.35f, 0.6f); break;
                case "thunder":
                    b = Buf(1.3f); NoiseBurst(b, 0, 0.001f, 0.05f, 1f, 0.9f, 0.3f, 38); Sweep(b, 0.05f, 0.01f, 0.9f, 0.02f, 0.45f, 39, 0.002f); Drive(b, 1.5f); break;
                case "zap":
                    b = Buf(0.28f);
                    {
                        var n = new Noise(40);
                        float ph = 0;
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = i / (float)SR; ph += 110f / SR;
                            b[i] += (Sqr(ph) * 0.4f + n.Next() * 0.6f) * (n.Next() > 0.2f ? 1f : 0.2f) * Env(t, 0.002f, 0.07f);
                        }
                        Tone(b, 0, 2400, 1800, 0.001f, 0.05f, 0.3f);
                    }
                    break;
                case "frost":
                    b = Buf(0.6f);
                    {
                        var r = new System.Random(41);
                        for (int k = 0; k < 9; k++) Tone(b, (float)r.NextDouble() * 0.2f, 2000 + (float)r.NextDouble() * 2400, 2200 + (float)r.NextDouble() * 2400, 0.001f, 0.07f, 0.25f);
                        Sweep(b, 0.8f, 0.4f, 0.3f, 0.01f, 0.2f, 42, 0.3f);
                    }
                    break;
                case "verdict":
                    b = Buf(0.9f); Thump(b, 0, 90, 40, 0.15f, 0.9f); Bell(b, 0, 340, 0.5f, 0.5f); NoiseBurst(b, 0, 0.001f, 0.04f, 0.6f, 0.6f, 0.1f, 43); Drive(b, 1.4f); break;
                case "raven":
                    b = Buf(0.55f);
                    for (int k = 0; k < 2; k++)
                    {
                        float s = k * 0.25f;
                        Tone(b, s, 640, 430, 0.01f, 0.08f, 0.6f, 1, 0.02f, 30f);
                        NoiseBurst(b, s, 0.01f, 0.06f, 0.3f, 0.4f, 0.1f, (uint)(44 + k));
                    }
                    Drive(b, 2f);
                    break;
                case "charm":
                    b = Buf(0.9f); foreach (var f in new[] { 523.25f, 659.25f, 783.99f, 1046.5f }) Tone(b, 0, f, f * 1.02f, 0.03f, 0.35f, 0.25f, 3, 0.01f, 6f); break;
                case "wave":
                    b = Buf(1.2f); Sweep(b, 0.02f, 0.25f, 1f, 0.4f, 0.4f, 46, 0.005f); break;
                case "telegraph":
                    b = Buf(0.45f); Sweep(b, 0.05f, 0.3f, 0.7f, 0.35f, 0.05f, 47, 0.05f); Tone(b, 0, 200, 420, 0.3f, 0.05f, 0.2f, 3); break;
                case "explode":
                    b = Buf(0.9f); Thump(b, 0, 110, 30, 0.2f, 1f); NoiseBurst(b, 0, 0.002f, 0.25f, 1f, 0.25f, 0.005f, 48); Drive(b, 2.5f); break;
                case "slam":
                    b = Buf(0.6f); Thump(b, 0, 90, 32, 0.18f, 1f); NoiseBurst(b, 0, 0.001f, 0.12f, 0.8f, 0.3f, 0.01f, 49); Drive(b, 2f); break;
                case "roar":
                    b = Buf(1.6f);
                    {
                        var n = new Noise(50);
                        var lp = new Lp();
                        float ph = 0, ph2 = 0;
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = i / (float)SR;
                            float f = 85f + 25f * Mathf.Sin(t * 3f) + 6f * Mathf.Sin(t * 37f);
                            ph += f / SR; ph2 += f * 1.5f / SR;
                            float x = Saw(ph) * 0.6f + Saw(ph2) * 0.3f + n.Next() * 0.5f;
                            b[i] = lp.Run(x, 0.12f + 0.05f * Mathf.Sin(t * 2f)) * Env(t, 0.15f, 0.6f);
                        }
                        Drive(b, 3f);
                    }
                    break;
                case "howl":
                    b = Buf(2.0f);
                    {
                        float ph = 0;
                        var n = new Noise(51);
                        var lp = new Lp();
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = i / (float)SR;
                            float f = t < 0.5f ? Mathf.Lerp(300, 620, t / 0.5f) : Mathf.Lerp(620, 430, (t - 0.5f) / 1.5f);
                            f *= 1f + 0.015f * Mathf.Sin(t * 6f * 2f * Mathf.PI);
                            ph += f / SR;
                            float x = Sin(ph) + 0.3f * Sin(ph * 2f) + 0.1f * lp.Run(n.Next(), 0.2f);
                            b[i] = x * Env(t, 0.2f, 0.7f);
                        }
                    }
                    break;
                case "ui_move":
                    b = Buf(0.06f); Tone(b, 0, 1800, 1800, 0.001f, 0.015f, 0.4f, 3); break;
                case "ui_ok":
                    b = Buf(0.25f); Tone(b, 0, 660, 660, 0.002f, 0.06f, 0.5f, 3); Tone(b, 0.06f, 990, 990, 0.002f, 0.1f, 0.5f, 3); break;
                case "ui_back":
                    b = Buf(0.25f); Tone(b, 0, 660, 660, 0.002f, 0.06f, 0.5f, 3); Tone(b, 0.06f, 440, 440, 0.002f, 0.1f, 0.5f, 3); break;
                case "ui_open":
                    b = Buf(0.5f); Sweep(b, 0.05f, 0.3f, 0.6f, 0.2f, 0.15f, 52, 0.05f); Tone(b, 0.1f, 440, 440, 0.05f, 0.2f, 0.25f, 3); break;
                case "ui_deny":
                    b = Buf(0.22f); Tone(b, 0, 180, 160, 0.002f, 0.08f, 0.6f, 2); break;
                case "heartbeat":
                    b = Buf(0.7f); Thump(b, 0, 70, 40, 0.08f, 1f); Thump(b, 0.22f, 65, 38, 0.1f, 0.8f); break;
                case "horn":
                    b = Buf(2.4f);
                    {
                        float ph = 0, ph2 = 0;
                        var lp = new Lp();
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = i / (float)SR;
                            float f = 110f * (1f + 0.004f * Mathf.Sin(t * 5f * 6.28f));
                            ph += f / SR; ph2 += f * 1.5f / SR;
                            float x = Saw(ph) + 0.5f * Saw(ph2);
                            b[i] = lp.Run(x, 0.06f + 0.04f * Env(t, 0.4f, 0.8f)) * Env(t, 0.35f, 0.9f);
                        }
                    }
                    break;
                case "level":
                    b = Buf(0.9f); for (int k = 0; k < 4; k++) Bell(b, k * 0.08f, 523.25f * Mathf.Pow(2f, k * 4 / 12f), 0.5f, 0.25f); break;
                case "arrow":
                    b = Buf(0.15f); Sweep(b, 0.7f, 0.3f, 0.6f, 0.005f, 0.05f, 53, 0.3f); break;
                case "bell":
                    b = Buf(2.5f); Bell(b, 0, 196f, 1.5f, 1f); break;
                case "splash":
                    b = Buf(0.4f);
                    {
                        var r = new System.Random(54);
                        for (int k = 0; k < 6; k++) Tone(b, (float)r.NextDouble() * 0.15f, 300 + (float)r.NextDouble() * 500, 900 + (float)r.NextDouble() * 400, 0.002f, 0.04f, 0.3f);
                        NoiseBurst(b, 0, 0.005f, 0.08f, 0.5f, 0.3f, 0.05f, 55);
                    }
                    break;
                case "chain":
                    b = Buf(0.5f); for (int k = 0; k < 4; k++) Bell(b, k * 0.06f, 900 + k * 130, 0.12f, 0.3f); break;
                case "defy":
                    b = Buf(1.5f); Thump(b, 0, 80, 30, 0.3f, 1f); foreach (var f in new[] { 146.83f, 220f, 293.66f, 440f }) Tone(b, 0.05f, f, f, 0.05f, 0.7f, 0.3f, 1); Drive(b, 1.5f); break;
                default:
                    b = Buf(0.1f); Tone(b, 0, 440, 440, 0.001f, 0.05f, 0.4f); break;
            }
            return Clip(id, b);
        }
    }
}
