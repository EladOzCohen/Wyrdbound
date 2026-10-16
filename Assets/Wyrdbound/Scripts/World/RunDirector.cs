using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public sealed class RunSummary
    {
        public bool Won;
        public string Cause = "", Realm = "", Weapon = "";
        public float Time;
        public int Kills, Depth, Embers, Keys, Mead, Heat;
        public string Bounty = "";
        public List<Boon> Boons = new List<Boon>();
    }

    /// <summary>Runs an escape: builds chambers, offers doors, hands out rewards, stages bosses and the surface.</summary>
    public sealed class RunDirector : MonoBehaviour
    {
        public static RunDirector I;

        public bool Cleared;
        readonly List<Door> doors = new List<Door>();
        Encounter enc;
        bool newRegion;
        float bleedT;

        static readonly God[] Olympians = { God.Thor, God.Freyja, God.Tyr, God.Skadi, God.Odin, God.Njord };

        RunState Run => RunState.Current;
        public IReadOnlyList<Door> Doors => doors;
        public Encounter CurrentEncounter => enc;

        void Awake() => I = this;

        public static void SpawnMinion(string id, Vector2 pos)
        {
            var def = EnemyDefs.Get(id);
            var run = RunState.Current;
            if (def == null || run == null || World.Root == null) return;
            if (World.Grid != null) pos = World.Grid.NearestFloor(pos, def.Radius);
            var e = Enemy.Spawn(def, pos, false, run.EnemyHpMult * 0.8f, run.EnemyDmgMult, 0.6f);
            e.Minion = true;
            I?.enc?.Add(e);
        }

        // ------------------------------------------------------------------ run lifecycle

        public void StartRun(string weapon)
        {
            var d = SaveSystem.Data;
            var run = new RunState(Environment.TickCount ^ (d.runs * 7919 + 17), weapon);
            RunState.Current = run;
            d.runs++;
            SaveSystem.Save();
            var p = Game.I.Player;
            p.gameObject.SetActive(true);
            p.Equip(weapon);
            p.ResetForRun();
            newRegion = true;
            run.EnterRegion();
            BuildRoom(new RoomPlan { Kind = RoomKind.Combat, Reward = Reward.Boon, God = PickGod(run.Rng, null), Seed = run.Rng.Next() });
        }

        public void OnRunStarted() => OnEntered();

        public RunSummary EndRun(bool won)
        {
            var run = Run;
            var d = SaveSystem.Data;
            var s = new RunSummary { Won = won };
            if (run == null) return s;
            run.Won = won;
            s.Cause = Player.I != null && !string.IsNullOrEmpty(Player.I.DeathCause) ? Player.I.DeathCause : run.LastHitBy;
            s.Realm = run.Room != null && run.Room.Kind == RoomKind.Surface ? "Midgard" : run.Biome.Name;
            s.Weapon = Meta.Weapon(run.Weapon).Name;
            s.Time = run.Time;
            s.Kills = run.Kills;
            s.Depth = run.Depth + 1;
            s.Embers = run.Embers;
            s.Keys = run.Keys;
            s.Mead = run.Mead;
            s.Heat = run.Heat;
            s.Boons.AddRange(run.Boons.Owned);

            d.embers += run.Embers;
            d.keys += run.Keys;
            d.mead += run.Mead;
            d.SetFlag("weapon_used_" + run.Weapon);
            if (won)
            {
                d.clears++;
                d.SetFlag("cleared");
                d.AddCount("clear_" + run.Weapon);
                if (d.bestTime <= 0f || run.Time < d.bestTime) d.bestTime = run.Time;
                if (run.Heat > d.bestHeat)
                {
                    if (run.Heat > 0)
                    {
                        int k = (run.Heat + 1) / 2, m = 1 + run.Heat / 4;
                        d.keys += k;
                        d.mead += m;
                        s.Bounty = "The Norns reward your new wyrd: +" + k + " Grave-Keys, +" + m + " Mead";
                    }
                    d.bestHeat = run.Heat;
                }
            }
            else
            {
                d.deaths++;
                d.lastDeath = s.Cause;
                d.AddCount("death_" + run.Biome.Id);
                if (Boss.Current != null) d.AddCount("deathto_" + Boss.Current.Id);
            }
            SaveSystem.Save();
            Game.I.Audio.Intensity(0f);
            return s;
        }

        void Update()
        {
            var run = Run;
            if (run == null || Game.I.Mode != GameMode.Run || Game.I.Paused) return;
            float dt = Time.deltaTime;
            run.Time += dt;
            run.RegionTime += dt;
            enc?.Tick(dt);
            if (run.RegionLimit > 0f && run.RegionTime > run.RegionLimit && run.Room != null && run.Room.Kind != RoomKind.Surface)
            {
                bleedT -= dt;
                if (bleedT <= 0f && Player.I != null && !Player.I.Dead)
                {
                    bleedT = 8f;
                    var h = new HitInfo { Damage = 5f, Kind = HitKind.Hazard, Cause = "Garmr's Hunger" };
                    Combat.Deal(Player.I, h);
                }
            }
        }

        // ------------------------------------------------------------------ chambers

        public void TakeDoor(Door d)
        {
            var run = Run;
            if (run == null || !Cleared || Game.I.Transitioning) return;
            var plan = d.Plan;
            var from = run.Room;
            if (from != null && from.Kind == RoomKind.Passage)
            {
                run.Region++;
                run.RoomInRegion = 0;
                run.EnterRegion();
                newRegion = true;
            }
            else run.RoomInRegion++;
            run.Depth++;
            Game.I.Transition(() => BuildRoom(plan), OnEntered);
        }

        void BuildRoom(RoomPlan plan)
        {
            var run = Run;
            run.Room = plan;
            Cleared = false;
            doors.Clear();
            enc = null;
            Game.I.ClearLevel();
            RuneStone.CollectAll();
            Game.I.UI.CloseAll();

            bool surface = plan.Kind == RoomKind.Surface;
            Biome b = surface ? Biomes.Surface : run.Biome;
            var rng = new Rng(plan.Seed);
            var next = surface ? new List<RoomPlan>() : NextOptions(plan);
            RoomGrid g;
            switch (plan.Kind)
            {
                case RoomKind.Combat: g = RoomGen.Combat(rng, b, next.Count, false); break;
                case RoomKind.MiniBoss: g = RoomGen.Combat(rng, b, next.Count, true); break;
                case RoomKind.Boss: g = RoomGen.Arena(rng, b, 11, b.Index != 1); break;
                case RoomKind.Surface: g = RoomGen.Cave(); break;
                default: g = RoomGen.Small(rng, b, next.Count); break;
            }
            bool carve = plan.Kind == RoomKind.Combat || plan.Kind == RoomKind.MiniBoss || plan.Kind == RoomKind.Boss;
            LevelBuilder.Build(g, b, plan.Seed, carve, false);

            var p = Game.I.Player;
            p.Teleport(g.Entry);
            p.Facing = Vector2.up;
            Game.I.Cam.Snap(g.Entry);

            bool reveal = SaveSystem.Data.Weave("dark") == 0;
            for (int i = 0; i < next.Count && i < g.DoorSpots.Count; i++)
                doors.Add(Door.Create(g.DoorSpots[i], next[i], b, reveal || next[i].Kind == RoomKind.Boss || next[i].Kind == RoomKind.Surface));

            SaveSystem.Data.SetFlag("reached_" + b.Id);
            SaveSystem.Data.SetFlag("codex_realm_" + b.Id);

            Game.I.Audio.Intensity(0f);
            if (plan.Kind == RoomKind.Boss) Game.I.Audio.Music(b.Boss == "hel" ? "boss_hel" : "boss");
            else Game.I.Audio.Music(b.Music);

            switch (plan.Kind)
            {
                case RoomKind.Combat:
                case RoomKind.MiniBoss:
                    enc = new Encounter(run, rng, plan.Kind == RoomKind.MiniBoss, b);
                    break;
                case RoomKind.Shop: SetupShop(rng); Clear(); break;
                case RoomKind.Rest: SetupSpring(new Vector2(0f, 1.2f), 0.4f); Clear(); break;
                case RoomKind.Story: SetupStory(plan); Clear(); break;
                case RoomKind.Passage:
                    Pickup.Spawn(new Vector2(0f, 0.5f), Reward.Silver, God.None, 40 + run.Region * 15);
                    SetupSpring(new Vector2(-4f, 1.5f), 0.2f);
                    Clear();
                    break;
                case RoomKind.Surface: SetupSurface(); break;
            }
        }

        void OnEntered()
        {
            var run = Run;
            if (run == null || run.Room == null) return;
            var b = run.Room.Kind == RoomKind.Surface ? Biomes.Surface : run.Biome;
            if (newRegion)
            {
                newRegion = false;
                Game.I.UI.Banner(b.Name.ToUpper(), b.Subtitle, b.Accent, 2.8f);
                Game.I.Audio.Play("horn", 0.45f);
                if (run.Region > 0 && SaveSystem.Data.keepsake == "bowl" && Player.I != null) Player.I.HealAmount(Player.I.MaxHP * 0.25f);
            }
            switch (run.Room.Kind)
            {
                case RoomKind.Combat:
                case RoomKind.MiniBoss:
                    enc?.Start();
                    break;
                case RoomKind.Boss:
                    StartBoss(b);
                    break;
                case RoomKind.Surface:
                    Game.I.UI.Banner("MIDGARD", "The cave where Loki lies bound", Pal.Gold, 3f);
                    break;
            }
        }

        void Clear()
        {
            Cleared = true;
            foreach (var d in doors) d.SetOpen();
        }

        public void OnEncounterCleared(Encounter e)
        {
            var run = Run;
            if (run == null || e.IsBoss) return;
            var plan = run.Room;
            Game.I.SlowMo(0.3f, 0.6f);
            Game.I.Cam.Punch(0.5f);
            Game.I.Audio.Intensity(0f);
            Game.I.Audio.Play("level", 0.5f, 0.8f);
            Game.I.UI.Toast("The dead lie still", Pal.Dim);
            var at = World.Grid.NearestFloor(e.LastKill, 0.5f);
            Pickup.Spawn(at, plan.Reward, plan.God, Amount(plan.Reward));
            if (plan.Kind == RoomKind.MiniBoss)
                Pickup.Spawn(at + new Vector2(1.6f, 0f), Reward.Embers, God.None, 20 + run.Region * 10);
            run.Boons.OnRoomClear();
            RuneStone.RecallAll();
            SaveSystem.Save();
            Clear();
        }

        int Amount(Reward r)
        {
            var run = Run;
            switch (r)
            {
                case Reward.Silver: return run.Rng.Range(28, 46) + run.Region * 12;
                case Reward.Embers: return run.Rng.Range(10, 18) + run.Region * 6;
                case Reward.Key: return 1;
                case Reward.Mead: return 1;
            }
            return 1;
        }

        // ------------------------------------------------------------------ door options

        List<RoomPlan> NextOptions(RoomPlan cur)
        {
            var run = Run;
            var rng = run.Rng;
            var list = new List<RoomPlan>();
            var b = run.Biome;
            if (cur.Kind == RoomKind.Boss)
            {
                bool last = run.Region >= Biomes.Run.Length - 1;
                list.Add(new RoomPlan { Kind = last ? RoomKind.Surface : RoomKind.Passage, Reward = last ? Reward.Surface : Reward.Passage, Seed = rng.Next() });
                return list;
            }
            if (cur.Kind == RoomKind.Passage)
            {
                list.Add(new RoomPlan { Kind = RoomKind.Combat, Reward = Reward.Boon, God = PickGod(rng, null), Seed = rng.Next() });
                return list;
            }
            int next = run.RoomInRegion + 1;
            int bossIdx = b.Rooms - 1, shopIdx = bossIdx - 1;
            if (next >= bossIdx) { list.Add(new RoomPlan { Kind = RoomKind.Boss, Reward = Reward.Boss, Seed = rng.Next() }); return list; }
            if (next == shopIdx) { list.Add(new RoomPlan { Kind = RoomKind.Shop, Reward = Reward.Shop, Seed = rng.Next() }); return list; }

            float c = rng.Value;
            int count = c < 0.15f ? 1 : c < 0.72f ? 2 : 3;
            var used = new HashSet<Reward>();
            var gods = new HashSet<God>();
            if ((next == 3 || next == 4) && !run.MiniThisRegion && (next == 4 || rng.Chance(0.5f)))
            {
                run.MiniThisRegion = true;
                var g = PickGod(rng, gods);
                gods.Add(g);
                list.Add(new RoomPlan { Kind = RoomKind.MiniBoss, Reward = Reward.Boon, God = g, Seed = rng.Next() });
                count = Mathf.Max(count, 2);
            }
            int guard = 0;
            while (list.Count < count && guard++ < 30)
            {
                float roll = rng.Value;
                if (roll < 0.08f && next >= 2 && !used.Contains(Reward.Rest))
                {
                    used.Add(Reward.Rest);
                    list.Add(new RoomPlan { Kind = RoomKind.Rest, Reward = Reward.Rest, Seed = rng.Next() });
                    continue;
                }
                if (roll < 0.17f && !run.StoryThisRegion && next >= 1)
                {
                    run.StoryThisRegion = true;
                    used.Add(Reward.Story);
                    list.Add(new RoomPlan { Kind = RoomKind.Story, Reward = Reward.Story, Npc = run.Region < 2 ? "hodr" : "hermod", Seed = rng.Next() });
                    continue;
                }
                var r = PickReward(rng, used);
                var plan = new RoomPlan { Kind = RoomKind.Combat, Reward = r, Seed = rng.Next() };
                if (r == Reward.Boon)
                {
                    plan.God = PickGod(rng, gods);
                    gods.Add(plan.God);
                    if (gods.Count >= 2) used.Add(Reward.Boon);
                }
                else used.Add(r);
                list.Add(plan);
            }
            return list;
        }

        Reward PickReward(Rng rng, HashSet<Reward> used)
        {
            var run = Run;
            var opts = new List<(Reward r, float w)>
            {
                (Reward.Boon, 42f), (Reward.Silver, 13f), (Reward.Embers, 11f), (Reward.Key, 5f), (Reward.Mead, 2.5f + run.Region * 1.5f),
            };
            if (run.Boons.Upgradeable(rng, 1).Count > 0) opts.Add((Reward.Apple, 12f));
            var p = Player.I;
            opts.Add((Reward.Salve, p != null && p.HP < p.MaxHP * 0.7f ? 12f : 4f));
            if (run.Boons.CountGod(God.Ratatoskr) < 5) opts.Add((Reward.Ratatoskr, 5f));
            opts.RemoveAll(o => used.Contains(o.r));
            if (opts.Count == 0) return Reward.Silver;
            return rng.Weighted(opts, o => o.w).r;
        }

        God PickGod(Rng rng, HashSet<God> exclude)
        {
            var run = Run;
            var inRun = new List<God>();
            foreach (var g in Olympians) if (run.Boons.CountGod(g) > 0) inRun.Add(g);
            var pool = new List<God>(inRun.Count >= 4 ? inRun : new List<God>(Olympians));
            if (exclude != null) pool.RemoveAll(g => exclude.Contains(g));
            if (pool.Count == 0) pool.AddRange(Olympians);
            return rng.Weighted(pool, g => run.Boons.CountGod(g) > 0 ? 1f : 1.3f);
        }

        // ------------------------------------------------------------------ rewards

        public void OfferBoons(God g)
        {
            var run = Run;
            if (run == null) return;
            var d = SaveSystem.Data;
            var offers = run.Boons.RollOffers(g, run.Rng, run.BoonChoices, run.Stats.RarityBonus, g != God.Ratatoskr && run.FirstBoonMinRare());
            if (offers.Count == 0)
            {
                int n = run.AddSilver(60);
                Game.I.UI.Toast(Pal.GodName(g) + " has nothing more to give. +" + n + " Silver", Pal.Silver);
                return;
            }
            d.SetFlag("met_" + g);
            d.SetFlag("codex_god_" + g);
            Game.I.Audio.Play("boon", 0.8f);
            Story.Play("god_" + g.ToString().ToLower(), () => Game.I.UI.Push(new BoonScreen(g, offers)));
        }

        public void OfferApple()
        {
            var run = Run;
            var list = run.Boons.Upgradeable(run.Rng, 3);
            if (list.Count == 0)
            {
                int n = run.AddSilver(50);
                Game.I.UI.Toast("Iðunn's apple turns to silver in your hand. +" + n, Pal.Silver);
                return;
            }
            Game.I.Audio.Play("level", 0.7f);
            Game.I.UI.Push(new AppleScreen(list));
        }

        // ------------------------------------------------------------------ special chambers

        void SetupSpring(Vector2 pos, float frac)
        {
            var root = World.Root;
            var sr = Mats.SR(root, "spring", PropArt.Get("well", Biomes.Hall), Sort.Y(pos.y));
            sr.transform.position = pos;
            sr.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            World.Grid.Obstacles.Add(new Obstacle(pos, 0.9f, false, "spring"));
            bool used = false;
            Interactable.Add(sr.gameObject, 2.2f, () => "Drink from Hvergelmir (restore " + Mathf.RoundToInt(frac * 100f) + "% Health)", () =>
            {
                used = true;
                Player.I.HealAmount(Player.I.MaxHP * frac);
                Game.I.Audio.Play("splash", 0.8f);
                Game.I.Audio.Play("level", 0.5f);
                Vfx.Ring(pos, 2f, U.Hex("5ec8ff"), 0.5f);
                sr.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            }, () => !used);
        }

        sealed class Ware
        {
            public string Name;
            public Reward Icon;
            public God God;
            public int Price;
            public Action Buy;
        }

        void SetupShop(Rng rng)
        {
            var run = Run;
            var npc = LevelBuilder.Npc("modgud", new Vector2(0f, 3.2f), false, U.Hex("bfe4ff").A(0.25f));
            npc.SetMarker(Story.HasNew("modgud"));
            SaveSystem.Data.SetFlag("codex_modgud");
            Interactable.Add(npc.gameObject, 2.2f, () => "Speak with Móðguðr", () =>
            {
                npc.SetMarker(false);
                Story.Play("modgud", () => Game.I.Hub.OfferGift("modgud", null));
            });

            var all = new List<Ware>();
            var g = PickGod(rng, null);
            all.Add(new Ware { Name = "Blessing of " + Pal.GodName(g), Icon = Reward.Boon, God = g, Price = 150, Buy = () => OfferBoons(g) });
            all.Add(new Ware { Name = "Iðunn's Golden Apple", Icon = Reward.Apple, Price = 110, Buy = OfferApple });
            all.Add(new Ware { Name = "Eir's Salve (restore 35%)", Icon = Reward.Salve, Price = 60, Buy = () => Player.I.HealAmount(Player.I.MaxHP * 0.35f) });
            all.Add(new Ware { Name = "Ratatoskr's Gossip", Icon = Reward.Ratatoskr, God = God.Ratatoskr, Price = 120, Buy = () => OfferBoons(God.Ratatoskr) });
            all.Add(new Ware { Name = "Grave-Key", Icon = Reward.Key, Price = 90, Buy = () => run.Keys++ });
            all.Add(new Ware { Name = "Mead of the Dead", Icon = Reward.Mead, Price = 160, Buy = () => run.Mead++ });
            all.Add(new Ware { Name = "Fate-Thread (re-weave a choice)", Icon = Reward.Story, Price = 70, Buy = () => run.Rerolls++ });
            rng.Shuffle(all);
            if (all.FindIndex(w => w.Icon == Reward.Salve) > 2) all.Insert(0, all.Find(w => w.Icon == Reward.Salve));
            for (int i = 0; i < 3; i++)
            {
                var w = all[i];
                int price = run.Price(w.Price);
                var pos = new Vector2(-4.5f + i * 4.5f, -0.4f);
                var go = new GameObject("ware");
                go.transform.SetParent(World.Root, false);
                go.transform.position = pos;
                var ped = Mats.SR(go.transform, "ped", PropArt.Get("stone", run.Biome), Sort.Y(pos.y));
                ped.transform.localScale = new Vector3(0.7f, 0.45f, 1f);
                var icon = Mats.SR(go.transform, "icon", SpriteLib.RewardIcon(w.Icon == Reward.Story ? Reward.Story : w.Icon, w.God, false), Sort.Y(pos.y) + 1);
                icon.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                var glow = Mats.SR(go.transform, "glow", SpriteLib.Glow, Sort.Y(pos.y), Pickup.Tint(w.Icon, w.God).A(0.4f), true);
                glow.transform.localPosition = icon.transform.localPosition;
                glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                var label = LevelBuilder.Label(go.transform, price + " silver", pos + new Vector2(0f, 0.55f), Pal.Silver, 0.8f);
                World.Grid.Obstacles.Add(new Obstacle(pos, 0.45f, false, "ware"));
                bool sold = false;
                Interactable.Add(go, 1.8f, () => "Buy " + w.Name + " — " + U.Tint(price + " silver", run.Silver >= price ? Pal.Silver : Pal.Danger), () =>
                {
                    if (run.Silver < price)
                    {
                        Game.I.Audio.Play("ui_deny");
                        Game.I.UI.Toast("Not enough silver", Pal.Danger);
                        return;
                    }
                    run.Silver -= price;
                    sold = true;
                    Game.I.Audio.Play("coin");
                    icon.enabled = glow.enabled = false;
                    label.text = "SOLD";
                    label.color = Pal.Dim;
                    w.Buy();
                }, () => !sold);
            }
        }

        void SetupStory(RoomPlan plan)
        {
            string id = plan.Npc ?? "hodr";
            var npc = LevelBuilder.Npc(id, new Vector2(0f, 1.8f), false, Story.Who(id).Color.A(0.25f));
            npc.SetMarker(true);
            bool done = false;
            string name = Story.Who(id).Name;
            Interactable.Add(npc.gameObject, 2.2f, () => "Speak with " + name, () =>
            {
                done = true;
                npc.SetMarker(false);
                SaveSystem.Data.SetFlag("codex_" + id);
                Story.Play(id, () => StoryGift(id));
            }, () => !done);
        }

        void StoryGift(string id)
        {
            var run = Run;
            if (run == null) return;
            if (id == "hodr")
            {
                var list = run.Boons.Upgradeable(run.Rng, 1);
                if (list.Count > 0)
                {
                    run.Boons.LevelUp(list[0], 1);
                    Game.I.UI.Toast("Höðr's blind blessing: " + list[0].Def.Name + " grows stronger", Pal.Gold);
                }
                else Game.I.UI.Toast("+" + run.AddSilver(60) + " Silver", Pal.Silver);
                Game.I.Audio.Play("level");
            }
            else
            {
                Player.I.HealAmount(Player.I.MaxHP * 0.25f);
                Game.I.UI.Toast("Hermóðr shares his provisions. +" + run.AddSilver(80) + " Silver", Pal.Silver);
                Game.I.Audio.Play("coin");
            }
        }

        void StartBoss(Biome b)
        {
            var run = Run;
            var def = EnemyDefs.Get(b.Boss);
            if (def == null) { Clear(); return; }
            SaveSystem.Data.SetFlag("codex_" + def.Id);
            Story.Play("boss_" + def.Id, () =>
            {
                if (run != RunState.Current) return;
                float hp = 1f + 0.2f * SaveSystem.Data.Weave("hardened");
                float dmg = 1f + 0.2f * SaveSystem.Data.Weave("fierce");
                var boss = Enemy.Spawn(def, World.Grid.NearestFloor(new Vector2(0f, 3.5f), def.Radius), false, hp, dmg, 1.4f);
                enc = new Encounter(run, boss);
                Game.I.UI.Banner(def.Name.ToUpper(), def.Title, def.Blood, 2.4f);
                Game.I.Audio.Play("roar", 0.8f);
                Game.I.Audio.Intensity(1f);
                Game.I.Cam.Shake(0.4f);
            });
        }

        public void OnBossDefeated(Boss b)
        {
            var run = Run;
            if (run == null) return;
            var d = SaveSystem.Data;
            bool first = d.Count("bosskill_" + b.Id) == 0;
            d.AddCount("bosskill_" + b.Id);
            var at = World.Grid.NearestFloor(b.Pos, 0.6f);
            Game.I.AfterReal(1.6f, () =>
            {
                if (run != RunState.Current) return;
                Game.I.UI.Banner(b.Def.Name.ToUpper() + " FALLS", first ? "The Warden is overcome for the first time" : "The way upward opens", b.Def.Blood, 2.4f);
                Pickup.Spawn(at, Reward.Embers, God.None, (first ? 50 : 20) + run.Region * 15);
                if (first)
                {
                    Pickup.Spawn(at + new Vector2(-1.8f, 0f), Reward.Key, God.None, 2);
                    Pickup.Spawn(at + new Vector2(1.8f, 0f), Reward.Mead, God.None, 1);
                }
                Game.I.Audio.Intensity(0f);
                Game.I.Audio.Music(run.Biome.Music);
                SaveSystem.Save();
                Story.Play("bossdown_" + b.Id, () => Clear());
            });
        }

        void SetupSurface()
        {
            var loki = LevelBuilder.Npc("loki", new Vector2(0f, 5.4f), false, Pal.Narfi.A(0.3f));
            loki.TrackPlayer = false;
            var sigyn = LevelBuilder.Npc("sigyn", new Vector2(2.1f, 5.7f), true, Pal.Gold.A(0.25f));
            sigyn.TrackPlayer = false;
            loki.SetMarker(true);
            bool talked = false;
            Interactable.Add(loki.gameObject, 3f, () => "Approach your father", () =>
            {
                talked = true;
                loki.SetMarker(false);
                SaveSystem.Data.SetFlag("codex_loki");
                SaveSystem.Data.SetFlag("codex_sigyn");
                Story.Play("surface", AfterSurface);
            }, () => !talked);
        }

        void AfterSurface()
        {
            var d = SaveSystem.Data;
            if (string.IsNullOrEmpty(d.ending) && StoryDB.EndingReady(d))
            {
                Story.Play("ending", () =>
                {
                    Game.I.Audio.Music("ending");
                    Game.I.UI.Push(new CardsScreen(StoryDB.EndingCards(d.ending), Fade));
                });
                return;
            }
            Game.I.Hub.OfferGift("sigyn", Fade);
        }

        void Fade()
        {
            var p = Player.I;
            Game.I.UI.Banner("THE DEAD CANNOT LINGER", "Midgard's air unthreads you, strand by strand", Pal.Narfi, 3f);
            Game.I.Audio.Play("boon", 0.6f, 0.6f);
            if (p != null)
            {
                p.Controllable = false;
                Vfx.Motes(p.Center, 40, Pal.Narfi, 1f, 3f, 2f);
            }
            Game.I.AfterReal(2.6f, () => Game.I.OnRunWon());
        }

        public void Abandon()
        {
            if (Run == null) return;
            if (Player.I != null) Player.I.DeathCause = "his own surrender";
            var s = EndRun(false);
            Game.I.UI.CloseAll();
            Game.I.UI.Push(new DeathScreen(s, () => Game.I.GoHub(true)));
        }
    }
}
