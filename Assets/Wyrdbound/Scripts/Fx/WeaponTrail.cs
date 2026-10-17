using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A glowing ribbon swept by the weapon between grip and tip during swings (a "smear").</summary>
    public sealed class WeaponTrail : MonoBehaviour
    {
        struct Sample
        {
            public Vector3 A, B;
            public float T;
            public Color C;
        }

        const float Life = 0.14f;
        readonly List<Sample> samples = new List<Sample>();
        Mesh mesh;
        MeshRenderer mr;
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color> cols = new List<Color>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<int> tris = new List<int>();
        static Texture2D grad;

        public static WeaponTrail Create(Transform parent)
        {
            var go = new GameObject("weaponTrail");
            go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;
            var t = go.AddComponent<WeaponTrail>();
            var mf = go.AddComponent<MeshFilter>();
            t.mr = go.AddComponent<MeshRenderer>();
            t.mesh = new Mesh();
            t.mesh.MarkDynamic();
            mf.sharedMesh = t.mesh;
            var mat = new Material(Mats.AdditiveTop);
            mat.mainTexture = Grad();
            t.mr.sharedMaterial = mat;
            t.mr.sortingOrder = Sort.FxHigh - 5;
            return t;
        }

        static Texture2D Grad()
        {
            if (grad != null) return grad;
            grad = new Texture2D(4, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var c = new Color[4 * 32];
            for (int y = 0; y < 32; y++)
            {
                float v = y / 31f;
                float a = Mathf.Pow(v, 1.6f);
                for (int x = 0; x < 4; x++) c[y * 4 + x] = new Color(a, a, a, a);
            }
            grad.SetPixels(c);
            grad.Apply();
            return grad;
        }

        public void Feed(bool active, Vector3 a, Vector3 b, Color c)
        {
            float now = Time.time;
            if (active) samples.Add(new Sample { A = a, B = b, T = now, C = c });
            samples.RemoveAll(s => now - s.T > Life);
            Rebuild(now);
        }

        void Rebuild(float now)
        {
            mesh.Clear();
            if (samples.Count < 2) return;
            verts.Clear(); cols.Clear(); uvs.Clear(); tris.Clear();
            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                float age = Mathf.Clamp01((now - s.T) / Life);
                float a = (1f - age) * (1f - age);
                verts.Add(s.A + Vector3.Lerp(Vector3.zero, s.B - s.A, 0.15f));
                verts.Add(s.B);
                cols.Add(s.C.A(a * 0.9f));
                cols.Add(s.C.A(a * 0.9f));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                if (i > 0)
                {
                    int k = i * 2;
                    tris.Add(k - 2); tris.Add(k - 1); tris.Add(k);
                    tris.Add(k - 1); tris.Add(k + 1); tris.Add(k);
                }
            }
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }
    }
}
