using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Shared materials and SpriteRenderer construction.</summary>
    public static class Mats
    {
        static Material sprite, additive, unlit;

        /// <summary>Sprite material that ignores the light buffer (distant backdrops, UI-like world art).</summary>
        public static Material Unlit
        {
            get
            {
                if (unlit == null)
                {
                    unlit = new Material(Sprite) { name = "WyrdUnlit" };
                    unlit.SetFloat("_Lit", 0f);
                }
                return unlit;
            }
        }

        public static Material Sprite
        {
            get
            {
                if (sprite == null) sprite = Make("Shaders/WyrdSprite", "Sprites/Default");
                return sprite;
            }
        }

        public static Material Additive
        {
            get
            {
                if (additive == null) additive = Make("Shaders/WyrdAdditive", "Sprites/Default");
                return additive;
            }
        }

        static Material Make(string resource, string fallback)
        {
            var sh = Resources.Load<Shader>(resource);
            if (sh == null)
            {
                Debug.LogWarning("[Wyrdbound] Shader " + resource + " missing, falling back to " + fallback);
                sh = Shader.Find(fallback);
            }
            return new Material(sh) { name = resource };
        }

        public static SpriteRenderer SR(Transform parent, string name, Sprite s, int order, bool additive = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sharedMaterial = additive ? Additive : Sprite;
            sr.sortingOrder = order;
            return sr;
        }

        public static SpriteRenderer SR(Transform parent, string name, Sprite s, int order, Color c, bool additive = false)
        {
            var sr = SR(parent, name, s, order, additive);
            sr.color = c;
            return sr;
        }
    }
}
