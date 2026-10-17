using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Anything that can be hit: Narfi, the dead, bosses. Handles health and armor, knockback,
    /// statuses, hit-flash, y-sorting and procedural squash/bob animation.
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        public static readonly List<Entity> All = new List<Entity>();

        public Team Team;
        public string Id = "", DisplayName = "";
        public float MaxHP = 10f, HP = 10f, Armor, MaxArmor;
        public float Radius = 0.42f, Mass = 1f;
        public bool Flying, Dead, IsBoss, Elite;
        public Vector2 Knock;
        public Vector2 Facing = Vector2.right;
        public float VisualHeight = 1.4f;
        public readonly Status St = new Status();
        public float LastHitTime = -99f;

        public Transform Visual;
        public SpriteRenderer Body, ShadowR;
        public Rig Rig;
        public Model3D Model;
        Transform overlay;
        protected float hurtT, deadT;
        protected float flash;
        protected Color flashColor = Color.white;
        protected float squash;
        protected float bobT;
        protected float baseScale = 1f;
        protected float lift;
        MaterialPropertyBlock mpb;
        SpriteRenderer barBg, barFill, barArmor, statusIcon;
        float poisonTick;
        float barShow;

        public Vector2 Pos
        {
            get { var p = transform.position; return new Vector2(p.x, p.y); }
            set { transform.position = new Vector3(value.x, value.y, 0f); }
        }

        public Vector2 Center => Pos + new Vector2(0f, VisualHeight * 0.45f);
        public virtual bool Targetable => !Dead;
        public virtual bool Invulnerable => false;
        public virtual bool KnockImmune => Armor > 0f || Mass >= 6f;

        protected virtual void OnEnable() { All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        /// <summary>Build an animated cut-out rig for this character (falls back to a still sprite).</summary>
        protected void BuildRig(string artId, float shadowSize, bool healthBar)
        {
            Visual = new GameObject("visual").transform;
            Visual.SetParent(transform, false);
            Model = Model3D.Build(Visual, artId);
            if (Model != null)
            {
                Body = Mats.SR(Visual, "body", null, 0);
                VisualHeight = Model.Spec.Height * 0.9f;
            }
            else
            {
                Rig = Rig.Build(Visual, artId);
                Body = Rig.Main;
                View3D.Stand(Visual);
            }
            BuildCommon(shadowSize, healthBar);
        }

        protected void BuildVisual(Sprite body, float shadowSize, bool healthBar)
        {
            Visual = new GameObject("visual").transform;
            Visual.SetParent(transform, false);
            Body = Mats.SR(Visual, "body", body, 0);
            View3D.Stand(Visual);
            BuildCommon(shadowSize, healthBar);
        }

        public void SetAlpha(float a)
        {
            if (Model != null)
            {
                Body.color = new Color(1f, 1f, 1f, a);
                Model.SetVisible(Body.enabled && a > 0.5f);
            }
            else if (Rig != null) Rig.SetColor(new Color(1f, 1f, 1f, a));
            else if (Body != null) Body.color = new Color(1f, 1f, 1f, a);
        }

        public void SetVisible(bool v)
        {
            if (Model != null)
            {
                Body.enabled = v;
                Model.SetVisible(v && Body.color.a > 0.5f);
            }
            else if (Rig != null) Rig.SetVisible(v);
            else if (Body != null) Body.enabled = v;
        }

        public bool Visible => Body != null && Body.enabled;

        public void Afterimage(Color c, float life)
        {
            if (Model != null) Model.Afterimage(c, life);
            else if (Rig != null) Rig.Afterimage(c, life);
            else Vfx.Afterimage(Body, c, life);
        }

        /// <summary>Override to tell the rig what this character is doing.</summary>
        protected virtual void FillRig(ref RigInput r) { }
        protected virtual float RigMaxSpeed => 3f;

        void BuildCommon(float shadowSize, bool healthBar)
        {
            ShadowR = Mats.SR(transform, "shadow", SpriteLib.Shadow, Sort.Shadow);
            ShadowR.transform.localScale = new Vector3(shadowSize, shadowSize, 1f);
            mpb = new MaterialPropertyBlock();
            if (healthBar)
            {
                overlay = View3D.UprightChild(transform, "overlay");
                barBg = Mats.SR(overlay, "barbg", SpriteLib.Square, Sort.Fx - 5, new Color(0, 0, 0, 0.75f));
                barFill = Mats.SR(overlay, "bar", SpriteLib.Square, Sort.Fx - 4, Pal.Blood);
                barArmor = Mats.SR(overlay, "bararmor", SpriteLib.Square, Sort.Fx - 3, Pal.Armor);
                SetBarVisible(false);
            }
            if (overlay == null) overlay = View3D.UprightChild(transform, "overlay");
            statusIcon = Mats.SR(overlay, "status", Runes.Sprite(Runes.Tiwaz), Sort.Fx - 2, true);
            statusIcon.sharedMaterial = Mats.AdditiveTop;
            if (barBg != null) barBg.sharedMaterial = barFill.sharedMaterial = barArmor.sharedMaterial = Mats.SpriteTop;
            statusIcon.enabled = false;
        }

        float BarY => Model != null ? Model.Spec.Height * 0.68f + 0.35f : VisualHeight * baseScale + 0.25f;

        void SetBarVisible(bool v)
        {
            if (barBg == null) return;
            barBg.enabled = barFill.enabled = v;
            barArmor.enabled = v && Armor > 0f;
        }

        public void Flash(float amount = 1f, Color? c = null)
        {
            flash = Mathf.Max(flash, amount);
            flashColor = c ?? Color.white;
        }

        public void Squash(float amount) => squash = amount;

        /// <summary>Apply damage after all modifiers. Returns damage dealt, or -1 when ignored.</summary>
        public virtual float TakeHit(HitInfo hit)
        {
            if (Dead || Invulnerable) return -1f;
            float dmg = hit.Damage * DamageTakenMult(hit);
            if (dmg <= 0f) return 0f;
            float toHp = dmg;
            bool armorHit = false;
            if (Armor > 0f)
            {
                float a = Mathf.Min(Armor, dmg);
                Armor -= a;
                toHp = dmg - a;
                armorHit = true;
                if (Armor <= 0f) OnArmorBreak();
            }
            HP -= toHp;
            LastHitTime = Time.time;
            Flash(1f, armorHit ? Pal.Armor : Color.white);
            squash = Rig != null ? 0.1f : 0.2f;
            hurtT = 0.28f;
            barShow = 3f;
            if (hit.Knock > 0f && !KnockImmune) Knock += hit.Dir.Norm(Vector2.zero) * (hit.Knock / Mathf.Max(0.5f, Mass));
            if (!hit.Silent) Vfx.Damage(Pos + new Vector2(0f, VisualHeight + 0.2f), dmg, hit.Crit, Team == Team.Player);
            OnHurt(hit, dmg);
            if (HP <= 0f)
            {
                HP = 0f;
                Die(hit);
            }
            return dmg;
        }

        protected virtual float DamageTakenMult(in HitInfo hit) => 1f;
        protected virtual void OnHurt(HitInfo hit, float dmg) { }

        protected virtual void OnArmorBreak()
        {
            Game.I.Audio.Play("armor", 0.9f, 0.8f);
            Vfx.Burst(Center, 14, Pal.Armor, 8f, 0.45f, 0.4f);
            Vfx.Text(Pos + new Vector2(0, VisualHeight + 0.6f), "ARMOR BROKEN", Pal.Armor, 0.7f);
        }

        public abstract void Die(HitInfo hit);

        public void Heal(float amount)
        {
            if (Dead) return;
            HP = Mathf.Min(MaxHP, HP + amount);
        }

        /// <summary>Move by a displacement plus knockback, sliding against the room.</summary>
        protected void Physics(float dt, Vector2 displacement)
        {
            Vector2 total = displacement + Knock * dt;
            float kmag = Knock.magnitude;
            Knock *= Mathf.Exp(-9f * dt);
            if (Knock.sqrMagnitude < 0.01f) Knock = Vector2.zero;
            if (World.Grid != null)
            {
                Pos = World.Grid.Move(Pos, total, Radius, Flying, out bool blocked);
                if (blocked && kmag > 7f) { OnSlam(kmag); Knock = Vector2.zero; }
            }
            else Pos += total;
        }

        protected virtual void OnSlam(float force) { }

        protected void TickStatus(float dt)
        {
            var s = St;
            if (s.Chill > 0)
            {
                s.ChillHold -= dt;
                if (s.ChillHold <= 0f)
                {
                    s.ChillDecay += dt;
                    if (s.ChillDecay > 0.9f) { s.ChillDecay = 0f; s.Chill--; }
                }
            }
            s.Enthralled = Mathf.Max(0f, s.Enthralled - dt);
            s.Charmed = Mathf.Max(0f, s.Charmed - dt);
            s.Marked = Mathf.Max(0f, s.Marked - dt);
            s.Stun = Mathf.Max(0f, s.Stun - dt);
            s.Frozen = Mathf.Max(0f, s.Frozen - dt);
            s.Slowed = Mathf.Max(0f, s.Slowed - dt);
            if (s.VerdictT > 0f)
            {
                s.VerdictT -= dt;
                if (s.VerdictT <= 0f)
                {
                    s.VerdictT = -1f;
                    RunState.Current?.Boons.ResolveVerdict(this);
                }
            }
            if (s.Poison > 0f)
            {
                s.Poison -= dt;
                poisonTick -= dt;
                if (poisonTick <= 0f)
                {
                    poisonTick = 0.5f;
                    var h = new HitInfo { Damage = s.PoisonDps * 0.5f, Kind = HitKind.Hazard, Cause = "Venom", Dir = Vector2.zero };
                    Combat.Deal(this, h);
                    Vfx.Motes(Center, 3, Pal.Venom, 0.4f, 1f, 0.6f);
                }
            }
        }

        protected void UpdateVisual(float dt, Vector2 vel)
        {
            if (Visual == null) return;
            int order = Sort.Y(Pos.y);
            if (Rig != null) Rig.SetOrder(order);
            else Body.sortingOrder = order;
            if (ShadowR != null) ShadowR.enabled = lift < 3f && Visible;

            flash = Mathf.Max(0f, flash - dt * 7f);
            hurtT = Mathf.Max(0f, hurtT - dt);
            if (Dead) deadT += dt;
            if (Rig != null) mpb.Clear(); else Body.GetPropertyBlock(mpb);
            float fl = flash;
            Color fc = flashColor;
            if (St.Frozen > 0f) { fl = Mathf.Max(fl, 0.55f); fc = Pal.Frost; }
            else if (St.Charmed > 0f) { fl = Mathf.Max(fl, 0.3f + 0.1f * Mathf.Sin(Time.time * 8f)); fc = Pal.God(God.Freyja); }
            else if (St.Chill > 0) { fl = Mathf.Max(fl, St.Chill * 0.035f); fc = Pal.Frost; }
            mpb.SetFloat("_Flash", fl);
            mpb.SetColor("_FlashColor", fc);
            if (Rig != null) Rig.SetBlock(mpb); else Body.SetPropertyBlock(mpb);

            squash = U.Damp(squash, 0f, 10f, dt);
            float speed = vel.magnitude;
            float side = Facing.x < -0.05f ? -1f : 1f;
            float sx = baseScale * (1f + squash), sy = baseScale * (1f - squash * 0.8f);
            if (Model != null)
            {
                Model.SetFlash(fl, fc);
                Visual.localScale = Vector3.one * baseScale;
                Visual.localPosition = new Vector3(0f, 0f, -lift * 0.8f);
                Model.Squash(squash);
                Model.SetVisible(Body.enabled && Body.color.a > 0.5f);
                var ri = new RigInput
                {
                    Speed = St.Frozen > 0f ? 0f : speed, MaxSpeed = RigMaxSpeed, Hurt = hurtT / 0.28f,
                    Dead = Dead, DeadT = deadT, Spawn = 1f
                };
                FillRig(ref ri);
                Model.Animate(ri, Facing, St.Frozen > 0f ? 0f : dt);
            }
            else Visual.localScale = new Vector3(side * sx, sy, 1f);
            if (Model != null) { }
            else if (Rig != null)
            {
                Visual.localPosition = new Vector3(0f, lift, 0f);
                Visual.localRotation = Quaternion.identity;
                var ri = new RigInput
                {
                    Speed = St.Frozen > 0f ? 0f : speed, MaxSpeed = RigMaxSpeed, Hurt = hurtT / 0.28f,
                    Dead = Dead, DeadT = deadT, Spawn = 1f
                };
                FillRig(ref ri);
                Rig.Animate(ri, St.Frozen > 0f ? 0f : dt);
            }
            else
            {
                if (speed > 0.3f) bobT += dt * (6f + speed);
                else bobT = U.Damp(bobT, Mathf.Round(bobT / Mathf.PI) * Mathf.PI, 10f, dt);
                float bob = Mathf.Abs(Mathf.Sin(bobT)) * 0.07f * Mathf.Clamp01(speed / 3f);
                Visual.localPosition = new Vector3(0f, bob + lift, 0f);
                Visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(-vel.x * 1.3f, -8f, 8f));
            }

            if (barBg != null)
            {
                barShow -= dt;
                bool show = !Dead && barShow > 0f && (HP < MaxHP || Armor < MaxArmor);
                SetBarVisible(show);
                if (show)
                {
                    float w = Mathf.Clamp(Radius * 2.4f, 0.8f, 2.2f);
                    float y = BarY;
                    barBg.transform.localPosition = new Vector3(0f, y, 0f);
                    barBg.transform.localScale = new Vector3(w + 0.08f, 0.16f, 1f);
                    float f = Mathf.Clamp01(HP / MaxHP);
                    barFill.transform.localPosition = new Vector3(-w * 0.5f + w * f * 0.5f, y, 0f);
                    barFill.transform.localScale = new Vector3(w * f, 0.1f, 1f);
                    if (MaxArmor > 0f && Armor > 0f)
                    {
                        float af = Mathf.Clamp01(Armor / MaxArmor);
                        barArmor.transform.localPosition = new Vector3(-w * 0.5f + w * af * 0.5f, y + 0.1f, 0f);
                        barArmor.transform.localScale = new Vector3(w * af, 0.06f, 1f);
                    }
                }
            }

            if (statusIcon != null)
            {
                int rune = -1;
                Color c = Color.white;
                if (St.VerdictT > 0f) { rune = Runes.Tiwaz; c = Pal.God(God.Tyr); }
                else if (St.Marked > 0f) { rune = Runes.Ansuz; c = Pal.God(God.Odin); }
                else if (St.Charmed > 0f) { rune = Runes.Fehu; c = Pal.God(God.Freyja); }
                else if (St.Enthralled > 0f) { rune = Runes.Berkano; c = Pal.God(God.Freyja); }
                else if (St.Chill > 0) { rune = Runes.Isa; c = Pal.Frost; }
                statusIcon.enabled = rune >= 0 && !Dead;
                if (rune >= 0)
                {
                    statusIcon.sprite = Runes.Sprite(rune);
                    float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 10f);
                    float sz = 0.45f * pulse * (St.VerdictT > 0f ? 1.3f : 1f);
                    statusIcon.transform.localScale = new Vector3(sz, sz, 1f);
                    statusIcon.transform.localPosition = new Vector3(0f, BarY + 0.4f, 0f);
                    statusIcon.color = c;
                }
            }
        }
    }
}
