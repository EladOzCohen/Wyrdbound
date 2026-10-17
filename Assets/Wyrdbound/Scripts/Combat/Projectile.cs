using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class ProjSpec
    {
        public Sprite Sprite;
        public float Radius = 0.22f, Life = 3f, Scale = 1f;
        public int Pierce;
        public Color Color = Color.white;
        public bool Additive, Spin, Lob, Deflectable = true, HitObstacles = true;
        public float Homing;
        public float LobHeight = 2.5f;
        public Color Trail = new Color(0, 0, 0, 0);
        public Action<Projectile, Entity> OnHit;
        public Action<Projectile> OnEnd;
        public bool EndOnExpireOnly;
    }

    /// <summary>Arrows, bolts, globs, thrown spears and rune-stones.</summary>
    public sealed class Projectile : MonoBehaviour
    {
        public ProjSpec Spec;
        public Vector2 Vel;
        public HitInfo Hit;
        public Team Target;
        public Entity Owner;
        public bool Ended;
        float t;
        Vector2 lobFrom, lobTo;
        float lobTime;
        int pierceLeft;
        readonly HashSet<Entity> hits = new HashSet<Entity>();
        SpriteRenderer sr, shadow;
        float trailT;

        public Vector2 Pos
        {
            get { var p = transform.position; return new Vector2(p.x, p.y); }
            set { transform.position = new Vector3(value.x, value.y, 0f); }
        }

        public static Projectile Spawn(ProjSpec spec, Vector2 pos, Vector2 vel, HitInfo hit, Team target, Entity owner)
        {
            var go = new GameObject("proj");
            go.transform.SetParent(World.Root, false);
            var p = go.AddComponent<Projectile>();
            p.Spec = spec;
            p.Vel = vel;
            p.Hit = hit;
            p.Target = target;
            p.Owner = owner;
            p.Pos = pos;
            p.pierceLeft = spec.Pierce;
            p.sr = Mats.SR(go.transform, "sprite", spec.Sprite, Sort.Fx - 20, spec.Color, spec.Additive);
            p.sr.sharedMaterial = spec.Additive ? Mats.AdditiveTop : Mats.SpriteTop;
            p.sr.transform.localScale = new Vector3(spec.Scale, spec.Scale, 1f);
            p.shadow = Mats.SR(go.transform, "shadow", SpriteLib.Shadow, Sort.Shadow, new Color(1, 1, 1, 0.5f));
            p.shadow.transform.localScale = new Vector3(spec.Scale * 0.5f, spec.Scale * 0.5f, 1f);
            if (spec.Additive || spec.Trail.a > 0f) Lights.Attach(go.transform, new Vector2(0f, 0.55f), 1.7f * spec.Scale, spec.Additive ? spec.Color : spec.Trail, 0.65f);
            p.Orient();
            return p;
        }

        public static Projectile SpawnLob(ProjSpec spec, Vector2 from, Vector2 to, float time, HitInfo hit, Team target, Entity owner)
        {
            spec.Lob = true;
            var p = Spawn(spec, from, Vector2.zero, hit, target, owner);
            p.lobFrom = from;
            p.lobTo = to;
            p.lobTime = Mathf.Max(0.1f, time);
            return p;
        }

        void Orient()
        {
            if (Spec.Spin) return;
            if (Vel.sqrMagnitude > 0.001f) sr.transform.rotation = Quaternion.Euler(0, 0, Vel.Ang());
        }

        public void End(bool landed)
        {
            if (Ended) return;
            Ended = true;
            Spec.OnEnd?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Reflect toward a new team (player deflects).</summary>
        public void Deflect(Vector2 dir, Team newTarget)
        {
            Vel = dir.normalized * Vel.magnitude * 1.2f;
            Target = newTarget;
            Hit.PlayerOwned = newTarget == Team.Enemy;
            Hit.Source = newTarget == Team.Enemy ? Player.I : Hit.Source;
            hits.Clear();
            t = 0f;
        }

        void Update()
        {
            if (Ended) return;
            float dt = Time.deltaTime;
            t += dt;

            if (Spec.Lob)
            {
                float k = Mathf.Clamp01(t / lobTime);
                Pos = Vector2.Lerp(lobFrom, lobTo, k);
                float h = Spec.LobHeight * 4f * k * (1f - k);
                sr.transform.localPosition = new Vector3(0f, h, 0f);
                if (Spec.Spin) sr.transform.Rotate(0, 0, 540f * dt);
                shadow.transform.localScale = Vector3.one * Spec.Scale * Mathf.Lerp(0.3f, 0.6f, 1f - h / Mathf.Max(0.1f, Spec.LobHeight));
                sr.sortingOrder = Sort.Y(Pos.y) + 50;
                if (k >= 1f) End(true);
                return;
            }

            if (t >= Spec.Life) { End(false); return; }

            if (Spec.Homing > 0f)
            {
                var tgt = Combat.Nearest(Pos, Target, 9f, null, hits);
                if (tgt != null)
                {
                    var want = (tgt.Pos - Pos).normalized * Vel.magnitude;
                    Vel = Vector2.Lerp(Vel, want, 1f - Mathf.Exp(-Spec.Homing * dt));
                }
            }

            Pos += Vel * dt;
            Orient();
            if (Spec.Spin) sr.transform.Rotate(0, 0, 900f * dt);
            sr.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            sr.sortingOrder = Sort.Y(Pos.y) + 50;

            if (Spec.Trail.a > 0f)
            {
                trailT -= dt;
                if (trailT <= 0f)
                {
                    trailT = 0.02f;
                    Vfx.Spawn(SpriteLib.Glow, Pos + new Vector2(0, 0.55f), Vector2.zero, 0.22f, Spec.Radius * 3f, 0.05f, Spec.Trail, Spec.Trail.A(0f), true);
                }
            }

            if (!World.InRoom(Pos, 2f)) { End(false); return; }
            if (Spec.HitObstacles && World.Grid != null && World.Grid.BlocksShot(Pos)) { End(false); return; }

            foreach (var e in Entity.All.ToArray())
            {
                if (e == null || e.Dead || e.Team != Target || !e.Targetable || hits.Contains(e) || e == Owner) continue;
                if ((e.Pos - Pos).sqrMagnitude > (Spec.Radius + e.Radius) * (Spec.Radius + e.Radius)) continue;
                hits.Add(e);
                var h = Hit;
                h.Dir = Vel.Norm(Vector2.right);
                Combat.Deal(e, h);
                Spec.OnHit?.Invoke(this, e);
                if (pierceLeft-- <= 0) { End(false); return; }
            }
        }
    }
}
