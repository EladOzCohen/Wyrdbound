using UnityEngine;

namespace Wyrdbound
{
    /// <summary>Turns a RoomGrid into a living chamber: painted floor, props, braziers, mist and motes.</summary>
    public static class LevelBuilder
    {
        public static Ambient Build(RoomGrid g, Biome b, int seed, bool carving, bool runner)
        {
            World.Grid = g;
            World.Biome = b;
            var root = World.Root;
            var floor = Mats.SR(root, "floor", RoomArt.PaintFloor(g, b, seed, carving, runner), Sort.Floor);
            floor.transform.position = new Vector3(g.Origin.x, g.Origin.y, 0f);
            var amb = root.gameObject.AddComponent<Ambient>();
            amb.Init(g, b, seed);
            foreach (var o in g.Obstacles) Prop(root, o, b, true, amb);
            foreach (var o in g.Decor) Prop(root, o, b, false, amb);
            var cam = Game.I.Cam;
            cam.Cam.backgroundColor = b.Bg;
            cam.Bounds = g.Bounds;
            return amb;
        }

        public static SpriteRenderer Prop(Transform root, Obstacle o, Biome b, bool solid, Ambient amb)
        {
            var sr = Mats.SR(root, o.Kind, PropArt.Get(o.Kind, b), solid ? Sort.Y(o.Pos.y) : Sort.FloorFx + 20);
            sr.transform.position = new Vector3(o.Pos.x, o.Pos.y, 0f);
            if (solid)
            {
                var sh = Mats.SR(sr.transform, "shadow", SpriteLib.Shadow, Sort.Shadow, new Color(1, 1, 1, 0.7f));
                sh.transform.localScale = new Vector3(o.R * 3.2f, o.R * 3.2f, 1f);
            }
            if (o.Kind == "brazier") amb?.AddFlame(o.Pos + new Vector2(0f, 0.85f), Pal.Ember);
            if (o.Kind == "pillar" || o.Kind == "stone" || o.Kind == "statue") amb?.AddGlow(o.Pos + new Vector2(0f, 1.4f), b.Accent, 2.2f);
            return sr;
        }

        /// <summary>A world-space text label (prices, names).</summary>
        public static TextMesh Label(Transform parent, string text, Vector2 pos, Color c, float size = 1f)
        {
            var go = new GameObject("label");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Fonts.Main;
            tm.fontSize = 64;
            tm.characterSize = 0.045f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = c;
            tm.text = text;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Fonts.Main.material;
            mr.sortingOrder = Sort.Text - 10;
            return tm;
        }

        /// <summary>Place a non-hostile character in the world.</summary>
        public static NpcView Npc(string art, Vector2 pos, bool faceLeft, Color glow)
        {
            var go = new GameObject("npc_" + art);
            go.transform.SetParent(World.Root, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var v = go.AddComponent<NpcView>();
            v.Init(art, faceLeft, glow);
            if (World.Grid != null) World.Grid.Obstacles.Add(new Obstacle(pos, 0.45f, false, "npc"));
            return v;
        }
    }
}
