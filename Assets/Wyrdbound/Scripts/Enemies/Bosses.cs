using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A Warden of Helheim: phases, enrage, and a bar across the top of the screen.</summary>
    public abstract class Boss : Enemy
    {
        public static Boss Current;
        protected int phase = 1;
        protected bool enraged;
        public override int Phase => phase;
        public override bool KnockImmune => true;
        public string Title => Def.Title;

        protected override void Setup(EnemyDef def, Vector2 pos, bool elite, float hpMult, float dmgMult, float spawnDelay)
        {
            foreach (var a in def.Attacks) if (a.Kind == AtkKind.Ring) a.Repeat = 1;
            base.Setup(def, pos, false, hpMult, dmgMult, spawnDelay);
            float bossMult = RunState.Current != null ? RunState.Current.Stats.BossHpMult : 1f;
            MaxHP = HP = HP * bossMult;
            if (bossMult > 1f) attackSpeed = 1.12f;
            Current = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Current == this) Current = null;
        }

        protected override void OnHurt(HitInfo hit, float dmg)
        {
            Vfx.Burst(Center, 3, Def.Blood, 6f, 0.3f, 0.3f);
            if (!enraged && HP < MaxHP * 0.5f && HP > 0f) Enrage();
        }

        protected virtual void Enrage()
        {
            enraged = true;
            phase = Mathf.Max(phase, 2);
            speedMult *= 1.18f;
            attackSpeed *= 1.15f;
            Game.I.Audio.Play("roar", 0.8f);
            Game.I.Cam.Shake(0.4f);
            Vfx.Ring(Pos, 4f, Pal.Danger, 0.6f);
            Flash(1f, Pal.Danger);
        }

        protected override void OnDied(HitInfo hit)
        {
            Game.I.SlowMo(0.2f, 1.2f);
            Game.I.Cam.Shake(0.7f);
            Game.I.Cam.Punch(1.2f);
            Vfx.Ring(Pos, 6f, Def.Blood, 0.9f);
            Vfx.Burst(Center, 40, Def.Blood, 14f, 0.8f, 0.6f);
            Game.I.Audio.Play("roar", 0.7f, 0.7f);
            foreach (var e in Combat.AllOf(Team.Enemy)) if (e != this) e.Die(HitInfo.Of(0f, HitKind.Effect, null, Vector2.zero));
            RunDirector.I?.OnBossDefeated(this);
        }

        /// <summary>A telegraphed line strike across the arena.</summary>
        protected void LineStrike(Vector2 from, Vector2 dir, float len, float width, float delay, float dmg, Color c, string sfx = "slam")
        {
            var me = this;
            Vfx.TeleLine(from, dir, len, width, delay, Pal.Danger);
            Game.I.After(delay, () =>
            {
                if (me == null || me.Dead) return;
                var h = EHit(cur);
                h.Damage = dmg * DmgMult;
                Combat.HitLine(from, from + dir * len, width, Team.Player, h);
                Vfx.Seg(from, from + dir * len, width * 0.9f, c, 0.25f);
                Vfx.Seg(from, from + dir * len, width * 0.3f, Color.white, 0.18f);
                Game.I.Audio.Play(sfx, 0.4f, 1.2f);
                Game.I.Cam.Shake(0.12f);
            });
        }

        /// <summary>A telegraphed circle eruption.</summary>
        protected void Eruption(Vector2 at, float r, float delay, float dmg, Color c, string sfx = "slam")
        {
            var me = this;
            if (World.Grid != null) at = World.Grid.NearestFloor(at, 0.3f);
            Vfx.TeleCircle(at, r, delay, Pal.Danger);
            Game.I.After(delay, () =>
            {
                if (me == null || me.Dead) return;
                var h = EHit(cur);
                h.Damage = dmg * DmgMult;
                Combat.HitCircle(at, r, Team.Player, h);
                Vfx.Ring(at, r, c, 0.35f);
                Vfx.Burst(at, 12, c, 8f, 0.4f, 0.4f);
                Game.I.Audio.Play(sfx, 0.45f, 1.1f);
            });
        }
    }

    /// <summary>Váli, the Wolf-Brother. Narfi's killer, and his brother.</summary>
    public sealed class ValiBoss : Boss
    {
        protected override void OnCustom(AttackDef a)
        {
            var p = Player.I;
            if (p == null) return;
            if (a.Tag == "rush")
            {
                Game.I.Audio.Play("howl", 0.6f, 1.2f);
                var b = World.Grid.Bounds;
                int n = phase >= 2 ? 4 : 3;
                for (int i = 0; i < n; i++)
                {
                    bool horiz = (i % 2 == 0);
                    float off = (i - (n - 1) * 0.5f) * 2.6f;
                    Vector2 from = horiz ? new Vector2(b.xMin, p.Pos.y + off) : new Vector2(p.Pos.x + off, b.yMin);
                    Vector2 dir = horiz ? Vector2.right : Vector2.up;
                    float delay = 0.9f + i * 0.25f;
                    LineStrike(from, dir, horiz ? b.width : b.height, 1.3f, delay, a.Damage, U.Hex("3a3f4a"), "dash");
                    var f2 = from;
                    Game.I.After(delay, () =>
                    {
                        for (int k = 0; k < 6; k++)
                            Vfx.Spawn(CharArt.Get("helhound"), f2 + dir * k * 0.5f, dir * 40f, 0.5f, 1.2f, 1.2f, new Color(0.2f, 0.2f, 0.25f, 0.8f), new Color(0, 0, 0, 0), false, 0f, 0f, 0f, Sort.FxHigh, dir.x < 0 ? -1f : 1f);
                    });
                }
            }
            else if (a.Tag == "howl")
            {
                Game.I.Audio.Play("howl", 0.9f);
                Game.I.Cam.Shake(0.3f);
                for (int i = 0; i < 4; i++) RunDirector.SpawnMinion("bone_rat", Pos + U.Dir(i * 90f + 45f) * 2.5f);
                var h = EHit(a);
                int ring = 16;
                for (int i = 0; i < ring; i++)
                {
                    var d = U.Dir(i * 360f / ring);
                    Projectile.Spawn(Spec(new AttackDef { Proj = "orb", ProjColor = Pal.Danger, Range = 14f, Speed = 7f }), Pos + d, d * 7f, h, Team.Player, this);
                }
            }
        }

        protected override void Enrage()
        {
            base.Enrage();
            Game.I.UI.Banner("VÁLI HOWLS", "\"...brother...\"", Pal.Danger, 1.6f);
            Game.I.Audio.Play("howl", 1f, 0.9f);
        }
    }

    /// <summary>A body segment of Níðhöggr; blows land on the dragon itself.</summary>
    public sealed class BossSegment : Entity
    {
        public Entity Owner;
        float contactCd;
        static Material segMat, spineMat;
        MeshRenderer ball, spine;
        MaterialPropertyBlock mpb3;

        public override bool Targetable => Owner != null && Owner.Targetable && gameObject.activeSelf;
        public override bool Invulnerable => Owner == null || Owner.Invulnerable;
        public override bool KnockImmune => true;

        public static BossSegment Make(Entity owner, Sprite s, float radius, float scale)
        {
            var go = new GameObject("segment");
            go.transform.SetParent(World.Root, false);
            var seg = go.AddComponent<BossSegment>();
            seg.Owner = owner;
            seg.Team = Team.Enemy;
            seg.Radius = radius;
            seg.Mass = 99f;
            seg.Id = owner.Id;
            seg.DisplayName = owner.DisplayName;
            seg.MaxHP = seg.HP = 1f;
            seg.VisualHeight = 1.2f;
            seg.BuildVisual(s, radius * 2.4f, false);
            seg.baseScale = scale;
            // 3D: a scaled, spined body coil in place of the sprite
            if (segMat == null) { segMat = ProcMesh.Mat(U.Hex("2e4a2a"), Pal.Venom, 0.9f, 0.4f); spineMat = ProcMesh.Mat(U.Hex("c8b48a"), Pal.Venom, 0.4f, 0.2f); }
            seg.Body.sprite = null;
            float rr = radius * 1.05f * scale;
            seg.ball = ProcMesh.Part(go.transform, "coil", ProcMesh.Sphere, segMat, new Vector3(0f, 0f, -rr * 0.8f), new Vector3(rr * 2.1f, rr * 2.1f, rr * 1.9f));
            seg.spine = ProcMesh.Part(go.transform, "spine", ProcMesh.Cone, spineMat, new Vector3(0f, 0f, -rr * 1.6f), new Vector3(rr * 0.6f, rr * 1.1f, rr * 0.6f));
            seg.spine.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            seg.mpb3 = new MaterialPropertyBlock();
            return seg;
        }

        public override float TakeHit(HitInfo hit)
        {
            if (Owner == null || Owner.Dead) return -1f;
            Flash(1f);
            return Owner.TakeHit(hit);
        }

        public override void Die(HitInfo hit) { }

        public void Follow(Vector2 p, Vector2 facing, bool hidden)
        {
            Pos = p;
            Facing = facing;
            Body.enabled = !hidden;
            ShadowR.enabled = !hidden;
        }

        void Update()
        {
            if (Game.I == null || Game.I.Paused || Owner == null) return;
            float dt = Time.deltaTime;
            contactCd -= dt;
            UpdateVisual(dt, Vector2.zero);
            if (ball != null)
            {
                ball.enabled = spine.enabled = Body.enabled;
                mpb3.SetFloat("_Flash", flash);
                mpb3.SetColor("_FlashColor", flashColor);
                ball.SetPropertyBlock(mpb3);
                spine.SetPropertyBlock(mpb3);
            }
            if (!Body.enabled || contactCd > 0f) return;
            var p = Player.I;
            if (p != null && !p.Dead && Vector2.Distance(p.Pos, Pos) < Radius + p.Radius)
            {
                contactCd = 0.8f;
                var h = HitInfo.Of(9f, HitKind.Enemy, Owner, (p.Pos - Pos).Norm(Vector2.up), 9f);
                h.Cause = Owner.DisplayName;
                Combat.Deal(p, h);
            }
        }
    }

    /// <summary>Níðhöggr, the dragon who gnaws the root of the World-Tree.</summary>
    public sealed class NidhoggBoss : Boss
    {
        readonly List<BossSegment> segs = new List<BossSegment>();
        readonly List<Vector2> trail = new List<Vector2>();
        bool under;

        protected override void Setup(EnemyDef def, Vector2 pos, bool elite, float hpMult, float dmgMult, float spawnDelay)
        {
            base.Setup(def, pos, elite, hpMult, dmgMult, spawnDelay);
            baseScale = 1f;
            var spr = CharArt.Get("nidhogg_seg");
            for (int i = 0; i < 9; i++)
            {
                var s = BossSegment.Make(this, spr, 0.85f - i * 0.04f, 1f - i * 0.05f);
                s.Pos = pos - new Vector2(0f, 0.9f * (i + 1));
                segs.Add(s);
            }
            for (int i = 0; i < 120; i++) trail.Add(pos - new Vector2(0f, i * 0.1f));
        }

        protected override void Update()
        {
            base.Update();
            if (Dead)
            {
                foreach (var s in segs) if (s != null) Destroy(s.gameObject);
                segs.Clear();
                return;
            }
            if ((trail[0] - Pos).sqrMagnitude > 0.01f)
            {
                trail.Insert(0, Pos);
                if (trail.Count > 140) trail.RemoveAt(trail.Count - 1);
            }
            for (int i = 0; i < segs.Count; i++)
            {
                int idx = Mathf.Min(trail.Count - 1, (i + 1) * 9);
                var p = trail[idx];
                var prev = trail[Mathf.Max(0, idx - 3)];
                segs[i].Follow(p, (prev - p).Norm(Vector2.right), under || Spawning);
            }
            if (under)
            {
                SetVisible(false);
                if (ShadowR != null) ShadowR.enabled = false;
            }
        }

        public override bool Targetable => base.Targetable && !under;
        public override bool Invulnerable => base.Invulnerable || under;

        protected override void OnCustom(AttackDef a)
        {
            if (a.Tag != "burrow") return;
            var me = this;
            under = true;
            Vfx.Dust(Pos, 16, U.Hex("5a6a3a"), 1.4f);
            Game.I.Audio.Play("roar", 0.6f, 0.7f);
            int n = phase >= 2 ? 4 : 3;
            for (int i = 0; i < n; i++)
            {
                float t0 = 0.4f + i * 0.85f;
                Game.I.After(t0, () =>
                {
                    if (me == null || me.Dead || Player.I == null) return;
                    var at = Player.I.Pos;
                    Eruption(at, 2.2f, 0.75f, a.Damage, Pal.Venom, "slam");
                    Game.I.After(0.75f, () =>
                    {
                        if (me == null || me.Dead) return;
                        Pos = World.Grid != null ? World.Grid.NearestFloor(at, Radius) : at;
                        var h = EHit(a);
                        for (int k = 0; k < 8; k++)
                        {
                            var d = U.Dir(k * 45f);
                            Projectile.Spawn(Spec(new AttackDef { Proj = "bone", Range = 10f, Speed = 7f }), Pos + d, d * 7f, h, Team.Player, me);
                        }
                    });
                });
            }
            Game.I.After(0.4f + n * 0.85f + 0.4f, () =>
            {
                if (me == null) return;
                under = false;
                SetVisible(true);
                Vfx.Dust(Pos, 16, U.Hex("5a6a3a"), 1.4f);
                Game.I.Cam.Shake(0.3f);
            });
        }

        protected override void Enrage()
        {
            base.Enrage();
            Game.I.UI.Banner("NÍÐHÖGGR HUNGERS", "The root trembles", Pal.Venom, 1.6f);
        }
    }

    /// <summary>Garmr, the blood-chested hound at the gate.</summary>
    public sealed class GarmrBoss : Boss
    {
        float chainAngle;

        protected override void OnCustom(AttackDef a)
        {
            if (a.Tag != "chains") return;
            int n = phase >= 2 ? 8 : 4;
            chainAngle += 22.5f;
            Game.I.Audio.Play("chain", 0.8f);
            for (int i = 0; i < n; i++)
            {
                var d = U.Dir(chainAngle + i * 360f / n);
                LineStrike(Pos, d, 14f, 1.2f, Windup + 0.3f, a.Damage, U.Hex("8d939e"), "chain");
            }
        }

        protected override void Enrage()
        {
            base.Enrage();
            Game.I.UI.Banner("GARMR BREAKS A LINK", "The chain will not hold forever", Pal.Blood, 1.6f);
            foreach (var a in Def.Attacks) if (a.Kind == AtkKind.Ring) a.Repeat = 2;
        }

        protected override void OnDied(HitInfo hit)
        {
            foreach (var a in Def.Attacks) if (a.Kind == AtkKind.Ring) a.Repeat = 1;
            base.OnDied(hit);
        }
    }

    /// <summary>Hel, Queen of the Dead. Two lives in her own house.</summary>
    public sealed class HelBoss : Boss
    {
        bool risen;
        float riseT;
        float beamAngle;

        public override bool Invulnerable => base.Invulnerable || riseT > 0f;

        protected override void Update()
        {
            if (riseT > 0f)
            {
                riseT -= Time.deltaTime;
                Flash(0.6f, Pal.Hel);
                HP = Mathf.MoveTowards(HP, MaxHP, MaxHP * Time.deltaTime / 2.2f);
                if (UnityEngine.Random.value < 0.4f) Vfx.Motes(Center, 2, Pal.Hel, 1f, 2f, 1f);
                UpdateVisual(Time.deltaTime, Vector2.zero);
                return;
            }
            base.Update();
        }

        public override void Die(HitInfo hit)
        {
            if (!risen)
            {
                risen = true;
                HP = 1f;
                riseT = 2.4f;
                phase = 3;
                speedMult = 1.25f;
                attackSpeed = 1.25f;
                CancelAttack();
                Game.I.SlowMo(0.3f, 0.6f);
                Game.I.Audio.Play("bell", 0.9f, 0.8f);
                Vfx.Ring(Pos, 6f, Pal.Hel, 1f);
                Game.I.UI.Banner("HEL RISES", "\"You cannot kill Death in her own house, little brother.\"", Pal.Hel, 2.4f);
                foreach (var a in Def.Attacks) if (a.Kind == AtkKind.Ring) a.Repeat = 2;
                return;
            }
            foreach (var a in Def.Attacks) if (a.Kind == AtkKind.Ring) a.Repeat = 1;
            base.Die(hit);
        }

        protected override void OnCustom(AttackDef a)
        {
            var p = Player.I;
            if (p == null) return;
            if (a.Tag == "hands")
            {
                Game.I.Audio.Play("bell", 0.4f, 1.6f);
                int n = phase >= 3 ? 9 : 6;
                Eruption(p.Pos, 1.6f, 1.0f, a.Damage, Pal.Hel);
                for (int i = 0; i < n; i++)
                {
                    var at = p.Pos + UnityEngine.Random.insideUnitCircle * 6f;
                    Eruption(at, 1.6f, 1.0f + i * 0.06f, a.Damage, Pal.Hel);
                }
            }
            else if (a.Tag == "beams")
            {
                Game.I.Audio.Play("horn", 0.6f, 1.3f);
                int waves = 3, lines = phase >= 3 ? 3 : 2;
                for (int w = 0; w < waves; w++)
                {
                    float ang = beamAngle + w * 30f;
                    for (int l = 0; l < lines; l++)
                    {
                        var d = U.Dir(ang + l * 180f / lines);
                        LineStrike(Pos - d * 15f, d, 30f, 1.4f, 0.9f + w * 0.9f, a.Damage, Pal.Hel, "zap");
                    }
                }
                beamAngle += 45f;
            }
        }

        protected override void Enrage()
        {
            base.Enrage();
            Game.I.UI.Banner("THE HALF-DEAD HALF", "Her living face goes still", Pal.Hel, 1.6f);
        }
    }
}
