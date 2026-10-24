using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Narfi, son of Loki. Movement, dash, cast, call, defiance and the weapon rig.</summary>
    public sealed class Player : Entity
    {
        public static Player I;

        public Weapon Weapon;
        public Vector2 Aim = Vector2.right;
        public bool Controllable = true;
        public int DashCharges;
        public int CastAmmo;
        public float Fury;
        public float SinceDash = 9f;
        public Interactable Focus;
        public Transform WeaponPivot;
        public SpriteRenderer WeaponSR;
        public float WeaponAngle;
        public bool WeaponSwinging;
        public bool WeaponHidden;
        public float WeaponExtend;
        public string DeathCause = "";

        readonly PlayerStats hubStats = new PlayerStats();
        float dashRecharge, dashT, dashBuffer, castLock, iframes, lungeT, stepT, afterT;
        Vector2 dashDir, dashFrom, moveVel, lungeVel;
        bool requireRelease;
        SpriteRenderer soulLight;
        WeaponTrail trail;
        Color trailColor = new Color(0.82f, 1f, 0.92f);
        float castAnim;
        float noGodT = -9f;
        float sideCd, dashSpeed = 23f;
        bool sidestep;
        int sideFlip = 1;

        float WeaponReach
        {
            get
            {
                switch (Weapon != null ? Weapon.Id : "axe")
                {
                    case "spear": return 1.7f;
                    case "bow": return 0.5f;
                    case "seax": return 0.75f;
                }
                return 1.15f;
            }
        }

        public PlayerStats S => RunState.Current != null ? RunState.Current.Stats : hubStats;
        public bool Dashing => dashT > 0f;
        public override bool Invulnerable => iframes > 0f || Dashing || !Controllable;
        public float IFrames => iframes;
        /// <summary>Height of Narfi's chest: the plane mouse-aiming is measured on.</summary>
        float AimHeight => Model != null ? Model.Spec.Height * 0.55f : 0.5f;
        public float SidestepCooldown => sideCd;

        public static Player Create(Transform parent)
        {
            var go = new GameObject("Narfi");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<Player>();
            p.Init();
            return p;
        }

        void Init()
        {
            I = this;
            Team = Team.Player;
            Id = "narfi";
            DisplayName = "Narfi";
            Radius = 0.36f;
            MaxHP = HP = S.MaxHP;
            VisualHeight = CharArt.Height("narfi");
            BuildRig("narfi", 1.0f, false);
            soulLight = Lights.Attach(transform, new Vector2(0f, 0.6f), 6.5f, Color.Lerp(Color.white, Pal.Narfi, 0.35f), 0.62f);

            WeaponPivot = new GameObject("weaponPivot").transform;
            WeaponPivot.SetParent(transform, false);
            WeaponPivot.localPosition = new Vector3(0f, 0.55f, 0f);
            WeaponSR = Mats.SR(WeaponPivot, "weapon", null, 0);
            WeaponSR.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            trail = WeaponTrail.Create(transform.parent);
            OnStatsChanged();
            DashCharges = S.DashCharges;
            CastAmmo = S.CastMax;
        }

        public void Equip(string id)
        {
            switch (id)
            {
                case "spear": Weapon = new SpearWeapon(); break;
                case "bow": Weapon = new BowWeapon(); break;
                case "seax": Weapon = new SeaxWeapon(); break;
                default: Weapon = new AxeWeapon(); break;
            }
            Weapon.Init(this);
            WeaponSR.sprite = SpriteLib.Weapon(Weapon.Id);
            Model?.SetWeapon(ModelSpecs.PlayerWeapon(Weapon.Id));
        }

        public void OnStatsChanged()
        {
            MaxHP = S.MaxHP;
            HP = Mathf.Min(HP, MaxHP);
            DashCharges = Mathf.Min(DashCharges, S.DashCharges);
        }

        /// <summary>Reset for a new escape or a return to the hall.</summary>
        public void ResetForRun()
        {
            Dead = false;
            St.Clear();
            Knock = Vector2.zero;
            MaxHP = S.MaxHP;
            HP = MaxHP;
            DashCharges = S.DashCharges;
            CastAmmo = S.CastMax;
            Fury = 0f;
            iframes = 0f;
            dashT = 0f;
            Weapon?.Cancel();
            gameObject.SetActive(true);
            SetVisible(true);
        }

        public void Teleport(Vector2 p)
        {
            Pos = p;
            moveVel = Vector2.zero;
            Knock = Vector2.zero;
            dashT = 0f;
            lungeT = 0f;
        }

        public void BlockInputsUntilRelease() => requireRelease = true;

        public Vector2 AttackDir()
        {
            var inp = Game.I.Input;
            if (inp.MouseAim) return Aim;
            return Combat.Assist(Pos, Aim, 7f, 32f);
        }

        public void Lunge(float dist, float time)
        {
            if (dist <= 0f) return;
            lungeVel = AttackDir() * (dist / Mathf.Max(0.01f, time));
            lungeT = time;
        }

        public void AddFury(float v)
        {
            if (RunState.Current == null) return;
            Fury = Mathf.Min(100f, Fury + v * S.FuryMult);
        }

        public void HealAmount(float amount)
        {
            if (Dead) return;
            float a = amount * S.HealMult;
            float before = HP;
            HP = Mathf.Min(MaxHP, HP + a);
            float gained = HP - before;
            if (gained >= 1f)
            {
                Vfx.Text(Pos + new Vector2(0f, VisualHeight + 0.3f), "+" + Mathf.RoundToInt(gained), Pal.Heal, 1f);
                Vfx.Motes(Center, 8, Pal.Heal, 0.6f, 1.6f, 0.9f);
            }
        }

        public void ModifyIncoming(ref HitInfo h)
        {
            if (Invulnerable) return;
            if (S.Dodge > 0f && Random.value < S.Dodge)
            {
                h.Damage = 0f;
                Vfx.Text(Pos + new Vector2(0f, VisualHeight + 0.3f), "DODGE", Pal.God(God.Ratatoskr), 0.8f);
                iframes = 0.25f;
                return;
            }
            h.Damage *= 1f - S.DamageReduction;
            var run = RunState.Current;
            if (run != null && h.Source != null && h.Source.St.Chill > 0)
                h.Damage *= 1f - run.Boons.V("skadi_snowblind");
        }

        public override float TakeHit(HitInfo hit)
        {
            float dealt = base.TakeHit(hit);
            if (dealt <= 0f) return dealt;
            iframes = 0.75f;
            Game.I.Hitstop(0.09f);
            Game.I.Cam.Shake(0.35f);
            Game.I.Cam.Kick(hit.Dir, 0.25f);
            Game.I.Audio.Play("hurt", 0.9f);
            Game.I.UI.HurtFlash();
            Vfx.Burst(Center, 10, Pal.Blood, 7f, 0.35f, 0.35f);
            var run = RunState.Current;
            if (run != null)
            {
                run.LastHitBy = hit.Cause;
                if (!Dead) run.Boons.OnPlayerHit(hit.Source);
            }
            Fury = Mathf.Min(100f, Fury + dealt * 0.9f * S.FuryMult);
            return dealt;
        }

        public override void Die(HitInfo hit)
        {
            var run = RunState.Current;
            if (run != null)
            {
                if (run.DefianceLeft > 0)
                {
                    run.DefianceLeft--;
                    Revive(0.4f, "DEFIANCE", "Narfi refuses his wyrd");
                    return;
                }
                if (SaveSystem.Data.keepsake == "hel_mask" && !run.MaskUsed)
                {
                    run.MaskUsed = true;
                    Revive(0.3f, "HEL'S HALF-MASK", "\"Not yet, little brother.\"");
                    return;
                }
            }
            Dead = true;
            DeathCause = string.IsNullOrEmpty(hit.Cause) ? (run != null ? run.LastHitBy : "") : hit.Cause;
            Weapon?.Cancel();
            Game.I.OnPlayerDied();
        }

        void Revive(float frac, string title, string sub)
        {
            Dead = false;
            HP = Mathf.Max(1f, MaxHP * frac);
            iframes = 2.2f;
            Game.I.Audio.Play("defy");
            Game.I.Hitstop(0.25f);
            Game.I.Cam.Shake(0.6f);
            Vfx.Ring(Pos, 4f, Pal.Narfi, 0.6f);
            Vfx.Flash(Center, 3f, Pal.Narfi, 0.5f);
            Vfx.Motes(Center, 24, Pal.Narfi, 1f, 3f, 1.2f);
            foreach (var e in Combat.InRadius(Pos, 4.5f, Team.Enemy))
                if (!e.KnockImmune) e.Knock += (e.Pos - Pos).Norm(Vector2.up) * 14f;
            Game.I.UI.Banner(title, sub, Pal.Narfi, 1.6f);
        }

        void UpdateAim(GameInput inp)
        {
            if (inp.MouseAim)
            {
                // aim from the body to the cursor, both measured at chest height (true to what the player sees)
                var w = Game.I.Cam.ScreenToWorldAt(inp.MouseScreen, AimHeight);
                var d = w - Pos;
                if (d.sqrMagnitude > 0.04f) Aim = d.normalized;
            }
            else if (inp.Stick2.sqrMagnitude > 0.2f) Aim = inp.Stick2.normalized;
            else if (inp.Move.sqrMagnitude > 0.1f) Aim = inp.Move.normalized;
        }

        void TryDash(Vector2 dir)
        {
            if (DashCharges <= 0) { dashBuffer = 0.14f; return; }
            if (dir.sqrMagnitude < 0.1f) dir = Facing;
            DashCharges--;
            dashRecharge = S.DashRecharge;
            dashT = 0.17f;
            dashSpeed = 23f;
            sidestep = false;
            dashDir = dir.normalized;
            dashFrom = Pos;
            dashBuffer = 0f;
            lungeT = 0f;
            Weapon?.Cancel();
            Facing = dashDir;
            Game.I.Audio.Play("dash", 0.7f);
            Vfx.Dust(Pos, 5, World.Biome.Fog, 0.7f);
            RunState.Current?.Boons.OnDashStart(Pos, dashDir);
        }

        /// <summary>
        /// Sidestep ("pounce"): a short, very fast hop to the side of where Narfi is aiming, with i-frames.
        /// It has its own short cooldown and does not spend dash charges, so it is the tool for dodging
        /// a swing without giving up position. Moving left/right picks the side; standing still alternates.
        /// </summary>
        void TrySidestep(Vector2 move)
        {
            if (sideCd > 0f || Dashing) return;
            var aim = Aim.sqrMagnitude > 0.01f ? Aim.normalized : Facing.normalized;
            var left = new Vector2(-aim.y, aim.x);
            int side;
            if (move.sqrMagnitude > 0.1f && Mathf.Abs(Vector2.Dot(move.normalized, left)) > 0.2f) side = Vector2.Dot(move, left) > 0f ? 1 : -1;
            else { side = sideFlip; sideFlip = -sideFlip; }
            sideCd = 0.55f;
            sidestep = true;
            dashT = 0.13f;
            dashSpeed = 19f;
            dashDir = left * side;
            dashFrom = Pos;
            lungeT = 0f;
            Weapon?.Cancel();
            Facing = aim;
            Game.I.Audio.Play("dash", 0.55f, 1.25f);
            Vfx.Dust(Pos, 4, World.Biome.Fog, 0.6f);
            Model?.PlayOnce(side > 0 ? "Dodge_Left" : "Dodge_Right", 0.22f);
        }

        void TryCast()
        {
            if (castLock > 0f) return;
            if (CastAmmo <= 0)
            {
                Game.I.Audio.Play("ui_deny", 0.6f);
                Vfx.Text(Pos + new Vector2(0f, VisualHeight + 0.3f), "No runes!", Pal.Dim, 0.7f);
                return;
            }
            CastAmmo--;
            castLock = 0.2f;
            castAnim = 0.32f;
            var run = RunState.Current;
            God g = run != null ? run.Boons.CastGod : God.None;
            Color col = g == God.None ? Pal.Narfi : Pal.God(g);
            Vector2 dir = AttackDir();
            Vector2 from = Pos + dir * 0.4f;
            Facing = dir;
            Game.I.Audio.Play("cast", 0.7f);
            Vfx.Flash(Center + dir * 0.5f, 0.6f, col, 0.15f);
            if (g == God.Odin)
            {
                var h = HitInfo.Of(run.Boons.V("odin_cast"), HitKind.Cast, this, dir);
                h.CritBonus = 0.3f;
                var spec = new ProjSpec
                {
                    Sprite = SpriteLib.Feather, Scale = 1.8f, Radius = 0.45f, Life = 1.3f, Pierce = 2, Homing = 7f, Trail = col.A(0.5f), HitObstacles = false
                };
                spec.OnEnd = pr => RuneStone.Drop(World.Grid != null ? World.Grid.NearestFloor(pr.Pos, 0.3f) : pr.Pos, null, col);
                Projectile.Spawn(spec, from, dir * 15f, h, Team.Enemy, this);
                Game.I.Audio.Play("raven", 0.5f);
                return;
            }
            bool landedOnFoe = false;
            var hit = HitInfo.Of(30f, HitKind.Cast, this, dir, 2f);
            var rs = new ProjSpec { Sprite = SpriteLib.RuneStone, Color = col, Scale = 0.9f, Radius = 0.35f, Life = 0.45f, Trail = col.A(0.55f) };
            rs.OnHit = (pr, e) =>
            {
                landedOnFoe = true;
                Game.I.Audio.Play("cast_land", 0.8f);
                RuneStone.Drop(e.Pos, e, col);
                run?.Boons.OnCastImpact(e.Pos);
            };
            rs.OnEnd = pr =>
            {
                if (landedOnFoe) return;
                var p = World.Grid != null ? World.Grid.NearestFloor(pr.Pos, 0.3f) : pr.Pos;
                Game.I.Audio.Play("cast_land", 0.6f);
                RuneStone.Drop(p, null, col);
                run?.Boons.OnCastImpact(pr.Pos);
            };
            Projectile.Spawn(rs, from, dir * 18f, hit, Team.Enemy, this);
        }

        void TryCall()
        {
            var run = RunState.Current;
            if (run == null) return;
            if (!run.Boons.HasCall)
            {
                if (Fury >= 100f && Time.time - noGodT > 3f) { noGodT = Time.time; Vfx.Text(Pos + new Vector2(0f, VisualHeight + 0.3f), "No god answers...", Pal.Dim, 0.7f); }
                return;
            }
            if (Fury < 100f)
            {
                Game.I.Audio.Play("ui_deny", 0.5f);
                return;
            }
            Fury = 0f;
            run.Boons.Call();
        }

        void Update()
        {
            if (Game.I == null || Game.I.Paused) return;
            float dt = Time.deltaTime;
            if (Dead) { UpdateVisual(dt, Vector2.zero); return; }
            var inp = Game.I.Input;
            TickStatus(dt);
            iframes = Mathf.Max(0f, iframes - dt);
            castLock -= dt;
            castAnim = Mathf.Max(0f, castAnim - dt);
            sideCd = Mathf.Max(0f, sideCd - dt);
            if (RunState.Current != null) { var g = RunState.Current.Boons.SlotGod(BoonSlot.Attack); trailColor = g == God.None ? new Color(0.82f, 1f, 0.92f) : Pal.God(g); }
            SinceDash += dt;
            dashBuffer -= dt;

            if (requireRelease && !inp.AnyActionHeld) requireRelease = false;
            bool canAct = Controllable && !requireRelease && St.Stun <= 0f;
            UpdateAim(inp);

            if (DashCharges < S.DashCharges && !Dashing)
            {
                dashRecharge -= dt;
                if (dashRecharge <= 0f)
                {
                    DashCharges++;
                    dashRecharge = S.DashRecharge;
                    if (dashBuffer > 0f && canAct) TryDash(inp.Move);
                }
            }

            Vector2 vel;
            if (Dashing)
            {
                dashT -= dt;
                vel = dashDir * dashSpeed;
                Physics(dt, vel * dt);
                afterT -= dt;
                if (afterT <= 0f)
                {
                    afterT = 0.04f;
                    Afterimage(Pal.Narfi.A(0.45f), 0.22f);
                }
                if (dashT <= 0f)
                {
                    SinceDash = 0f;
                    moveVel = dashDir * S.MoveSpeed * 0.6f;
                    if (!sidestep) RunState.Current?.Boons.OnDashEnd(dashFrom, Pos);
                    sidestep = false;
                }
            }
            else
            {
                Vector2 mv = canAct ? inp.Move : Vector2.zero;
                float spd = S.MoveSpeed * (Weapon != null ? Weapon.MoveMult : 1f) * St.SpeedMult;
                moveVel = U.Damp(moveVel, mv * spd, 16f, dt);
                Vector2 disp = moveVel * dt;
                if (lungeT > 0f)
                {
                    lungeT -= dt;
                    disp += lungeVel * dt;
                }
                Physics(dt, disp);
                vel = moveVel;
                if (Weapon != null && Weapon.Busy) Facing = Aim;
                else if (mv.sqrMagnitude > 0.05f) Facing = mv;
                else if (inp.MouseAim) Facing = Aim;

                if (mv.sqrMagnitude > 0.2f)
                {
                    stepT -= dt;
                    if (stepT <= 0f) { stepT = 0.28f; Vfx.Dust(Pos, 1, World.Biome.Fog, 0.4f); }
                }
            }

            if (canAct)
            {
                if (inp.DashDown) TryDash(inp.Move);
                if (inp.SidestepDown) TrySidestep(inp.Move);
                Weapon?.Tick(dt, inp, !Dashing);
                if (inp.CastDown && !Dashing) TryCast();
                if (inp.CallDown) TryCall();
                if (inp.InteractDown && Focus != null && !Dashing) Focus.Use();
            }
            else Weapon?.Tick(dt, inp, false);

            Focus = Interactable.Nearest(Pos);
            RunState.Current?.Boons.Tick(dt);

            UpdateVisual(dt, vel);
            UpdateWeaponVisual();
            float flick = iframes > 0f && !Dashing ? (Mathf.Sin(Time.time * 40f) > 0f ? 0.45f : 1f) : 1f;
            SetAlpha(flick);
            soulLight.color = Color.Lerp(Color.white, Pal.Narfi, 0.35f).A(0.6f + 0.05f * Mathf.Sin(Time.time * 2f));
        }

        void UpdateWeaponVisual()
        {
            if (WeaponSR == null) return;
            float side = Facing.x < -0.05f ? -1f : 1f;
            float ang;
            if (WeaponSwinging) ang = WeaponAngle;
            else
            {
                var elbow = Rig != null ? Rig.Bone("foreN") : null;
                if (elbow != null)
                {
                    var dir = Rig.HandWorld - new Vector2(elbow.position.x, elbow.position.y);
                    ang = dir.Ang() + side * 28f;
                }
                else ang = side > 0 ? -55f : -125f;
            }
            ang = Mathf.Repeat(ang + 180f, 360f) - 180f;
            if (Model != null)
            {
                var hand = Model.HandWorld;
                WeaponPivot.position = new Vector3(hand.x, hand.y, 0f);
            }
            else if (Rig != null)
            {
                var hand = Rig.HandWorld;
                WeaponPivot.position = new Vector3(hand.x, hand.y, 0f);
            }
            else WeaponPivot.localPosition = new Vector3(side * 0.18f, 0.55f + Visual.localPosition.y, 0f);
            WeaponPivot.rotation = Quaternion.Euler(0f, 0f, ang);
            WeaponSR.flipY = ang > 90f || ang < -90f;
            WeaponSR.transform.localPosition = new Vector3(WeaponExtend, 0f, 0f);
            bool behind = Mathf.Sin(ang * Mathf.Deg2Rad) > 0.35f;
            WeaponSR.sortingOrder = Body.sortingOrder + (behind ? -5 : 3);
            WeaponSR.enabled = !Dead && !WeaponHidden && Model == null;
            WeaponSR.color = new Color(1f, 1f, 1f, Body.color.a);
            trail?.Feed(WeaponSwinging && !WeaponHidden, WeaponPivot.position, WeaponPivot.position + WeaponPivot.right * WeaponReach, trailColor);
        }

        /// <summary>A melee step started: play a matching 3D swing whose contact frame lands on the strike.</summary>
        public void OnSwingBegin(Step s, float speed)
        {
            if (Model == null) return;
            float windup = s.Windup / speed, total = (s.Windup + s.Active + s.Recover * 0.8f) / speed;
            string clip;
            if (s.Shape == 2) clip = "2H_Melee_Attack_Spinning";
            else if (s.Shape == 1) clip = "1H_Melee_Attack_Stab";
            else if (s.Heavy) clip = "2H_Melee_Attack_Chop";
            else clip = s.Flip ? "1H_Melee_Attack_Slice_Horizontal" : "1H_Melee_Attack_Slice_Diagonal";
            Model.SwingTimed(clip, windup, total);
        }

        protected override float RigMaxSpeed => S.MoveSpeed;

        protected override void FillRig(ref RigInput r)
        {
            if (Dashing && sidestep) return;   // the 3D model plays its own side-dodge clip
            if (Dashing) { r.Action = RigAction.Dash; return; }
            if (Model != null)
            {
                if (castAnim > 0f) { r.Action = RigAction.Throw; r.ActionT = 1f - castAnim / 0.32f; }
                else if (WeaponSwinging && Weapon != null && Weapon.Id == "bow") { r.Action = RigAction.Shoot; r.ActionT = 1f; }
                return;
            }
            if (castAnim > 0f) { r.Action = RigAction.Throw; r.ActionT = 1f - castAnim / 0.32f; }
            if (WeaponSwinging)
            {
                float side = Visual.localScale.x < 0f ? -1f : 1f;
                r.ArmOverride = true;
                r.AimLocal = side > 0 ? WeaponAngle : 180f - WeaponAngle;
            }
        }

        protected override void OnSlam(float force) { }
    }
}
