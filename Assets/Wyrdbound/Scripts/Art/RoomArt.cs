using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Paints a whole chamber floor (stone, grout, cliff faces, carvings, decals) into one sprite.</summary>
    public static class RoomArt
    {
        public const int PPU = 32;

        public static Sprite PaintFloor(RoomGrid g, Biome b, int seed, bool carving, bool runner)
        {
            int TW = g.W * PPU, TH = g.H * PPU;
            var pix = new Pix(TW, TH);
            var P = pix.P;
            const float cliffH = 0.95f;
            float inv = 1f / PPU;

            for (int py = 0; py < TH; py++)
            {
                float v = (py + 0.5f) * inv;
                int ty = Mathf.FloorToInt(v);
                float fy = v - ty;
                for (int px = 0; px < TW; px++)
                {
                    float u = (px + 0.5f) * inv;
                    int tx = Mathf.FloorToInt(u);
                    float fx = u - tx;
                    int i = py * TW + px;
                    if (g.IsFloor(tx, ty))
                    {
                        Color c = FloorColor(b, u, v, seed);
                        float ao = 1f;
                        ao *= Edge(g.IsFloor(tx - 1, ty), fx);
                        ao *= Edge(g.IsFloor(tx + 1, ty), 1f - fx);
                        ao *= Edge(g.IsFloor(tx, ty - 1), fy);
                        ao *= Edge(g.IsFloor(tx, ty + 1), 1f - fy);
                        if (!g.IsFloor(tx - 1, ty - 1)) ao *= Corner(fx, fy);
                        if (!g.IsFloor(tx + 1, ty - 1)) ao *= Corner(1f - fx, fy);
                        if (!g.IsFloor(tx - 1, ty + 1)) ao *= Corner(fx, 1f - fy);
                        if (!g.IsFloor(tx + 1, ty + 1)) ao *= Corner(1f - fx, 1f - fy);
                        c = c.Lit(ao);
                        if (!g.IsFloor(tx, ty - 1) && fy < 0.07f) c = c.Lit(1.35f);
                        if (!g.IsFloor(tx, ty + 1) && fy > 0.95f) c = c.Lit(0.6f);
                        if (runner && Mathf.Abs(u - g.W * 0.5f) < 2.2f)
                        {
                            float rx = Mathf.Abs(u - g.W * 0.5f);
                            Color rc = rx > 1.9f ? Pal.Gold.Lit(0.55f) : U.Hex("3a1f4a").Lit(0.85f + 0.2f * Noise.Fbm(u * 3f, v * 0.5f, seed + 5, 2));
                            c = Color.Lerp(c, rc, 0.92f);
                        }
                        P[i] = c;
                    }
                    else if (g.IsFloor(tx, ty + 1) && (1f - fy) < cliffH)
                    {
                        float depth = (1f - fy) / cliffH;
                        float strata = Noise.Value(u * 6f, v * 1.2f, seed + 3) * 0.25f + Noise.Value(u * 1.5f, v * 8f, seed + 4) * 0.2f;
                        Color c = b.Cliff.Lit(1.25f - depth * 0.85f + strata);
                        float a = 1f - Mathf.Clamp01((depth - 0.55f) / 0.45f);
                        P[i] = new Color(c.r, c.g, c.b, a);
                    }
                }
            }

            pix.Mask = true;
            Decals(pix, g, b, seed);
            if (carving) Carving(pix, g, b);
            pix.Mask = false;
            return pix.ToSprite(Vector2.zero, PPU);
        }

        static float Edge(bool neighbourFloor, float d) => neighbourFloor ? 1f : Mathf.Lerp(0.55f, 1f, U.Smooth(d / 0.45f));

        static float Corner(float fx, float fy)
        {
            float d = Mathf.Sqrt(fx * fx + fy * fy);
            return Mathf.Lerp(0.7f, 1f, U.Smooth(d / 0.45f));
        }

        static Color FloorColor(Biome b, float u, float v, int seed)
        {
            float tex = Noise.Fbm(u * 2.2f, v * 2.2f, seed, 3);
            switch (b.Style)
            {
                case FloorStyle.Stones:
                    {
                        var cell = Noise.Cell(u * 0.85f, v * 0.85f, seed);
                        float edge = cell.y - cell.x;
                        if (edge < 0.06f) return b.Grout.Lit(0.9f + tex * 0.3f);
                        Color c = Color.Lerp(b.Stone, b.StoneVar, cell.z).Lit(0.82f + tex * 0.35f);
                        return c.Lit(Mathf.Lerp(0.78f, 1.04f, U.Smooth(edge / 0.3f)));
                    }
                case FloorStyle.Slabs:
                    {
                        int row = Mathf.FloorToInt(v / 1.5f);
                        float off = (row & 1) * 1.25f;
                        float su = (u + off) / 2.5f, sv = v / 1.5f;
                        int col = Mathf.FloorToInt(su);
                        float fx = su - col, fy = sv - row;
                        if (fx < 0.018f || fx > 0.982f || fy < 0.03f || fy > 0.97f) return b.Grout.Lit(0.9f + tex * 0.3f);
                        float id = Noise.Hash(col, row, seed);
                        Color c = Color.Lerp(b.Stone, b.StoneVar, id).Lit(0.85f + tex * 0.3f);
                        float bevel = Mathf.Min(Mathf.Min(fx, 1f - fx) * 2.5f, Mathf.Min(fy, 1f - fy) * 1.5f);
                        return c.Lit(Mathf.Lerp(0.85f, 1.05f, U.Smooth(bevel / 0.2f)));
                    }
                case FloorStyle.Obsidian:
                    {
                        float su = u / 3f, sv = v / 3f;
                        float fx = su - Mathf.Floor(su), fy = sv - Mathf.Floor(sv);
                        if (fx < 0.012f || fx > 0.988f || fy < 0.012f || fy > 0.988f) return Color.Lerp(b.Grout, b.Accent, 0.35f);
                        float swirl = Noise.Fbm(u * 0.6f + tex, v * 0.6f, seed + 9, 3);
                        Color c = Color.Lerp(b.Stone, b.StoneVar, swirl).Lit(0.8f + tex * 0.3f);
                        float sheen = Mathf.Clamp01(1f - Mathf.Abs(fx - fy) * 4f) * 0.12f;
                        return c.Lit(1f + sheen);
                    }
                case FloorStyle.Hall:
                    {
                        float su = u / 2f, sv = v / 2f;
                        int col = Mathf.FloorToInt(su), row = Mathf.FloorToInt(sv);
                        float fx = su - col, fy = sv - row;
                        if (fx < 0.02f || fx > 0.98f || fy < 0.02f || fy > 0.98f) return b.Grout;
                        float id = Noise.Hash(col, row, seed);
                        return Color.Lerp(b.Stone, b.StoneVar, id).Lit(0.85f + tex * 0.28f);
                    }
                default:
                    {
                        var cell = Noise.Cell(u * 0.5f, v * 0.5f, seed);
                        float edge = cell.y - cell.x;
                        Color c = Color.Lerp(b.Stone, b.StoneVar, Noise.Fbm(u * 0.7f, v * 0.7f, seed + 2, 3)).Lit(0.75f + tex * 0.45f);
                        if (edge < 0.03f) c = Color.Lerp(c, b.Grout, 0.65f);
                        return c;
                    }
            }
        }

        static void Decals(Pix p, RoomGrid g, Biome b, int seed)
        {
            var rng = new Rng(seed * 7 + 11);
            float S = PPU;
            int TW = p.W, TH = p.H;
            switch (b.DecalKind)
            {
                case 0:
                case 2:
                    for (int y = 0; y < TH; y += 1)
                        for (int x = 0; x < TW; x += 1)
                        {
                            int i = y * TW + x;
                            if (p.P[i].a < 0.99f) continue;
                            float n = Noise.Fbm(x / S * 0.35f, y / S * 0.35f, seed + 31, 3);
                            float t = Mathf.Clamp01((n - (b.DecalKind == 2 ? 0.58f : 0.62f)) * 7f);
                            if (t > 0f) p.Blend(x, y, b.Decal, t * 0.22f);
                        }
                    break;
                case 1:
                    for (int k = 0; k < 6; k++)
                    {
                        var c = Spot(g, rng);
                        if (c.x < 0) continue;
                        float rx = rng.Range(0.8f, 1.8f) * S, ry = rx * rng.Range(0.5f, 0.8f);
                        p.Ellipse(c.x, c.y, rx, ry, Pal.Venom.Lit(0.45f).A(0.55f), 4f);
                        p.Ellipse(c.x - rx * 0.2f, c.y + ry * 0.1f, rx * 0.6f, ry * 0.5f, Pal.Venom.Lit(0.7f).A(0.35f), 3f);
                    }
                    for (int k = 0; k < 10; k++)
                    {
                        var a = Spot(g, rng);
                        if (a.x < 0) continue;
                        var d = rng.OnCircle() * rng.Range(1.5f, 4f) * S;
                        p.Taper(a.x, a.y, a.x + d.x, a.y + d.y, 5f, 1f, U.Hex("2a2216").A(0.85f));
                    }
                    break;
                case 3:
                    for (int k = 0; k < 9; k++)
                    {
                        var a = Spot(g, rng);
                        if (a.x < 0) continue;
                        Vector2 pos = a;
                        var dir = rng.OnCircle();
                        for (int s = 0; s < 6; s++)
                        {
                            dir = dir.Rot(rng.Range(-40f, 40f));
                            var nxt = pos + dir * rng.Range(0.3f, 0.8f) * S;
                            p.Capsule(pos.x, pos.y, nxt.x, nxt.y, 1.2f, b.Accent.A(0.85f));
                            pos = nxt;
                        }
                    }
                    break;
            }
            for (int k = 0; k < 14; k++)
            {
                var a = Spot(g, rng);
                if (a.x < 0) continue;
                Vector2 pos = a;
                var dir = rng.OnCircle();
                int segs = rng.Range(2, 6);
                for (int s = 0; s < segs; s++)
                {
                    dir = dir.Rot(rng.Range(-50f, 50f));
                    var nxt = pos + dir * rng.Range(0.2f, 0.7f) * S;
                    p.Capsule(pos.x, pos.y, nxt.x, nxt.y, 0.9f, b.Grout.A(0.7f));
                    pos = nxt;
                }
            }
            for (int k = 0; k < 4; k++)
            {
                var a = Spot(g, rng);
                if (a.x < 0) continue;
                p.Ellipse(a.x, a.y, rng.Range(0.5f, 1.4f) * S, rng.Range(0.3f, 0.7f) * S, Pal.Blood.Lit(0.4f).A(0.35f), 6f);
            }
        }

        static Vector2 Spot(RoomGrid g, Rng rng)
        {
            for (int t = 0; t < 30; t++)
            {
                int x = rng.Range(1, g.W - 1), y = rng.Range(1, g.H - 1);
                if (g.IsFloor(x, y) && g.IsFloor(x + 1, y) && g.IsFloor(x, y + 1) && g.IsFloor(x - 1, y) && g.IsFloor(x, y - 1))
                    return new Vector2((x + rng.Value) * PPU, (y + rng.Value) * PPU);
            }
            return new Vector2(-1, -1);
        }

        /// <summary>A rune circle carved at the centre of the chamber.</summary>
        static void Carving(Pix p, RoomGrid g, Biome b)
        {
            float cx = g.W * 0.5f * PPU, cy = g.H * 0.5f * PPU;
            if (!g.IsFloor(g.W / 2, g.H / 2)) return;
            float R = 3.4f * PPU;
            Color c = b.Accent.A(0.16f);
            p.Ring(cx, cy, R, 3f, c);
            p.Ring(cx, cy, R * 0.72f, 2f, c);
            p.Ring(cx, cy, R * 0.25f, 2f, c);
            for (int i = 0; i < 16; i++)
            {
                var d = U.Dir(i * 22.5f + 11.25f);
                Runes.DrawCentered(p, (i * 5 + 3) % Runes.Count, cx + d.x * R * 0.86f, cy + d.y * R * 0.86f, 0.5f * PPU, 2.2f, c);
            }
            for (int i = 0; i < 8; i++)
            {
                var d = U.Dir(i * 45f);
                p.Capsule(cx + d.x * R * 0.27f, cy + d.y * R * 0.27f, cx + d.x * R * 0.7f, cy + d.y * R * 0.7f, 1.2f, c);
            }
        }
    }
}
