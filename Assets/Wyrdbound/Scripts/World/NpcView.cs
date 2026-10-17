using UnityEngine;

namespace Wyrdbound
{
    /// <summary>
    /// A friendly figure that idles, breathes and turns toward Narfi: a 3D model when the cast list has
    /// one (see ModelSpecs), otherwise the animated 2D rig standing upright in the world.
    /// </summary>
    public sealed class NpcView : MonoBehaviour
    {
        public SpriteRenderer Body;
        public string Art;
        public Rig Rig;
        public Model3D Model;
        SpriteRenderer aura;
        Transform visual, overlay;
        float t, baseX = 1f;
        bool faceLeft;
        public bool TrackPlayer = true;
        SpriteRenderer marker;
        Vector2 facing;

        public void Init(string art, bool left, Color glow)
        {
            Art = art;
            faceLeft = left;
            facing = left ? new Vector2(-0.6f, -0.8f) : new Vector2(0.6f, -0.8f);
            visual = new GameObject("visual").transform;
            visual.SetParent(transform, false);
            Model = Model3D.Build(visual, art);
            if (Model != null) Body = Mats.SR(visual, "body", null, 0);
            else
            {
                Rig = Rig.Build(visual, art);
                Body = Rig.Main;
                View3D.Stand(visual);
            }
            var sh = Mats.SR(transform, "shadow", SpriteLib.Shadow, Sort.Shadow);
            sh.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            if (glow.a > 0f)
            {
                aura = Mats.SR(transform, "aura", SpriteLib.Light, Sort.FloorFx + 3, glow, true);
                aura.transform.localScale = new Vector3(4f, 4f, 1f);
                Lights.Attach(transform, new Vector2(0f, 1f), 4.5f, glow.A(1f), 0.7f);
            }
            overlay = View3D.UprightChild(transform, "overlay");
            marker = Mats.SR(overlay, "marker", SpriteLib.Icon("story", false), Sort.Fx);
            marker.sharedMaterial = Mats.SpriteTop;
            marker.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            marker.enabled = false;
            t = Random.value * 10f;
        }

        /// <summary>Show a floating marker when this character has something new to say.</summary>
        public void SetMarker(bool on) { if (marker != null) marker.enabled = on; }

        float Height => Model != null ? Model.Spec.Height * 0.68f + 0.3f : CharArt.Height(Art);

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            var p = Player.I;
            bool near = TrackPlayer && p != null && Vector2.Distance(p.Pos, transform.position) < 5f;
            if (Model != null)
            {
                Vector2 want = near ? (p.Pos - (Vector2)transform.position) : (faceLeft ? new Vector2(-0.6f, -0.8f) : new Vector2(0.6f, -0.8f));
                facing = Vector2.Lerp(facing, want.normalized, 1f - Mathf.Exp(-6f * dt));
                Model.Animate(new RigInput { MaxSpeed = 3f, Spawn = 1f }, facing, dt);
            }
            else
            {
                bool left = faceLeft;
                if (near) left = p.Pos.x < transform.position.x;
                baseX = Mathf.MoveTowards(baseX, left ? -1f : 1f, dt * 8f);
                float sx = Mathf.Abs(baseX) < 0.2f ? Mathf.Sign(baseX == 0f ? 1f : baseX) * 0.2f : baseX;
                visual.localScale = new Vector3(sx, 1f, 1f);
                Rig.SetOrder(Sort.Y(transform.position.y));
                Rig.Animate(new RigInput { MaxSpeed = 3f, Spawn = 1f }, dt);
            }
            if (aura != null) aura.color = aura.color.A(0.18f + 0.05f * Mathf.Sin(t * 1.3f));
            if (marker != null && marker.enabled)
                marker.transform.localPosition = new Vector3(0f, Height + 0.5f + Mathf.Sin(t * 3f) * 0.1f, 0f);
        }
    }
}
