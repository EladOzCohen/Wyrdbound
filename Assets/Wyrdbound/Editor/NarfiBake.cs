using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Wyrdbound.EditorTools
{
    /// <summary>
    /// Narfi is a MakeHuman body (tools/blender/narfi.py -> Art/Narfi/Narfi.fbx) on a Mixamo-named skeleton, but the
    /// game plays Legacy clips made for KayKit's rig. This tool retargets KayKit's clips onto him through Unity's
    /// Humanoid system once, in the editor, and bakes the result into plain Legacy clips, so at runtime he is just
    /// another Model3D (Resources/Models/Narfi.prefab) with an Animation component.
    ///   Menu: Wyrdbound > Bake Narfi     Batch: -executeMethod Wyrdbound.EditorTools.NarfiBake.Bake
    /// </summary>
    public sealed class NarfiBake : AssetPostprocessor
    {
        const string Dir = "Assets/Wyrdbound/Art/Narfi/";
        const string NarfiFbx = Dir + "Narfi.fbx", AnimsFbx = Dir + "KayKitAnims.fbx", ClipDir = Dir + "Clips/";
        const string KayKitSource = "Assets/ThirdParty/Resources/KayKit/Adventurers/Characters/RogueHooded.fbx";
        const string Prefab = "Assets/Wyrdbound/Resources/Models/Narfi.prefab";
        const float Fps = 30f, ArmsIn = 22f, WeaponScale = 0.62f;

        static readonly string[] Clips =
        {
            "Idle", "Walking_A", "Running_A", "Dodge_Forward", "Dodge_Backward", "Dodge_Left", "Dodge_Right",
            "1H_Melee_Attack_Chop", "1H_Melee_Attack_Slice_Diagonal", "1H_Melee_Attack_Slice_Horizontal", "1H_Melee_Attack_Stab",
            "2H_Melee_Attack_Chop", "2H_Melee_Attack_Spinning", "2H_Melee_Attack_Stab", "1H_Ranged_Shoot", "1H_Ranged_Aiming",
            "Spellcasting", "Spellcast_Shoot", "Throw", "Hit_A", "Hit_B", "Death_A", "Death_B", "Cheer", "Interact", "PickUp",
        };
        static readonly HashSet<string> Loops = new HashSet<string> { "Idle", "Walking_A", "Running_A", "Spellcasting", "1H_Ranged_Aiming", "2H_Melee_Attack_Spinning" };

        // Unity human bone -> KayKit bone / Mixamo bone
        static readonly string[,] Map =
        {
            { "Hips", "hips", "Hips" }, { "Spine", "spine", "Spine" }, { "Chest", "chest", "Spine1" }, { "UpperChest", null, "Spine2" },
            { "Neck", null, "Neck" }, { "Head", "head", "Head" },
            { "LeftShoulder", null, "LeftShoulder" }, { "LeftUpperArm", "upperarm.l", "LeftArm" }, { "LeftLowerArm", "lowerarm.l", "LeftForeArm" }, { "LeftHand", "wrist.l", "LeftHand" },
            { "RightShoulder", null, "RightShoulder" }, { "RightUpperArm", "upperarm.r", "RightArm" }, { "RightLowerArm", "lowerarm.r", "RightForeArm" }, { "RightHand", "wrist.r", "RightHand" },
            { "LeftUpperLeg", "upperleg.l", "LeftUpLeg" }, { "LeftLowerLeg", "lowerleg.l", "LeftLeg" }, { "LeftFoot", "foot.l", "LeftFoot" }, { "LeftToes", "toes.l", "LeftToeBase" },
            { "RightUpperLeg", "upperleg.r", "RightUpLeg" }, { "RightLowerLeg", "lowerleg.r", "RightLeg" }, { "RightFoot", "foot.r", "RightFoot" }, { "RightToes", "toes.r", "RightToeBase" },
        };

        public override uint GetVersion() => 2;

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialSearch = ModelImporterMaterialSearch.Local;
            mi.importAnimation = assetPath == AnimsFbx;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var ti = (TextureImporter)assetImporter;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 2048;
        }

        [MenuItem("Wyrdbound/Bake Narfi")]
        public static void Bake()
        {
            var log = new StringBuilder();
            if (!File.Exists(AnimsFbx))
            {
                AssetDatabase.CopyAsset(KayKitSource, AnimsFbx);
                AssetDatabase.ImportAsset(AnimsFbx, ImportAssetOptions.ForceUpdate);
            }
            MakeHumanoid(AnimsFbx, 1, "", true);
            MakeHumanoid(NarfiFbx, 2, "mixamorig:", false);

            var narfiAsset = AssetDatabase.LoadAssetAtPath<GameObject>(NarfiFbx);
            var narfi = (GameObject)Object.Instantiate(narfiAsset);
            var kk = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(AnimsFbx));
            var na = narfi.GetComponent<Animator>();
            var ka = kk.GetComponent<Animator>();
            log.AppendLine("Narfi avatar human=" + (na.avatar != null && na.avatar.isHuman) + " valid=" + (na.avatar != null && na.avatar.isValid));
            log.AppendLine("KayKit avatar human=" + (ka.avatar != null && ka.avatar.isHuman) + " valid=" + (ka.avatar != null && ka.avatar.isValid));

            foreach (var rr in narfi.GetComponentsInChildren<Renderer>())
                foreach (var m in rr.sharedMaterials)
                    log.AppendLine($"  {rr.name}: {(m != null ? m.name : "-")} tex={(m != null && m.mainTexture != null ? m.mainTexture.name : "none")} col={(m != null ? m.color.ToString() : "")}");
            var source = new Dictionary<string, AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AnimsFbx))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) source[c.name.Replace("Rig|", "")] = c;

            // the bones to record: Narfi's whole skeleton (meshes are skinned to it)
            var hips = Find(narfi.transform, "mixamorig:Hips");
            var bones = new List<Transform>(hips.GetComponentsInChildren<Transform>());

            Directory.CreateDirectory(ClipDir);
            AnimationMode.StartAnimationMode();
            var baked = new List<AnimationClip>();
            foreach (var name in Clips)
            {
                if (!source.TryGetValue(name, out var src)) { log.AppendLine("missing clip " + name); continue; }
                var clip = BakeClip(narfi, na, src, name, bones);
                log.AppendLine($"  {name}: {src.length:F2}s, {AnimationUtility.GetCurveBindings(clip).Length} curves");
                var path = ClipDir + name + ".anim";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(clip, path);
                baked.Add(clip);
            }

            // KayKit weapons sit in a "handslot.r" bone: give Narfi the same slot, posed and scaled to match
            Sample(na, source["Idle"], 0f);
            Sample(ka, source["Idle"], 0f);
            // sizes compare by head height (KayKit's hood would skew a bounds measure)
            float hn = Find(narfi.transform, "mixamorig:Head").position.y, hk = Find(kk.transform, "head").position.y, r = hn / hk;
            log.AppendLine($"head heights narfi={hn:F3} kaykit={hk:F3}; narfi model height={Height(narfi):F3}");
            foreach (var side in new[] { "r", "l" })
            {
                var kSlot = Find(kk.transform, "handslot." + side);
                var kHand = Find(kk.transform, "wrist." + side);
                var nHand = Find(narfi.transform, side == "r" ? "mixamorig:RightHand" : "mixamorig:LeftHand");
                var slot = new GameObject("handslot." + side).transform;
                slot.SetParent(nHand, false);
                slot.rotation = narfi.transform.rotation * (Quaternion.Inverse(kk.transform.rotation) * kSlot.rotation);
                slot.position = nHand.position + narfi.transform.rotation * (Quaternion.Inverse(kk.transform.rotation) * (kSlot.position - kHand.position)) * ((1.6f / 2.5f) / (1.85f / 1.91f));
                // weapons: the on-screen size they have on KayKit bodies (1.6 tall from 2.5 units; Narfi 1.85 from 1.91), made
                // smaller for his realistic hands
                float want = kSlot.lossyScale.x * (1.6f / 2.5f) / (1.85f / 1.91f) * WeaponScale;
                slot.localScale = Vector3.one * (want / nHand.lossyScale.x);
            }

            AnimationMode.StopAnimationMode();
            // the runtime prefab: the model, its baked clips, no Animator
            Object.DestroyImmediate(na);
            narfi.transform.position = Vector3.zero;
            var anim = narfi.AddComponent<Animation>();
            foreach (var c in baked) anim.AddClip(c, c.name);
            anim.clip = baked.Find(c => c.name == "Idle");
            anim.playAutomatically = false;
            ResetPose(narfi, bones);
            narfi.name = "Narfi";
            PrefabUtility.SaveAsPrefabAsset(narfi, Prefab);
            Object.DestroyImmediate(narfi);
            Object.DestroyImmediate(kk);
            AssetDatabase.SaveAssets();
            log.AppendLine($"baked {baked.Count} clips -> {Prefab}");
            File.WriteAllText("NarfiBake.txt", log.ToString());
            Debug.Log("[Wyrdbound] Narfi bake:\n" + log);
        }

        static void MakeHumanoid(string path, int col, string prefix, bool clips)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var skel = new List<SkeletonBone>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                skel.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            var human = new List<HumanBone>();
            for (int i = 0; i < Map.GetLength(0); i++)
            {
                if (Map[i, col] == null) continue;
                var hb = new HumanBone { humanName = Map[i, 0], boneName = prefix + Map[i, col] };
                hb.limit.useDefaultValues = true;
                human.Add(hb);
            }
            var hd = new HumanDescription
            {
                human = human.ToArray(), skeleton = skel.ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
            mi.humanDescription = hd;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.animationType = ModelImporterAnimationType.Human;
            if (clips)
            {
                var list = mi.defaultClipAnimations;
                foreach (var c in list)
                {
                    // keep every motion in the pose (the game moves the character itself)
                    c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = c.keepOriginalPositionY = c.keepOriginalPositionXZ = true;
                    c.loopTime = Loops.Contains(c.name.Replace("Rig|", ""));
                }
                mi.clipAnimations = list;
            }
            mi.SaveAndReimport();
        }

        static AnimationClip BakeClip(GameObject root, Animator a, AnimationClip src, string name, List<Transform> bones)
        {
            int frames = Mathf.Max(2, Mathf.CeilToInt(src.length * Fps) + 1);
            var arms = new[] { Find(root.transform, "mixamorig:LeftArm"), Find(root.transform, "mixamorig:RightArm") };
            int n = bones.Count;
            var curves = new AnimationCurve[n * 4 + 3];
            for (int i = 0; i < curves.Length; i++) curves[i] = new AnimationCurve();
            for (int f = 0; f < frames; f++)
            {
                float t = Mathf.Min(f / Fps, src.length);
                Sample(a, src, t);
                // KayKit's chibi bodies hold their arms wide of a big belly; bring Narfi's in toward his sides
                foreach (var arm in arms)
                {
                    var dir = (arm.GetChild(0).position - arm.position).normalized;
                    var axis = Vector3.Cross(dir, Vector3.down);
                    if (axis.sqrMagnitude > 1e-4f) arm.rotation = Quaternion.AngleAxis(ArmsIn * Mathf.Clamp01(axis.magnitude * 1.5f), axis.normalized) * arm.rotation;
                }
                for (int b = 0; b < n; b++)
                {
                    var q = bones[b].localRotation;
                    curves[b * 4].AddKey(t, q.x);
                    curves[b * 4 + 1].AddKey(t, q.y);
                    curves[b * 4 + 2].AddKey(t, q.z);
                    curves[b * 4 + 3].AddKey(t, q.w);
                }
                var p = bones[0].localPosition;
                curves[n * 4].AddKey(t, p.x);
                curves[n * 4 + 1].AddKey(t, p.y);
                curves[n * 4 + 2].AddKey(t, p.z);
            }
            var clip = new AnimationClip { name = name, legacy = true, frameRate = Fps };
            clip.wrapMode = Loops.Contains(name) ? WrapMode.Loop : WrapMode.ClampForever;
            for (int b = 0; b < n; b++)
            {
                var path = AnimationUtility.CalculateTransformPath(bones[b], root.transform);
                // drop bones that never move (fingers, eyes): they stay at rest
                bool moves = false;
                for (int k = 0; k < 4 && !moves; k++) moves = Range(curves[b * 4 + k]) > 1e-4f;
                if (!moves && b > 0) continue;
                clip.SetCurve(path, typeof(Transform), "localRotation.x", curves[b * 4]);
                clip.SetCurve(path, typeof(Transform), "localRotation.y", curves[b * 4 + 1]);
                clip.SetCurve(path, typeof(Transform), "localRotation.z", curves[b * 4 + 2]);
                clip.SetCurve(path, typeof(Transform), "localRotation.w", curves[b * 4 + 3]);
            }
            var hp = AnimationUtility.CalculateTransformPath(bones[0], root.transform);
            clip.SetCurve(hp, typeof(Transform), "localPosition.x", curves[n * 4]);
            clip.SetCurve(hp, typeof(Transform), "localPosition.y", curves[n * 4 + 1]);
            clip.SetCurve(hp, typeof(Transform), "localPosition.z", curves[n * 4 + 2]);
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        static float Range(AnimationCurve c)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var k in c.keys) { lo = Mathf.Min(lo, k.value); hi = Mathf.Max(hi, k.value); }
            return hi - lo;
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            // the editor's animation-preview sampler: retargets Humanoid clips through the Animator's avatar
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(a.gameObject, clip, t);
            AnimationMode.EndSampling();
        }

        static void ResetPose(GameObject go, List<Transform> bones)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(NarfiFbx);
            foreach (var b in bones)
            {
                var path = AnimationUtility.CalculateTransformPath(b, go.transform);
                var rest = asset.transform.Find(path);
                if (rest == null) continue;
                b.localPosition = rest.localPosition;
                b.localRotation = rest.localRotation;
            }
        }

        static float Height(GameObject go)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                lo = Mathf.Min(lo, r.bounds.min.y);
                hi = Mathf.Max(hi, r.bounds.max.y);
            }
            return hi - lo;
        }

        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
