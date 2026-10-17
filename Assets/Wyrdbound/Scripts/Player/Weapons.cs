using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Base for Narfi's Hel-forged arms.</summary>
    public abstract class Weapon
    {
        protected Player P;
        public abstract string Id { get; }
        public float MoveMult = 1f;
        public bool Busy;

        public virtual void Init(Player p) { P = p; }
        public abstract void Tick(float dt, GameInput inp, bool allowed);

        public virtual void Cancel()
        {
            Busy = false;
            MoveMult = 1f;
            if (P != null)
            {
                P.WeaponSwinging = false;
                P.WeaponExtend = 0f;
            }
        }

        protected float Speed => Mathf.Max(0.3f, P.S.AttackSpeed);

        protected HitInfo MakeHit(float dmg, HitKind kind, float knock) => HitInfo.Of(dmg, kind, P, Vector2.zero, knock);

        protected Color FxColor(HitKind kind)
        {
            var run = RunState.Current;
            God g = God.None;
            if (run != null) g = run.Boons.SlotGod(kind == HitKind.Special ? BoonSlot.Special : BoonSlot.Attack);
            return g == God.None ? new Color(0.82f, 1f, 0.92f) : Pal.God(g);
        }

        protected void Impact(int hits, float shake, float stop, Vector2 dir, bool heavy)
        {
            if (hits <= 0) return;
            Game.I.Hitstop(stop);
            Game.I.Cam.Shake(shake);
            Game.I.Cam.Kick(dir, heavy ? 0.3f : 0.12f);
            Game.I.Audio.Play(heavy ? "hit_heavy" : "hit", 0.8f);
        }

        protected virtual float TargetMult(Entity e, Vector2 dir) => 1f;

        /// <summary>Resolve a melee strike. shape: 0 arc, 1 thrust line, 2 full circle.</summary>
        protected int Strike(Vector2 origin, Vector2 dir, int shape, float range, float arc, float width, HitInfo h, HashSet<Entity> hit)
        {
            int n = 0;
            Color c = FxColor(h.Kind);
            foreach (var e in Combat.AllOf(Team.Enemy))
            {
                if (hit.Contains(e)) continue;
                Vector2 d = e.Pos - origin;
                float dist = d.magnitude;
                bool inside;
                if (shape == 2) inside = dist <= range + e.Radius;
                else if (shape == 1)
                {
                    float along = Vector2.Dot(d, dir);
                    inside = along >= -0.4f && along <= range + e.Radius && Mathf.Abs(Vector2.Dot(d, dir.Rot(90f))) <= width * 0.5f + e.Radius;
                }
                else
                {
                    inside = dist <= range + e.Radius &&
                             (dist < e.Radius + 0.45f || Vector2.Angle(dir, d) <= arc * 0.5f + Mathf.Rad2Deg * Mathf.Atan2(e.Radius, Mathf.Max(0.1f, dist)));
                }
                if (!inside) continue;
                hit.Add(e);
                var hh = h;
                hh.Dir = d.Norm(dir);
                hh.Damage *= TargetMult(e, dir);
                Combat.Deal(e, hh);
                Vfx.Directional(e.Center, hh.Dir, 5, c, 30f, 9f, 0.35f);
                n++;
            }
            return n;
        }
    }

    public sealed class Step
    {
        public float Windup = 0.1f, Active = 0.08f, Recover = 0.2f, Damage = 20f, Range = 2.2f, Arc = 140f, Width = 1f, Lunge = 0.3f, Knock = 3f;
        public float Shake = 0.08f, Stop = 0.035f, SwingFrom = 70f, SwingTo = -70f, Extend;
        public int Shape, SlashArc = 150;
        public bool Flip, Heavy;
        public string Sfx = "swing";
    }

    /// <summary>Combo-driven melee weapon with windup / strike / recovery and input buffering.</summary>
    public abstract class MeleeWeapon : Weapon
    {
        protected Step[] Combo;
        protected Step SpecialStep;
        protected float SpecialCooldown;
        protected int idx;
        protected int phase;
        protected float t;
        protected bool queued, queuedSpecial;
        protected float comboWindow;
        protected Step cur;
        protected HitKind kind;
        protected Vector2 dir;
        protected float sideSign = 1f;
        protected readonly HashSet<Entity> hits = new HashSet<Entity>();
        protected float specialCd;
        bool dashStrike;

        protected bool Idle => phase == 0;

        public override void Tick(float dt, GameInput inp, bool allowed)
        {
            specialCd -= dt;
            comboWindow -= dt;
            if (phase == 0 && comboWindow <= 0f) idx = 0;
            if (allowed)
            {
                if (inp.AttackDown) { if (phase == 0) Begin(Combo[idx], HitKind.Attack); else queued = true; }
                if (inp.SpecialDown) { if (phase == 0) TrySpecial(); else queuedSpecial = true; }
            }
            if (phase == 0) return;
            t += dt * Speed;
            float baseAng = dir.Ang();
            switch (phase)
            {
                case 1:
                    {
                        float k = U.OutCubic(t / Mathf.Max(0.01f, cur.Windup));
                        P.WeaponSwinging = true;
                        P.WeaponAngle = baseAng + sideSign * cur.SwingFrom * (1f + 0.15f * k);
                        P.WeaponExtend = -0.12f * k;
                        MoveMult = 0.3f;
                        if (t >= cur.Windup) { DoStrike(); phase = 2; t = 0f; }
                        break;
                    }
                case 2:
                    {
                        float k = U.OutCubic(t / Mathf.Max(0.01f, cur.Active));
                        P.WeaponAngle = baseAng + sideSign * Mathf.Lerp(cur.SwingFrom, cur.SwingTo, k);
                        P.WeaponExtend = cur.Extend * Mathf.Sin(k * Mathf.PI);
                        MoveMult = 0.2f;
                        if (t >= cur.Active) { phase = 3; t = 0f; }
                        break;
                    }
                case 3:
                    {
                        P.WeaponAngle = baseAng + sideSign * cur.SwingTo;
                        P.WeaponExtend = 0f;
                        MoveMult = 0.6f;
                        if (queuedSpecial && t >= cur.Recover * 0.4f) { End(false); TrySpecial(); return; }
                        if (queued && kind == HitKind.Attack && t >= cur.Recover * 0.45f) { End(true); Begin(Combo[idx], HitKind.Attack); return; }
                        if (t >= cur.Recover) End(kind == HitKind.Attack);
                        break;
                    }
            }
        }

        protected void Begin(Step s, HitKind k)
        {
            cur = s;
            kind = k;
            phase = 1;
            t = 0f;
            queued = queuedSpecial = false;
            Busy = true;
            hits.Clear();
            dir = P.AttackDir();
            P.Facing = dir;
            sideSign = (s.Flip ? -1f : 1f) * (dir.x < 0f ? -1f : 1f);
            dashStrike = k == HitKind.Attack && P.SinceDash < 0.2f;
            P.OnSwingBegin(s, Speed);
            OnBegin(s, k);
        }

        protected void End(bool advanceCombo)
        {
            phase = 0;
            Busy = false;
            MoveMult = 1f;
            P.WeaponSwinging = false;
            P.WeaponExtend = 0f;
            if (advanceCombo) { idx = (idx + 1) % Combo.Length; comboWindow = 0.32f; }
            else idx = 0;
            OnEnd();
        }

        void DoStrike()
        {
            float dmg = cur.Damage * (dashStrike ? 1.5f : 1f);
            P.Lunge(cur.Lunge * (dashStrike ? 1.6f : 1f), 0.09f);
            var h = MakeHit(dmg, kind, cur.Knock);
            Vector2 origin = P.Pos + new Vector2(0f, 0.15f);
            int n = Strike(origin, dir, cur.Shape, cur.Range, cur.Arc, cur.Width, h, hits);
            Color c = FxColor(kind);
            Vector2 fx = P.Pos + new Vector2(0f, 0.45f);
            if (cur.Shape == 1)
            {
                Vfx.Seg(fx, fx + dir * cur.Range, cur.Width * 0.55f, c.A(0.8f), 0.12f);
                Vfx.Seg(fx, fx + dir * cur.Range * 1.05f, cur.Width * 0.18f, Color.white, 0.09f);
                Vfx.Directional(fx + dir * cur.Range, dir, 4, c, 15f, 10f, 0.4f);
            }
            else if (cur.Shape == 2)
            {
                Vfx.Slash(fx, dir.Ang(), cur.Range * 0.95f, 360, c, 0.22f, sideSign < 0f);
                Vfx.Ring(P.Pos, cur.Range, c.A(0.6f), 0.25f, 0.5f);
            }
            else
            {
                bool cw = (cur.SwingTo - cur.SwingFrom) * sideSign < 0f;
                float mid = dir.Ang() + (cur.SwingFrom + cur.SwingTo) * 0.5f * sideSign;
                Vfx.Slash(fx, mid, cur.Range * 0.95f, cur.SlashArc, c, cur.Heavy ? 0.22f : 0.16f, cw);
            }
            Game.I.Audio.Play(cur.Heavy ? "swing_heavy" : cur.Sfx, cur.Heavy ? 0.6f : 0.45f, cur.Heavy ? 0.85f : 1.05f);
            Impact(n, cur.Shake, cur.Stop, dir, cur.Heavy);
            OnStrike(cur, kind, n);
        }

        protected virtual void TrySpecial()
        {
            if (specialCd > 0f || SpecialStep == null) return;
            specialCd = SpecialCooldown;
            Begin(SpecialStep, HitKind.Special);
        }

        protected virtual void OnBegin(Step s, HitKind k) { }
        protected virtual void OnStrike(Step s, HitKind k, int n) { }
        protected virtual void OnEnd() { }

        public override void Cancel()
        {
            base.Cancel();
            phase = 0;
            queued = queuedSpecial = false;
            idx = 0;
        }
    }

    // ---------------------------------------------------------------------- the four arms

    /// <summary>Bitter Edge, the Bone-Bitten Axe.</summary>
    public sealed class AxeWeapon : MeleeWeapon
    {
        public override string Id => "axe";

        public override void Init(Player p)
        {
            base.Init(p);
            Combo = new[]
            {
                new Step { Windup = 0.1f, Active = 0.08f, Recover = 0.18f, Damage = 22f, Range = 2.3f, Arc = 150f, Lunge = 0.35f, Knock = 3.5f, SwingFrom = 80f, SwingTo = -80f },
                new Step { Windup = 0.1f, Active = 0.08f, Recover = 0.2f, Damage = 26f, Range = 2.3f, Arc = 150f, Lunge = 0.35f, Knock = 3.5f, SwingFrom = 80f, SwingTo = -80f, Flip = true },
                new Step { Windup = 0.22f, Active = 0.1f, Recover = 0.34f, Damage = 45f, Range = 2.7f, Arc = 110f, Lunge = 0.6f, Knock = 9f, SwingFrom = 110f, SwingTo = -30f, SlashArc = 120, Heavy = true, Shake = 0.3f, Stop = 0.07f },
            };
            SpecialStep = new Step { Windup = 0.14f, Active = 0.14f, Recover = 0.36f, Damage = 32f, Range = 2.7f, Shape = 2, Lunge = 0f, Knock = 7f, SwingFrom = 0f, SwingTo = -360f, Heavy = true, Shake = 0.22f, Stop = 0.05f };
            SpecialCooldown = 0f;
        }

        protected override void OnStrike(Step s, HitKind k, int n)
        {
            if (s != Combo[2]) return;
            var tip = P.Pos + dir * 2.1f;
            Vfx.Ring(tip, 1.4f, FxColor(k), 0.3f);
            Vfx.Dust(tip, 6, World.Biome.Fog, 0.8f);
            Game.I.Audio.Play("slam", 0.5f, 1.3f);
            int m = Strike(tip, dir, 2, 1.4f, 360f, 1f, MakeHit(16f, HitKind.Attack, 6f), hits);
            if (m > 0) Game.I.Cam.Shake(0.1f);
        }
    }

    /// <summary>Gjallr, the Spear of the Drowned.</summary>
    public sealed class SpearWeapon : MeleeWeapon
    {
        public override string Id => "spear";
        float holdT;
        bool thrown;
        Step spin;

        public override void Init(Player p)
        {
            base.Init(p);
            Combo = new[]
            {
                new Step { Shape = 1, Windup = 0.08f, Active = 0.08f, Recover = 0.18f, Damage = 18f, Range = 3.4f, Width = 0.95f, Lunge = 0.25f, Knock = 2.5f, SwingFrom = 0f, SwingTo = 0f, Extend = 0.9f },
                new Step { Shape = 1, Windup = 0.08f, Active = 0.08f, Recover = 0.2f, Damage = 18f, Range = 3.4f, Width = 0.95f, Lunge = 0.25f, Knock = 2.5f, SwingFrom = 4f, SwingTo = 4f, Extend = 0.9f },
                new Step { Shape = 1, Windup = 0.15f, Active = 0.1f, Recover = 0.3f, Damage = 32f, Range = 3.9f, Width = 1.1f, Lunge = 0.55f, Knock = 7f, SwingFrom = 0f, SwingTo = 0f, Extend = 1.2f, Heavy = true, Shake = 0.2f, Stop = 0.05f },
            };
            spin = new Step { Shape = 2, Windup = 0.06f, Active = 0.18f, Recover = 0.3f, Damage = 40f, Range = 3.1f, Knock = 8f, SwingFrom = 0f, SwingTo = -360f, Heavy = true, Shake = 0.25f, Stop = 0.05f };
        }

        public override void Tick(float dt, GameInput inp, bool allowed)
        {
            if (inp.Attack && allowed) holdT += dt; else holdT = 0f;
            if (allowed && Idle && holdT > 0.42f)
            {
                holdT = -99f;
                Game.I.Audio.Play("telegraph", 0.3f, 1.8f);
                Begin(spin, HitKind.Attack);
            }
            base.Tick(dt, inp, allowed);
            P.WeaponHidden = thrown;
        }

        protected override void TrySpecial()
        {
            if (thrown) return;
            thrown = true;
            var d = P.AttackDir();
            P.Facing = d;
            Game.I.Audio.Play("swing_heavy", 0.5f, 1.2f);
            var h = MakeHit(35f, HitKind.Special, 4f);
            ThrownWeapon.Throw(ThrownWeapon.Mode.Boomerang, SpriteLib.Weapon("spear"), P.Pos + d * 0.5f, d, 24f, 10f, h, 25f, 0, FxColor(HitKind.Special).A(0.6f), false, () => thrown = false);
        }

        public override void Cancel()
        {
            base.Cancel();
            holdT = 0f;
        }
    }

    /// <summary>Fang and Claw, the Twin Seaxes.</summary>
    public sealed class SeaxWeapon : MeleeWeapon
    {
        public override string Id => "seax";
        bool outFlying;

        public override void Init(Player p)
        {
            base.Init(p);
            Combo = new[]
            {
                new Step { Windup = 0.05f, Active = 0.06f, Recover = 0.09f, Damage = 9f, Range = 1.9f, Arc = 110f, Lunge = 0.25f, Knock = 1.5f, SwingFrom = 60f, SwingTo = -60f, SlashArc = 120, Shake = 0.04f, Stop = 0.02f },
                new Step { Windup = 0.05f, Active = 0.06f, Recover = 0.09f, Damage = 9f, Range = 1.9f, Arc = 110f, Lunge = 0.25f, Knock = 1.5f, SwingFrom = 60f, SwingTo = -60f, Flip = true, SlashArc = 120, Shake = 0.04f, Stop = 0.02f },
                new Step { Windup = 0.05f, Active = 0.06f, Recover = 0.09f, Damage = 10f, Range = 1.9f, Arc = 110f, Lunge = 0.25f, Knock = 1.5f, SwingFrom = 60f, SwingTo = -60f, SlashArc = 120, Shake = 0.04f, Stop = 0.02f },
                new Step { Windup = 0.05f, Active = 0.06f, Recover = 0.1f, Damage = 11f, Range = 1.9f, Arc = 110f, Lunge = 0.25f, Knock = 1.5f, SwingFrom = 60f, SwingTo = -60f, Flip = true, SlashArc = 120, Shake = 0.04f, Stop = 0.02f },
                new Step { Windup = 0.12f, Active = 0.08f, Recover = 0.26f, Damage = 24f, Range = 2.2f, Arc = 90f, Lunge = 0.75f, Knock = 6f, SwingFrom = 20f, SwingTo = -20f, SlashArc = 90, Heavy = true, Shake = 0.18f, Stop = 0.05f },
            };
            SpecialCooldown = 0.5f;
        }

        protected override float TargetMult(Entity e, Vector2 d) => Vector2.Dot(e.Facing.Norm(Vector2.right), d) > 0.3f ? 1.5f : 1f;

        protected override void TrySpecial()
        {
            if (outFlying || specialCd > 0f) return;
            specialCd = SpecialCooldown;
            outFlying = true;
            var d = P.AttackDir();
            P.Facing = d;
            Game.I.Audio.Play("swing", 0.5f, 1.4f);
            var h = MakeHit(16f, HitKind.Special, 2f);
            ThrownWeapon.Throw(ThrownWeapon.Mode.Bounce, SpriteLib.Weapon("seax"), P.Pos + d * 0.4f, d, 22f, 7f, h, 8f, 3, FxColor(HitKind.Special).A(0.6f), true, () => outFlying = false);
        }
    }

    /// <summary>Ullr's Yew, the Winter Bow.</summary>
    public sealed class BowWeapon : Weapon
    {
        public override string Id => "bow";
        bool drawing;
        float drawT, cooldown, specialCd;
        bool pinged;
        const float Full = 0.45f, PerfectEnd = 0.62f;

        public override void Tick(float dt, GameInput inp, bool allowed)
        {
            cooldown -= dt;
            specialCd -= dt;
            var d = P.AttackDir();
            if (allowed && !drawing && inp.AttackDown && cooldown <= 0f)
            {
                drawing = true;
                drawT = 0f;
                pinged = false;
                Busy = true;
            }
            if (drawing)
            {
                drawT += dt * Speed;
                MoveMult = 0.45f;
                P.WeaponSwinging = true;
                P.WeaponAngle = d.Ang();
                P.WeaponExtend = -0.1f * Mathf.Clamp01(drawT / Full);
                P.Facing = d;
                if (!pinged && drawT >= Full)
                {
                    pinged = true;
                    Game.I.Audio.Play("ui_move", 0.7f, 1.4f);
                    Vfx.Flash(P.Center + d * 0.6f, 0.5f, Pal.Frost, 0.12f);
                }
                if (drawT >= Full && drawT <= PerfectEnd)
                    Vfx.Spawn(SpriteLib.Glow, P.Center + d * 0.7f, Vector2.zero, 0.05f, 0.6f, 0.6f, Pal.Frost.A(0.6f), Pal.Frost.A(0f), true);
                if (!inp.Attack || !allowed) Release(d, allowed);
                return;
            }
            if (allowed && inp.SpecialDown && specialCd <= 0f) Volley(d);
            MoveMult = 1f;
            if (cooldown <= -0.15f) { P.WeaponSwinging = false; P.WeaponExtend = 0f; Busy = false; }
        }

        void Release(Vector2 d, bool fire)
        {
            drawing = false;
            Busy = false;
            MoveMult = 1f;
            cooldown = 0.16f;
            if (!fire) return;
            bool perfect = drawT >= Full && drawT <= PerfectEnd;
            float dmg = drawT < 0.12f ? 10f : Mathf.Lerp(14f, 34f, Mathf.Clamp01(drawT / Full));
            if (perfect) dmg = 48f;
            Color c = FxColor(HitKind.Attack);
            var spec = new ProjSpec
            {
                Sprite = SpriteLib.Arrow(U.Hex("8a6a44"), perfect ? Pal.Frost : U.Hex("d9dde4")), Radius = 0.3f, Life = 0.6f, Scale = perfect ? 1.25f : 1f,
                Pierce = perfect ? 2 : 0, Trail = perfect ? Pal.Frost.A(0.6f) : c.A(0.3f)
            };
            var h = MakeHit(dmg, HitKind.Attack, perfect ? 5f : 2f);
            spec.OnHit = (pr, e) =>
            {
                Vfx.Directional(e.Center, pr.Vel, 5, c, 25f, 8f, 0.35f);
                Game.I.Audio.Play("hit", 0.6f, 1.2f);
                Game.I.Hitstop(perfect ? 0.05f : 0.02f);
                if (perfect) Game.I.Cam.Shake(0.15f);
            };
            Projectile.Spawn(spec, P.Pos + d * 0.5f, d * 26f, h, Team.Enemy, P);
            Game.I.Audio.Play("arrow", 0.6f, perfect ? 0.8f : 1.1f);
            if (perfect)
            {
                Vfx.Flash(P.Center + d * 0.8f, 0.9f, Pal.Frost, 0.15f);
                Vfx.Text(P.Pos + new Vector2(0f, P.VisualHeight + 0.4f), "POWER SHOT", Pal.Frost, 0.6f);
            }
            P.Lunge(0f, 0f);
            P.WeaponExtend = 0f;
        }

        void Volley(Vector2 d)
        {
            specialCd = 0.75f;
            Busy = true;
            cooldown = 0.25f;
            P.WeaponSwinging = true;
            P.WeaponAngle = d.Ang();
            P.Facing = d;
            Color c = FxColor(HitKind.Special);
            for (int i = 0; i < 5; i++)
            {
                var dd = d.Rot(-25f + i * 12.5f);
                var spec = new ProjSpec { Sprite = SpriteLib.Arrow(U.Hex("8a6a44"), c), Radius = 0.28f, Life = 0.45f, Trail = c.A(0.35f) };
                spec.OnHit = (pr, e) => Game.I.Audio.Play("hit", 0.4f, 1.3f);
                Projectile.Spawn(spec, P.Pos + dd * 0.5f, dd * 24f, MakeHit(14f, HitKind.Special, 3f), Team.Enemy, P);
            }
            Game.I.Audio.Play("arrow", 0.8f, 0.7f);
            Vfx.Flash(P.Center + d * 0.6f, 0.7f, c, 0.12f);
        }

        public override void Cancel()
        {
            base.Cancel();
            drawing = false;
        }
    }
}
