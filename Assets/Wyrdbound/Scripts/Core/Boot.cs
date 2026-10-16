using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Creates the game in whatever scene is loaded. The project needs no authored scenes.</summary>
    public static class Boot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (Game.I != null) return;
            var go = new GameObject("Wyrdbound");
            go.AddComponent<Game>();
        }
    }
}
