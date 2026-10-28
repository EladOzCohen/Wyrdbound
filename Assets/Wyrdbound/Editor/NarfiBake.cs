using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Wyrdbound.EditorTools
{
    /// <summary>
    /// Narfi is a MakeHuman body (tools/blender/narfi.py -> Art/Narfi/Narfi.fbx) on a Mixamo-named skeleton, but the
    /// game plays Legacy clips by KayKit's names. This tool retargets clips onto him through Unity's Humanoid system
    /// once, in the editor, and bakes the result into plain Legacy clips, so at runtime he is just another Model3D
    /// (Resources/Models/Narfi.prefab) with an Animation component.
    /// Sources: Quaternius' Universal Animation Library 1 + 2 (CC0, realistic human motion: the sword swings, walk,
    /// jog, roll, hits, death) and KayKit for the moves UAL lacks (side dodges, bow, spin, cheer).
    ///   Menu: Wyrdbound > Bake Narfi     Batch: -executeMethod Wyrdbound.EditorTools.NarfiBake.Bake
    /// The UAL FBX files come from tools/blender/vendor/ual (gitignored; see docs/HANDOFF.md).
    /// </summary>
    public sealed class NarfiBake : AssetPostprocessor
    {
        const string Dir = "Assets/Wyrdbound/Art/Narfi/";
        const string NarfiFbx = Dir + "Narfi.fbx", ClipDir = Dir + "Clips/";
        const string KK = Dir + "KayKitAnims.fbx", U1 = Dir + "UAL1.fbx", U2 = Dir + "UAL2.fbx";
        const string KayKitSource = "Assets/ThirdParty/Resources/KayKit/Adventurers/Characters/RogueHooded.fbx";
        const string UalVendor = "tools/blender/vendor/ual/";
        const string Prefab = "Assets/Wyrdbound/Resources/Models/Narfi.prefab";
        const float Fps = 30f, ArmsIn = 22f, WeaponScale = 0.62f, Contact = 0.42f;

        /// <summary>One baked clip: the game's name for it, where it comes from, and how it is shaped.</summary>
        sealed class Recipe
        {
            public string Name, File, Clip, Then;   // Then = a recovery clip played straight after
            public bool Loop, Swing;                // Swing: retime so the fastest hand frame lands at Contact (Model3D.SwingTimed)
        }

        static Recipe R(string name, string file, string clip, string then = null, bool loop = false, bool swing = false) =>
            new Recipe { Name = name, File = file, Clip = clip, Then = then, Loop = loop, Swing = swing };

        static readonly Recipe[] Recipes =
        {
            R("Idle", U1, "Sword_Idle", loop: true),
            R("Walking_A", U1, "Walk_Loop", loop: true),
            R("Running_A", U1, "Jog_Fwd_Loop", loop: true),
            R("Dodge_Forward", KK, "Dodge_Forward"),   // the dash lasts 0.17 s: too short for UAL's 1.5 s roll
            R("Dodge_Backward", KK, "Dodge_Backward"), R("Dodge_Left", KK, "Dodge_Left"), R("Dodge_Right", KK, "Dodge_Right"),
            R("1H_Melee_Attack_Slice_Diagonal", U2, "Sword_Regular_A", "Sword_Regular_A_Rec", swing: true),
            R("1H_Melee_Attack_Slice_Horizontal", U2, "Sword_Regular_B", "Sword_Regular_B_Rec", swing: true),
            R("1H_Melee_Attack_Chop", U2, "Sword_Regular_C", swing: true),
            R("2H_Melee_Attack_Chop", U1, "Sword_Attack", swing: true),
            R("1H_Melee_Attack_Stab", U2, "Sword_Dash", swing: true),
            R("2H_Melee_Attack_Spinning", KK, "2H_Melee_Attack_Spinning", loop: true), R("2H_Melee_Attack_Stab", KK, "2H_Melee_Attack_Stab"),
            R("1H_Ranged_Shoot", KK, "1H_Ranged_Shoot"), R("1H_Ranged_Aiming", KK, "1H_Ranged_Aiming", loop: true),
            R("Spellcasting", U1, "Spell_Simple_Idle_Loop", loop: true), R("Spellcast_Shoot", U1, "Spell_Simple_Shoot"),
            R("Throw", U2, "OverhandThrow"),
            R("Hit_A", U1, "Hit_Chest"), R("Hit_B", U1, "Hit_Head"),
            R("Death_A", U1, "Death01"), R("Death_B", U2, "Hit_Knockback"),
            R("Cheer", KK, "Cheer"), R("Interact", U1, "Interact"), R("PickUp", U1, "PickUp_Table"),
        };

        // Unity human bone -> KayKit / Mixamo (Narfi) / Unreal-style (UAL) bone
        static readonly string[,] Map =
        {
            { "Hips", "hips", "Hips", "pelvis" }, { "Spine", "spine", "Spine", "spine_01" }, { "Chest", "chest", "Spine1", "spine_02" },
            { "UpperChest", null, "Spine2", "spine_03" }, { "Neck", null, "Neck", "neck_01" }, { "Head", "head", "Head", "Head" },
            { "LeftShoulder", null, "LeftShoulder", "clavicle_l" }, { "LeftUpperArm", "upperarm.l", "LeftArm", "upperarm_l" },
            { "LeftLowerArm", "lowerarm.l", "LeftForeArm", "lowerarm_l" }, { "LeftHand", "wrist.l", "LeftHand", "hand_l" },
            { "RightShoulder", null, "RightShoulder", "clavicle_r" }, { "RightUpperArm", "upperarm.r", "RightArm", "upperarm_r" },
            { "RightLowerArm", "lowerarm.r", "RightForeArm", "lowerarm_r" }, { "RightHand", "wrist.r", "RightHand", "hand_r" },
            { "LeftUpperLeg", "upperleg.l", "LeftUpLeg", "thigh_l" }, { "LeftLowerLeg", "lowerleg.l", "LeftLeg", "calf_l" },
            { "LeftFoot", "foot.l", "LeftFoot", "foot_l" }, { "LeftToes", "toes.l", "LeftToeBase", "ball_l" },
            { "RightUpperLeg", "upperleg.r", "RightUpLeg", "thigh_r" }, { "RightLowerLeg", "lowerleg.r", "RightLeg", "calf_r" },
            { "RightFoot", "foot.r", "RightFoot", "foot_r" }, { "RightToes", "toes.r", "RightToeBase", "ball_r" },
        };
        static readonly string[,] Fingers = { { "Thumb", "Thumb", "thumb" }, { "Index", "Index", "index" }, { "Middle", "Middle", "middle" }, { "Ring", "Ring", "ring" }, { "Little", "Pinky", "pinky" } };
        static readonly string[] Joints = { "Proximal", "Intermediate", "Distal" };

        public override uint GetVersion() => 3;

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importBlendShapes = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialSearch = ModelImporterMaterialSearch.Local;
            mi.importAnimation = assetPath != NarfiFbx;
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
            // the source rigs, copied in (all gitignored: they are inputs, the baked clips are what ships)
            if (!File.Exists(KK)) AssetDatabase.CopyAsset(KayKitSource, KK);
            CopyIn(UalVendor + "u1/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx", U1);
            CopyIn(UalVendor + "u2/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx", U2);
            AssetDatabase.Refresh();
            MakeHumanoid(KK, 1, true);
            MakeHumanoid(U1, 3, true);
            MakeHumanoid(U2, 3, true);
            MakeHumanoid(NarfiFbx, 2, false);

            var narfi = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NarfiFbx));
            var kk = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(KK));
            var na = narfi.GetComponent<Animator>();
            var ka = kk.GetComponent<Animator>();
            log.AppendLine("Narfi avatar human=" + (na.avatar != null && na.avatar.isHuman) + ", KayKit human=" + (ka.avatar != null && ka.avatar.isHuman));

            var sources = new Dictionary<string, Dictionary<string, AnimationClip>>();
            foreach (var file in new[] { KK, U1, U2 })
            {
                var d = new Dictionary<string, AnimationClip>();
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(file))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview__")) d[ClipName(c.name)] = c;
                sources[file] = d;
                var av = AssetDatabase.LoadAssetAtPath<Avatar>(file);
                log.AppendLine($"{Path.GetFileName(file)}: {d.Count} clips, human avatar={(av != null && av.isHuman)}");
            }

            // the bones to record: Narfi's whole skeleton (meshes are skinned to it)
            var bones = new List<Transform>(Find(narfi.transform, "mixamorig:Hips").GetComponentsInChildren<Transform>());

            Directory.CreateDirectory(ClipDir);
            AnimationMode.StartAnimationMode();
            var baked = new List<AnimationClip>();
            foreach (var rc in Recipes)
            {
                var src = sources[rc.File];
                if (!src.TryGetValue(rc.Clip, out var a) || (rc.Then != null && !src.ContainsKey(rc.Then))) { log.AppendLine("missing clip " + rc.Clip); continue; }
                var parts = rc.Then != null ? new[] { a, src[rc.Then] } : new[] { a };
                var clip = BakeClip(narfi, na, parts, rc, bones, out string note);
                log.AppendLine($"  {rc.Name} <- {Path.GetFileNameWithoutExtension(rc.File)}:{rc.Clip}{(rc.Then != null ? "+" + rc.Then : "")} {clip.length:F2}s {note}");
                var path = ClipDir + rc.Name + ".anim";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(clip, path);
                baked.Add(clip);
            }

            // KayKit weapons sit in a "handslot.r" bone: give Narfi the same slot, posed to match in KayKit's idle
            var kIdle = sources[KK]["Idle"];
            Sample(na, kIdle, 0f);
            Sample(ka, kIdle, 0f);
            const float game = (1.6f / 2.5f) / (1.85f / 1.91f);   // KayKit bodies 1.6 tall from 2.5 units; Narfi 1.85 from 1.91
            foreach (var side in new[] { "r", "l" })
            {
                var kSlot = Find(kk.transform, "handslot." + side);
                var kHand = Find(kk.transform, "wrist." + side);
                var nHand = Find(narfi.transform, side == "r" ? "mixamorig:RightHand" : "mixamorig:LeftHand");
                var slot = new GameObject("handslot." + side).transform;
                slot.SetParent(nHand, false);
                slot.rotation = narfi.transform.rotation * (Quaternion.Inverse(kk.transform.rotation) * kSlot.rotation);
                slot.position = nHand.position + narfi.transform.rotation * (Quaternion.Inverse(kk.transform.rotation) * (kSlot.position - kHand.position)) * game;
                // weapons keep their on-screen KayKit size, made a little smaller for his realistic hands
                slot.localScale = Vector3.one * (kSlot.lossyScale.x * game * WeaponScale / nHand.lossyScale.x);
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

        static void CopyIn(string from, string to)
        {
            if (File.Exists(to)) return;
            if (!File.Exists(from)) throw new FileNotFoundException("Quaternius UAL missing (see docs/HANDOFF.md): " + from);
            File.Copy(from, to);
        }

        static string ClipName(string n) => n.Substring(n.LastIndexOf('|') + 1);

        static void MakeHumanoid(string path, int col, bool clips)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(path);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var names = new HashSet<string>();
            var skel = new List<SkeletonBone>();
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
            {
                names.Add(t.name);
                skel.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            }
            string prefix = col == 2 ? "mixamorig:" : "";
            var human = new List<HumanBone>();
            void Add(string h, string b)
            {
                if (b == null || !names.Contains(prefix + b)) return;
                var hb = new HumanBone { humanName = h, boneName = prefix + b };
                hb.limit.useDefaultValues = true;
                human.Add(hb);
            }
            for (int i = 0; i < Map.GetLength(0); i++) Add(Map[i, 0], Map[i, col]);
            // fingers (KayKit has none): Mixamo LeftHandIndex1..3, UAL index_01_l..03
            if (col != 1)
                foreach (var s in new[] { "Left", "Right" })
                    for (int f = 0; f < Fingers.GetLength(0); f++)
                        for (int j = 0; j < 3; j++)
                            Add($"{s} {Fingers[f, 0]} {Joints[j]}", col == 2 ? $"{s}Hand{Fingers[f, 1]}{j + 1}" : $"{Fingers[f, 2]}_0{j + 1}_{(s == "Left" ? "l" : "r")}");
            mi.humanDescription = new HumanDescription
            {
                human = human.ToArray(), skeleton = skel.ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
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
                    string n = ClipName(c.name);
                    c.loopTime = n.EndsWith("_Loop") || n == "Sword_Idle" || n == "Idle" || n.StartsWith("Walking") || n.StartsWith("Running");
                }
                mi.clipAnimations = list;
            }
            mi.SaveAndReimport();
        }

        static AnimationClip BakeClip(GameObject root, Animator a, AnimationClip[] parts, Recipe rc, List<Transform> bones, out string note)
        {
            var arms = new[] { Find(root.transform, "mixamorig:LeftArm"), Find(root.transform, "mixamorig:RightArm") };
            var hand = Find(root.transform, "mixamorig:RightHand");
            bool kaykit = rc.File == KK;
            int n = bones.Count;
            var times = new List<float>();
            var rot = new List<Quaternion[]>();
            var hipPos = new List<Vector3>();
            var handPos = new List<Vector3>();
            float offset = 0f, firstLen = parts[0].length;
            for (int p = 0; p < parts.Length; p++)
            {
                var src = parts[p];
                int frames = Mathf.Max(2, Mathf.CeilToInt(src.length * Fps) + 1);
                for (int f = p == 0 ? 0 : 1; f < frames; f++)
                {
                    float t = Mathf.Min(f / Fps, src.length);
                    Sample(a, src, t);
                    // KayKit's chibi bodies hold their arms wide of a big belly; bring Narfi's in toward his sides
                    if (kaykit)
                        foreach (var arm in arms)
                        {
                            var dir = (arm.GetChild(0).position - arm.position).normalized;
                            var axis = Vector3.Cross(dir, Vector3.down);
                            if (axis.sqrMagnitude > 1e-4f) arm.rotation = Quaternion.AngleAxis(ArmsIn * Mathf.Clamp01(axis.magnitude * 1.5f), axis.normalized) * arm.rotation;
                        }
                    times.Add(offset + t);
                    var q = new Quaternion[n];
                    for (int b = 0; b < n; b++) q[b] = bones[b].localRotation;
                    rot.Add(q);
                    hipPos.Add(bones[0].localPosition);
                    handPos.Add(hand.position);
                }
                offset += src.length;
            }
            float len = offset;
            note = "";

            // swings: the contact frame (fastest hand, within the attack itself) moves to Contact of the clip
            if (rc.Swing && times.Count > 3)
            {
                int best = 1;
                float bestV = 0f;
                for (int i = 1; i < times.Count && times[i] <= firstLen + 1e-3f; i++)
                {
                    float v = (handPos[i] - handPos[i - 1]).magnitude / Mathf.Max(1e-4f, times[i] - times[i - 1]);
                    if (v > bestV) { bestV = v; best = i; }
                }
                float tc = Mathf.Clamp(times[best], 0.05f * len, 0.9f * len), want = Contact * len;
                for (int i = 0; i < times.Count; i++)
                {
                    float t = times[i];
                    times[i] = t <= tc ? t * want / tc : want + (t - tc) * (len - want) / (len - tc);
                }
                note = $"contact {tc / len:P0}->{Contact:P0}";
            }

            var clip = new AnimationClip { name = rc.Name, legacy = true, frameRate = Fps };
            clip.wrapMode = rc.Loop ? WrapMode.Loop : WrapMode.ClampForever;
            var c = new AnimationCurve[4];
            for (int b = 0; b < n; b++)
            {
                for (int k = 0; k < 4; k++) c[k] = new AnimationCurve();
                for (int i = 0; i < times.Count; i++)
                {
                    var q = rot[i][b];
                    c[0].AddKey(times[i], q.x); c[1].AddKey(times[i], q.y); c[2].AddKey(times[i], q.z); c[3].AddKey(times[i], q.w);
                }
                // drop bones that never move (eyes, unanimated fingers): they stay at rest
                if (b > 0 && Unchanging(rot, b)) continue;
                var path = AnimationUtility.CalculateTransformPath(bones[b], root.transform);
                clip.SetCurve(path, typeof(Transform), "localRotation.x", c[0]);
                clip.SetCurve(path, typeof(Transform), "localRotation.y", c[1]);
                clip.SetCurve(path, typeof(Transform), "localRotation.z", c[2]);
                clip.SetCurve(path, typeof(Transform), "localRotation.w", c[3]);
            }
            var hp = AnimationUtility.CalculateTransformPath(bones[0], root.transform);
            for (int k = 0; k < 3; k++)
            {
                var pc = new AnimationCurve();
                for (int i = 0; i < times.Count; i++) pc.AddKey(times[i], hipPos[i][k]);
                clip.SetCurve(hp, typeof(Transform), k == 0 ? "localPosition.x" : k == 1 ? "localPosition.y" : "localPosition.z", pc);
            }
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        static bool Unchanging(List<Quaternion[]> rot, int b)
        {
            var q0 = rot[0][b];
            foreach (var q in rot) if (Quaternion.Angle(q[b], q0) > 0.05f) return false;
            return true;
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

        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
    }
}
