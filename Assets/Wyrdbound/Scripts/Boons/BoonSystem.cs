using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class Boon
    {
        public BoonDef Def;
        public Rarity Rarity;
        public int Level = 1;
        public float Value => Def.Value(Rarity, Level);
    }

    public struct BoonOffer
    {
        public BoonDef Def;
        public Rarity Rarity;
        public Boon Replaces;
    }

    /// <summary>
    /// The blessings Narfi carries this escape, and every effect they produce:
    /// lightning, frostbite and shatter, verdicts, ravens, charms and tides.
    /// </summary>
    public sealed class BoonSystem
    {
        public readonly List<Boon> Owned = new List<Boon>();
        readonly Boon[] slots = new Boon[5];
        float ravenT = 3f;
        public float OdinCall;

        static readonly Dictionary<string, string[]> Prereq = new Dictionary<string, string[]>
        {
            { "thor_megin", new[] { "thor_attack", "thor_special", "thor_cast", "thor_dash", "thor_call", "thor_resolve" } },
            { "skadi_shatter", new[] { "skadi_attack", "skadi_special", "skadi_cast", "skadi_dash", "skadi_call" } },
            { "skadi_patience", new[] { "skadi_attack", "skadi_special", "skadi_cast", "skadi_dash", "skadi_call" } },
            { "skadi_snowblind", new[] { "skadi_attack", "skadi_special", "skadi_cast", "skadi_dash", "skadi_call" } },
            { "tyr_swift", new[] { "tyr_attack", "tyr_special", "tyr_law" } },
            { "odin_eye", new[] { "odin_attack", "odin_special", "odin_cast", "odin_lore", "odin_dash" } },
            { "freyja_wiles", new[] { "freyja_attack", "freyja_special", "freyja_dash" } },
            { "njord_riptide", new[] { "njord_attack", "njord_special", "njord_dash", "njord_call" } },
        };

        static Player P => Player.I;
        static RunState Run => RunState.Current;

        public Boon Slot(BoonSlot s) => s == BoonSlot.Passive ? null : slots[(int)s];
        public God SlotGod(BoonSlot s) { var b = Slot(s); return b != null ? b.Def.God : God.None; }

        public Boon Get(string id)
        {
            for (int i = 0; i < Owned.Count; i++) if (Owned[i].Def.Id == id) return Owned[i];
            return null;
        }

        public bool Has(string id) => Get(id) != null;
        public float V(string id) { var b = Get(id); return b != null ? b.Value : 0f; }

        public bool HasGod(God g)
        {
            foreach (var b in Owned) if (b.Def.God == g || b.Def.God2 == g) return true;
            return false;
        }

        public int CountGod(God g)
        {
            int n = 0;
            foreach (var b in Owned) if (b.Def.God == g) n++;
            return n;
        }

        // ------------------------------------------------------------------ acquisition

        public void Add(BoonDef def, Rarity r)
        {
            var b = new Boon { Def = def, Rarity = r, Level = 1 };
            if (def.Slot != BoonSlot.Passive)
            {
                var old = slots[(int)def.Slot];
                if (old != null) Owned.Remove(old);
                slots[(int)def.Slot] = b;
            }
            Owned.Add(b);
            if (def.Id == "njord_wealth") Run.AddSilver(Mathf.RoundToInt(b.Value), false);
            Run.RecalcStats();
            SaveSystem.Data.SetFlag("boon_" + def.Id);
        }

        public void LevelUp(Boon b, int n)
        {
            b.Level += n;
            Run.RecalcStats();
        }

        bool PrereqOk(BoonDef d)
        {
            if (d.IsDuo)
            {
                bool a = false, b = false;
                foreach (var id in d.ReqA) if (Has(id)) { a = true; break; }
                foreach (var id in d.ReqB) if (Has(id)) { b = true; break; }
                return a && b;
            }
            if (Prereq.TryGetValue(d.Id, out var req))
            {
                foreach (var id in req) if (Has(id)) return true;
                return false;
            }
            return true;
        }

        public List<BoonOffer> RollOffers(God god, Rng rng, int count, float rarityBonus, bool minRare)
        {
            var pool = new List<BoonDef>();
            foreach (var d in BoonDefs.All)
            {
                bool mine = d.IsDuo ? (d.God == god || d.God2 == god) : d.God == god;
                if (!mine || Has(d.Id) || !PrereqOk(d)) continue;
                pool.Add(d);
            }
            var result = new List<BoonOffer>();
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                var d = rng.Weighted(pool, x =>
                {
                    if (x.IsDuo) return 2.5f;
                    if (x.Slot == BoonSlot.Call) return Slot(BoonSlot.Call) == null ? 0.9f : 0.4f;
                    if (x.Slot != BoonSlot.Passive) return Slot(x.Slot) == null ? 1.8f : 0.6f;
                    return 1f;
                });
                pool.Remove(d);
                Rarity r = Rarity.Common;
                if (d.IsDuo) r = Rarity.Duo;
                else
                {
                    float roll = rng.Value;
                    if (roll < 0.02f + rarityBonus * 0.25f) r = Rarity.Heroic;
                    else if (roll < 0.08f + rarityBonus * 0.6f) r = Rarity.Epic;
                    else if (roll < 0.28f + rarityBonus) r = Rarity.Rare;
                    if (minRare && r == Rarity.Common) r = Rarity.Rare;
                }
                result.Add(new BoonOffer { Def = d, Rarity = r, Replaces = d.Slot != BoonSlot.Passive ? Slot(d.Slot) : null });
            }
            return result;
        }

        public List<Boon> Upgradeable(Rng rng, int count)
        {
            var list = new List<Boon>();
            foreach (var b in Owned) if (b.Def.PerLevel > 0f) list.Add(b);
            rng.Shuffle(list);
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        public void ApplyStats(PlayerStats s)
        {
            s.MoveSpeed *= 1f + V("rat_paws");
            s.AttackSpeed += V("rat_teeth");
            if (Has("rat_climb")) { s.DashCharges += 1; s.DashRecharge /= 1f + V("rat_climb"); }
            s.ShopDiscount += V("rat_gossip");
            if (Has("rat_acorn")) { s.CastMax += 1; s.Magnet = Mathf.Max(s.Magnet, V("rat_acorn")); }
            s.Dodge += V("rat_slip");
            s.MaxHP += V("njord_calm");
            if (Has("tyr_hand")) { s.MaxHP *= 0.8f; s.DmgMult += V("tyr_hand"); }
            s.CritChance += V("odin_lore");
            s.CritMult += V("odin_eye");
            if (Has("njord_wealth")) s.SilverMult += 0.2f;
        }

        // ------------------------------------------------------------------ the damage pipeline

        float SlotPct(BoonSlot slot)
        {
            var b = Slot(slot);
            if (b == null) return 0f;
            switch (b.Def.God)
            {
                case God.Skadi:
                case God.Odin:
                case God.Freyja:
                case God.Njord:
                    return b.Value;
            }
            return 0f;
        }

        public void ModifyOutgoing(ref HitInfo h, Entity t)
        {
            var S = Run.Stats;
            float add = S.DmgMult - 1f;
            if (h.Kind == HitKind.Attack) add += SlotPct(BoonSlot.Attack);
            if (h.Kind == HitKind.Special) add += SlotPct(BoonSlot.Special);
            if (t.St.Chill > 0) add += V("skadi_patience");
            if (t.Armor > 0f || t.IsBoss) add += V("thor_bane");
            if (S.BossDmg > 0f && (t.IsBoss || t.Armor > 0f || t.Elite)) add += S.BossDmg;
            if (S.FirstStrike > 0f && t.HP >= t.MaxHP - 0.01f && t.Armor >= t.MaxArmor - 0.01f) add += S.FirstStrike;
            if (h.God == God.Thor)
            {
                add += V("thor_megin");
                if (Has("duo_thor_skadi") && t.St.Chill > 0) add += 0.5f;
            }
            if (h.Cause == "Verdict")
            {
                add += V("tyr_swift");
                if (Has("duo_tyr_freyja") && t.St.Enthralled > 0f) add += 0.4f;
            }
            h.Damage *= Mathf.Max(0.1f, 1f + add);

            if (h.Kind == HitKind.Attack && SlotGod(BoonSlot.Attack) == God.Njord) h.Knock += 9f;
            if (h.Kind == HitKind.Special && SlotGod(BoonSlot.Special) == God.Njord) h.Knock += 15f;

            bool canCrit = !h.NoCrit && (h.Kind != HitKind.Effect || h.God == God.Odin || (h.God == God.Thor && Has("duo_thor_odin")));
            if (!canCrit) return;
            float c = S.CritChance + h.CritBonus;
            if (h.Kind == HitKind.Attack && SlotGod(BoonSlot.Attack) == God.Odin) c += 0.15f;
            if (h.Kind == HitKind.Special && SlotGod(BoonSlot.Special) == God.Odin) c += 0.2f;
            if (h.God == God.Thor) c += 0.15f;
            if (Has("duo_odin_freyja") && (t.St.Enthralled > 0f || t.St.Charmed > 0f)) c += 0.2f;
            bool marked = t.St.Marked > 0f;
            if (OdinCall > 0f || marked || Random.value < c)
            {
                h.Crit = true;
                float mult = S.CritMult;
                if (marked) { mult += V("odin_dash"); t.St.Marked = 0f; }
                h.Damage *= mult;
            }
        }

        public void AfterHit(HitInfo h, Entity t, float dealt)
        {
            if (h.Crit)
            {
                Game.I.Audio.Play("crit", 0.8f);
                Vfx.Flash(t.Center, 1.1f, Pal.God(God.Odin), 0.2f);
            }
            if (h.NoProc) return;
            if (h.Kind == HitKind.Attack) GodEffect(Slot(BoonSlot.Attack), t, false);
            else if (h.Kind == HitKind.Special) GodEffect(Slot(BoonSlot.Special), t, true);
            if (!t.Dead && t.St.Enthralled > 0f && Has("freyja_wiles") && Random.value < V("freyja_wiles")) Charm(t, 3f);
            if (!t.Dead && Has("duo_njord_tyr") && h.Knock >= 8f) ApplyVerdict(t, 60f);
        }

        void GodEffect(Boon b, Entity t, bool special)
        {
            if (b == null) return;
            switch (b.Def.God)
            {
                case God.Thor:
                    if (special) ChainLightning(t, b.Value, 4); else Lightning(t, b.Value);
                    break;
                case God.Skadi: Chill(t, special ? 2 : 1); break;
                case God.Tyr: ApplyVerdict(t, b.Value); break;
                case God.Freyja: Enthrall(t, 4f); break;
            }
        }

        // ------------------------------------------------------------------ elemental effects

        public void Lightning(Entity t, float dmg)
        {
            if (t == null || t.Dead) return;
            Color c = Pal.God(God.Thor);
            Vfx.Bolt(t.Pos + new Vector2(Random.Range(-1.2f, 1.2f), 9f), t.Pos + new Vector2(0f, 0.3f), c, 0.25f, 0.2f);
            Vfx.Light(t.Pos, 2.5f, c.A(0.5f), 0.3f);
            Vfx.Burst(t.Pos, 6, c, 6f, 0.25f, 0.3f);
            Game.I.Audio.Play("zap", 0.55f);
            Combat.Deal(t, HitInfo.Effect(dmg, God.Thor, Vector2.zero));
            if (Has("duo_thor_skadi") && !t.Dead) Chill(t, 2);
        }

        public void ChainLightning(Entity first, float dmg, int bounces)
        {
            if (first == null) return;
            Color c = Pal.God(God.Thor);
            var hit = new HashSet<Entity> { first };
            Entity cur = first;
            Vfx.Flash(first.Center, 0.8f, c);
            Combat.Deal(first, HitInfo.Effect(dmg, God.Thor, Vector2.zero));
            for (int i = 0; i < bounces; i++)
            {
                var next = Combat.Nearest(cur.Pos, Team.Enemy, 5.5f, null, hit);
                if (next == null) break;
                Vfx.Bolt(cur.Center, next.Center, c, 0.2f, 0.12f);
                Combat.Deal(next, HitInfo.Effect(dmg, God.Thor, Vector2.zero));
                if (Has("duo_thor_skadi") && !next.Dead) Chill(next, 2);
                hit.Add(next);
                cur = next;
            }
            Game.I.Audio.Play("zap", 0.6f, 1.2f);
        }

        public void Chill(Entity t, int n)
        {
            if (t == null || t.Dead) return;
            t.St.Chill = Mathf.Min(10, t.St.Chill + n);
            t.St.ChillHold = 4f;
            t.St.ChillDecay = 0f;
            Vfx.Motes(t.Center, 2, Pal.Frost, 0.4f, 0.8f, 0.5f);
            if (t.St.Chill >= 10) Shatter(t);
        }

        public void Shatter(Entity t)
        {
            t.St.Chill = 0;
            float dmg = 50f + V("skadi_shatter");
            float r = 1.8f * (Has("duo_skadi_njord") ? 2f : 1f);
            Vfx.Ring(t.Pos, r, Pal.Frost, 0.4f);
            Vfx.Burst(t.Center, 18, Pal.Frost, 10f, 0.45f, 0.45f);
            for (int i = 0; i < 8; i++)
            {
                var d = U.Dir(i * 45f + Random.Range(-15f, 15f));
                Vfx.Spawn(SpriteLib.Shard, t.Center, d * Random.Range(5f, 9f), 0.4f, 0.6f, 0.3f, Color.white, Pal.Frost.A(0f), false, d.Ang(), 0f, 3f, Sort.Fx);
            }
            Vfx.Text(t.Pos + new Vector2(0f, t.VisualHeight + 0.5f), "SHATTER", Pal.Frost, 0.8f);
            Game.I.Audio.Play("frost", 1f, 0.9f);
            Game.I.Cam.Shake(0.2f);
            Combat.HitCircle(t.Pos, r, Team.Enemy, HitInfo.Effect(dmg, God.Skadi, Vector2.zero));
        }

        public void ApplyVerdict(Entity t, float dmg)
        {
            if (t == null || t.Dead || t.St.VerdictT > 0f) return;
            t.St.VerdictT = Has("tyr_swift") ? 0.77f : 1.1f;
            t.St.VerdictDmg = dmg;
            Vfx.Ring(t.Pos, 0.9f, Pal.God(God.Tyr), 0.3f, 1.4f);
        }

        public void ResolveVerdict(Entity t)
        {
            if (t == null || t.Dead) return;
            Color c = Pal.God(God.Tyr);
            var h = HitInfo.Effect(t.St.VerdictDmg, God.Tyr, Vector2.zero);
            h.Cause = "Verdict";
            Vfx.Slash(t.Center, 45f, 1.4f, 90, c, 0.22f);
            Vfx.Slash(t.Center, 135f, 1.4f, 90, c, 0.22f, true);
            Vfx.Flash(t.Center, 1.3f, c, 0.2f);
            Game.I.Audio.Play("verdict", 0.8f);
            Game.I.Cam.Shake(0.12f);
            Combat.Deal(t, h);
            if (Has("duo_tyr_freyja")) Enthrall(t, 4f);
            if (Has("duo_thor_tyr")) Lightning(t, 40f);
        }

        public void Enthrall(Entity t, float secs)
        {
            if (t == null || t.Dead) return;
            if (t.St.Enthralled <= 0f) Vfx.Motes(t.Center, 4, Pal.God(God.Freyja), 0.5f, 1.4f, 0.8f);
            t.St.Enthralled = Mathf.Max(t.St.Enthralled, secs);
        }

        public void Charm(Entity t, float secs)
        {
            if (t == null || t.Dead || t.IsBoss) return;
            if (Has("duo_skadi_freyja")) { secs += 2f; Chill(t, 4); }
            if (t.Dead) return;
            t.St.Charmed = Mathf.Max(t.St.Charmed, secs);
            Vfx.Ring(t.Pos, 1f, Pal.God(God.Freyja), 0.35f);
            Vfx.Motes(t.Center, 8, Pal.God(God.Freyja), 0.6f, 1.6f, 1f);
            Game.I.Audio.Play("charm", 0.5f);
        }

        public void Mark(Entity t)
        {
            if (t == null || t.Dead) return;
            t.St.Marked = 6f;
        }

        // ------------------------------------------------------------------ dash / cast / call

        public void OnDashStart(Vector2 from, Vector2 dir)
        {
            var b = Slot(BoonSlot.Dash);
            if (b == null) return;
            Color c = Pal.God(b.Def.God);
            switch (b.Def.God)
            {
                case God.Thor:
                    {
                        var e = Combat.Nearest(from, Team.Enemy, 6.5f);
                        if (e != null) Lightning(e, b.Value);
                        break;
                    }
                case God.Tyr:
                    {
                        var z = Zone.Spawn(from, 1.5f, 1.6f, 0.25f, HitInfo.Effect(b.Value, God.Tyr, Vector2.zero), Team.Enemy, c, null, Runes.Tiwaz);
                        z.Spinning = true;
                        break;
                    }
                case God.Njord:
                    Vfx.Ring(from, 2.4f, c, 0.35f);
                    Game.I.Audio.Play("wave", 0.5f, 1.4f);
                    Combat.HitCircle(from, 2.4f, Team.Enemy, HitInfo.Effect(b.Value, God.Njord, Vector2.zero, 10f));
                    break;
            }
        }

        public void OnDashEnd(Vector2 from, Vector2 to)
        {
            var b = Slot(BoonSlot.Dash);
            if (b == null) return;
            Color c = Pal.God(b.Def.God);
            switch (b.Def.God)
            {
                case God.Skadi:
                    {
                        int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / 0.9f));
                        for (int i = 0; i <= n; i++)
                            Zone.Spawn(Vector2.Lerp(from, to, i / (float)n), 0.8f, 2.5f, 0.4f, HitInfo.Effect(b.Value, God.Skadi, Vector2.zero), Team.Enemy, Pal.Frost, e => Chill(e, 1));
                        break;
                    }
                case God.Odin:
                    foreach (var e in Combat.InRadius(to, 2.6f, Team.Enemy)) { Mark(e); Vfx.Flash(e.Center, 0.6f, c); }
                    for (int i = 0; i < 6; i++)
                    {
                        var d = U.Dir(i * 60f);
                        Vfx.Spawn(SpriteLib.Feather, to, d * 4f, 0.5f, 0.6f, 0.3f, Color.white, Color.white.A(0f), false, d.Ang(), 200f, 3f);
                    }
                    break;
                case God.Freyja:
                    Vfx.Ring(to, 1.8f, c, 0.35f);
                    Game.I.Audio.Play("charm", 0.4f, 1.3f);
                    Combat.HitCircle(to, 1.8f, Team.Enemy, HitInfo.Effect(b.Value, God.Freyja, Vector2.zero), null, e => Enthrall(e, 4f));
                    break;
            }
        }

        public God CastGod => SlotGod(BoonSlot.Cast);

        public void OnCastImpact(Vector2 pos)
        {
            var b = Slot(BoonSlot.Cast);
            if (b == null) return;
            float v = b.Value;
            Color c = Pal.God(b.Def.God);
            switch (b.Def.God)
            {
                case God.Thor:
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 p = pos + Random.insideUnitCircle * 1.4f;
                        Game.I.After(i * 0.13f, () =>
                        {
                            Vfx.Bolt(p + new Vector2(Random.Range(-1f, 1f), 9f), p, c, 0.28f, 0.24f);
                            Vfx.Ring(p, 1.2f, c, 0.25f);
                            Vfx.Light(p, 3f, c.A(0.6f), 0.3f);
                            Game.I.Audio.Play("thunder", 0.6f);
                            Game.I.Cam.Shake(0.12f);
                            Combat.HitCircle(p, 1.3f, Team.Enemy, HitInfo.Effect(v, God.Thor, Vector2.zero));
                        });
                    }
                    break;
                case God.Skadi:
                    Vfx.Ring(pos, 2.6f, Pal.Frost, 0.4f);
                    Vfx.Burst(pos, 20, Pal.Frost, 9f);
                    Game.I.Audio.Play("frost");
                    Combat.HitCircle(pos, 2.6f, Team.Enemy, HitInfo.Effect(v, God.Skadi, Vector2.zero), null, e => { Chill(e, 4); e.St.Frozen = Mathf.Max(e.St.Frozen, 1f); });
                    break;
                case God.Tyr:
                    {
                        var z = Zone.Spawn(pos, 2f, 3f, 0.3f, HitInfo.Effect(v, God.Tyr, Vector2.zero), Team.Enemy, c, null, Runes.Tiwaz);
                        z.Spinning = true;
                        Game.I.Audio.Play("verdict", 0.5f, 1.3f);
                        break;
                    }
                case God.Freyja:
                    Vfx.Ring(pos, 2.5f, c, 0.4f);
                    Combat.HitCircle(pos, 2.5f, Team.Enemy, HitInfo.Effect(v, God.Freyja, Vector2.zero), null, e => Charm(e, 4f));
                    break;
                case God.Njord:
                    {
                        var z = Zone.Spawn(pos, 3f, 1.1f, 0.08f, new HitInfo { Damage = 0f }, Team.Enemy, c, e =>
                        {
                            var d = (pos - e.Pos);
                            if (d.magnitude > 0.4f && !e.IsBoss) e.Knock += d.normalized * 2.2f;
                        }, Runes.Laguz);
                        z.Spinning = true;
                        z.OnEnd = zz =>
                        {
                            Vfx.Ring(pos, 3f, c, 0.4f);
                            Game.I.Audio.Play("wave", 0.6f, 1.2f);
                            Combat.HitCircle(pos, 3f, Team.Enemy, HitInfo.Effect(v, God.Njord, Vector2.zero, 9f));
                        };
                        Game.I.Audio.Play("splash", 0.6f);
                        break;
                    }
            }
        }

        public bool HasCall => Slot(BoonSlot.Call) != null;

        public void Call()
        {
            var b = Slot(BoonSlot.Call);
            if (b == null || P == null) return;
            float v = b.Value;
            God g = b.Def.God;
            Color c = Pal.God(g);
            Game.I.Hitstop(0.12f);
            Game.I.Cam.Shake(0.4f);
            Game.I.Cam.Punch(0.6f);
            Vfx.Ring(P.Pos, 4f, c, 0.6f);
            Vfx.Flash(P.Center, 3f, c, 0.4f);
            Game.I.UI.Banner(Pal.GodName(g).ToUpper() + "!", b.Def.Name, c, 1.4f);
            switch (g)
            {
                case God.Thor:
                    Game.I.Audio.Play("thunder");
                    for (int i = 0; i < 12; i++)
                    {
                        Game.I.After(0.1f + i * 0.11f, () =>
                        {
                            if (P == null) return;
                            var list = Combat.InRadius(P.Pos, 12f, Team.Enemy);
                            if (list.Count > 0) Lightning(list[Random.Range(0, list.Count)], v);
                            else
                            {
                                var p = P.Pos + Random.insideUnitCircle * 6f;
                                Vfx.Bolt(p + new Vector2(0, 9f), p, c);
                            }
                        });
                    }
                    break;
                case God.Skadi:
                    Game.I.Audio.Play("frost", 1f, 0.7f);
                    foreach (var e in Combat.AllOf(Team.Enemy)) { e.St.Frozen = Mathf.Max(e.St.Frozen, v); Chill(e, 5); }
                    break;
                case God.Tyr:
                    {
                        Game.I.Audio.Play("verdict");
                        var z = Zone.Spawn(P.Pos, 2.6f, 5f, 0.2f, HitInfo.Effect(v, God.Tyr, Vector2.zero), Team.Enemy, c, null, Runes.Tiwaz);
                        z.Follow = P.transform;
                        z.Spinning = true;
                        break;
                    }
                case God.Odin:
                    Game.I.Audio.Play("raven");
                    OdinCall = v;
                    foreach (var e in Combat.AllOf(Team.Enemy)) { e.St.Slowed = v; e.St.SlowFactor = 0.4f; }
                    break;
                case God.Freyja:
                    Game.I.Audio.Play("charm");
                    foreach (var e in Combat.AllOf(Team.Enemy)) Charm(e, v);
                    P.HealAmount(15f);
                    break;
                case God.Njord:
                    Game.I.Audio.Play("wave");
                    Vfx.Ring(P.Pos, 7f, c, 0.7f);
                    Combat.HitCircle(P.Pos, 7f, Team.Enemy, HitInfo.Effect(v, God.Njord, Vector2.zero, 16f));
                    break;
            }
        }

        // ------------------------------------------------------------------ events

        public void OnPlayerHit(Entity attacker)
        {
            if (P == null) return;
            if (Has("thor_resolve"))
            {
                var e = attacker != null && !attacker.Dead && attacker.Team == Team.Enemy ? attacker : Combat.Nearest(P.Pos, Team.Enemy, 8f);
                if (e != null) Lightning(e, V("thor_resolve"));
            }
            if (Has("tyr_law"))
                foreach (var e in Combat.InRadius(P.Pos, 3.5f, Team.Enemy)) ApplyVerdict(e, V("tyr_law"));
        }

        public void OnEnemyKilled(Entity e)
        {
            if (Has("freyja_folk") && Random.value < 0.25f && P != null)
            {
                P.HealAmount(V("freyja_folk"));
                Vfx.Motes(e.Center, 5, Pal.God(God.Freyja), 0.5f, 1.5f, 0.8f);
            }
        }

        public void OnRoomClear()
        {
            if (Has("freyja_neck") && P != null) P.HealAmount(V("freyja_neck"));
        }

        public void OnSlam(Entity e)
        {
            if (Has("njord_riptide")) Combat.Deal(e, HitInfo.Effect(V("njord_riptide"), God.Njord, Vector2.zero));
            if (Has("duo_skadi_njord")) Chill(e, 3);
        }

        public void Tick(float dt)
        {
            if (OdinCall > 0f) OdinCall -= dt;
            if (Has("odin_ravens"))
            {
                ravenT -= dt;
                if (ravenT <= 0f)
                {
                    ravenT = 3f;
                    var list = Combat.AllOf(Team.Enemy);
                    if (list.Count > 0 && P != null)
                    {
                        var e = list[Random.Range(0, list.Count)];
                        var from = P.Center + new Vector2(0f, 1.5f);
                        var d = (e.Center - from);
                        Vfx.Spawn(SpriteLib.Feather, from, d * 4f, 0.25f, 0.9f, 0.7f, Color.white, Color.white.A(0.2f), false, d.Ang());
                        Vfx.Line(from, e.Center, 0.12f, Pal.God(God.Odin), 0.2f);
                        Game.I.Audio.Play("raven", 0.35f, 1.2f);
                        Combat.Deal(e, HitInfo.Effect(V("odin_ravens"), God.Odin, d.normalized));
                        Mark(e);
                    }
                }
            }
        }
    }
}
