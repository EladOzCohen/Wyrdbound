using UnityEngine;

namespace Wyrdbound
{
    /// <summary>A rune-carved arch leading onward. Shows what lies beyond once the chamber is cleared.</summary>
    public sealed class Door : MonoBehaviour
    {
        public RoomPlan Plan;
        public bool Open;
        Vector2 spot;
        SpriteRenderer arch, portal, icon, iconGlow;
        float t;
        Color col;
        bool used;

        public static Door Create(Vector2 spot, RoomPlan plan, Biome b, bool reveal)
        {
            var go = new GameObject("door");
            go.transform.SetParent(World.Root, false);
            go.transform.position = new Vector3(spot.x, spot.y, 0f);
            var d = go.AddComponent<Door>();
            d.Plan = plan;
            d.spot = spot;
            d.col = DoorColor(plan);
            d.arch = Mats.SR(go.transform, "arch", PropArt.Get("door", b), Sort.Y(spot.y) - 5);
            d.arch.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            d.portal = Mats.SR(go.transform, "portal", SpriteLib.Glow, Sort.Y(spot.y) - 4, d.col.A(0f), true);
            d.portal.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            d.portal.transform.localScale = new Vector3(2.6f, 3.4f, 1f);
            Sprite ic = reveal ? SpriteLib.RewardIcon(IconReward(plan), plan.God, false) : Runes.Sprite(Runes.Perthro);
            d.iconGlow = Mats.SR(go.transform, "iconGlow", SpriteLib.Glow, Sort.Fx - 30, d.col.A(0.25f), true);
            d.iconGlow.transform.localScale = new Vector3(2f, 2f, 1f);
            d.icon = Mats.SR(go.transform, "icon", ic, Sort.Fx - 29);
            d.icon.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            Interactable.Add(go, 2.2f, () => (reveal ? plan.Label() : "An unknown path") + "  " + U.Tint("(enter)", Pal.Dim), () => d.Enter(), () => d.Open && !d.used);
            return d;
        }

        static Reward IconReward(RoomPlan p)
        {
            switch (p.Kind)
            {
                case RoomKind.Shop: return Reward.Shop;
                case RoomKind.Rest: return Reward.Rest;
                case RoomKind.Story: return Reward.Story;
                case RoomKind.Boss: return Reward.Boss;
                case RoomKind.Passage: return Reward.Passage;
                case RoomKind.Surface: return Reward.Surface;
            }
            return p.Reward;
        }

        static Color DoorColor(RoomPlan p)
        {
            switch (p.Kind)
            {
                case RoomKind.Boss: return Pal.Danger;
                case RoomKind.MiniBoss: return Pal.Gold;
                case RoomKind.Shop: return Pal.Silver;
                case RoomKind.Rest: return U.Hex("5ec8ff");
                case RoomKind.Story: return Pal.Hel;
                case RoomKind.Surface: return Pal.Gold;
            }
            switch (p.Reward)
            {
                case Reward.Boon: return Pal.God(p.God);
                case Reward.Ratatoskr: return Pal.God(God.Ratatoskr);
                case Reward.Apple: return Pal.Gold;
                case Reward.Embers: return Pal.Ember;
                case Reward.Salve: return Pal.Heal;
                case Reward.Mead: return Pal.Mead;
            }
            return Pal.Bone;
        }

        public void SetOpen()
        {
            if (Open) return;
            Open = true;
            Lights.Attach(transform, new Vector2(0f, 1.6f), 3.6f, col, 0.75f);
            Vfx.Flash(spot + new Vector2(0f, 1.6f), 1.6f, col, 0.4f);
            Vfx.Motes(spot + new Vector2(0f, 1.2f), 12, col, 1f, 1.5f, 1f);
        }

        void Enter()
        {
            if (!Open || used) return;
            used = true;
            Game.I.Audio.Play("door", 0.7f);
            RunDirector.I.TakeDoor(this);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            float bob = Mathf.Sin(t * 2.2f) * 0.12f;
            icon.transform.localPosition = new Vector3(0f, 4.15f + bob, 0f);
            iconGlow.transform.localPosition = icon.transform.localPosition;
            float a = Open ? 0.45f + 0.15f * Mathf.Sin(t * 3f) : 0f;
            portal.color = col.A(a);
            icon.color = Open ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.75f);
            iconGlow.color = col.A(Open ? 0.35f : 0.1f);
            if (Open && !used && Random.value < dt * 8f)
                Vfx.Spawn(SpriteLib.Mote, spot + new Vector2(Random.Range(-1f, 1f), Random.Range(0.3f, 2.6f)), new Vector2(0f, 0.6f), 1f, 0.3f, 0.05f, col, col.A(0f), true);
            var p = Player.I;
            if (Open && !used && p != null && !p.Dead && Vector2.Distance(p.Pos, spot - new Vector2(0f, 0.35f)) < 0.75f) Enter();
        }
    }
}
