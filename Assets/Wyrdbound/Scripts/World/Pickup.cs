using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A chamber's reward, waiting on the floor: blessings, apples, silver, embers, keys, mead, salves.</summary>
    public sealed class Pickup : MonoBehaviour
    {
        public Reward Reward;
        public God God;
        public int Amount;
        SpriteRenderer icon, glow, light;
        float t;
        bool taken;
        Vector2 pos;

        public static Pickup Spawn(Vector2 pos, Reward r, God g, int amount)
        {
            if (World.Grid != null) pos = World.Grid.NearestFloor(pos, 0.5f);
            var go = new GameObject("pickup_" + r);
            go.transform.SetParent(World.Root, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var p = go.AddComponent<Pickup>();
            p.Reward = r;
            p.God = g;
            p.Amount = amount;
            p.pos = pos;
            Color c = Tint(r, g);
            p.light = Mats.SR(go.transform, "light", SpriteLib.Light, Sort.FloorFx + 4, c.A(0.9f), true);
            Lights.ToLayer(p.light);
            p.light.transform.localScale = new Vector3(4f, 4f, 1f);
            var up = View3D.UprightChild(go.transform);
            p.glow = Mats.SR(up, "glow", SpriteLib.Glow, Sort.Y(pos.y) - 1, c.A(0.5f), true);
            p.glow.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
            p.icon = Mats.SR(up, "icon", SpriteLib.RewardIcon(r, g, false), Sort.Y(pos.y));
            p.icon.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            var sh = Mats.SR(go.transform, "shadow", SpriteLib.Shadow, Sort.Shadow, new Color(1, 1, 1, 0.6f));
            sh.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            Vfx.Ring(pos, 1.4f, c, 0.5f);
            Vfx.Motes(pos + new Vector2(0f, 0.6f), 14, c, 0.6f, 2f, 1f);
            Game.I.Audio.Play("pickup", 0.4f, 0.8f);
            return p;
        }

        public static Color Tint(Reward r, God g)
        {
            switch (r)
            {
                case Reward.Boon: return Pal.God(g);
                case Reward.Ratatoskr: return Pal.God(God.Ratatoskr);
                case Reward.Apple: return Pal.Gold;
                case Reward.Embers: return Pal.Ember;
                case Reward.Key: return Pal.Key;
                case Reward.Mead: return Pal.Mead;
                case Reward.Salve: return Pal.Heal;
            }
            return Pal.Silver;
        }

        void Update()
        {
            if (taken) return;
            float dt = Time.deltaTime;
            t += dt;
            float bob = 0.55f + Mathf.Sin(t * 2.6f) * 0.1f;
            icon.transform.localPosition = new Vector3(0f, bob, 0f);
            glow.transform.localPosition = icon.transform.localPosition;
            glow.transform.localScale = Vector3.one * (1.7f + 0.15f * Mathf.Sin(t * 4f));
            var p = Player.I;
            if (p != null && !p.Dead && p.Controllable && Vector2.Distance(p.Pos, pos) < 0.95f) Take();
        }

        void Take()
        {
            taken = true;
            var run = RunState.Current;
            var ui = Game.I.UI;
            Color c = Tint(Reward, God);
            Vfx.Flash(pos + new Vector2(0f, 0.6f), 1.2f, c, 0.25f);
            Vfx.Motes(pos + new Vector2(0f, 0.6f), 10, c, 0.5f, 2.5f, 0.8f);
            switch (Reward)
            {
                case Reward.Boon: RunDirector.I.OfferBoons(God); break;
                case Reward.Ratatoskr: RunDirector.I.OfferBoons(God.Ratatoskr); break;
                case Reward.Apple: RunDirector.I.OfferApple(); break;
                case Reward.Silver:
                    {
                        int n = run != null ? run.AddSilver(Amount) : Amount;
                        Game.I.Audio.Play("coin");
                        ui.Toast("+" + n + " Hel-Silver", Pal.Silver);
                        break;
                    }
                case Reward.Embers:
                    if (run != null) run.Embers += Amount;
                    Game.I.Audio.Play("boon", 0.5f, 1.4f);
                    ui.Toast("+" + Amount + " Soul-Embers", Pal.Ember);
                    break;
                case Reward.Key:
                    if (run != null) run.Keys += Amount;
                    Game.I.Audio.Play("chain", 0.7f);
                    ui.Toast("+" + Amount + " Grave-Key" + (Amount > 1 ? "s" : ""), Pal.Key);
                    break;
                case Reward.Mead:
                    if (run != null) run.Mead += Amount;
                    Game.I.Audio.Play("pickup", 0.8f, 0.9f);
                    ui.Toast("+" + Amount + " Mead of the Dead", Pal.Mead);
                    break;
                case Reward.Salve:
                    if (Player.I != null) Player.I.HealAmount(Player.I.MaxHP * 0.3f);
                    Game.I.Audio.Play("level", 0.6f);
                    ui.Toast("Eir's Salve mends your wounds", Pal.Heal);
                    break;
            }
            Destroy(gameObject);
        }
    }
}
