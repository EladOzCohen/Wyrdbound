using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A chamber's fight: waves of the dead scaled by depth, realm and the Norns' weave.</summary>
    public sealed class Encounter
    {
        readonly List<List<string>> waves = new List<List<string>>();
        readonly List<Enemy> alive = new List<Enemy>();
        readonly RunState run;
        readonly Rng rng;
        int wave = -1;
        float delay = -1f, waveT;
        bool done, started;
        public Vector2 LastKill;
        public bool IsBoss;

        public int Alive => alive.Count;

        public Encounter(RunState run, Rng rng, bool miniBoss, Biome b)
        {
            this.run = run;
            this.rng = rng;
            float swarm = 1f + 0.2f * SaveSystem.Data.Weave("swarm");
            float budget = (8f + run.Depth * 1.1f + run.Region * 3f) * swarm;
            if (run.Depth == 0) budget = 5.5f;
            var pool = new List<string>(b.Pool);
            if (run.RoomInRegion >= 3 && b.Late != null) pool.AddRange(b.Late);

            if (miniBoss)
            {
                waves.Add(new List<string> { b.MiniBoss });
                waves.Add(Fill(pool, budget * 0.45f));
                return;
            }
            int n = run.Depth < 2 ? 2 : (rng.Chance(0.45f) ? 3 : 2);
            for (int i = 0; i < n; i++) waves.Add(Fill(pool, budget / n * (i == n - 1 ? 1.2f : 1f)));
        }

        /// <summary>An encounter around a single boss (minions join it).</summary>
        public Encounter(RunState run, Enemy boss)
        {
            this.run = run;
            rng = run.Rng;
            IsBoss = true;
            started = true;
            wave = 0;
            waves.Add(new List<string>());
            alive.Add(boss);
        }

        List<string> Fill(List<string> pool, float budget)
        {
            var list = new List<string>();
            int guard = 0;
            while (budget > 0.3f && guard++ < 40)
            {
                string id = rng.Pick(pool);
                var d = EnemyDefs.Get(id);
                if (d == null) continue;
                if (d.Cost > budget + 0.6f && list.Count > 0) { if (guard > 25) break; continue; }
                if (id == "bone_rat") { list.Add(id); list.Add(id); list.Add(id); budget -= d.Cost * 3f; }
                else { list.Add(id); budget -= d.Cost; }
            }
            if (list.Count == 0) list.Add(pool[0]);
            return list;
        }

        public void Start()
        {
            if (started) return;
            started = true;
            delay = 0.5f;
            Game.I.Audio.Intensity(1f);
        }

        public void Add(Enemy e)
        {
            if (e != null) alive.Add(e);
        }

        void NextWave()
        {
            wave++;
            waveT = 0f;
            var ids = waves[wave];
            var p = Player.I;
            Vector2 avoid = p != null ? p.Pos : Vector2.zero;
            for (int i = 0; i < ids.Count; i++)
            {
                var def = EnemyDefs.Get(ids[i]);
                if (def == null) continue;
                var pos = World.Grid.RandomFloor(rng, def.Radius + 0.2f, avoid, def.Id == run.Biome.MiniBoss ? 3f : 4.5f);
                if (def.Id == run.Biome.MiniBoss) pos = World.Grid.NearestFloor(new Vector2(0f, 2f), def.Radius);
                bool elite = def.Cost >= 1f && def.Id != "bell" && def.Armor <= 0f && rng.Chance(run.EliteChance);
                var e = Enemy.Spawn(def, pos, elite, run.EnemyHpMult, run.EnemyDmgMult, 0.8f + i * 0.07f);
                alive.Add(e);
                if (def.Id == run.Biome.MiniBoss)
                {
                    Game.I.UI.Banner(def.Name.ToUpper(), def.Title, Pal.Gold, 2f);
                    Game.I.Audio.Play("roar", 0.6f, 1.1f);
                }
            }
            Game.I.Audio.Intensity(1f);
        }

        public void Tick(float dt)
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                var e = alive[i];
                if (e == null) { alive.RemoveAt(i); continue; }
                if (e.Dead) { LastKill = e.Pos; alive.RemoveAt(i); }
            }
            if (done || !started) return;
            if (delay > 0f)
            {
                delay -= dt;
                if (delay <= 0f) NextWave();
                return;
            }
            waveT += dt;
            bool last = wave >= waves.Count - 1;
            if (!last)
            {
                if (alive.Count == 0 || (alive.Count <= 1 && waveT > 4f)) NextWave();
            }
            else if (alive.Count == 0 && waveT > 0.2f)
            {
                done = true;
                RunDirector.I.OnEncounterCleared(this);
            }
        }
    }
}
