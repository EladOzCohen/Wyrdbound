using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Procedural chamber layouts: floating platforms over the void of Ginnungagap.</summary>
    public static class RoomGen
    {
        const byte F = RoomGrid.Floor, V = RoomGrid.Void;

        public static RoomGrid Combat(Rng rng, Biome b, int doors, bool big)
        {
            int w = rng.Range(24, 31) + (big ? 4 : 0);
            int h = rng.Range(17, 21) + (big ? 2 : 0);
            var g = new RoomGrid(w, h);
            int x0 = 1, x1 = w - 2, y0 = 2, y1 = h - 3;
            int shape = rng.Range(0, 7);

            if (shape == 4)
            {
                int cw = Mathf.RoundToInt(w * 0.55f), ch = Mathf.RoundToInt(h * 0.55f);
                g.FillRect(w / 2 - cw / 2, h / 2 - ch / 2, w / 2 + cw / 2, h / 2 + ch / 2, F);
                for (int i = 0; i < 4; i++)
                {
                    int rw = rng.Range(6, 13), rh = rng.Range(4, 9);
                    int rx = rng.Range(x0, x1 - rw + 1), ry = rng.Range(y0, y1 - rh + 1);
                    g.FillRect(rx, ry, rx + rw - 1, ry + rh - 1, F);
                }
            }
            else g.FillRect(x0, y0, x1, y1, F);

            switch (shape)
            {
                case 0: Chamfer(g, x0, y0, x1, y1, 3); break;
                case 1:
                    {
                        int cw = w / 5, ch = h / 4;
                        g.FillRect(x0, y0, x0 + cw - 1, y0 + ch - 1, V);
                        g.FillRect(x1 - cw + 1, y0, x1, y0 + ch - 1, V);
                        g.FillRect(x0, y1 - ch + 1, x0 + cw - 1, y1, V);
                        g.FillRect(x1 - cw + 1, y1 - ch + 1, x1, y1, V);
                        break;
                    }
                case 2:
                    {
                        Chamfer(g, x0, y0, x1, y1, 2);
                        int pw = w / 4, ph = h / 4;
                        g.FillRect(w / 2 - pw / 2, h / 2 - ph / 2 + 1, w / 2 + pw / 2, h / 2 + ph / 2 + 1, V);
                        break;
                    }
                case 3:
                    {
                        Chamfer(g, x0, y0, x1, y1, 2);
                        int c = w / 2;
                        g.FillRect(c - 1, y0, c + 1, y1, V);
                        int by = rng.Range(y0 + 5, y1 - 3);
                        g.FillRect(c - 1, by, c + 1, by + 2, F);
                        break;
                    }
                case 5: Chamfer(g, x0, y0, x1, y1, Mathf.Min(w, h) / 3); break;
                case 6:
                    {
                        Chamfer(g, x0, y0, x1, y1, 2);
                        int my = h / 2;
                        g.FillRect(x0, my, x1, my + 1, V);
                        int n = rng.Range(2, 4);
                        for (int i = 0; i < n; i++)
                        {
                            int bx = Mathf.RoundToInt(Mathf.Lerp(x0 + 3, x1 - 4, (i + 0.5f) / n));
                            g.FillRect(bx, my, bx + 2, my + 1, F);
                        }
                        break;
                    }
            }

            int pits = shape == 2 || shape == 3 ? rng.Range(0, 2) : rng.Range(0, 4);
            for (int i = 0; i < pits; i++)
            {
                int pw = rng.Range(2, 4), ph = rng.Range(2, 4);
                int px = rng.Range(x0 + 3, x1 - 3 - pw), py = rng.Range(y0 + 5, y1 - 3 - ph);
                if (Mathf.Abs(px + pw / 2 - w / 2) < 3) continue;
                g.FillRect(px, py, px + pw - 1, py + ph - 1, V);
            }

            g.Smooth();
            Finish(g, rng, b, doors, 2, y1);
            Props(g, rng, b, rng.Range(2, 7));
            return g;
        }

        static void Chamfer(RoomGrid g, int x0, int y0, int x1, int y1, int c)
        {
            for (int i = 0; i < c; i++)
                for (int j = 0; j < c - i; j++)
                {
                    g[x0 + j, y0 + i] = V;
                    g[x1 - j, y0 + i] = V;
                    g[x0 + j, y1 - i] = V;
                    g[x1 - j, y1 - i] = V;
                }
        }

        static int[] DoorCols(int w, int doors)
        {
            switch (doors)
            {
                case 0: return new int[0];
                case 1: return new[] { w / 2 };
                case 2: return new[] { Mathf.RoundToInt(w * 0.3f), Mathf.RoundToInt(w * 0.7f) };
                default: return new[] { Mathf.RoundToInt(w * 0.2f), w / 2, Mathf.RoundToInt(w * 0.8f) };
            }
        }

        /// <summary>Carve entry and exit corridors, prune unreachable floor, record spots.</summary>
        static void Finish(RoomGrid g, Rng rng, Biome b, int doors, int y0, int yTop)
        {
            int ex = g.W / 2;
            g.FillRect(ex - 1, y0, ex + 1, y0 + 3, F);
            var cols = DoorCols(g.W, doors);
            foreach (int c in cols)
            {
                g.FillRect(c - 1, yTop - 1, c + 1, yTop, F);
                for (int y = yTop - 2; y > y0; y--)
                {
                    bool hit = g.IsFloor(c, y) && g.IsFloor(c - 1, y) && g.IsFloor(c + 1, y);
                    g.FillRect(c - 1, y, c + 1, y, F);
                    if (hit) break;
                }
            }
            g.KeepConnected(ex, y0 + 1);
            g.Entry = g.Center(ex, y0 + 1);
            g.DoorSpots.Clear();
            foreach (int c in cols) g.DoorSpots.Add(g.Origin + new Vector2(c + 0.5f, yTop + 1f));
        }

        static void Props(RoomGrid g, Rng rng, Biome b, int count)
        {
            if (b.Props == null || b.Props.Length == 0) return;
            bool mirror = rng.Chance(0.5f);
            int placed = 0;
            for (int tries = 0; tries < 200 && placed < count; tries++)
            {
                var p = g.Origin + new Vector2(rng.Range(2f, g.W - 2f), rng.Range(3f, g.H - 3f));
                string kind = rng.Pick(b.Props);
                if (!PropOk(g, p)) continue;
                Place(g, p, kind, rng);
                placed++;
                if (mirror && placed < count)
                {
                    var m = new Vector2(-p.x, p.y);
                    if (PropOk(g, m)) { Place(g, m, kind, rng); placed++; }
                }
            }
            int decor = rng.Range(3, 9);
            for (int i = 0; i < decor; i++)
            {
                var p = g.Origin + new Vector2(rng.Range(2f, g.W - 2f), rng.Range(3f, g.H - 3f));
                if (g.Free(p, 0.6f)) g.Decor.Add(new Obstacle(p, 0.4f, false, rng.Chance(0.5f) ? "bones" : "rubble"));
            }
        }

        static bool PropOk(RoomGrid g, Vector2 p)
        {
            if (!g.Free(p, 1.4f) || !g.FreeOfObstacles(p, 2.2f)) return false;
            if (Vector2.Distance(p, g.Entry) < 4f) return false;
            foreach (var d in g.DoorSpots) if (Vector2.Distance(p, d) < 3.5f) return false;
            if (p.magnitude < 2.5f) return false;
            return true;
        }

        static void Place(RoomGrid g, Vector2 p, string kind, Rng rng)
        {
            if (kind == "bones") { g.Decor.Add(new Obstacle(p, 0.5f, false, kind)); return; }
            float r = kind == "tree" || kind == "statue" ? 0.6f : kind == "brazier" ? 0.4f : 0.5f;
            g.Obstacles.Add(new Obstacle(p, r, kind != "brazier", kind));
        }

        /// <summary>Round boss arena.</summary>
        public static RoomGrid Arena(Rng rng, Biome b, int radius, bool pillars)
        {
            int w = radius * 2 + 6, h = radius * 2 + 7;
            var g = new RoomGrid(w, h);
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x + 0.5f - cx, dy = (y + 0.5f - cy) * 1.08f;
                    if (dx * dx + dy * dy < radius * radius) g[x, y] = F;
                }
            int top = 0;
            for (int y = h - 1; y >= 0; y--) if (g.IsFloor(w / 2, y)) { top = y; break; }
            int bottom = 0;
            for (int y = 0; y < h; y++) if (g.IsFloor(w / 2, y)) { bottom = y; break; }
            g.FillRect(w / 2 - 1, bottom - 1, w / 2 + 1, bottom + 2, F);
            g.FillRect(w / 2 - 1, top, w / 2 + 1, top + 1, F);
            g.KeepConnected(w / 2, h / 2);
            g.Entry = g.Center(w / 2, bottom + 1);
            g.DoorSpots.Add(g.Origin + new Vector2(w / 2 + 0.5f, top + 2f));
            if (pillars)
                for (int i = 0; i < 4; i++)
                {
                    var p = U.Dir(45f + i * 90f) * radius * 0.62f;
                    g.Obstacles.Add(new Obstacle(p, 0.55f, true, "pillar"));
                }
            return g;
        }

        /// <summary>Small chamber for shops, springs, passages and story encounters.</summary>
        public static RoomGrid Small(Rng rng, Biome b, int doors)
        {
            int w = 20, h = 15;
            var g = new RoomGrid(w, h);
            int x0 = 1, x1 = w - 2, y0 = 2, y1 = h - 3;
            g.FillRect(x0, y0, x1, y1, F);
            Chamfer(g, x0, y0, x1, y1, 3);
            Finish(g, rng, b, doors, y0, y1);
            for (int i = 0; i < 4; i++)
            {
                var p = g.Origin + new Vector2(rng.Range(2f, w - 2f), rng.Range(3f, h - 3f));
                if (g.Free(p, 0.6f)) g.Decor.Add(new Obstacle(p, 0.4f, false, "rubble"));
            }
            return g;
        }

        /// <summary>Éljúðnir, Hel's hall. Station positions are fixed (see Hub).</summary>
        public static RoomGrid Hall()
        {
            int w = 44, h = 32;
            var g = new RoomGrid(w, h);
            g.FillRect(4, 3, 39, 27, F);
            Chamfer(g, 4, 3, 39, 27, 4);
            g.FillRect(17, 26, 26, 29, F);
            g.FillRect(1, 12, 4, 18, F);
            g.FillRect(39, 12, 42, 18, F);
            g.FillRect(18, 1, 25, 3, F);
            g.KeepConnected(w / 2, h / 2);
            g.Entry = new Vector2(0, -10.5f);
            foreach (float x in new[] { -9f, 9f })
                foreach (float y in new[] { -5f, 1f, 7f })
                    g.Obstacles.Add(new Obstacle(new Vector2(x, y), 0.6f, true, "pillar"));
            g.Obstacles.Add(new Obstacle(new Vector2(-4.5f, 11.5f), 0.4f, false, "brazier"));
            g.Obstacles.Add(new Obstacle(new Vector2(4.5f, 11.5f), 0.4f, false, "brazier"));
            g.Obstacles.Add(new Obstacle(new Vector2(-15.5f, -9.5f), 0.4f, false, "brazier"));
            return g;
        }

        /// <summary>The cave where Loki lies bound.</summary>
        public static RoomGrid Cave()
        {
            int w = 30, h = 22;
            var g = new RoomGrid(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - w * 0.5f) / (w * 0.42f), dy = (y + 0.5f - h * 0.5f) / (h * 0.42f);
                    if (dx * dx + dy * dy < 1f) g[x, y] = F;
                }
            g.FillRect(w / 2 - 1, 1, w / 2 + 1, 4, F);
            g.KeepConnected(w / 2, h / 2);
            g.Entry = g.Center(w / 2, 3);
            g.Obstacles.Add(new Obstacle(new Vector2(-6f, 2f), 0.6f, true, "stone"));
            g.Obstacles.Add(new Obstacle(new Vector2(6.5f, 1f), 0.6f, true, "stone"));
            g.Obstacles.Add(new Obstacle(new Vector2(0f, 6.2f), 1.1f, true, "binding"));
            return g;
        }
    }
}
