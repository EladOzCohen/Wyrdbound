# WYRDBOUND — Saga I: The Bound Son

A Norse-mythology roguelike hack-and-slash in the spirit of *Hades*, built in **Unity 6**.

> *In the age before the Twilight, the trickster Loki brought death to Baldr the Bright.
> The gods turned his son Váli into a wolf — and the wolf tore apart his brother, Narfi.
> With Narfi's entrails, they bound Loki beneath the earth.
> Narfi died. But the dead do not rest in Helheim. Not when their father screams.*

You are **Narfi**, son of Loki, waking dead in the hall of your half-sister **Hel**. Each escape climbs through four realms of the dead to reach the cave where your father lies bound. Every death returns you to Hel's hall, where the story moves forward, the dead remember you, and you grow stronger.

**Everything is generated in code.** That covers every character, floor, rune, icon and UI frame, every sound effect, and the full generative Nordic score (drones, frame drums, plucked lyre, bowed tagelharpa). The project ships with no art or audio files.

---

## Features

- **Fast, telegraphed combat.** Dash with i-frames, dash-strikes, hit-stop, screen shake and knockback into walls.
- **Four weapons:**
  - Bitter Edge, a three-hit axe combo with a whirling special
  - Gjallr, a spear with a hold-to-spin attack and a returning throw
  - Ullr's Yew, a bow with charged shots, a perfect-release Power Shot and a volley
  - Fang & Claw, twin seaxes with backstab bonuses and a bounding thrown blade
- **Rune-cast.** You throw rune-stones that lodge in foes, then take them back from the fallen.
- **58 blessings** from Thor, Freyja, Týr, Skaði, Odin, Njörðr and Ratatoskr, including 8 Duo blessings. Each god has a signature mechanic:
  - Thor: lightning and chain-lightning
  - Skaði: Frostbite and Shatter
  - Týr: Verdict
  - Odin: crits and Raven's Mark
  - Freyja: Enthrall and Charm
  - Njörðr: tides and wall-slams
- **Four realms:** Niflhel, Náströnd, Gjöll and Helgrind. Chambers are procedurally generated platforms over the void, with **20 foes**, armored elites, **3 mini-bosses** and **4 bosses**: Váli the Wolf-Brother, Níðhöggr, Garmr and Hel, who has two lives.
- **Hel's hall (Éljúðnir)**, the hub between escapes:
  - Talk to Hel, Baldr, Nanna, Mímir, Ganglöt the slow handmaid, the Norns, and later your brother.
  - Gift Mead to earn keepsakes.
  - Read Mímir's Codex.
- **Meta-progression:**
  - The Rune-Stone of Narfi: 10 permanent upgrades bought with Soul-Embers
  - Hel's Armory: unlock weapons with Grave-Keys
  - Keepsakes, Fate-Threads (rerolls) and Death Defiance
- **The Weave of Wyrd**: 10 optional heat modifiers set by the Norns, with bounties for new records.
- **A branching story** of ~180 conversations, gated by progress, deaths, kills and relationships. It ends in a **two-path ending** that sets up Saga II.
- **Accessibility:** *Odin's Grace* (damage reduction that grows with each death), toggles for screen shake, damage numbers and hurt flashes, and volume controls.
- **Input:** full keyboard + mouse and gamepad support through the Input System.

---

## Getting started

1. Install **Unity 6.6 (6000.6.6f1)** through Unity Hub.
2. In Unity Hub choose **Add → Add project from disk** and select this folder.
3. Open the project. On first open, the editor creates `Assets/Scenes/Main.unity` automatically and adds it to the build.
4. Press **Play**. The game bootstraps itself from code in any scene.

### Controls

| Action | Keyboard / Mouse | Gamepad |
|---|---|---|
| Move | WASD / Arrows | Left stick |
| Aim | Mouse | Right stick (or move direction, with aim assist) |
| Attack | Left mouse / J | X |
| Special | Right mouse / K | Y |
| Cast rune | Q / L / Middle mouse | RT / RB |
| Dash | Space / Shift | A |
| Interact | E | B |
| Call a god (full Fury) | F | LT / LB |
| Re-weave blessings | R | Back |
| Pause | Esc / Tab | Start |

### Building

- From the editor: **Wyrdbound → Build Windows**. The output goes to `Builds/Windows/Wyrdbound.exe`.
- From the command line:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -executeMethod Wyrdbound.EditorTools.WyrdboundSetup.BuildWindows -logFile build.log
```

### Automated play-test

The build contains a bot that plays the game end to end. It goes title → intro → hall → every menu → a full escape (combat, blessings, shop, doors, bosses) → death → return. Along the way it writes screenshots and a log of every error. It uses its own save file.

```bash
Builds/Windows/Wyrdbound.exe -wyrdtest -wyrdout ./autotest -screen-fullscreen 0 -screen-width 1600 -screen-height 900
```

Other editor menu items: **Wyrdbound → Reset Save Data** and **Wyrdbound → Open Save Folder**.

---

## Project layout

```
Assets/Wyrdbound/
  Scripts/
    Core/      Game (services, time, flow), Boot, input, save, types, utilities, AutoTest
    Art/       Pix (CPU SDF canvas), CharArt, PropArt, RoomArt, SpriteLib, Runes, palette
    Audio/     Synth (SFX), MusicGen (two-layer generative score), AudioManager
    Fx/        VfxSystem (particles, telegraphs, damage numbers), CameraRig, Fonts
    World/     RoomGrid, RoomGen, LevelBuilder, Ambient, Door, Pickup, Encounter, RunDirector, RunState
    Combat/    Combat pipeline, Entity, Projectile, Zone
    Player/    Player, Weapons, ThrownWeapon, RuneStone
    Boons/     BoonDefs (all 58), BoonSystem (all effects)
    Enemies/   EnemyDefs, data-driven Enemy AI, Bosses
    Hub/       Hel's hall, meta-progression definitions
    Story/     Story engine, StoryDB (all dialogue), Codex
    UI/        uGUI built from code: HUD, dialogue, blessings, hall menus, title, pause, settings
  Resources/Shaders/   premultiplied sprite + additive glow shaders
  Editor/              project setup, build and save tools
docs/
  SAGA_BIBLE.md   story, cast, and the shape of the series
  ROADMAP.md      what comes next
  HANDOFF.md      development notes
```

### Replacing the art or fonts later

- **Fonts:** drop a font at `Assets/Wyrdbound/Resources/Fonts/Main.ttf` (and optionally `Title.ttf`). The whole game picks it up.
- **Sprites:** every sprite comes from `CharArt.Get(id)`, `PropArt.Get(kind)` or `SpriteLib`. Commissioned art can be swapped in at those three entry points without touching gameplay code.

---

*Original code and content. Norse mythology is public domain; all characterisation and writing here is original.*
