using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Small math / color helpers used everywhere.</summary>
    public static class U
    {
        public static Vector3 V3(this Vector2 v, float z = 0f) => new Vector3(v.x, v.y, z);
        public static Vector2 XY(this Vector3 v) => new Vector2(v.x, v.y);
        public static float Ang(this Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

        public static Vector2 Dir(float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
        }

        public static Vector2 Rot(this Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public static Vector2 Norm(this Vector2 v, Vector2 fallback)
        {
            float m = v.magnitude;
            return m > 1e-5f ? v / m : fallback;
        }

        public static Color A(this Color c, float a) { c.a = a; return c; }

        public static Color Lit(this Color c, float k) =>
            new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

        public static Color Mix(this Color a, Color b, float t) => Color.Lerp(a, b, t);

        public static Color Hex(string h)
        {
            if (!h.StartsWith("#")) h = "#" + h;
            return ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
        public static string Tint(string text, Color c) => "<color=#" + Hex(c) + ">" + text + "</color>";

        public static float Damp(float a, float b, float lambda, float dt) => Mathf.Lerp(a, b, 1f - Mathf.Exp(-lambda * dt));
        public static Vector2 Damp(Vector2 a, Vector2 b, float lambda, float dt) => Vector2.Lerp(a, b, 1f - Mathf.Exp(-lambda * dt));

        public static float OutCubic(float t) { t = Mathf.Clamp01(t); float u = 1f - t; return 1f - u * u * u; }
        public static float InCubic(float t) { t = Mathf.Clamp01(t); return t * t * t; }
        public static float OutQuad(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
        public static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float l2 = ab.sqrMagnitude;
            float t = l2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2) : 0f;
            return Vector2.Distance(p, a + ab * t);
        }

        public static float Approach(float cur, float target, float delta) =>
            cur < target ? Mathf.Min(cur + delta, target) : Mathf.Max(cur - delta, target);

        public static float DeltaAngle(Vector2 a, Vector2 b) => Mathf.Abs(Mathf.DeltaAngle(a.Ang(), b.Ang()));

        public static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
        public static string Num(float v) => Mathf.RoundToInt(v).ToString();

        public static string Clock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }

    /// <summary>Sorting orders for the 3/4 top-down view. Characters sort by world y.</summary>
    public static class Sort
    {
        public const int Void = -32500, Mist = -32400, Floor = -32000, FloorFx = -31500, Telegraph = -31000, Shadow = -30500;
        public const int Fx = 24000, FxHigh = 25000, Fog = 28000, Text = 30000;
        public static int Y(float y) => Mathf.RoundToInt(-y * 100f);
    }

    /// <summary>Deterministic random source (runs are seeded so rooms are reproducible).</summary>
    public sealed class Rng
    {
        readonly System.Random r;
        public static readonly Rng Fx = new Rng(Environment.TickCount);

        public Rng(int seed) { r = new System.Random(seed); }

        public float Value => (float)r.NextDouble();
        public int Next() => r.Next();
        public float Range(float a, float b) => a + (b - a) * Value;
        public int Range(int a, int bExclusive) => bExclusive <= a ? a : r.Next(a, bExclusive);
        public bool Chance(float p) => Value < p;
        public float Sign => Value < 0.5f ? -1f : 1f;

        public Vector2 InCircle()
        {
            float a = Value * Mathf.PI * 2f, d = Mathf.Sqrt(Value);
            return new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
        }

        public Vector2 OnCircle()
        {
            float a = Value * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        public T Pick<T>(IList<T> l) => l[r.Next(l.Count)];

        public T Weighted<T>(IList<T> items, Func<T, float> weight)
        {
            float total = 0f;
            foreach (var it in items) total += Mathf.Max(0f, weight(it));
            if (total <= 0f) return items[r.Next(items.Count)];
            float x = Value * total;
            foreach (var it in items)
            {
                x -= Mathf.Max(0f, weight(it));
                if (x <= 0f) return it;
            }
            return items[items.Count - 1];
        }

        public void Shuffle<T>(IList<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--)
            {
                int j = r.Next(i + 1);
                (l[i], l[j]) = (l[j], l[i]);
            }
        }
    }
}
