using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Anything Narfi can use: NPCs, doors, stations, springs, shop wares.</summary>
    public sealed class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public Func<string> Prompt;
        public Func<bool> Enabled;
        public Action OnUse;
        public float Radius = 1.6f;
        public Vector2 Offset;

        public Vector2 Pos
        {
            get { var p = transform.position; return new Vector2(p.x, p.y) + Offset; }
        }

        public static Interactable Add(GameObject go, float radius, Func<string> prompt, Action use, Func<bool> enabled = null)
        {
            var i = go.AddComponent<Interactable>();
            i.Radius = radius;
            i.Prompt = prompt;
            i.OnUse = use;
            i.Enabled = enabled;
            return i;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool Usable => Enabled == null || Enabled();

        public void Use()
        {
            if (!Usable) return;
            OnUse?.Invoke();
        }

        public static Interactable Nearest(Vector2 p)
        {
            Interactable best = null;
            float bd = float.MaxValue;
            foreach (var i in All)
            {
                if (i == null || !i.isActiveAndEnabled || !i.Usable) continue;
                float d = Vector2.Distance(p, i.Pos);
                if (d <= i.Radius && d < bd) { bd = d; best = i; }
            }
            return best;
        }
    }
}
