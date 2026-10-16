using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Data-driven foe. Movement styles and telegraphed attacks come from its EnemyDef;
    /// bosses subclass this to add phases and bespoke patterns.
    /// </summary>
    public class Enemy : Entity
    {
        public static event Action<Enemy> Killed;

        public EnemyDef Def;
        public float DmgMult = 1f;
        public bool Spawning;
        public bool Minion;

        protected enum S { Move, Windup, Act, Recover }
        protected S state = S.Move;
        protected float stT;
        protected AttackDef cur;
        protected float[] cds;
        protected Vector2 aimDir = Vector2.right, aimPoint;
        protected VfxSystem.Tele tele;
        protected float speedMult = 1f, attackSpeed = 1f, windupScale = 1f;
        protected bool burrowed;
        protected int repeatLeft;
        protected float repeatT;
        protected readonly HashSet<Entity> actHits = new HashSet<Entity>();
        protected Vector2 leapFrom;
        float actDist, thinkT, strafeT, strafeSign = 1f, teleportCd = 2f, spawnT, spawnTotal = 1f, dustT;
        Vector2 vel;
        bool exploded;
        SpriteRenderer aura;

        public override bool Targetable => !Dead && !Spawning && !burrowed;
        public override bool Invulnerable => Spawning || burrowed;

        public static Enemy Spawn(EnemyDef def, Vector2 pos, bool elite, float hpMult, float dmgMult, float spawnDelay = 0.8f)
        {
            var go = new GameObject(def.Id);
            go.transform.SetParent(World.Root, false);
            Enemy e;
            switch (def.Id)
            {
                case "vali": e = go.AddComponent<ValiBoss>(); break;
                case "nidhogg": e = go.AddComponent<NidhoggBoss>(); break;
                case "garmr": e = go.AddComponent<GarmrBoss>(); break;
                case "hel": e = go.AddComponent<HelBoss>(); break;
                default: e = go.AddComponent<Enemy>(); break;
            }
            e.Setup(def, pos, elite, hpMult, dmgMult, spawnDelay);
            return e;
        }

        protected virtual void Setup(EnemyDef def, Vector2 pos, bool elite, float hpMult, float dmgMult, float spawnDelay)
        {
            Def = def;
            Id = def.Id;
            Team = Team.Enemy;
            Elite = elite && !def.Boss;
            DisplayName = (Elite ? "Armored " : "") + def.Name;
            hpMult *= def.Boss ? EnemyDefs.BossHpScale : EnemyDefs.HpScale;
            MaxHP = HP = def.HP * hpMult;
            Radius = def.Radius;
            Mass = def.Mass;
            Flying = def.Flying;
            IsBoss = def.Boss;
            MaxArmor = Armor = def.Armor * hpMult + (Elite ? def.HP * hpMult * 0.7f : 0f);
            DmgMult = dmgMult * (Elite ? 1.15f : 1f);
            baseScale = def.Scale * (Elite ? 1.12f : 1f);
            VisualHeight = CharArt.Height(def.Art);
            BuildRig(def.Art, Mathf.Max(0.9f, def.Radius * 2.6f), !def.Boss);
            Lights.Attach(transform, new Vector2(0f, VisualHeight * 0.5f), Mathf.Max(1.5f, def.Radius * 3.2f), def.Blood, def.Boss ? 0.55f : 0.3f);
            if (Elite)
            {
                aura = Mats.SR(transform, "aura", SpriteLib.Light, Sort.FloorFx + 2, Pal.Gold.A(0.3f), true);
                Lights.Attach(transform, new Vector2(0f, 0.6f), 2.2f, Pal.Gold, 0.5f);
                aura.transform.localScale = new Vector3(3f, 3f, 1f);
            }
            cds = new float[def.Attacks.Length];
            for (int i = 0; i < cds.Length; i++) cds[i] = UnityEngine.Random.Range(0.4f, 1.1f) + def.Attacks[i].Cooldown * 0.25f;
            Pos = pos;
            if (Player.I != null) Facing = (Player.I.Pos - pos).Norm(Vector2.right);
            Spawning = spawnDelay > 0f;
            spawnT = spawnDelay;
            spawnTotal = Mathf.Max(0.01f, spawnDelay);
            if (Spawning)
            {
                SetAlpha(0f);
                Vfx.TeleCircle(pos, Mathf.Max(0.8f, def.Radius * 1.8f), spawnDelay, World.Biome.Accent);
                Game.I.Audio.Play("spawn", 0.35f);
            }
        }

        protected Entity Target()
        {
            if (St.Charmed > 0f) return Combat.Nearest(Pos, Team.Enemy, 30f, this);
            var p = Player.I;
            return p != null && !p.Dead ? p : null;
        }

        protected Team TargetTeam => St.Charmed > 0f ? Team.Enemy : Team.Player;

        protected virtual void Update()
        {
            if (Game.I == null || Game.I.Paused) return;
            float dt = Time.deltaTime;
            if (Dead)
            {
                UpdateVisual(dt, Vector2.zero);
                float k = Mathf.Clamp01((deadT - 0.35f) / 0.35f);
                SetAlpha(1f - k);
                if (ShadowR != null) ShadowR.color = new Color(1, 1, 1, 1f - k);
                if (k > 0f && UnityEngine.Random.value < 0.5f) Vfx.Motes(Center, 1, Def.Blood, 0.5f, 1.5f, 0.6f);
                if (deadT > 0.75f) Destroy(gameObject);
                return;
            }
            if (Spawning)
            {
                spawnT -= dt;
                SetAlpha(Mathf.Clamp01((1f - spawnT / spawnTotal) * 2.5f));
                if (spawnT <= 0f)
                {
                    Spawning = false;
                    SetAlpha(1f);
                    Vfx.Dust(Pos, 6, World.Biome.Fog, 0.8f);
                    Vfx.Flash(Center, 0.8f, World.Biome.Accent, 0.2f);
                }
                UpdateVisual(dt, Vector2.zero);
                return;
            }

            TickStatus(dt);
            for (int i = 0; i < cds.Length; i++) cds[i] -= dt * attackSpeed;

            if (St.Disabled)
            {
                CancelAttack();
                Physics(dt, Vector2.zero);
                UpdateVisual(dt, Vector2.zero);
                return;
            }

            var tgt = Target();
            float spd = Def.Speed * St.SpeedMult * speedMult;
            Vector2 move = Vector2.zero;
            switch (state)
            {
                case S.Move:
                    if (tgt != null)
                    {
                        move = Movement(dt, tgt) * spd;
                        var to = tgt.Pos - Pos;
                        if (to.sqrMagnitude > 0.01f) Facing = Def.Move == MoveKind.Flyer && move.sqrMagnitude > 0.1f ? move.normalized : to.normalized;
                        thinkT -= dt;
                        if (thinkT <= 0f)
                        {
                            thinkT = 0.12f;
                            var a = ChooseAttack(tgt);
                            if (a != null) BeginAttack(a, tgt);
                        }
                    }
                    break;
                case S.Windup:
                    WindupUpdate(dt, tgt);
                    break;
                case S.Act:
                    move = ActUpdate(dt);
                    break;
                case S.Recover:
                    stT += dt * attackSpeed;
                    if (stT >= cur.Recover) { state = S.Move; OnAttackDone(cur); }
                    break;
            }

            if (!Flying || Def.Move != MoveKind.Flyer)
            {
                foreach (var o in Entity.All)
                {
                    if (o == this || o.Dead || o.Flying != Flying) continue;
                    var d = Pos - o.Pos;
                    float min = Radius + o.Radius;
                    float m = d.magnitude;
                    if (m < min && m > 0.001f) move += d / m * (min - m) * (o is Player ? 10f : 6f);
                }
            }

            vel = move;
            if (state == S.Act && cur != null && cur.Kind == AtkKind.Leap) { }
            else Physics(dt, move * dt);
            UpdateVisual(dt, vel);

            if (burrowed)
            {
                SetVisible(false);
                if (ShadowR != null) ShadowR.enabled = false;
                dustT -= dt;
                if (dustT <= 0f) { dustT = 0.08f; Vfx.Dust(Pos, 1, U.Hex("6a5a3a"), 0.6f); }
            }
            else if (!Visible) SetVisible(true);
            if (aura != null) aura.color = Pal.Gold.A(Armor > 0f ? 0.25f + 0.08f * Mathf.Sin(Time.time * 4f) : 0f);
        }

        protected virtual Vector2 Movement(float dt, Entity tgt)
        {
            Vector2 to = tgt.Pos - Pos;
            float d = to.magnitude;
            Vector2 dir = d > 0.01f ? to / d : Vector2.zero;
            strafeT -= dt;
            if (strafeT <= 0f) { strafeT = UnityEngine.Random.Range(1.2f, 2.6f); strafeSign = -strafeSign; }
            switch (Def.Move)
            {
                case MoveKind.Chase:
                    return d > Def.PrefDist ? dir : Vector2.zero;
                case MoveKind.Kite:
                    if (d < Def.PrefDist * 0.7f) return -dir;
                    if (d > Def.PrefDist * 1.3f) return dir;
                    return dir.Rot(90f) * strafeSign * 0.6f;
                case MoveKind.Strafe:
                    return (d > Def.PrefDist ? dir : Vector2.zero) + dir.Rot(90f) * strafeSign * 0.6f;
                case MoveKind.Teleport:
                    teleportCd -= dt;
                    if (teleportCd <= 0f) { DoTeleport(tgt); teleportCd = UnityEngine.Random.Range(3f, 5f); }
                    return d < 3f ? -dir * 0.6f : Vector2.zero;
                case MoveKind.Flyer:
                    return (dir.Rot(90f) * strafeSign + dir * Mathf.Clamp((d - Def.PrefDist) * 0.5f, -1f, 1f)).normalized;
                case MoveKind.Burrow:
                    if (d > Def.PrefDist + 0.5f) { burrowed = true; return dir * 1.5f; }
                    return Vector2.zero;
            }
            return Vector2.zero;
        }

        void DoTeleport(Entity tgt)
        {
            if (World.Grid == null) return;
            for (int i = 0; i < 20; i++)
            {
                var p = tgt.Pos + UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(4.5f, 7.5f);
                if (!World.Grid.Free(p, Radius + 0.2f)) continue;
                Vfx.Flash(Center, 1f, Def.Blood, 0.25f);
                Vfx.Motes(Center, 8, Def.Blood, 0.5f, 1f, 0.5f);
                Pos = p;
                Vfx.Flash(Center, 1f, Def.Blood, 0.25f);
                Game.I.Audio.Play("dash", 0.3f, 0.6f);
                return;
            }
        }

        protected virtual AttackDef ChooseAttack(Entity tgt)
        {
            float d = Vector2.Distance(Pos, tgt.Pos);
            AttackDef pick = null;
            float total = 0f;
            for (int i = 0; i < Def.Attacks.Length; i++)
            {
                var a = Def.Attacks[i];
                if (cds[i] > 0f || d > a.Range || d < a.MinRange || a.MinPhase > Phase) continue;
                total += a.Weight;
                if (UnityEngine.Random.value * total <= a.Weight) pick = a;
            }
            return pick;
        }

        public virtual int Phase => 1;

        protected void BeginAttack(AttackDef a, Entity tgt, float windupMul = 1f)
        {
            cur = a;
            state = S.Windup;
            stT = 0f;
            windupScale = windupMul;
            if (tgt != null)
            {
                aimPoint = tgt.Pos;
                aimDir = (tgt.Pos - Pos).Norm(Facing);
                Facing = aimDir;
            }
            if (windupMul >= 1f) repeatLeft = a.Repeat - 1;
            if (burrowed)
            {
                burrowed = false;
                SetVisible(true);
                Vfx.Dust(Pos, 10, U.Hex("6a5a3a"), 1f);
                Game.I.Audio.Play("slam", 0.4f, 1.4f);
            }
            int idx = Array.IndexOf(Def.Attacks, a);
            if (idx >= 0 && windupMul >= 1f) cds[idx] = a.Cooldown;
            CreateTele();
            if (a.Windup * windupMul >= 0.6f) Game.I.Audio.Play("telegraph", 0.25f, 0.9f);
            OnBeginAttack(a);
        }

        protected virtual void OnBeginAttack(AttackDef a) { }

        protected float Windup => cur.Windup * windupScale;

        void CreateTele()
        {
            tele = null;
            Color c = St.Charmed > 0f ? Pal.God(God.Freyja) : Pal.Danger;
            float w = Windup / attackSpeed;
            switch (cur.Kind)
            {
                case AtkKind.Swipe: tele = Vfx.TeleCone(Pos, aimDir, cur.Size, cur.Arc, w, c); break;
                case AtkKind.Lunge:
                case AtkKind.Charge: tele = Vfx.TeleLine(Pos, aimDir, cur.Dist + Radius, Mathf.Max(0.8f, cur.Size * 2f), w, c); break;
                case AtkKind.Slam:
                case AtkKind.Explode: tele = Vfx.TeleCircle(Pos, cur.Size, w, c); break;
                case AtkKind.Leap: tele = Vfx.TeleCircle(ClampToFloor(aimPoint), cur.Size, w + cur.Active, c); break;
                case AtkKind.Shoot: tele = Vfx.TeleLine(Pos, aimDir, Mathf.Min(cur.Range, 7f), 0.25f, w, c); break;
            }
        }

        Vector2 ClampToFloor(Vector2 p) => World.Grid != null ? World.Grid.NearestFloor(p, Radius) : p;

        void WindupUpdate(float dt, Entity tgt)
        {
            stT += dt * attackSpeed;
            float k = stT / Mathf.Max(0.01f, Windup);
            if (tgt != null && k < cur.TrackUntil)
            {
                aimPoint = tgt.Pos;
                aimDir = (tgt.Pos - Pos).Norm(aimDir);
                Facing = aimDir;
            }
            if (tele != null)
            {
                if (cur.Kind == AtkKind.Leap) tele.Move(ClampToFloor(aimPoint), aimDir);
                else tele.Move(Pos, aimDir);
            }
            squash = -0.12f * Mathf.Clamp01(k);
            flash = Mathf.Max(flash, 0.25f * k);
            flashColor = Pal.Danger;
            if (cur.Kind == AtkKind.Ring || cur.Kind == AtkKind.Summon || cur.Kind == AtkKind.Custom)
                if (UnityEngine.Random.value < 0.3f) Vfx.Motes(Center, 1, Def.Blood, 0.8f, 1f, 0.4f);
            if (stT >= Windup) Execute();
        }

        protected HitInfo EHit(AttackDef a)
        {
            float dmg = a.Damage * DmgMult * (St.Enthralled > 0f ? 0.7f : 1f);
            var h = HitInfo.Of(dmg, HitKind.Enemy, this, aimDir, a.Knock);
            h.Cause = string.IsNullOrEmpty(Def.Title) ? Def.Name : Def.Name;
            return h;
        }

        protected virtual void Execute()
        {
            tele?.Kill();
            tele = null;
            var h = EHit(cur);
            var tt = TargetTeam;
            stT = 0f;
            state = S.Act;
            actHits.Clear();
            switch (cur.Kind)
            {
                case AtkKind.Swipe:
                    Combat.HitArc(Pos, aimDir, cur.Size, cur.Arc, tt, h, null, null, this);
                    Vfx.Slash(Pos + new Vector2(0f, 0.5f), aimDir.Ang(), cur.Size, Mathf.Clamp((int)cur.Arc, 60, 220), Def.Blood.Mix(Pal.Danger, 0.5f), 0.18f);
                    Game.I.Audio.Play("swing", 0.45f, 0.85f);
                    if (cur.ZoneDps > 0f)
                    {
                        Color zc = cur.Tag == "blaze" ? Pal.Blood : Pal.Venom;
                        for (int i = 1; i <= 3; i++)
                        {
                            var zp = Pos + aimDir * cur.Size * (0.25f + 0.25f * i);
                            Zone.Spawn(zp, 1.1f + 0.25f * i, 4f, 0.5f, HitInfo.Of(cur.ZoneDps * 0.5f, HitKind.Hazard, this, Vector2.zero), tt, zc);
                        }
                    }
                    break;
                case AtkKind.Lunge:
                case AtkKind.Charge:
                    actDist = 0f;
                    Game.I.Audio.Play(cur.Kind == AtkKind.Charge ? "dash" : "swing", 0.45f, 0.8f);
                    break;
                case AtkKind.Slam:
                    DoSlam(Pos, cur.Size, h, tt);
                    break;
                case AtkKind.Leap:
                    leapFrom = Pos;
                    aimPoint = ClampToFloor(aimPoint);
                    Game.I.Audio.Play("swing_heavy", 0.5f, 0.7f);
                    break;
                case AtkKind.Shoot:
                    FireShots(h, tt);
                    repeatT = cur.RepeatGap;
                    break;
                case AtkKind.Ring:
                    FireRing(h, tt);
                    repeatT = cur.RepeatGap;
                    break;
                case AtkKind.Lob:
                    FireLobs(h, tt);
                    break;
                case AtkKind.Explode:
                    Explode();
                    return;
                case AtkKind.Summon:
                    for (int i = 0; i < cur.Count; i++)
                    {
                        var p = Pos + U.Dir(i * 360f / cur.Count + 45f) * 2.2f;
                        RunDirector.SpawnMinion(cur.Summon, p);
                    }
                    Game.I.Audio.Play("howl", 0.3f, 1.4f);
                    Vfx.Ring(Pos, 2.5f, Def.Blood, 0.5f);
                    break;
                case AtkKind.Custom:
                    OnCustom(cur);
                    break;
            }
        }

        protected void DoSlam(Vector2 at, float size, HitInfo h, Team tt)
        {
            Combat.HitCircle(at, size, tt, h, null, null, this);
            Vfx.Ring(at, size, Def.Blood.Mix(Pal.Danger, 0.4f), 0.35f);
            Vfx.Dust(at, 8, World.Biome.Fog, 1f);
            Game.I.Audio.Play("slam", 0.6f);
            Game.I.Cam.Shake(Mathf.Clamp(size * 0.07f, 0.08f, 0.35f));
        }

        protected virtual void OnCustom(AttackDef a) { }
        protected virtual void OnAttackDone(AttackDef a) { }

        protected ProjSpec Spec(AttackDef a)
        {
            var s = new ProjSpec { Radius = 0.25f, Life = a.Range / Mathf.Max(1f, a.Speed) + 0.6f };
            switch (a.Proj)
            {
                case "arrow": s.Sprite = SpriteLib.Arrow(U.Hex("4a3a2a"), Pal.Bone); break;
                case "glob": s.Sprite = SpriteLib.Glob; s.Spin = true; break;
                case "shard": s.Sprite = SpriteLib.Shard; break;
                case "bone": s.Sprite = SpriteLib.BoneShard; s.Scale = 1.1f; break;
                default: s.Sprite = SpriteLib.Orb; s.Color = a.ProjColor; s.Additive = true; s.Scale = 0.9f; s.Trail = a.ProjColor.A(0.35f); break;
            }
            return s;
        }

        void FireShots(HitInfo h, Team tt)
        {
            int n = Mathf.Max(1, cur.Count);
            for (int i = 0; i < n; i++)
            {
                float ang = n > 1 ? Mathf.Lerp(-cur.Spread * 0.5f, cur.Spread * 0.5f, i / (float)(n - 1)) : 0f;
                var d = aimDir.Rot(ang);
                Projectile.Spawn(Spec(cur), Pos + d * (Radius + 0.2f), d * cur.Speed, h, tt, this);
            }
            Game.I.Audio.Play(cur.Proj == "arrow" ? "arrow" : "cast", 0.35f, 1.1f);
        }

        void FireRing(HitInfo h, Team tt)
        {
            int n = Mathf.Max(3, cur.Count);
            float off = UnityEngine.Random.Range(0f, 360f / n);
            for (int i = 0; i < n; i++)
            {
                var d = U.Dir(off + i * 360f / n);
                Projectile.Spawn(Spec(cur), Pos + d * (Radius + 0.2f), d * cur.Speed, h, tt, this);
            }
            Vfx.Ring(Pos, 1.5f, cur.ProjColor, 0.3f);
            Game.I.Audio.Play("cast", 0.4f, 0.8f);
        }

        void FireLobs(HitInfo h, Team tt)
        {
            for (int i = 0; i < Mathf.Max(1, cur.Count); i++)
            {
                var target = aimPoint + (i > 0 ? UnityEngine.Random.insideUnitCircle * 2.8f : Vector2.zero);
                if (World.Grid != null) target = World.Grid.NearestFloor(target, 0.3f);
                float size = cur.Size;
                float zone = cur.ZoneDps;
                Vfx.TeleCircle(target, size, 0.75f, Pal.Danger);
                var spec = Spec(cur);
                var a = cur;
                spec.OnEnd = pr =>
                {
                    Combat.HitCircle(target, size, tt, h, null, null, this);
                    Vfx.Burst(target, 8, a.Proj == "shard" ? Pal.Frost : Pal.Venom, 6f, 0.35f, 0.35f);
                    Game.I.Audio.Play(a.Proj == "shard" ? "frost" : "splash", 0.45f);
                    if (zone > 0f) Zone.Spawn(target, size, 4f, 0.5f, HitInfo.Of(zone * 0.5f, HitKind.Hazard, null, Vector2.zero), tt, Pal.Venom);
                };
                Projectile.SpawnLob(spec, Pos + new Vector2(0f, 0.5f), target, 0.75f, h, tt, this);
            }
            Game.I.Audio.Play("swing", 0.3f, 0.6f);
        }

        void Explode()
        {
            if (exploded) return;
            exploded = true;
            var h = EHit(cur);
            Combat.HitCircle(Pos, cur.Size, Team.Player, h, null, null, this);
            Combat.HitCircle(Pos, cur.Size, Team.Enemy, h, null, null, this);
            Vfx.Ring(Pos, cur.Size, Pal.Venom, 0.4f);
            Vfx.Burst(Center, 24, Pal.Venom, 10f, 0.5f, 0.45f);
            Vfx.Flash(Center, cur.Size, Pal.Venom, 0.25f);
            Game.I.Audio.Play("explode", 0.8f);
            Game.I.Cam.Shake(0.25f);
            Die(HitInfo.Of(0f, HitKind.Effect, null, Vector2.zero));
        }

        protected virtual Vector2 ActUpdate(float dt)
        {
            stT += dt * attackSpeed;
            var tt = TargetTeam;
            switch (cur.Kind)
            {
                case AtkKind.Lunge:
                case AtkKind.Charge:
                    {
                        float speed = cur.Dist / Mathf.Max(0.05f, cur.Active);
                        actDist += speed * dt;
                        foreach (var e in Combat.InRadius(Pos, Radius + Mathf.Max(0.3f, cur.Size * 0.5f), tt))
                        {
                            if (e == this || actHits.Contains(e)) continue;
                            actHits.Add(e);
                            Combat.Deal(e, EHit(cur));
                        }
                        if (cur.Kind == AtkKind.Charge && UnityEngine.Random.value < 0.5f) Afterimage(Def.Blood.A(0.4f), 0.2f);
                        if (actDist >= cur.Dist || stT >= cur.Active * 1.3f) EndAct();
                        return aimDir * speed;
                    }
                case AtkKind.Leap:
                    {
                        float k = Mathf.Clamp01(stT / Mathf.Max(0.05f, cur.Active));
                        Pos = Vector2.Lerp(leapFrom, aimPoint, U.Smooth(k));
                        lift = Mathf.Sin(k * Mathf.PI) * 2.2f;
                        if (k >= 1f)
                        {
                            lift = 0f;
                            DoSlam(Pos, cur.Size, EHit(cur), tt);
                            EndAct();
                        }
                        return Vector2.zero;
                    }
                case AtkKind.Shoot:
                case AtkKind.Ring:
                    if (repeatLeft > 0)
                    {
                        repeatT -= dt;
                        if (repeatT <= 0f)
                        {
                            repeatLeft--;
                            repeatT = cur.RepeatGap;
                            var tgt = Target();
                            if (tgt != null) aimDir = (tgt.Pos - Pos).Norm(aimDir);
                            if (cur.Kind == AtkKind.Shoot) FireShots(EHit(cur), tt); else FireRing(EHit(cur), tt);
                        }
                        return Vector2.zero;
                    }
                    break;
            }
            if (stT >= cur.Active) EndAct();
            return Vector2.zero;
        }

        protected void EndAct()
        {
            if (repeatLeft > 0 && (cur.Kind == AtkKind.Lunge || cur.Kind == AtkKind.Swipe || cur.Kind == AtkKind.Charge))
            {
                repeatLeft--;
                BeginAttack(cur, Target(), 0.55f);
                return;
            }
            state = S.Recover;
            stT = 0f;
        }

        protected void CancelAttack()
        {
            tele?.Kill();
            tele = null;
            lift = 0f;
            if (state != S.Move) { state = S.Move; stT = 0f; }
        }

        protected override float DamageTakenMult(in HitInfo hit)
        {
            if (Def.Shielded && Armor <= 0f && (hit.Kind == HitKind.Attack || hit.Kind == HitKind.Special || hit.Kind == HitKind.Cast) && St.Disabled == false)
            {
                if (Vector2.Dot(hit.Dir.Norm(Vector2.zero), Facing) < -0.35f)
                {
                    Vfx.Flash(Center + Facing * 0.5f, 0.6f, Pal.Armor, 0.12f);
                    Game.I.Audio.Play("armor", 0.5f, 1.3f);
                    return 0.12f;
                }
            }
            return 1f;
        }

        protected override void OnHurt(HitInfo hit, float dmg)
        {
            if (state == S.Windup && !IsBoss && !Elite && Armor <= 0f && dmg >= MaxHP * 0.25f && Def.Mass < 3f)
            {
                CancelAttack();
                St.Stun = Mathf.Max(St.Stun, 0.2f);
            }
            Vfx.Burst(Center, 4, Def.Blood, 6f, 0.3f, 0.3f);
        }

        protected override void OnSlam(float force)
        {
            Vfx.Dust(Pos, 5, World.Biome.Fog, 0.8f);
            Game.I.Audio.Play("hit_heavy", 0.4f, 1.2f);
            RunState.Current?.Boons.OnSlam(this);
        }

        public override void Die(HitInfo hit)
        {
            if (Dead) return;
            Dead = true;
            HP = 0f;
            tele?.Kill();
            lift = 0f;
            Vfx.Burst(Center, 16, Def.Blood, 9f, 0.45f, 0.45f);
            Vfx.Motes(Center, 10, World.Biome.Accent, 0.5f, 2f, 1.1f);
            Vfx.Flash(Center, 1.2f, Def.Blood, 0.2f);
            Game.I.Audio.Play("die", 0.6f);
            Flash(1f);
            SaveSystem.Data.kills++;
            SaveSystem.Data.AddCount("kill_" + Id);
            SaveSystem.Data.SetFlag("codex_" + Id);
            var run = RunState.Current;
            if (run != null)
            {
                run.Kills++;
                run.Boons.OnEnemyKilled(this);
            }
            if (Def.ExplodeOnDeath && !exploded)
            {
                exploded = true;
                var at = Pos;
                var size = 2.2f;
                var h = HitInfo.Of(14f * DmgMult, HitKind.Enemy, null, Vector2.zero, 8f);
                h.Cause = Def.Name;
                Vfx.TeleCircle(at, size, 0.55f, Pal.Danger);
                Game.I.After(0.55f, () =>
                {
                    Combat.HitCircle(at, size, Team.Player, h);
                    Combat.HitCircle(at, size, Team.Enemy, h);
                    Vfx.Ring(at, size, Pal.Venom, 0.4f);
                    Vfx.Burst(at, 20, Pal.Venom, 9f, 0.45f, 0.45f);
                    Game.I.Audio.Play("explode", 0.7f);
                    Game.I.Cam.Shake(0.2f);
                });
            }
            OnDied(hit);
            Killed?.Invoke(this);
        }

        protected virtual void OnDied(HitInfo hit) { }

        protected override float RigMaxSpeed => Mathf.Max(1.5f, Def != null ? Def.Speed : 3f);

        /// <summary>Translate the AI's attack state into an animation pose.</summary>
        protected override void FillRig(ref RigInput r)
        {
            r.Underground = burrowed;
            if (Spawning) r.Spawn = 1f - Mathf.Clamp01(spawnT / spawnTotal);
            if (Dead || cur == null || state == S.Move) return;
            var kind = cur.Kind;
            float wind = stT / Mathf.Max(0.01f, Windup);
            switch (state)
            {
                case S.Windup:
                    r.ActionT = wind;
                    switch (kind)
                    {
                        case AtkKind.Slam:
                        case AtkKind.Explode: r.Action = RigAction.SlamWindup; break;
                        case AtkKind.Shoot: r.Action = RigAction.Shoot; break;
                        case AtkKind.Ring:
                        case AtkKind.Summon:
                        case AtkKind.Custom:
                        case AtkKind.Lob: r.Action = RigAction.Cast; break;
                        default: r.Action = RigAction.Windup; break;
                    }
                    break;
                case S.Act:
                    r.ActionT = Mathf.Clamp01(stT / Mathf.Max(0.05f, cur.Active));
                    switch (kind)
                    {
                        case AtkKind.Swipe: r.Action = RigAction.Strike; break;
                        case AtkKind.Lunge:
                        case AtkKind.Charge:
                        case AtkKind.Leap: r.Action = RigAction.Lunge; break;
                        case AtkKind.Slam: r.Action = RigAction.SlamStrike; break;
                        case AtkKind.Shoot: r.Action = RigAction.Shoot; r.ActionT = 1f; break;
                        default: r.Action = RigAction.Cast; break;
                    }
                    break;
                case S.Recover:
                    float k = stT / Mathf.Max(0.05f, cur.Recover);
                    if (k < 0.45f)
                    {
                        if (kind == AtkKind.Swipe) { r.Action = RigAction.Strike; r.ActionT = 1f; }
                        else if (kind == AtkKind.Slam) { r.Action = RigAction.SlamStrike; r.ActionT = 1f; }
                    }
                    break;
            }
        }
    }
}
