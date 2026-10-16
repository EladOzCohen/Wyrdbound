using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Global handles to the chamber currently loaded.</summary>
    public static class World
    {
        public static RoomGrid Grid;
        public static Transform Root;
        public static Biome Biome = Biomes.Niflhel;

        public static bool InRoom(Vector2 p, float margin = 0f)
        {
            if (Grid == null) return true;
            var b = Grid.Bounds;
            return p.x > b.xMin - margin && p.x < b.xMax + margin && p.y > b.yMin - margin && p.y < b.yMax + margin;
        }
    }
}
