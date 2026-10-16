using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A thrown weapon that flies out and returns (spear) or bounds between foes (seax).</summary>
    public sealed class ThrownWeapon : MonoBehaviour
    {
        public enum Mode { Boomerang, Bounce }

        Mode mode;
        Vector2 vel;
        float speed, outDist, traveled, life;
        bool back;
        int bounces;
        HitInfo hit;
        float backDamage;
        readonly HashSet<Entity> hitOut = new HashSet<Entity>();
        readonly HashSet<Entity> hitBack = new HashSet<Entity>();
        Entity target;
        Action onReturn;
        SpriteRenderer sr;
        Color trail;
        bool spin;

        Vector2 Pos
        {
            get { var p = transform.position; return new Vector2(p.x, p.y); }
            set { transform.position = new Vector3(value.x, value.y, 0f); }
        }

        public static ThrownWeapon Throw(Mode mode, Sprite sprite, Vector2 from, Vector2 dir, float speed, float dist, HitInfo hit, float backDamage, int bounces, Color trail, bool spin, Action onReturn)
        {
            var go = new GameObject("thrown");
            go.transform.SetParent(World.Root, false);
            var t = go.AddComponent<ThrownWeapon>();
            t.mode = mode;
            t.Pos = from;
            t.vel = dir.normalized * speed;
            t.speed = speed;
            t.outDist = dist;
            t.hit = hit;
            t.backDamage = backDamage;
            t.bounces = bounces;
            t.trail = trail;
            t.spin = spin;
            t.onReturn = onReturn;
            t.sr = Mats.SR(go.transform, "sprite", sprite, Sort.Fx - 20);
            t.sr.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            t.sr.transform.rotation = Quaternion.Euler(0, 0, dir.Ang());
            if (mode == Mode.Bounce) t.target = Combat.Nearest(from + dir * 3f, Team.Enemy, 7f);
            return t;
        }

        void Update()
        {
            var p = Player.I;
            float dt = Time.deltaTime;
            life += dt;
            if (p == null || life > 5f) { Finish(); return; }

            if (!back)
            {
                if (mode == Mode.Bounce && target != null && !target.Dead)
                {
                    var to = target.Pos - Pos;
                    vel = Vector2.Lerp(vel, to.normalized * speed, 1f - Mathf.Exp(-14f * dt));
                    if (to.magnitude < 0.5f + target.Radius)
                    {
                        Strike(target, hitOut, hit.Damage);
                        bounces--;
                        target = bounces > 0 ? Combat.Nearest(Pos, Team.Enemy, 7f, null, hitOut) : null;
                        if (target == null) back = true;
                    }
                }
                Pos += vel * dt;
                traveled += speed * dt;
                if (traveled >= outDist && (mode == Mode.Boomerang || target == null)) back = true;
                if (World.Grid != null && (World.Grid.BlocksShot(Pos) || !World.InRoom(Pos, -0.5f))) back = true;
                if (mode == Mode.Boomerang) SweepHits(hitOut, hit.Damage);
            }
            else
            {
                var to = (p.Pos - Pos);
                speed = Mathf.Min(speed + 50f * dt, 34f);
                vel = to.Norm(Vector2.zero) * speed;
                Pos += vel * dt;
                SweepHits(hitBack, backDamage);
                if (to.magnitude < 0.7f) { Finish(); return; }
            }

            if (spin) sr.transform.Rotate(0, 0, 1080f * dt);
            else sr.transform.rotation = Quaternion.Euler(0, 0, (back ? -vel : vel).Ang() + (back ? 180f : 0f));
            sr.sortingOrder = Sort.Y(Pos.y) + 60;
            Vfx.Spawn(SpriteLib.Glow, Pos + new Vector2(0f, 0.55f), Vector2.zero, 0.18f, 0.6f, 0.1f, trail, trail.A(0f), true);
        }

        void SweepHits(HashSet<Entity> set, float dmg)
        {
            foreach (var e in Combat.InRadius(Pos, 0.55f, Team.Enemy))
                if (!set.Contains(e)) Strike(e, set, dmg);
        }

        void Strike(Entity e, HashSet<Entity> set, float dmg)
        {
            set.Add(e);
            var h = hit;
            h.Damage = dmg;
            h.Dir = vel.Norm(Vector2.right);
            Combat.Deal(e, h);
            Vfx.Directional(e.Center, h.Dir, 6, trail, 30f, 8f, 0.35f);
            Game.I.Audio.Play("hit", 0.6f, 1.1f);
            Game.I.Hitstop(0.025f);
        }

        void Finish()
        {
            onReturn?.Invoke();
            onReturn = null;
            Destroy(gameObject);
        }
    }
}
