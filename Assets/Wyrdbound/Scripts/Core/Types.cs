namespace Wyrdbound
{
    public enum God { None, Thor, Freyja, Tyr, Skadi, Odin, Njord, Ratatoskr }

    public enum Rarity { Common, Rare, Epic, Heroic, Duo }

    public enum BoonSlot { Attack, Special, Cast, Dash, Call, Passive }

    public enum Team { Player, Enemy, Neutral }

    public enum HitKind { Attack, Special, Cast, Dash, Call, Effect, Enemy, Hazard }

    /// <summary>What a chamber promises behind its door.</summary>
    public enum Reward { None, Boon, Apple, Silver, Embers, Key, Mead, Salve, Ratatoskr, Shop, Rest, Story, Hoard, Passage, Boss, Surface }

    public enum RoomKind { Combat, MiniBoss, Shop, Rest, Story, Boss, Passage, Hoard, Surface, Hub }

    public enum GameMode { Boot, Title, Hub, Run, Cutscene }
}
