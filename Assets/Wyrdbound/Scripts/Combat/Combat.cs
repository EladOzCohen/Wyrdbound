using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public struct HitInfo
    {
        public float Damage;
        public HitKind Kind;
        public God God;
        public Entity Source;
        public Vector2 Dir;
        public float Knock;
        public bool Crit, NoCrit, PlayerOwned, NoProc, Silent;
        public float CritBonus;
        public string Cause;

        public static HitInfo Of(float dmg, HitKind kind, Entity src, Vector2 dir, float knock = 0f)
        {
            return new HitInfo
            {
                Damage = dmg, Kind = kind, Source = src, Dir = dir, Knock = knock,
                PlayerOwned = src is Player, Cause = src != null ? src.DisplayName : ""
            };
        }

        /// <summary>A god-effect hit owned by the player (lightning, verdict, frost...).</summary>
        public static HitInfo Effect(float dmg, God god, Vector2 dir, float knock = 0f)
        {
            return new HitInfo
            {
                Damage = dmg, Kind = HitKind.Effect, God = god, Source = Player.I, Dir = dir, Knock = knock,
                PlayerOwned = true, NoProc = true, Cause = Pal.GodName(god)
            };
        }
    }

    /// <summary>Status effects carried by any entity.</summary>
    public sealed class Status
    {
        public int Chill;
        public float ChillHold;
        public float ChillDecay;
        public float Enthralled;
        public float Charmed;
        public float VerdictT = -1f;
        public float VerdictDmg;
        public float Marked;
        public float Stun;
        public float Frozen;
        public float Poison;
        public float PoisonDps;
        public float Slowed;
        public float SlowFactor = 1f;

        public bool Disabled => Stun > 0f || Frozen > 0f;

        public float SpeedMult
        {
            get
            {
                if (Disabled) return 0f;
                float m = 1f - 0.045f * Chill;
                if (Slowed > 0f) m *= SlowFactor;
                return Mathf.Max(0.2f, m);
            }
        }

        public void Clear()
        {
            Chill = 0; ChillHold = ChillDecay = 0f;
            Enthralled = Charmed = Marked = Stun = Frozen = Poison = Slowed = 0f;
            VerdictT = -1f; VerdictDmg = 0f; SlowFactor = 1f;
        }
    }

    /// <summary>Hit queries and the damage pipeline.</summary>
    public static class Combat
    {
        static readonly List<Entity> buf = new List<Entity>();

        static bool IsTarget(Entity e, Team team, Entity except)
        {
            if (e == null || e.Dead || e == except || !e.Targetable) return false;
            return e.Team == team;
        }

        /// <summary>Apply a hit through boons, defenses and on-hit procs. Returns damage dealt.</summary>
        public static float Deal(Entity t, HitInfo h)
        {
            if (t == null || t.Dead) return 0f;
            var run = RunState.Current;
            if (h.PlayerOwned && t.Team == Team.Enemy && run != null) run.Boons.ModifyOutgoing(ref h, t);
            if (t is Player p) p.ModifyIncoming(ref h);
            float dealt = t.TakeHit(h);
            if (dealt < 0f) return 0f;
            if (h.PlayerOwned && t.Team == Team.Enemy)
            {
                if (run != null) run.Boons.AfterHit(h, t, dealt);
                if (Player.I != null && h.Kind != HitKind.Call) Player.I.AddFury(dealt * 0.11f);
            }
            return dealt;
        }

        public static int HitArc(Vector2 origin, Vector2 dir, float radius, float arcDeg, Team team, HitInfo h,
            HashSet<Entity> exclude = null, Action<Entity> onHit = null, Entity except = null)
        {
            int n = 0;
            dir = dir.Norm(Vector2.right);
            buf.Clear();
            buf.AddRange(Entity.All);
            foreach (var e in buf)
            {
                if (!IsTarget(e, team, except) || (exclude != null && exclude.Contains(e))) continue;
                Vector2 d = e.Pos - origin;
                float dist = d.magnitude;
                if (dist > radius + e.Radius) continue;
                if (arcDeg < 360f && dist > e.Radius + 0.3f && Vector2.Angle(dir, d) > arcDeg * 0.5f + Mathf.Rad2Deg * Mathf.Atan2(e.Radius, Mathf.Max(0.1f, dist))) continue;
                var hh = h;
                hh.Dir = (dist > 0.01f ? d / dist : dir);
                exclude?.Add(e);
                Deal(e, hh);
                onHit?.Invoke(e);
                n++;
            }
            return n;
        }

        public static int HitCircle(Vector2 c, float r, Team team, HitInfo h, HashSet<Entity> exclude = null, Action<Entity> onHit = null, Entity except = null)
        {
            int n = 0;
            buf.Clear();
            buf.AddRange(Entity.All);
            foreach (var e in buf)
            {
                if (!IsTarget(e, team, except) || (exclude != null && exclude.Contains(e))) continue;
                Vector2 d = e.Pos - c;
                if (d.magnitude > r + e.Radius) continue;
                var hh = h;
                if (hh.Dir == Vector2.zero || h.Knock > 0f) hh.Dir = d.Norm(Vector2.up);
                exclude?.Add(e);
                Deal(e, hh);
                onHit?.Invoke(e);
                n++;
            }
            return n;
        }

        public static int HitLine(Vector2 a, Vector2 b, float width, Team team, HitInfo h, HashSet<Entity> exclude = null, Action<Entity> onHit = null, Entity except = null)
        {
            int n = 0;
            buf.Clear();
            buf.AddRange(Entity.All);
            var dir = (b - a).Norm(Vector2.right);
            foreach (var e in buf)
            {
                if (!IsTarget(e, team, except) || (exclude != null && exclude.Contains(e))) continue;
                if (U.SegDist(e.Pos, a, b) > width * 0.5f + e.Radius) continue;
                var hh = h;
                hh.Dir = dir;
                exclude?.Add(e);
                Deal(e, hh);
                onHit?.Invoke(e);
                n++;
            }
            return n;
        }

        public static Entity Nearest(Vector2 p, Team team, float maxDist, Entity except = null, HashSet<Entity> exclude = null)
        {
            Entity best = null;
            float bd = maxDist * maxDist;
            foreach (var e in Entity.All)
            {
                if (!IsTarget(e, team, except) || (exclude != null && exclude.Contains(e))) continue;
                float d = (e.Pos - p).sqrMagnitude;
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        public static List<Entity> InRadius(Vector2 p, float r, Team team)
        {
            var list = new List<Entity>();
            foreach (var e in Entity.All)
                if (IsTarget(e, team, null) && (e.Pos - p).magnitude <= r + e.Radius) list.Add(e);
            return list;
        }

        public static List<Entity> AllOf(Team team)
        {
            var list = new List<Entity>();
            foreach (var e in Entity.All) if (IsTarget(e, team, null)) list.Add(e);
            return list;
        }

        /// <summary>Find the enemy best matching an aim direction (gentle aim assist).</summary>
        public static Vector2 Assist(Vector2 origin, Vector2 aim, float range, float cone)
        {
            Entity best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Entity.All)
            {
                if (!IsTarget(e, Team.Enemy, null)) continue;
                var d = e.Pos - origin;
                float dist = d.magnitude;
                if (dist > range || dist < 0.1f) continue;
                float ang = Vector2.Angle(aim, d);
                if (ang > cone) continue;
                float score = ang * 0.08f + dist;
                if (score < bestScore) { bestScore = score; best = e; }
            }
            if (best == null) return aim;
            var to = (best.Pos - origin).normalized;
            return Vector2.Lerp(aim, to, 0.65f).normalized;
        }
    }
}
