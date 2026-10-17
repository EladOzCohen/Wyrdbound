using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Atmosphere for a chamber: drifting mist below and above, flickering fire, glowing runes, motes.</summary>
    public sealed class Ambient : MonoBehaviour
    {
        sealed class Mist { public SpriteRenderer Sr; public Vector2 Pos; public float Speed, Phase, Alpha; }
        sealed class Flame { public SpriteRenderer Core, Light; public Vector2 Pos; public Color Col; public float Phase; }
        sealed class GlowSpot { public SpriteRenderer Sr; public float Phase, Alpha; }

        readonly List<Mist> mists = new List<Mist>();
        readonly List<Flame> flames = new List<Flame>();
        readonly List<GlowSpot> glows = new List<GlowSpot>();
        Biome biome;
        Rect bounds;
        float emitT;

        public void Init(RoomGrid g, Biome b, int seed)
        {
            biome = b;
            bounds = g.Bounds;
            var rng = new Rng(seed + 5);
            int under = 14;
            for (int i = 0; i < under; i++)
            {
                var p = new Vector2(rng.Range(bounds.xMin - 6f, bounds.xMax + 6f), rng.Range(bounds.yMin - 6f, bounds.yMax + 4f));
                AddMist(p, rng.Range(7f, 14f), rng.Range(0.12f, 0.22f), Sort.Mist, rng);
            }
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector2(rng.Range(bounds.xMin, bounds.xMax), rng.Range(bounds.yMin - 2f, bounds.yMin + 3f));
                AddMist(p, rng.Range(8f, 12f), rng.Range(0.05f, 0.08f), Sort.Fog, rng);
            }
        }

        void AddMist(Vector2 p, float scale, float alpha, int order, Rng rng)
        {
            var sr = Mats.SR(transform, "mist", SpriteLib.Mist, order, biome.Fog.A(alpha));
            sr.transform.localScale = new Vector3(scale, scale * 0.6f, 1f);
            mists.Add(new Mist { Sr = sr, Pos = p, Speed = rng.Range(0.15f, 0.45f) * rng.Sign, Phase = rng.Range(0f, 6f), Alpha = alpha });
        }

        public void AddFlame(Vector2 p, Color c)
        {
            var core = Mats.SR(transform, "flame", SpriteLib.Glow, Sort.Y(p.y - 0.8f) + 1, c, true);
            var light = Mats.SR(transform, "flamelight", SpriteLib.Light, Sort.FloorFx + 1, c.A(0.9f), true);
            Lights.ToLayer(light);
            light.transform.position = p - new Vector2(0f, 0.8f);
            flames.Add(new Flame { Core = core, Light = light, Pos = p, Col = c, Phase = Random.value * 10f });
        }

        public void AddGlow(Vector2 p, Color c, float size)
        {
            var sr = Mats.SR(transform, "runeglow", SpriteLib.Light, Sort.FloorFx + 1, c.A(0.45f), true);
            Lights.ToLayer(sr);
            sr.transform.position = p;
            sr.transform.localScale = new Vector3(size, size, 1f);
            glows.Add(new GlowSpot { Sr = sr, Phase = Random.value * 10f, Alpha = 0.45f });
        }

        void Update()
        {
            float dt = Time.deltaTime, t = Time.time;
            foreach (var m in mists)
            {
                m.Pos.x += m.Speed * dt;
                if (m.Pos.x > bounds.xMax + 10f) m.Pos.x = bounds.xMin - 10f;
                if (m.Pos.x < bounds.xMin - 10f) m.Pos.x = bounds.xMax + 10f;
                m.Sr.transform.position = new Vector3(m.Pos.x, m.Pos.y + Mathf.Sin(t * 0.2f + m.Phase) * 0.4f, m.Sr.sortingOrder == Sort.Mist ? 2.5f : -2.6f);
                m.Sr.color = biome.Fog.A(m.Alpha * (0.8f + 0.2f * Mathf.Sin(t * 0.3f + m.Phase)));
            }
            foreach (var f in flames)
            {
                float flick = 0.85f + 0.15f * Mathf.PerlinNoise(t * 6f, f.Phase) + 0.05f * Mathf.Sin(t * 23f + f.Phase);
                f.Core.transform.position = View3D.At(f.Pos - new Vector2(0f, 0.85f)) + View3D.CamRot * Vector3.up * 0.85f;
                f.Core.transform.rotation = View3D.CamRot;
                f.Core.transform.localScale = new Vector3(0.9f * flick, 1.25f * flick, 1f);
                f.Light.transform.localScale = new Vector3(6.5f * flick, 6.5f * flick, 1f);
                f.Light.color = f.Col.A(0.95f * flick);
                if (Random.value < dt * 6f) Vfx.Spawn(SpriteLib.Mote, f.Pos + Random.insideUnitCircle * 0.2f, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1.2f, 2.2f)), Random.Range(0.6f, 1.1f), 0.3f, 0.05f, f.Col, f.Col.A(0f), true);
            }
            foreach (var g in glows) g.Sr.color = biome.Accent.A(g.Alpha * (0.7f + 0.3f * Mathf.Sin(t * 1.4f + g.Phase)));

            emitT -= dt;
            if (emitT > 0f) return;
            emitT = 0.07f;
            var cam = Game.I.Cam;
            float hh = cam.HalfH, hw = cam.HalfW;
            Vector2 c = cam.Center;
            var pos = c + new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));
            switch (biome.Amb)
            {
                case AmbientKind.Snow:
                    Vfx.Spawn(SpriteLib.Mote, pos + new Vector2(0f, hh * 0.5f), new Vector2(Random.Range(-1.2f, -0.4f), Random.Range(-1.6f, -0.8f)), 4f, 0.28f, 0.2f, Color.white.A(0.7f), Color.white.A(0f), true, 0f, 0f, 0f, Sort.Fog - 5);
                    break;
                case AmbientKind.Spores:
                    Vfx.Spawn(SpriteLib.Mote, pos, new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.2f, 0.6f)), 4f, 0.3f, 0.1f, Pal.Venom.A(0.55f), Pal.Venom.A(0f), true, 0f, 0f, 0f, Sort.Fog - 5);
                    break;
                case AmbientKind.Embers:
                    Vfx.Spawn(SpriteLib.Mote, pos - new Vector2(0f, hh * 0.4f), new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.4f)), 4f, 0.32f, 0.05f, (Random.value < 0.5f ? Pal.Hel : Pal.Ember).A(0.7f), Pal.Hel.A(0f), true, 0f, 0f, 0f, Sort.Fog - 5);
                    break;
                case AmbientKind.Ash:
                    Vfx.Spawn(SpriteLib.Mote, pos + new Vector2(0f, hh * 0.4f), new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.6f, -0.3f)), 5f, 0.25f, 0.2f, U.Hex("8a8478").A(0.6f), U.Hex("8a8478").A(0f), true, 0f, 0f, 0f, Sort.Fog - 5);
                    break;
                case AmbientKind.Motes:
                    Vfx.Spawn(SpriteLib.Mote, pos, new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.1f, 0.35f)), 5f, 0.3f, 0.05f, Pal.Gold.A(0.55f), Pal.Gold.A(0f), true, 0f, 0f, 0f, Sort.Fog - 5);
                    break;
                default:
                    Vfx.Spawn(SpriteLib.Mote, pos, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.05f, 0.3f)), 4f, 0.26f, 0.05f, biome.Accent.A(0.45f), biome.Accent.A(0f), true, 0f, 0f, 0f, Sort.Fog - 5);
                    break;
            }
        }
    }
}
