using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>How one character id is cast from the KayKit (CC0) models.</summary>
    public sealed class ModelSpec
    {
        public string Model = "";       // Resources path of the character FBX
        public string Proc;             // or a creature built in code: serpent, larva, bell, dragon
        public float Height = 1.6f;     // world height of a 2.5-unit KayKit body
        public Color Tint = Color.white;
        public float TintAmt;
        public Color Rim = new Color(0.55f, 0.7f, 0.9f);
        public float RimAmt = 0.55f;
        public Color Eyes = new Color(0.4f, 0.95f, 1f);
        public string[] Show = new string[0];   // built-in items to keep visible (others are hidden)
        public string[] Hide = new string[0];   // body parts to hide (hats, helmets)
        public string Weapon, Offhand;          // extra item models for the right / left hand slot
        public string Melee = "1H_Melee_Attack_Chop", Heavy = "2H_Melee_Attack_Chop", Stab = "1H_Melee_Attack_Stab";
        public string Shoot = "1H_Ranged_Shoot", Aim = "1H_Ranged_Aiming", Idle = "Idle";
        public bool ShowAll;             // every renderer is body (non-KayKit models)
        public bool Skeleton, Shamble;
        public string Walk = "Walking_A", Run = "Running_A", Dodge = "Dodge_Forward", Cast = "Spellcasting", Death, Hit;
        public float RawHeight = 2.5f;   // height of the source model in its own units
        public float Hover;              // flyers float this high above the floor
        public float RunRef = 4.2f;      // speed at which the run clip plays at 1x (for a 1.6-tall body)
        public Dictionary<string, Color> MatColor;   // material-name fragment -> colour multiplied in (Narfi's red hair)

        public bool IsSkeleton => Model.Contains("/Skeletons/");
    }

    /// <summary>The cast list: which characters are 3D models (everyone else stays a 2D rig billboard).</summary>
    public static class ModelSpecs
    {
        const string Adv = "KayKit/Adventurers/Characters/", Skel = "KayKit/Skeletons/Characters/";
        const string AdvItem = "KayKit/Adventurers/Models/", SkelItem = "KayKit/Skeletons/Models/";
        static Dictionary<string, ModelSpec> table;

        public static ModelSpec Get(string id)
        {
            if (table == null) Fill();
            return id != null && table.TryGetValue(id, out var s) ? s : null;
        }

        static void Add(string id, ModelSpec s) => table[id] = s;

        static void Fill()
        {
            table = new Dictionary<string, ModelSpec>();
            var hel = Pal.Hel;
            var venom = Pal.Venom;
            var ice = Pal.Frost;

            // Narfi and the living
            // Narfi is the MakeHuman body made in Blender (tools/blender/narfi.py), clips baked by Editor/NarfiBake.cs
            Add("narfi", new ModelSpec
            {
                Model = "Models/Narfi", ShowAll = true, RawHeight = 1.91f, Height = 1.85f, Rim = Pal.Narfi, RimAmt = 0.45f, Eyes = Pal.Narfi,
                Weapon = AdvItem + "axe_1handed", MatColor = new Dictionary<string, Color> { { "ponytail", U.Hex("c0532a") }, { "eyebrow", U.Hex("8a4a2a") } }
            });
            Add("loki", new ModelSpec { Model = Adv + "RogueHooded", Height = 1.75f, Tint = U.Hex("3a7a3a"), TintAmt = 0.5f, Rim = Pal.Gold, Show = new[] { "Knife", "Knife_Offhand" } });
            Add("sigyn", new ModelSpec { Model = Adv + "Mage", Height = 1.6f, Tint = U.Hex("6a8ac8"), TintAmt = 0.45f, Show = new[] { "Spellbook" } });
            Add("hermod", new ModelSpec { Model = Adv + "Rogue", Height = 1.65f, Tint = U.Hex("8a7a4a"), TintAmt = 0.4f, Show = new[] { "Knife" } });
            Add("hodr", new ModelSpec { Model = Adv + "Rogue", Height = 1.7f, Tint = U.Hex("6a7080"), TintAmt = 0.55f, Rim = Pal.Silver });
            Add("baldr", new ModelSpec { Model = Adv + "Knight", Height = 1.75f, Tint = U.Hex("f0d890"), TintAmt = 0.55f, Rim = Pal.Gold, RimAmt = 0.9f, Hide = new[] { "Knight_Helmet" } });
            Add("nanna", new ModelSpec { Model = Adv + "Mage", Height = 1.55f, Tint = U.Hex("d8a0b8"), TintAmt = 0.5f, Hide = new[] { "Mage_Hat" } });
            Add("modgud", new ModelSpec { Model = Adv + "Knight", Height = 1.8f, Tint = U.Hex("c8b070"), TintAmt = 0.5f, Show = new[] { "2H_Sword" } });
            Add("ganglot", new ModelSpec { Model = Adv + "Rogue", Height = 1.5f, Tint = U.Hex("5a5466"), TintAmt = 0.6f, Rim = hel });
            Add("hel", new ModelSpec { Model = Adv + "Mage", Height = 1.8f, Tint = U.Hex("6a5a8a"), TintAmt = 0.65f, Rim = hel, RimAmt = 0.9f, Show = new[] { "2H_Staff" } });
            Add("hel_boss", new ModelSpec { Model = Adv + "Mage", Height = 2.5f, Tint = U.Hex("5a4a7a"), TintAmt = 0.7f, Rim = hel, RimAmt = 1.1f, Show = new[] { "2H_Staff" } });

            // the gods (when they appear in person)
            Add("thor", new ModelSpec { Model = Adv + "Barbarian", Height = 1.9f, Tint = U.Hex("b04a3a"), TintAmt = 0.4f, Rim = Pal.God(God.Thor), Show = new[] { "2H_Axe" } });
            Add("freyja", new ModelSpec { Model = Adv + "Mage", Height = 1.7f, Tint = U.Hex("e0a060"), TintAmt = 0.5f, Rim = Pal.God(God.Freyja), Hide = new[] { "Mage_Hat" }, Show = new[] { "1H_Wand" } });
            Add("tyr", new ModelSpec { Model = Adv + "Knight", Height = 1.85f, Tint = U.Hex("a0603a"), TintAmt = 0.45f, Rim = Pal.God(God.Tyr), Show = new[] { "1H_Sword", "Round_Shield" } });
            Add("skadi", new ModelSpec { Model = Adv + "Rogue", Height = 1.75f, Tint = U.Hex("c8e0f0"), TintAmt = 0.55f, Rim = Pal.God(God.Skadi), Show = new[] { "2H_Crossbow" } });
            Add("odin", new ModelSpec { Model = Adv + "Mage", Height = 1.9f, Tint = U.Hex("5a6a8a"), TintAmt = 0.55f, Rim = Pal.God(God.Odin), Show = new[] { "2H_Staff" } });
            Add("njord", new ModelSpec { Model = Adv + "Barbarian", Height = 1.85f, Tint = U.Hex("3a7a9a"), TintAmt = 0.5f, Rim = Pal.God(God.Njord), Show = new[] { "1H_Axe" } });

            // Quaternius (CC0) beasts and spirits
            ModelSpec Wolf(float h, string tint, Color rim, Color eyes) => new ModelSpec
            {
                Model = "Quaternius/Wolf", ShowAll = true, RawHeight = 2.98f, Height = h, Tint = U.Hex(tint), TintAmt = 0.75f, Rim = rim, RimAmt = 0.8f, Eyes = eyes,
                Idle = "AnimalArmature|Idle", Walk = "AnimalArmature|Walk", Run = "AnimalArmature|Gallop", RunRef = 5.5f,
                Melee = "AnimalArmature|Attack", Heavy = "AnimalArmature|Attack", Stab = "AnimalArmature|Gallop_Jump", Cast = "AnimalArmature|Idle_2_HeadLow",
                Dodge = "AnimalArmature|Gallop", Death = "AnimalArmature|Death", Hit = "AnimalArmature|Idle_HitReact_Left",
                Shoot = "AnimalArmature|Attack", Aim = "AnimalArmature|Idle_2_HeadLow"
            };
            Add("helhound", Wolf(1.05f, "3a3046", hel, hel));
            Add("garmr", Wolf(2.3f, "3a2420", Pal.Blood, U.Hex("ffb020")));
            ModelSpec Flyer(string model, float h, float hover, string tint, float amt, Color rim) => new ModelSpec
            {
                Model = "Quaternius/" + model, ShowAll = true, RawHeight = 3.3f, Height = h, Hover = hover, Tint = U.Hex(tint), TintAmt = amt, Rim = rim, RimAmt = 1f,
                Idle = "CharacterArmature|Flying_Idle", Walk = "CharacterArmature|Flying_Idle", Run = "CharacterArmature|Fast_Flying", RunRef = 3f,
                Melee = "CharacterArmature|Headbutt", Heavy = "CharacterArmature|Headbutt", Stab = "CharacterArmature|Headbutt", Cast = "CharacterArmature|Punch",
                Shoot = "CharacterArmature|Punch", Aim = "CharacterArmature|Flying_Idle", Dodge = "CharacterArmature|Fast_Flying",
                Death = "CharacterArmature|Death", Hit = "CharacterArmature|HitReact"
            };
            Add("raven", Flyer("Pigeon", 1.05f, 0.9f, "2a2440", 0.85f, U.Hex("8a6ab8")));
            Add("wraith", Flyer("Ghost_Skull", 1.5f, 0.35f, "9ad8e0", 0.6f, Pal.Frost));
            Add("wight", Flyer("Ghost_Skull", 1.65f, 0.35f, "b8c8f0", 0.65f, ice));
            Add("bloat", new ModelSpec
            {
                Model = "Quaternius/GreenSpikyBlob", ShowAll = true, RawHeight = 4.16f, Height = 1.35f, Tint = U.Hex("6a7a3a"), TintAmt = 0.6f, Rim = venom, RimAmt = 0.9f,
                Idle = "CharacterArmature|Idle", Walk = "CharacterArmature|Walk", Run = "CharacterArmature|Walk", RunRef = 2f,
                Melee = "CharacterArmature|Bite_Front", Heavy = "CharacterArmature|Jump", Stab = "CharacterArmature|Bite_Front", Cast = "CharacterArmature|Jump",
                Shoot = "CharacterArmature|Bite_Front", Aim = "CharacterArmature|Idle", Dodge = "CharacterArmature|Jump",
                Death = "CharacterArmature|Death", Hit = "CharacterArmature|HitRecieve"
            });

            // creatures built in code
            Add("serpent", new ModelSpec { Proc = "serpent", Height = 1.6f, Tint = U.Hex("3a5a2a"), Rim = venom, RimAmt = 0.7f, Eyes = U.Hex("ffd040") });
            Add("larva", new ModelSpec { Proc = "larva", Height = 1.4f, Tint = U.Hex("c8b48a"), Rim = U.Hex("ffe0a0"), RimAmt = 0.5f, Eyes = Pal.Blood });
            Add("bell", new ModelSpec { Proc = "bell", Height = 1.25f, Tint = U.Hex("7a8a96"), Rim = ice, RimAmt = 0.8f, Eyes = ice });
            Add("nidhogg_head", new ModelSpec { Proc = "dragon", Height = 2.6f, Tint = U.Hex("2e4a2a"), Rim = venom, RimAmt = 0.9f, Eyes = U.Hex("ffcf3a") });

            // the dead of Hel
            Add("draugr", new ModelSpec { Model = Skel + "Skeleton_Warrior", Height = 1.6f, Tint = U.Hex("8a9a9a"), TintAmt = 0.3f, Rim = Pal.Frost, Weapon = SkelItem + "Skeleton_Axe", Shamble = true });
            Add("draugr_archer", new ModelSpec { Model = Skel + "Skeleton_Rogue", Height = 1.6f, Tint = U.Hex("8a9a9a"), TintAmt = 0.3f, Rim = Pal.Frost, Weapon = SkelItem + "Skeleton_Crossbow", Shoot = "2H_Ranged_Shoot", Aim = "2H_Ranged_Aiming" });
            Add("thrall", new ModelSpec { Model = Skel + "Skeleton_Warrior", Height = 2.0f, Tint = U.Hex("6a6a7a"), TintAmt = 0.4f, Rim = hel, Weapon = SkelItem + "Skeleton_Blade", Offhand = SkelItem + "Skeleton_Shield_Large_A", Shamble = true });
            Add("grave_jarl", new ModelSpec { Model = Skel + "Skeleton_Warrior", Height = 2.7f, Tint = U.Hex("b89a5a"), TintAmt = 0.45f, Rim = Pal.Gold, RimAmt = 0.8f, Eyes = Pal.Gold, Weapon = SkelItem + "Skeleton_Axe", Offhand = SkelItem + "Skeleton_Shield_Large_B", Melee = "2H_Melee_Attack_Chop" });
            Add("spitter", new ModelSpec { Model = Skel + "Skeleton_Mage", Height = 1.6f, Tint = venom, TintAmt = 0.35f, Rim = venom, Eyes = venom, Weapon = SkelItem + "Skeleton_Staff" });
            Add("oathbreaker", new ModelSpec { Model = Skel + "Skeleton_Rogue", Height = 1.85f, Tint = U.Hex("6a7a4a"), TintAmt = 0.45f, Rim = venom, Eyes = venom, Weapon = SkelItem + "Skeleton_Blade", Offhand = SkelItem + "Skeleton_Shield_Small_A" });
            Add("weaver", new ModelSpec { Model = Skel + "Skeleton_Mage", Height = 2.6f, Tint = U.Hex("5a7a3a"), TintAmt = 0.5f, Rim = venom, RimAmt = 1f, Eyes = venom, Weapon = SkelItem + "Skeleton_Staff" });
            Add("revenant", new ModelSpec { Model = Skel + "Skeleton_Minion", Height = 1.65f, Tint = U.Hex("9a5a5a"), TintAmt = 0.4f, Rim = Pal.Blood, Eyes = Pal.Blood, Weapon = SkelItem + "Skeleton_Blade", Offhand = SkelItem + "Skeleton_Blade", Melee = "Dualwield_Melee_Attack_Slice" });
            Add("hrimthurs", new ModelSpec { Model = Adv + "Barbarian", Height = 2.9f, Tint = U.Hex("9ad0f0"), TintAmt = 0.65f, Rim = ice, RimAmt = 1f, Show = new[] { "2H_Axe" }, Hide = new[] { "Barbarian_Hat" }, Melee = "2H_Melee_Attack_Chop" });
            Add("valkyrie", new ModelSpec { Model = Adv + "Knight", Height = 1.8f, Tint = U.Hex("d8c8a0"), TintAmt = 0.5f, Rim = U.Hex("ffd0a0"), Show = new[] { "1H_Sword", "Badge_Shield" }, Melee = "1H_Melee_Attack_Slice_Diagonal" });
            Add("warden", new ModelSpec { Model = Adv + "Knight", Height = 2.8f, Tint = U.Hex("4a5a7a"), TintAmt = 0.6f, Rim = ice, RimAmt = 1f, Show = new[] { "2H_Sword" }, Melee = "2H_Melee_Attack_Slice" });
            Add("einherjar", new ModelSpec { Model = Adv + "Knight", Height = 1.9f, Tint = U.Hex("7a3a3a"), TintAmt = 0.55f, Rim = hel, Show = new[] { "1H_Sword", "Spike_Shield" } });
            Add("vali", new ModelSpec { Model = Adv + "Barbarian", Height = 2.6f, Tint = U.Hex("7a4a2a"), TintAmt = 0.45f, Rim = Pal.Blood, RimAmt = 0.9f, Show = new[] { "1H_Axe", "1H_Axe_Offhand" }, Melee = "Dualwield_Melee_Attack_Chop" });
        }

        /// <summary>The 3D item a player weapon is shown as.</summary>
        public static string PlayerWeapon(string weaponId)
        {
            switch (weaponId)
            {
                case "spear": return AdvItem + "staff";
                case "bow": return AdvItem + "crossbow_1handed";
                case "seax": return AdvItem + "dagger";
                default: return AdvItem + "axe_1handed";
            }
        }
    }

    /// <summary>
    /// A KayKit character driven by the same RigInput the 2D rigs use: locomotion, attacks scrubbed in sync
    /// with the AI's windup/strike timing, casts, shots, dodges, hit reactions, deaths and spawn-from-the-
    /// ground. Lives on the entity's visual node; rotates to face the entity's facing on the floor plane.
    /// </summary>
    public sealed class Model3D : MonoBehaviour
    {
        static readonly string[] BodyParts = { "_Arm", "_Body", "_Head", "_Leg", "_Cape", "_Hat", "_Helmet", "_Hood", "_Cloak", "_Eyes", "_Jaw", "_Skull" };
        static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        public ModelSpec Spec;
        public string Id;
        Transform inst, handR, handL;
        Animation anim;
        ProcBody proc;
        float phase;
        readonly List<Renderer> rends = new List<Renderer>();
        readonly List<SkinnedMeshRenderer> skins = new List<SkinnedMeshRenderer>();
        MaterialPropertyBlock mpb;
        GameObject weaponGo;
        string cur;
        string oneShot;
        float oneShotT;
        float squash;
        bool visible = true, hiddenUnder;
        float lastHurt;
        float sink;
        bool deadPlayed;

        public static Model3D Build(Transform parent, string id)
        {
            var spec = ModelSpecs.Get(id);
            if (spec == null) return null;
            var prefab = spec.Proc == null ? Resources.Load<GameObject>(spec.Model) : null;
            if (spec.Proc == null && prefab == null) return null;
            var go = new GameObject("model_" + id);
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<Model3D>();
            m.Spec = spec;
            m.Id = id;
            m.mpb = new MaterialPropertyBlock();
            m.phase = Random.value * 6.28f;
            if (spec.Proc != null)
            {
                m.inst = new GameObject("proc").transform;
                m.inst.SetParent(go.transform, false);
                m.proc = ProcBody.Create(spec.Proc, m.inst, spec);
                m.rends.AddRange(m.proc.Renderers);
                go.transform.rotation = View3D.Face(Vector2.down);
                return m;
            }
            m.inst = Instantiate(prefab, go.transform, false).transform;
            m.inst.name = "mesh";
            m.inst.localPosition = Vector3.zero;
            m.inst.localRotation = Quaternion.identity;
            m.inst.localScale = Vector3.one * (spec.Height / spec.RawHeight);
            m.anim = m.inst.GetComponent<Animation>();
            m.mpb = new MaterialPropertyBlock();
            foreach (var t in m.inst.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "handslot.r") m.handR = t;
                else if (t.name == "handslot.l") m.handL = t;
            }
            m.SetupRenderers();
            if (!string.IsNullOrEmpty(spec.Weapon)) m.weaponGo = m.Attach(spec.Weapon, m.handR);
            if (!string.IsNullOrEmpty(spec.Offhand)) m.Attach(spec.Offhand, m.handL);
            if (m.anim != null)
            {
                m.anim.playAutomatically = false;
                m.anim.cullingType = AnimationCullingType.AlwaysAnimate;
                if (m.anim[spec.Idle] != null)
                {
                    m.anim.Play(spec.Idle);
                    m.anim[spec.Idle].wrapMode = WrapMode.Loop;
                    m.anim[spec.Idle].normalizedTime = Random.value;
                    m.cur = spec.Idle;
                    m.anim.Sample();
                }
            }
            go.transform.rotation = View3D.Face(Vector2.down);
            return m;
        }

        void SetupRenderers()
        {
            rends.Clear();
            skins.Clear();
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name;
                bool body = false;
                foreach (var p in BodyParts) if (n.Contains(p)) { body = true; break; }
                bool show = Spec.ShowAll || (body ? System.Array.IndexOf(Spec.Hide, n) < 0 : System.Array.IndexOf(Spec.Show, n) >= 0);
                r.gameObject.SetActive(show);
                if (!show) continue;
                Convert(r);
                rends.Add(r);
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.updateWhenOffscreen = true;
                    skins.Add(smr);
                }
            }
        }

        void Convert(Renderer r)
        {
            var src = r.sharedMaterials;
            var dst = new Material[src.Length];
            for (int i = 0; i < src.Length; i++) dst[i] = LitFor(src[i]);
            r.sharedMaterials = dst;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
        }

        Material LitFor(Material src)
        {
            bool glow = src != null && src.name.Contains("Glow");
            string sn = src != null ? src.name.ToLowerInvariant() : "";
            bool cards = sn.Contains("hair") || sn.Contains("ponytail") || sn.Contains("eyebrow") || sn.Contains("eyelash");
            var tex = src != null && src.HasProperty("_MainTex") ? src.mainTexture : null;
            string key = Id + "|" + (glow ? "glow" : (src != null ? src.name : "-") + "|" + (tex != null ? tex.name : "none"));
            if (matCache.TryGetValue(key, out var m) && m != null) return m;
            m = Mats.NewLit(tex, "lit_" + key);
            if (tex == null && src != null && src.HasProperty("_Color")) m.SetColor("_Color", src.color);
            if (glow)
            {
                m.SetColor("_Color", Color.black);
                m.SetColor("_Emission", Spec.Eyes * 2.2f);
                m.SetFloat("_Rim", 0f);
            }
            else
            {
                m.SetColor("_Tint", Spec.Tint);
                // WyrdAcc_ = gear painted in Blender with its final colours: never recoloured
                m.SetFloat("_TintAmt", sn.StartsWith("wyrdacc") ? 0f : Spec.TintAmt);
                m.SetColor("_RimColor", Spec.Rim);
                m.SetFloat("_Rim", Spec.RimAmt);
            }
            if (cards)
            {
                m.SetFloat("_Cutoff", 0.45f);
                m.SetFloat("_Cull", 0f);
            }
            if (Spec.MatColor != null)
                foreach (var kv in Spec.MatColor)
                    if (sn.Contains(kv.Key)) m.SetColor("_Color", m.GetColor("_Color") * kv.Value);
            matCache[key] = m;
            return m;
        }

        GameObject Attach(string path, Transform slot)
        {
            if (slot == null) return null;
            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            var go = Instantiate(prefab, slot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                Convert(r);
                rends.Add(r);
            }
            return go;
        }

        /// <summary>Swap the item in the right hand (the player's weapons).</summary>
        public void SetWeapon(string path)
        {
            if (weaponGo != null)
            {
                foreach (var r in weaponGo.GetComponentsInChildren<Renderer>(true)) rends.Remove(r);
                Destroy(weaponGo);
            }
            weaponGo = string.IsNullOrEmpty(path) ? null : Attach(path, handR);
            ApplyVisible();
        }

        // ------------------------------------------------------------------ state from the entity

        public Vector2 HandWorld => handR != null ? View3D.Flatten(handR.position) : View3D.Flatten(transform.position + View3D.CamRot * Vector3.up);

        public void SetVisible(bool v)
        {
            if (visible == v) return;
            visible = v;
            ApplyVisible();
        }

        void ApplyVisible()
        {
            bool on = visible && !hiddenUnder;
            foreach (var r in rends) if (r != null) r.enabled = on;
        }

        public void SetFlash(float amount, Color c)
        {
            mpb.Clear();
            mpb.SetFloat("_Flash", amount);
            mpb.SetColor("_FlashColor", c);
            foreach (var r in rends) if (r != null) r.SetPropertyBlock(mpb);
        }

        public void Squash(float s) => squash = s;

        /// <summary>Play a one-shot clip now (e.g. the player's swing); it wins over idle/run until it ends.</summary>
        public void PlayOnce(string clip, float duration, float startNorm = 0f)
        {
            if (anim == null || anim[clip] == null) return;
            var st = anim[clip];
            float len = st.length * (1f - startNorm);
            anim.CrossFade(clip, 0.06f, PlayMode.StopSameLayer);
            st.wrapMode = WrapMode.ClampForever;
            st.normalizedTime = startNorm;
            st.speed = len / Mathf.Max(0.05f, duration);
            cur = clip;
            oneShot = clip;
            oneShotT = duration;
        }

        /// <summary>
        /// Play an attack so that its contact frame (about 42% into a KayKit swing) lands exactly when the
        /// gameplay windup ends, however fast or slow the weapon is.
        /// </summary>
        public void SwingTimed(string clip, float windup, float total, float contact = 0.42f)
        {
            if (anim == null || anim[clip] == null) return;
            if (clip == cur && oneShot != null) clip = clip.Contains("Diagonal") ? "1H_Melee_Attack_Slice_Horizontal" : clip;
            float f = Mathf.Clamp01(windup / Mathf.Max(0.05f, total));
            float start = Mathf.Clamp((contact - f) / Mathf.Max(0.05f, 1f - f), 0f, 0.6f);
            PlayOnce(clip, total, start);
        }

        void Play(string clip, float speed = 1f, float fade = 0.15f, WrapMode wrap = WrapMode.Loop)
        {
            if (anim == null) return;
            if (anim[clip] == null) clip = Spec.Idle;
            if (anim[clip] == null) return;
            var st = anim[clip];
            if (cur != clip)
            {
                anim.CrossFade(clip, fade, PlayMode.StopSameLayer);
                st.wrapMode = wrap;
                if (wrap != WrapMode.Loop) st.normalizedTime = 0f;
                cur = clip;
            }
            st.speed = speed;
        }

        void Scrub(string clip, float norm)
        {
            if (anim == null) return;
            if (anim[clip] == null) return;
            var st = anim[clip];
            if (cur != clip)
            {
                anim.CrossFade(clip, 0.08f, PlayMode.StopSameLayer);
                st.wrapMode = WrapMode.ClampForever;
                cur = clip;
            }
            st.speed = 0f;
            st.normalizedTime = Mathf.Clamp01(norm);
        }

        public void Animate(in RigInput r, Vector2 facing, float dt)
        {
            if (proc != null) { AnimateProc(r, facing, dt); return; }
            if (anim == null) return;
            if (dt <= 0f) { anim.enabled = false; return; }
            anim.enabled = true;

            // face the way the character is facing on the floor (dead bodies stay put)
            if (!r.Dead)
            {
                var want = View3D.Face(facing);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, 1100f * dt);
            }

            bool under = r.Underground;
            if (under != hiddenUnder) { hiddenUnder = under; ApplyVisible(); }

            squash = U.Damp(squash, 0f, 10f, dt);
            float sx = 1f + squash, sy = 1f - squash * 0.8f;
            float spawnScale = 1f;

            if (oneShot != null)
            {
                oneShotT -= dt;
                if (oneShotT <= 0f) oneShot = null;
            }

            if (r.Dead)
            {
                if (!deadPlayed)
                {
                    deadPlayed = true;
                    oneShot = null;
                    string death = Spec.Death ?? (Spec.IsSkeleton ? "Death_C_Skeletons" : (Random.value < 0.5f ? "Death_A" : "Death_B"));
                    Play(death, 1.2f, 0.08f, WrapMode.ClampForever);
                }
                if (r.DeadT > 1.4f) sink = Mathf.Min(2.5f, sink + dt * 0.6f);
            }
            else if (r.Spawn < 1f)
            {
                deadPlayed = false;
                sink = 0f;
                if (Spec.IsSkeleton) Scrub("Spawn_Ground_Skeletons", Mathf.Lerp(0.05f, 0.95f, r.Spawn));
                else spawnScale = Mathf.Max(0.02f, U.OutBack(Mathf.Clamp01(r.Spawn)));
            }
            else if (r.Action == RigAction.Dash)
            {
                Play(Spec.Dodge, 1.3f, 0.05f, WrapMode.ClampForever);
            }
            else if (oneShot != null) { }
            else if (r.Action != RigAction.None)
            {
                float k = Mathf.Clamp01(r.ActionT);
                switch (r.Action)
                {
                    case RigAction.Windup: Scrub(Spec.Melee, Mathf.Lerp(0f, 0.3f, U.OutCubic(k))); break;
                    case RigAction.Strike: Scrub(Spec.Melee, Mathf.Lerp(0.3f, 0.62f, U.OutCubic(Mathf.Clamp01(k * 2.5f)))); break;
                    case RigAction.SlamWindup: Scrub(Spec.Heavy, Mathf.Lerp(0f, 0.32f, U.OutCubic(k))); break;
                    case RigAction.SlamStrike: Scrub(Spec.Heavy, Mathf.Lerp(0.32f, 0.6f, U.OutCubic(Mathf.Clamp01(k * 3f)))); break;
                    case RigAction.Lunge: Scrub(Spec.Stab, Mathf.Lerp(0.25f, 0.45f, k)); break;
                    case RigAction.Throw: Scrub("Throw", Mathf.Lerp(0.15f, 0.6f, k)); break;
                    case RigAction.Shoot:
                        if (k < 1f) Play(Spec.Aim, 1f, 0.12f);
                        else Scrub(Spec.Shoot, 0.45f);
                        break;
                    default: Play(Spec.Cast, 1f, 0.12f); break;
                }
            }
            else if (r.Hurt > 0.9f && Time.time - lastHurt > 0.3f)
            {
                lastHurt = Time.time;
                PlayOnce(Spec.Hit ?? (Random.value < 0.5f ? "Hit_A" : "Hit_B"), 0.3f, 0.05f);
            }
            else
            {
                float s = r.Speed;
                float scale = Spec.Height / 1.6f;
                if (s < 0.3f) Play(Spec.Idle, 1f, 0.2f);
                else if (Spec.Shamble) Play("Walking_D_Skeletons", Mathf.Clamp(s / (1.7f * scale), 0.6f, 2f), 0.15f);
                else if (s < 1.6f * scale) Play(Spec.Walk, Mathf.Clamp(s / (1.4f * scale), 0.6f, 1.6f), 0.15f);
                else Play(Spec.Run, Mathf.Clamp(s / (Spec.RunRef * scale), 0.7f, 1.7f), 0.12f);
            }

            float hover = Spec.Hover > 0f && !r.Dead ? Spec.Hover + Mathf.Sin(Time.time * 2.3f + phase) * 0.08f : 0f;
            inst.localPosition = new Vector3(0f, hover - sink, 0f);
            transform.localScale = new Vector3(sx * spawnScale, sy * spawnScale, sx * spawnScale);
        }

        void AnimateProc(in RigInput r, Vector2 facing, float dt)
        {
            if (dt <= 0f) return;
            if (!r.Dead) transform.rotation = Quaternion.RotateTowards(transform.rotation, View3D.Face(facing), 600f * dt);
            bool under = r.Underground && Spec.Proc != "dragon";
            if (under != hiddenUnder) { hiddenUnder = under; ApplyVisible(); }
            squash = U.Damp(squash, 0f, 10f, dt);
            transform.localScale = new Vector3(1f + squash, 1f - squash * 0.8f, 1f + squash);
            if (r.Dead && r.DeadT > 1.4f) sink = Mathf.Min(2.5f, sink + dt * 0.6f);
            var p = transform.position;
            proc.Tick(r, facing, new Vector2(p.x, p.y), sink, dt);
        }

        // ------------------------------------------------------------------ afterimages

        /// <summary>Freeze the current pose as a fading additive silhouette.</summary>
        public void Afterimage(Color c, float life)
        {
            if (!visible) return;
            foreach (var smr in skins)
            {
                if (smr == null || !smr.enabled || !smr.gameObject.activeInHierarchy) continue;
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                var go = new GameObject("ghost");
                go.transform.SetParent(World.Root, false);
                go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = Mats.Ghost;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                var g = go.AddComponent<GhostFade>();
                g.Init(mesh, mr, c, life);
            }
        }
    }

    /// <summary>Fades a baked afterimage out and cleans up its mesh.</summary>
    public sealed class GhostFade : MonoBehaviour
    {
        Mesh mesh;
        MeshRenderer mr;
        Color c;
        float life, t;
        MaterialPropertyBlock mpb;

        public void Init(Mesh m, MeshRenderer r, Color col, float l)
        {
            mesh = m; mr = r; c = col; life = Mathf.Max(0.05f, l);
            mpb = new MaterialPropertyBlock();
            Apply(1f);
        }

        void Apply(float k)
        {
            mpb.SetColor("_Color", new Color(c.r, c.g, c.b, c.a * k));
            mr.SetPropertyBlock(mpb);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = 1f - t / life;
            if (k <= 0f) { Destroy(gameObject); return; }
            Apply(k * k);
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
