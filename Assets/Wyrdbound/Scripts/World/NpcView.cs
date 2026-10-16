using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A friendly figure: breathing idle, aura, faces Narfi when he is near.</summary>
    public sealed class NpcView : MonoBehaviour
    {
        public SpriteRenderer Body;
        public string Art;
        SpriteRenderer aura;
        Transform visual;
        float t, baseX = 1f;
        bool faceLeft;
        public bool TrackPlayer = true;
        SpriteRenderer marker;

        public void Init(string art, bool left, Color glow)
        {
            Art = art;
            faceLeft = left;
            visual = new GameObject("visual").transform;
            visual.SetParent(transform, false);
            Body = Mats.SR(visual, "body", CharArt.Get(art), Sort.Y(transform.position.y));
            var sh = Mats.SR(transform, "shadow", SpriteLib.Shadow, Sort.Shadow);
            sh.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
            if (glow.a > 0f)
            {
                aura = Mats.SR(transform, "aura", SpriteLib.Light, Sort.FloorFx + 3, glow, true);
                aura.transform.localScale = new Vector3(4f, 4f, 1f);
            }
            marker = Mats.SR(transform, "marker", SpriteLib.Icon("story", false), Sort.Fx);
            marker.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            marker.enabled = false;
            t = Random.value * 10f;
        }

        /// <summary>Show a floating marker when this character has something new to say.</summary>
        public void SetMarker(bool on) { if (marker != null) marker.enabled = on; }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            bool left = faceLeft;
            var p = Player.I;
            if (TrackPlayer && p != null && Vector2.Distance(p.Pos, transform.position) < 5f)
                left = p.Pos.x < transform.position.x;
            baseX = U.Damp(baseX, left ? -1f : 1f, 10f, dt);
            float breathe = 1f + Mathf.Sin(t * 1.8f) * 0.015f;
            visual.localScale = new Vector3(baseX, breathe, 1f);
            Body.sortingOrder = Sort.Y(transform.position.y);
            if (aura != null) aura.color = aura.color.A(0.18f + 0.05f * Mathf.Sin(t * 1.3f));
            if (marker != null && marker.enabled)
                marker.transform.localPosition = new Vector3(0f, CharArt.Height(Art) + 0.5f + Mathf.Sin(t * 3f) * 0.1f, 0f);
        }
    }
}
