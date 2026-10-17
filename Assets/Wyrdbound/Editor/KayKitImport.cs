using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Wyrdbound.EditorTools
{
    /// <summary>
    /// Import rules for the KayKit (CC0) models under Assets/ThirdParty/Resources/KayKit.
    /// Characters carry all their clips and use the Legacy animation system, so the game can play and
    /// scrub them directly without animator controllers. Props import without rigs or animation.
    /// </summary>
    public sealed class KayKitImport : AssetPostprocessor
    {
        static bool IsKayKit(string path) => path.Replace('\\', '/').Contains("/Resources/KayKit/");
        static bool IsCharacter(string path) => IsKayKit(path) && path.Replace('\\', '/').Contains("/Characters/");

        void OnPreprocessModel()
        {
            if (!IsKayKit(assetPath)) return;
            var mi = (ModelImporter)assetImporter;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.isReadable = false;
            if (IsCharacter(assetPath))
            {
                mi.animationType = ModelImporterAnimationType.Legacy;
                mi.importAnimation = true;
                mi.animationCompression = ModelImporterAnimationCompression.KeyframeReductionAndCompression;
            }
            else
            {
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
            }
        }

        void OnPreprocessTexture()
        {
            if (!IsKayKit(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = true;
        }

        /// <summary>Write every character's clip list and every prop's size to KayKitReport.txt (for wiring them into the game).</summary>
        [MenuItem("Wyrdbound/KayKit Report")]
        public static void Report()
        {
            var sb = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/ThirdParty/Resources/KayKit" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var inst = (GameObject)Object.Instantiate(go);
                var b = new Bounds();
                bool first = true;
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                {
                    if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
                }
                sb.Append(path).Append("  size=").Append(b.size.ToString("F2")).Append(" center=").Append(b.center.ToString("F2"));
                var anim = inst.GetComponent<Animation>();
                if (anim != null)
                {
                    sb.Append("\n  clips:");
                    foreach (AnimationState st in anim) sb.Append(' ').Append(st.name).Append('(').Append(st.length.ToString("F2")).Append(')');
                }
                if (IsCharacter(path))
                {
                    sb.Append("\n  bones:");
                    foreach (var t in inst.GetComponentsInChildren<Transform>()) if (t.name.ToLower().Contains("hand") || t.name.ToLower().Contains("head")) sb.Append(' ').Append(t.name);
                    sb.Append("\n  renderers:");
                    foreach (var r in inst.GetComponentsInChildren<Renderer>()) sb.Append(' ').Append(r.name).Append(r.enabled ? "" : "(off)").Append('[').Append(r.sharedMaterial != null ? r.sharedMaterial.name : "-").Append(']');
                }
                sb.Append('\n');
                Object.DestroyImmediate(inst);
            }
            File.WriteAllText("KayKitReport.txt", sb.ToString());
            Debug.Log("[Wyrdbound] KayKit report written.");
        }
    }
}
