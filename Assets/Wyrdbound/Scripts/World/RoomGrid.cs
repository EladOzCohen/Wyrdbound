using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    public struct Obstacle
    {
        public Vector2 Pos;
        public float R;
        public bool BlocksShots;
        public string Kind;
        public Obstacle(Vector2 p, float r, bool blocks, string kind) { Pos = p; R = r; BlocksShots = blocks; Kind = kind; }
    }

    /// <summary>
    /// A chamber: a tile grid of floor over the void, plus round obstacles.
    /// The room is centred on the world origin. All collision is resolved here (no physics engine).
    /// </summary>
    public sealed class RoomGrid
    {
        public const byte Void = 0, Floor = 1;

        public readonly int W, H;
        public readonly byte[] T;
        public readonly Vector2 Origin;
        public readonly List<Obstacle> Obstacles = new List<Obstacle>();
        public readonly List<Obstacle> Decor = new List<Obstacle>();
        public Vector2 Entry;
        public readonly List<Vector2> DoorSpots = new List<Vector2>();

        static readonly Vector2[] Ring8 =
        {
            new Vector2(1, 0), new Vector2(0.7071f, 0.7071f), new Vector2(0, 1), new Vector2(-0.7071f, 0.7071f),
            new Vector2(-1, 0), new Vector2(-0.7071f, -0.7071f), new Vector2(0, -1), new Vector2(0.7071f, -0.7071f)
        };

        public RoomGrid(int w, int h)
        {
            W = w; H = h;
            T = new byte[w * h];
            Origin = new Vector2(-w * 0.5f, -h * 0.5f);
        }

        public byte this[int x, int y]
        {
            get => (x < 0 || y < 0 || x >= W || y >= H) ? Void : T[y * W + x];
            set { if (x >= 0 && y >= 0 && x < W && y < H) T[y * W + x] = value; }
        }

        public bool IsFloor(int x, int y) => this[x, y] == Floor;

        public Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x - Origin.x), Mathf.FloorToInt(p.y - Origin.y));

        public bool IsFloorAt(Vector2 p)
        {
            var c = Cell(p);
            return IsFloor(c.x, c.y);
        }

        public Vector2 Center(int x, int y) => Origin + new Vector2(x + 0.5f, y + 0.5f);
        public Rect Bounds => new Rect(Origin, new Vector2(W, H));

        public void FillRect(int x0, int y0, int x1, int y1, byte v)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    this[x, y] = v;
        }

        /// <summary>Can a circle of radius r stand here (all of it over floor)?</summary>
        public bool Free(Vector2 p, float r)
        {
            if (!IsFloorAt(p)) return false;
            float k = r * 0.8f;
            for (int i = 0; i < 8; i++)
                if (!IsFloorAt(p + Ring8[i] * k)) return false;
            return true;
        }

        public bool FreeOfObstacles(Vector2 p, float r)
        {
            foreach (var o in Obstacles)
                if ((p - o.Pos).sqrMagnitude < (r + o.R) * (r + o.R)) return false;
            return true;
        }

        bool Inside(Vector2 p, float r)
        {
            var b = Bounds;
            return p.x > b.xMin + r && p.x < b.xMax - r && p.y > b.yMin + r && p.y < b.yMax - r;
        }

        bool Ok(Vector2 p, float r, bool flying) => flying ? Inside(p, r) : Free(p, r);

        /// <summary>Slide a circle through the room. Returns the resolved position.</summary>
        public Vector2 Move(Vector2 pos, Vector2 delta, float r, bool flying, out bool blocked)
        {
            blocked = false;
            float len = delta.magnitude;
            if (len < 1e-6f) return pos;
            int steps = Mathf.Clamp(Mathf.CeilToInt(len / Mathf.Max(0.05f, r * 0.4f)), 1, 16);
            Vector2 d = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                var nx = new Vector2(pos.x + d.x, pos.y);
                if (Ok(nx, r, flying)) pos = nx; else blocked = true;
                var ny = new Vector2(pos.x, pos.y + d.y);
                if (Ok(ny, r, flying)) pos = ny; else blocked = true;
                if (flying) continue;
                for (int k = 0; k < Obstacles.Count; k++)
                {
                    var o = Obstacles[k];
                    Vector2 diff = pos - o.Pos;
                    float min = r + o.R;
                    float sq = diff.sqrMagnitude;
                    if (sq >= min * min) continue;
                    float dist = Mathf.Sqrt(sq);
                    Vector2 n = dist > 1e-4f ? diff / dist : Vector2.up;
                    var np = o.Pos + n * min;
                    if (Ok(np, r, false)) pos = np;
                    blocked = true;
                }
            }
            return pos;
        }

        public bool BlocksShot(Vector2 p)
        {
            foreach (var o in Obstacles)
                if (o.BlocksShots && (p - o.Pos).sqrMagnitude < o.R * o.R) return true;
            return false;
        }

        /// <summary>Is the straight path from a to b walkable for radius r?</summary>
        public bool PathClear(Vector2 a, Vector2 b, float r)
        {
            float len = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 0.35f));
            for (int i = 1; i <= n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                if (!Free(p, r) || !FreeOfObstacles(p, r)) return false;
            }
            return true;
        }

        public Vector2 RandomFloor(Rng rng, float r, Vector2 avoid, float avoidDist, int tries = 80)
        {
            Vector2 best = Entry;
            float bestScore = -1f;
            for (int i = 0; i < tries; i++)
            {
                var p = Origin + new Vector2(rng.Range(1f, W - 1f), rng.Range(1f, H - 1f));
                if (!Free(p, r + 0.3f) || !FreeOfObstacles(p, r + 0.2f)) continue;
                float d = Vector2.Distance(p, avoid);
                if (d >= avoidDist) return p;
                if (d > bestScore) { bestScore = d; best = p; }
            }
            return best;
        }

        public Vector2 NearestFloor(Vector2 p, float r)
        {
            if (Free(p, r)) return p;
            for (float rad = 0.25f; rad < 12f; rad += 0.25f)
                for (int i = 0; i < 16; i++)
                {
                    var q = p + U.Dir(i * 22.5f) * rad;
                    if (Free(q, r) && FreeOfObstacles(q, r)) return q;
                }
            return Entry;
        }

        /// <summary>Remove floor not reachable from (sx, sy).</summary>
        public void KeepConnected(int sx, int sy)
        {
            var seen = new bool[W * H];
            var q = new Queue<Vector2Int>();
            if (!IsFloor(sx, sy)) return;
            q.Enqueue(new Vector2Int(sx, sy));
            seen[sy * W + sx] = true;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    int nx = c.x + (i == 0 ? 1 : i == 1 ? -1 : 0), ny = c.y + (i == 2 ? 1 : i == 3 ? -1 : 0);
                    if (!IsFloor(nx, ny) || seen[ny * W + nx]) continue;
                    seen[ny * W + nx] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
            for (int i = 0; i < T.Length; i++) if (!seen[i]) T[i] = Void;
        }

        /// <summary>Remove one-tile slivers that look bad and trap entities.</summary>
        public void Smooth()
        {
            for (int pass = 0; pass < 2; pass++)
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        if (!IsFloor(x, y)) continue;
                        bool h = IsFloor(x - 1, y) || IsFloor(x + 1, y);
                        bool v = IsFloor(x, y - 1) || IsFloor(x, y + 1);
                        bool h2 = IsFloor(x - 1, y) && IsFloor(x + 1, y);
                        bool v2 = IsFloor(x, y - 1) && IsFloor(x, y + 1);
                        if (!h || !v) this[x, y] = Void;
                        else if (!h2 && !v2 && !(IsFloor(x - 1, y - 1) || IsFloor(x + 1, y + 1) || IsFloor(x - 1, y + 1) || IsFloor(x + 1, y - 1))) this[x, y] = Void;
                    }
        }
    }
}
