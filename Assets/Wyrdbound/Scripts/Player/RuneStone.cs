using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A cast rune lodged in a foe or lying on the floor, waiting to return to Narfi.</summary>
    public sealed class RuneStone : MonoBehaviour
    {
        public static readonly List<RuneStone> All = new List<RuneStone>();

        public Entity Stuck;
        float t;
        bool returning;
        SpriteRenderer sr, glow;

        public Vector2 Pos
        {
            get { var p = transform.position; return new Vector2(p.x, p.y); }
            set { transform.position = new Vector3(value.x, value.y, 0f); }
        }

        public static RuneStone Drop(Vector2 pos, Entity stuck, Color col)
        {
            var go = new GameObject("rune");
            go.transform.SetParent(World.Root, false);
            var r = go.AddComponent<RuneStone>();
            r.Pos = pos;
            r.Stuck = stuck;
            r.sr = Mats.SR(go.transform, "stone", SpriteLib.RuneStone, Sort.Y(pos.y), col);
            r.sr.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            r.glow = Mats.SR(go.transform, "glow", SpriteLib.Light, Sort.FloorFx, col.A(0.25f), true);
            r.glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            return r;
        }

        public static void RecallAll()
        {
            foreach (var r in All) { r.returning = true; r.Stuck = null; }
        }

        /// <summary>Instantly return every outstanding rune (chamber transition).</summary>
        public static void CollectAll()
        {
            if (Player.I != null) Player.I.CastAmmo = Player.I.S.CastMax;
            foreach (var r in All.ToArray()) if (r != null) Destroy(r.gameObject);
            All.Clear();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Update()
        {
            var p = Player.I;
            if (p == null) return;
            float dt = Time.deltaTime;
            t += dt;
            if (Stuck != null)
            {
                if (Stuck.Dead || !Stuck.gameObject.activeInHierarchy)
                {
                    var at = Stuck.Pos;
                    Stuck = null;
                    Pos = World.Grid != null ? World.Grid.NearestFloor(at, 0.3f) : at;
                }
                else
                {
                    Pos = Stuck.Pos;
                    sr.transform.localPosition = new Vector3(0f, Stuck.VisualHeight * 0.55f, 0f);
                    sr.sortingOrder = Stuck.Body != null ? Stuck.Body.sortingOrder + 2 : Sort.Y(Pos.y);
                    if (t > 10f) { returning = true; Stuck = null; }
                    return;
                }
            }
            sr.transform.localPosition = new Vector3(0f, 0.25f + 0.08f * Mathf.Sin(t * 4f), 0f);
            sr.sortingOrder = Sort.Y(Pos.y);
            if (t > 10f) returning = true;
            Vector2 to = p.Pos - Pos;
            float d = to.magnitude;
            if (returning) Pos += to.Norm(Vector2.zero) * Mathf.Min(d, 22f * dt);
            else if (d < p.S.Magnet) Pos += to.Norm(Vector2.zero) * Mathf.Min(d, 14f * dt);
            if (d < 0.6f && !p.Dead)
            {
                p.CastAmmo = Mathf.Min(p.S.CastMax, p.CastAmmo + 1);
                Game.I.Audio.Play("pickup", 0.35f, 1.5f);
                Destroy(gameObject);
            }
        }
    }
}
