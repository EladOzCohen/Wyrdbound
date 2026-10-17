using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// Builds a chamber's 3D body: the painted floor as a lit slab, sheer rock cliffs dropping from every
    /// edge into the void, and KayKit models for the solid props. The 2D tile grid stays the source of truth
    /// for collision; this is only how it looks.
    /// </summary>
    public static class Room3D
    {
        public const float CliffDepth = 9f;
        static readonly Dictionary<string, Texture2D> cliffTex = new Dictionary<string, Texture2D>();

        public static GameObject Floor(RoomGrid g, Biome b, Sprite painted, Transform root)
        {
            var go = new GameObject("floor3d");
            go.transform.SetParent(root, false);
            var mesh = new Mesh { name = "floor", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var col = new List<Color>();
            var top = new List<int>();
            var side = new List<int>();
            Vector2 o = g.Origin;
            float iw = 1f / g.W, ih = 1f / g.H;

            for (int y = 0; y < g.H; y++)
                for (int x = 0; x < g.W; x++)
                {
                    if (!g.IsFloor(x, y)) continue;
                    float x0 = o.x + x, y0 = o.y + y, x1 = x0 + 1f, y1 = y0 + 1f;
                    // top face, clockwise as seen from the camera (above): (x0,y0) (x0,y1) (x1,y1) (x1,y0)
                    int i = v.Count;
                    v.Add(new Vector3(x0, y0, 0f)); v.Add(new Vector3(x0, y1, 0f)); v.Add(new Vector3(x1, y1, 0f)); v.Add(new Vector3(x1, y0, 0f));
                    uv.Add(new Vector2(x * iw, y * ih)); uv.Add(new Vector2(x * iw, (y + 1) * ih)); uv.Add(new Vector2((x + 1) * iw, (y + 1) * ih)); uv.Add(new Vector2((x + 1) * iw, y * ih));
                    for (int k = 0; k < 4; k++) { n.Add(Vector3.back); col.Add(Color.white); }
                    top.Add(i); top.Add(i + 1); top.Add(i + 2);
                    top.Add(i); top.Add(i + 2); top.Add(i + 3);

                    // cliffs on every edge that faces the void
                    if (!g.IsFloor(x, y - 1)) Cliff(v, n, uv, col, side, new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f), Vector3.down, x0);
                    if (!g.IsFloor(x, y + 1)) Cliff(v, n, uv, col, side, new Vector3(x1, y1, 0f), new Vector3(x0, y1, 0f), Vector3.up, x0);
                    if (!g.IsFloor(x - 1, y)) Cliff(v, n, uv, col, side, new Vector3(x0, y1, 0f), new Vector3(x0, y0, 0f), Vector3.left, y0);
                    if (!g.IsFloor(x + 1, y)) Cliff(v, n, uv, col, side, new Vector3(x1, y0, 0f), new Vector3(x1, y1, 0f), Vector3.right, y0);
                }

            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetColors(col);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(top, 0);
            mesh.SetTriangles(side, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            painted.texture.filterMode = FilterMode.Bilinear;
            painted.texture.anisoLevel = 4;
            var floorMat = Mats.NewLit(painted.texture, "floor");
            floorMat.SetFloat("_Rim", 0f);
            floorMat.SetFloat("_Gloss", b.Style == FloorStyle.Obsidian ? 0.45f : 0.12f);
            var cliffMat = Mats.NewLit(CliffTexture(b), "cliff");
            cliffMat.SetFloat("_Rim", 0.25f);
            cliffMat.SetColor("_RimColor", b.Accent.Lit(0.6f));
            cliffMat.SetFloat("_Gloss", 0.05f);
            mr.sharedMaterials = new[] { floorMat, cliffMat };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            go.AddComponent<OwnedMesh>().Init(mesh, floorMat, cliffMat);
            return go;
        }

        /// <summary>One cliff face below a floor edge running a -> b (clockwise seen from outside).</summary>
        static void Cliff(List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<Color> col, List<int> tri, Vector3 a, Vector3 b, Vector3 normal, float u0)
        {
            const int rows = 3;
            float[] depth = { 0f, 0.8f, 3f, CliffDepth };
            float[] shade = { 1f, 0.75f, 0.35f, 0f };
            int baseI = v.Count;
            for (int r = 0; r <= rows; r++)
            {
                var dz = new Vector3(0f, 0f, depth[r]);
                // jitter the lower rows outward/inward so the rock reads as rough, not extruded
                float j = r == 0 ? 0f : (Noise.Hash(Mathf.RoundToInt(a.x * 7f + a.y * 13f), r, 3) - 0.5f) * 0.35f * r;
                var off = normal * j;
                v.Add(a + dz + off); v.Add(b + dz + off);
                n.Add(normal); n.Add(normal);
                float vv = depth[r] * 0.35f;
                uv.Add(new Vector2(u0 * 0.35f, vv)); uv.Add(new Vector2((u0 + 1f) * 0.35f, vv));
                var c = new Color(shade[r], shade[r], shade[r], 1f);
                col.Add(c); col.Add(c);
            }
            for (int r = 0; r < rows; r++)
            {
                int tl = baseI + r * 2, tr = tl + 1, bl = tl + 2, br = tl + 3;
                tri.Add(tl); tri.Add(tr); tri.Add(br);
                tri.Add(tl); tri.Add(br); tri.Add(bl);
            }
        }

        static Texture2D CliffTexture(Biome b)
        {
            if (cliffTex.TryGetValue(b.Id, out var t) && t != null) return t;
            const int S = 256, PX = 5, PY = 7;
            t = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4, name = "cliff_" + b.Id };
            var px = new Color[S * S];
            int seed = b.Id.GetHashCode() & 0xffff;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    // periodic Voronoi blocks (tiles seamlessly): broken, stacked rock faces
                    float u = x / (float)S * PX, w = y / (float)S * PY;
                    int xi = Mathf.FloorToInt(u), yi = Mathf.FloorToInt(w);
                    float f1 = 9f, f2 = 9f, id = 0f;
                    for (int j = -1; j <= 1; j++)
                        for (int i = -1; i <= 1; i++)
                        {
                            int cx = xi + i, cy = yi + j;
                            int hx = ((cx % PX) + PX) % PX, hy = ((cy % PY) + PY) % PY;
                            float qx = cx + 0.15f + 0.7f * Noise.Hash(hx, hy, seed);
                            float qy = cy + 0.15f + 0.7f * Noise.Hash(hx, hy, seed + 77);
                            float dx = (qx - u) * 0.75f, dy = (qy - w) * 1.35f;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            if (d < f1) { f2 = f1; f1 = d; id = Noise.Hash(hx, hy, seed + 991); }
                            else if (d < f2) f2 = d;
                        }
                    float edge = f2 - f1;
                    float grain = Noise.Value(x * 0.21f, y * 0.21f, seed + 5) * 0.18f + Noise.Value(x * 0.05f, y * 0.05f, seed + 6) * 0.2f;
                    float k = 0.62f + id * 0.45f + grain;
                    k *= Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(edge / 0.09f));       // dark cracks between blocks
                    k *= 1f + Mathf.Clamp01(0.25f - f1) * 0.6f;                      // blocks bulge toward their middle
                    var col = Color.Lerp(b.Cliff, b.Stone, 0.35f).Lit(1.25f * k);
                    px[y * S + x] = new Color(col.r, col.g, col.b, 1f);
                }
            t.SetPixels(px);
            t.Apply(true);
            cliffTex[b.Id] = t;
            return t;
        }

        // ------------------------------------------------------------------ props

        sealed class PropModel
        {
            public string Path;
            public float Height;   // world height to scale the model to
            public bool Tint = true;
            public PropModel(string p, float h, bool tint = true) { Path = p; Height = h; Tint = tint; }
        }

        const string Dun = "KayKit/Dungeon/Models/";
        static readonly Dictionary<string, PropModel> props = new Dictionary<string, PropModel>
        {
            { "pillar", new PropModel(Dun + "pillar_decorated", 3.0f) },
            { "stone", new PropModel(Dun + "rubble_half", 1.3f) },
            { "chest", new PropModel(Dun + "chest_gold", 0.8f, false) },
            { "table", new PropModel(Dun + "table_medium", 0.75f, false) },
        };

        /// <summary>A 3D prop for this kind, or null when the kind has no model (it stays an upright sprite).</summary>
        public static GameObject Prop(Transform root, string kind, Vector2 pos, float radius, Biome b, int seed)
        {
            if (!props.TryGetValue(kind, out var pm)) return null;
            var prefab = Resources.Load<GameObject>(pm.Path);
            if (prefab == null) return null;
            var go = new GameObject("prop_" + kind);
            go.transform.SetParent(root, false);
            go.transform.position = View3D.At(pos);
            var rng = new Rng(seed);
            go.transform.rotation = View3D.Face(U.Dir(rng.Range(0, 4) * 90f + rng.Range(-8f, 8f)));
            var inst = Object.Instantiate(prefab, go.transform, false);
            // measure, then scale to the wanted height and centre on the obstacle
            var bounds = Bounds(inst);
            float s = pm.Height / Mathf.Max(0.05f, bounds.size.y);
            inst.transform.localScale = Vector3.one * s;
            var c = bounds.center * s;
            inst.transform.localPosition = new Vector3(-c.x, 0f, -c.z);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var src = r.sharedMaterial;
                var m = PropMat(kind, src != null ? src.mainTexture : null, b, pm.Tint);
                var arr = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < arr.Length; i++) arr[i] = m;
                r.sharedMaterials = arr;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            return go;
        }

        static readonly Dictionary<string, Material> propMats = new Dictionary<string, Material>();

        static Material PropMat(string kind, Texture tex, Biome b, bool tint)
        {
            string key = kind + "|" + b.Id + "|" + (tex != null ? tex.name : "-");
            if (propMats.TryGetValue(key, out var m) && m != null) return m;
            m = Mats.NewLit(tex, "prop_" + key);
            if (tint)
            {
                m.SetColor("_Tint", Color.Lerp(b.StoneVar, Color.white, 0.25f));
                m.SetFloat("_TintAmt", 0.55f);
            }
            m.SetColor("_RimColor", b.Accent.Lit(0.7f));
            m.SetFloat("_Rim", 0.35f);
            propMats[key] = m;
            return m;
        }

        static Bounds Bounds(GameObject go)
        {
            var b = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                var m = go.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                var c = m.MultiplyPoint3x4(mb.center);
                var e = mb.extents;
                var ax = m.MultiplyVector(new Vector3(e.x, 0, 0));
                var ay = m.MultiplyVector(new Vector3(0, e.y, 0));
                var az = m.MultiplyVector(new Vector3(0, 0, e.z));
                var ext = new Vector3(Mathf.Abs(ax.x) + Mathf.Abs(ay.x) + Mathf.Abs(az.x), Mathf.Abs(ax.y) + Mathf.Abs(ay.y) + Mathf.Abs(az.y), Mathf.Abs(ax.z) + Mathf.Abs(ay.z) + Mathf.Abs(az.z));
                var wb = new Bounds(c, ext * 2f);
                if (first) { b = wb; first = false; } else b.Encapsulate(wb);
            }
            return b;
        }
    }

    /// <summary>Frees a runtime-built mesh and its materials with the chamber.</summary>
    public sealed class OwnedMesh : MonoBehaviour
    {
        Mesh mesh;
        Material[] mats;
        public void Init(Mesh m, params Material[] ms) { mesh = m; mats = ms; }
        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (mats != null) foreach (var m in mats) if (m != null) Destroy(m);
        }
    }
}
