using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A lingering area: venom pools, rime trails, blade rifts, whirlpools.</summary>
    public sealed class Zone : MonoBehaviour
    {
        public float Radius, Duration, Tick;
        public HitInfo Hit;
        public Team Target;
        public Color Color;
        public Action<Entity> OnTick;
        public Action<Zone> OnEnd;
        public Transform Follow;
        public bool Spinning;
        float t, tickT;
        SpriteRenderer fill, ring, glyph;

        public Vector2 Pos
        {
            get { var p = transform.position; return new Vector2(p.x, p.y); }
            set { transform.position = new Vector3(value.x, value.y, 0f); }
        }

        public static Zone Spawn(Vector2 pos, float radius, float duration, float tick, HitInfo hit, Team target, Color color,
            Action<Entity> onTick = null, int rune = -1)
        {
            var go = new GameObject("zone");
            go.transform.SetParent(World.Root, false);
            var z = go.AddComponent<Zone>();
            z.Pos = pos;
            z.Radius = radius;
            z.Duration = duration;
            z.Tick = tick;
            z.Hit = hit;
            z.Target = target;
            z.Color = color;
            z.OnTick = onTick;
            z.tickT = 0.05f;
            z.fill = Mats.SR(go.transform, "fill", SpriteLib.Disc, Sort.FloorFx + 5, color.A(0.22f), true);
            z.ring = Mats.SR(go.transform, "ring", SpriteLib.Ring, Sort.FloorFx + 6, color.A(0.7f), true);
            if (rune >= 0)
            {
                z.glyph = Mats.SR(go.transform, "glyph", Runes.Sprite(rune), Sort.FloorFx + 7, color.A(0.6f), true);
                z.glyph.transform.localScale = Vector3.one * radius * 0.9f;
            }
            z.Apply(0f);
            return z;
        }

        void Apply(float k)
        {
            float grow = Mathf.Min(1f, t / 0.15f);
            float fade = Duration > 0f ? Mathf.Clamp01((Duration - t) / 0.3f) : 1f;
            float s = Radius * 2f * grow;
            fill.transform.localScale = new Vector3(s, s, 1f);
            ring.transform.localScale = new Vector3(s, s, 1f);
            float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 9f);
            fill.color = Color.A(0.2f * fade * pulse);
            ring.color = Color.A(0.75f * fade);
            if (glyph != null)
            {
                glyph.color = Color.A(0.5f * fade);
                glyph.transform.Rotate(0, 0, 60f * Time.deltaTime);
            }
            if (Spinning) ring.transform.Rotate(0, 0, -240f * Time.deltaTime);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            if (Follow != null) Pos = new Vector2(Follow.position.x, Follow.position.y);
            Apply(t);
            if (t >= Duration)
            {
                OnEnd?.Invoke(this);
                Destroy(gameObject);
                return;
            }
            tickT -= dt;
            if (tickT > 0f) return;
            tickT = Tick;
            foreach (var e in Combat.InRadius(Pos, Radius, Target))
            {
                if (Hit.Damage > 0f)
                {
                    var h = Hit;
                    if (h.Knock > 0f) h.Dir = (e.Pos - Pos).Norm(Vector2.up);
                    Combat.Deal(e, h);
                }
                OnTick?.Invoke(e);
            }
        }
    }
}
