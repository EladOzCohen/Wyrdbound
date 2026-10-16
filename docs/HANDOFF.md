# WYRDBOUND — Saga I: The Bound Son — Handoff / Progress Notes

> **בעברית בקצרה:** הפרויקט הוא משחק Hack-and-Slash בסגנון Hades על מיתולוגיה נורדית, ב-Unity (C#).
> כל הגרפיקה והסאונד נוצרים בקוד (אין קבצי אמנות). נכתבו בערך 60% מהקוד: מנוע האמנות, הסאונד והמוזיקה,
> מערכת הקרב, השחקן ו-4 נשקים, 58 ברכות אלים, 20 אויבים ו-4 בוסים, יצירת חדרים, מטא-פרוגרסיה, ה-HUD ומנוע הסיפור.
> **מה חסר:** הקובץ המרכזי `Game.cs`, ניהול הריצה (חדרים, דלתות, פרסים, חנות), אולם Hel (ה-Hub), כל מסכי ה-UI,
> כל הדיאלוגים (התוכן של הסיפור), בדיקת קומפילציה והרצה ב-Unity.
> **לפני הסשן הבא:** צריך לסיים את ההורדה של Unity 6.6 (6000.6.6f1) ב-Unity Hub (היא עצרה באמצע) ולהתחבר לחשבון.
> בסשן הבא אפשר פשוט לכתוב: "תמשיך את Wyrdbound לפי docs/HANDOFF.md".

---

## 1. Decisions locked in (from the user)

| Topic | Decision |
|---|---|
| Engine | **Unity (C#)**. Target Unity 6.6 (6000.6.6f1). Its download is **paused/partial** in Unity Hub; the user must finish it and sign in. |
| Art | **All code-generated**: SDF shapes on a CPU canvas → textures at runtime. Built so commissioned art can replace it later. |
| View | **2D top-down 3/4** (like Hades), y-sorted sprites. |
| Story | **WYRDBOUND**: Narfi, son of Loki, escaping Helheim (details in §4). |
| Scope | "Serious", long, sellable. Built in milestones; Saga I = 4 realms + hub + story + meta. |

## 2. Technical architecture

- Project folder: `C:\Users\elado\Desktop\Wyrdbound` (not a git repo yet; `.gitignore` already written).
- `Packages/manifest.json`: ugui 2.0.0, inputsystem 1.11.2 (**verify the version against the installed editor**), core modules.
- Asmdef `Assets/Wyrdbound/Scripts/Wyrdbound.asmdef` references `UnityEngine.UI` and `Unity.InputSystem`. The versionDefine `WYRD_INPUTSYSTEM` is set only when the package exists. `GameInput` supports both the new and the legacy input backends.
- Editor asmdef: `Assets/Wyrdbound/Editor/Wyrdbound.Editor.asmdef` (the folder is still empty; see TODO).
- Shaders in `Assets/Wyrdbound/Resources/Shaders/`: `Wyrd/Sprite` (premultiplied alpha + `_Flash`/`_FlashColor` hit flash) and `Wyrd/Additive`. Loaded via `Resources.Load`. **All world textures are premultiplied; UI textures are straight alpha (`Pix.Bleed`)**.
- No scenes and no physics. `Boot` (TODO) creates a `Game` GameObject via `[RuntimeInitializeOnLoadMethod]`. Collision is tile-grid + circles in `RoomGrid.Move`.
- UI is uGUI built from code, with no EventSystem. Navigation goes through `MenuList` (keyboard, gamepad and mouse hover/click).
- World units: 1 tile = 1 unit. Sprites use 64 px per unit; floors use 32 px per unit. Ortho camera size 8.6.

## 3. Files written so far (all under `Assets/Wyrdbound/Scripts/`)

| File | Contents |
|---|---|
| Core/Util.cs | `U` helpers (vectors, colors, easing, `U.Clock`), `Sort` orders, `Rng` |
| Core/Types.cs | enums: God, Rarity, BoonSlot, Team, HitKind, Reward, RoomKind, GameMode |
| Core/GameInput.cs | device-agnostic input, edge detection, nav repeat, `Glyph(action)` |
| Core/Save.cs | `SaveData` (JSON, KV lists, flags/counters/affinity/runes/weave/seen), `Settings`, `SaveSystem` |
| Art/Pix.cs | CPU canvas: SDF circle/ellipse/capsule/taper/poly/rect/ring, glow, outline, shading, `Mask`, `Noise` (value/fbm/voronoi) |
| Art/Pal.cs, Runes.cs, Mats.cs | palette and god names/colors/runes; Elder Futhark stroke runes; materials and `Mats.SR` |
| Art/SpriteLib.cs | primitives, slashes, telegraph shapes, projectiles, all reward/god icons, weapon sprites, UI 9-slices, vignette |
| Art/CharArt.cs | humanoid/quadruped/serpent painters; all heroes, NPCs, gods, enemies, bosses. **Missing: `ganglot`, `hodr`, `hermod`** |
| Art/RoomArt.cs, PropArt.cs | floor painter (5 styles, AO, cliffs, decals, rune carving); props, door arch, hub stations (throne, armory, runestone, well, chest, pool, gate, hearth, table) |
| Audio/Synth.cs, MusicGen.cs, AudioManager.cs | ~40 synthesized SFX; generative 2-layer music (calm + fury) per track; pooled playback and crossfade |
| Fx/VfxSystem.cs, Fonts.cs, CameraRig.cs | pooled particles, telegraphs, damage numbers (`Vfx` static API); shake, kick, zoom punch |
| World/Biome.cs, RoomGrid.cs, RoomGen.cs, World.cs, RunState.cs, Interactable.cs | 4 realms + Hall + Surface; collision grid; combat/arena/small/hall/cave generators; `RoomPlan`; per-run state |
| Combat/Combat.cs, Entity.cs, Projectile.cs, Zone.cs | `HitInfo`, `Status`, damage pipeline `Combat.Deal`, hit queries, aim assist; base entity (armor, knockback, flash, health bar, status icons) |
| Player/Player.cs, Weapons.cs, ThrownWeapon.cs, RuneStone.cs | Narfi (dash, cast, Fury call, defiance, Hel's mask); Axe, Spear, Bow, Seax |
| Boons/BoonDefs.cs, BoonSystem.cs | 58 boons (6 gods + Ratatoskr + 8 duos) with all effects |
| Enemies/EnemyDefs.cs, Enemy.cs, Bosses.cs | 20 foes + 3 mini-bosses; data-driven AI; Váli, Níðhöggr (+ segments), Garmr, Hel (2 lives) |
| Hub/Meta.cs | `PlayerStats`, 10 Rune-Stone upgrades, 10 Weave (heat) options, 8 keepsakes, 4 weapons, `Meta.Build` |
| UI/UIKit.cs, UIRoot.cs, HUD.cs | uGUI helpers + `MenuList`; screen stack, fades, banners, toasts; full HUD incl. boss bar |
| Story/Story.cs | speakers, `Convo` (conditions/priority/flags/choices), `Story.Pick/Play/Say/Expand` |

## 4. Story bible (keep this — it is not yet written anywhere else)

**Premise.** Loki caused Baldr's death. The gods turned Loki's son Váli into a wolf; Váli tore apart his brother **Narfi**, and the gods bound Loki with Narfi's entrails beneath the earth, where Skaði's serpent drips venom on him and his wife **Sigyn** catches it in a bowl. Narfi wakes dead in **Éljúðnir**, the hall of his half-sister **Hel**, and keeps trying to escape upward to free his father. Every death returns him to the **Pool of Return**.

**Route (bottom → top):** Niflhel (boss **Váli**) → Náströnd (boss **Níðhöggr**) → Gjöll / Gjallarbrú (boss **Garmr** at Gnipahellir) → Helgrind (boss **Hel**) → the Surface (the cave of binding in Midgard).

**Cast and arcs:**
- **Hel**: the Hades analog. Cold, then loving. She fears Ragnarök because her dead would march and die again. Final boss; rises once ("You cannot kill Death in her own house"). Keepsake: Half-Mask.
- **Ganglöt**: Hel's very slow handmaid (from the myth). Greets each return and comments on the cause of death (Hypnos analog). Needs art and a "return" context.
- **Baldr**: killed by Loki's scheme, gentle mentor, forgives the son. **Twist:** he *wants* Ragnarök, because he returns after it. Keepsake: Draupnir's Echo.
- **Nanna**: Baldr's wife, died of grief. Motherly; asks about Sigyn. Keepsake: Linen.
- **Mímir**: head on a pedestal and keeper of the Codex. **Secretly Odin's spy.** Arc: hints → confession that Odin cursed Váli deliberately to create the binding and wants Ragnarök on his own terms. Keepsake: Draught.
- **Móðguðr**: guardian of the Gjallarbrú bridge and the shopkeeper in runs. Dry humor. Keepsake: Toll-Stone.
- **Váli**: Niflhel boss and tragic brother. Across defeats he regains words; after 3 defeats Hel brings him chained to the hall. Gifting him mead earns the keepsake Fang and sets up forgiveness. Required for the ending.
- **Loki & Sigyn** (surface): Loki is charming and manipulative. The bonds are Narfi's own entrails turned to iron, so cutting them would unmake Narfi. Sigyn (Narfi's mother) begs him to stop coming back, because each time he fades. Their talks advance with each clear.
- **The Norns**: appear at the Well in the hall after the first clear and run the Weave of Wyrd (heat). They reveal Narfi is "the knot".
- **Story-room NPCs**: **Höðr** (blind killer of Baldr, regions 1–2; gift: one boon level-up) and **Hermóðr** (who rode to Hel for Baldr, regions 3–4; gift: silver + heal). Both need art.
- **Gods and their motives**:
  - Thor pities Narfi and wants his own fate with Jörmungandr.
  - Freyja wants Narfi's soul for Fólkvangr and quietly opposes Odin.
  - Týr lost his hand binding Fenrir (Narfi's half-brother) and understands the price of bindings.
  - Skaði hung the serpent over Loki and helps only to see Loki's face.
  - Odin is cryptic, serves the prophecy, and is later revealed to have cursed Váli.
  - Njörðr is a fellow hostage of fate.
  - Ratatoskr is the gossiping squirrel and comic relief; he leaks hints.
- **Ending (Saga I)**: requires clears ≥ 5, Váli affinity ≥ 2, flags `norns_truth` and `mimir_confess`. At the surface Narfi chooses:
  - **A) Cut the bonds**: Loki is freed, Narfi unravels, Ragnarök begins. Teaser for Saga II "Twilight": a green-eyed boy at the prow of Naglfar.
  - **B) Hold the knot**: Narfi stays the binding forever and Odin's plan stalls. The Norns warn: "Threads can be cut by other hands."
  - Post-game continues either way (heat, runs).
- **Intro text cards**: "In the age before the Twilight, the trickster Loki brought death to Baldr the Bright. / The gods' vengeance fell not on Loki alone. / They turned his son Váli into a wolf — and the wolf tore apart his brother, Narfi. / With Narfi's entrails, they bound Loki beneath the earth, where venom drips upon his face. / Narfi died. / But the dead do not rest in Helheim. Not when their father screams."

**Dialogue contexts planned:** `hel, baldr, nanna, mimir, modgud, ganglot/return, vali (hall), hodr, hermod, norns, loki, sigyn, ending, god_thor … god_ratatoskr` (the first meeting is a long intro; later ones are short repeatable lines), `boss_vali, boss_nidhogg, boss_garmr, boss_hel` (pre-fight), `gift_<npc>`. Use the tokens `{cause} {runs} {clears} {deaths}`. Target about 100+ conversations.

## 5. TODO — remaining work, in order

1. **Core glue**
   - `Core/Game.cs`: singleton, `[DefaultExecutionOrder(-1000)]`, owns `Input, Audio, Cam, UI, Director`, `Mode`.
     - `Paused` = UI modal.
     - Timing: `Hitstop(s)`, `SlowMo(scale,s)`, `After(delay, Action)`. Timers run on scaled time and are cleared on level rebuild.
     - Flow: `Transition(mid)` coroutine using `UI.Fade`; `NewGame/Continue`, `GoHub(fromDeath)`, `StartRun()`, `OnPlayerDied()` → DeathScreen → hub, `OnRunWon()`, `QuitToTitle()`.
     - `Update`: Input → Audio.Tick → timers → timeScale → UI.Tick, plus pause key handling.
     - Disable other scene cameras. Player is persistent (child of Game); `World.Root` is per-level; `VfxSystem.I.Clear()` on rebuild.
   - `Core/Boot.cs`: `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` creates the Game.
2. **World flow**
   - `World/LevelBuilder.cs`: floor sprite, props with y-sort and shadows, braziers with flicker light, decor, mist, camera background = `Biome.Bg`.
   - `World/Ambient.cs`: drifting mist and per-biome particles.
   - `World/Door.cs`: arch, reward icon (or "?" with the `dark` weave); entering it calls `RunDirector.TakeDoor`.
   - `World/Pickup.cs`: rewards. Boon → god intro dialogue + BoonScreen; Apple → AppleScreen; currencies; Salve heals 30%.
   - `World/Encounter.cs`: wave budget = `(6 + depth*0.9 + region*2.5) * swarm`, 2–3 waves, elite rolls, minions.
   - `World/RunDirector.cs`:
     - `StartRun`, `BuildRoom(plan)`, `NextOptions` (boss at last index, shop before it, mini-boss at index 3–4, rest/story chances, distinct rewards, god variety ≤ 4).
     - `TakeDoor` (region advance via Passage, Sigyn's Bowl heal), `OnEncounterCleared` (slow-mo, reward, doors, `Boons.OnRoomClear`, `RuneStone.RecallAll`).
     - Shop (Móðguðr + 3 wares priced with `run.Price`), rest spring (40%), story NPCs, boss intro → spawn, `OnBossDefeated` (first-kill rewards, flags `bosskill_<id>`), hound timer bleed.
     - Static `SpawnMinion(id,pos)`, `I`.
   - At run end: carry embers/keys/mead to the save even on death; `deaths++` and `lastDeath`; `clears++`; heat bounty.
3. **Hub** (`Hub/Hub.cs`), built with `RoomGen.Hall()`. Positions:
   - Pool (0,-10.5) and Ganglöt
   - Hel's throne (0,10)
   - Escape gate (-15,10)
   - Armory (-16,0)
   - Rune-Stone (16,0)
   - Mímir (15,-8)
   - Baldr & Nanna by the hearth/table (-12,-7)
   - Norns' Well (15,9), after the first clear
   - Keepsake chest (-5,-11)
   - Váli chained (8,-11), after flag `vali_hall`
   - NPC talk → dialogue → gift menu (mead) → affinity and keepsake.
4. **UI screens** (`UI/Screens*.cs`):
   - Title (Continue / New Saga / Settings / Quit, animated rune ring), Pause, Settings
   - **DialogueScreen(Convo, Action)**: portrait from `CharArt.Get(art, true)`, typewriter, choices
   - BoonScreen (3 cards, reroll with Fate-Threads, rarity colors, "replaces"), AppleScreen
   - RuneStone, Weapons, Weave, Keepsakes, Codex (Mímir's Lore)
   - Death, Victory, Cards (intro/ending), generic Choice
5. **Content**: `Story/StoryDB.cs` with `Register()` (all speakers + ~100 convos) and `Story/Codex.cs` (lore entries).
6. **Art additions**: `ganglot`, `hodr`, `hermod` in `CharArt`.
7. **Editor**: `Editor/WyrdboundSetup.cs`. It runs on load to create `Assets/Scenes/Main.unity`, add it to Build Settings, and set `PlayerSettings` (product name, company, `activeInputHandler` = Both via SerializedObject). It also adds a menu item `Wyrdbound/Build Windows` for `-executeMethod`.
8. **Docs**: `README.md` (how to open, play and build; controls), `docs/SAGA_BIBLE.md` (expand §4), `docs/ROADMAP.md` (Saga II/III).
9. **Verify**:
   - Once Unity is installed, run `Unity.exe -batchmode -quit -projectPath … -logFile …` and fix compile errors. Check the inputsystem version in the editor's built-in package list.
   - Play-test. Tune balance and the cost of floor painting (about 0.6M pixels per room).
10. `git init` + first commit (only when the user asks).

### APIs already referenced by written code that must exist
- `Game.I.Input`, `Game.I.Audio`, `Game.I.Cam`, `Game.I.UI`, `Game.I.Mode`, `Game.I.Paused`
- `Game.I.After(float, Action)`, `Game.I.Hitstop(float)`, `Game.I.SlowMo(float scale, float secs)`, `Game.I.OnPlayerDied()`
- `RunDirector.I`, `RunDirector.SpawnMinion(string, Vector2)`, `RunDirector.I.OnBossDefeated(Boss)`
- `new DialogueScreen(Convo, Action)`, `StoryDB.Register()`
- `UIRoot.Banner/HurtFlash/Toast/Fade/Push/Pop` (done)

### Known nits to check while compiling
- `Vfx.Spawn` "aspect" is a vertical scale; flips use a negative aspect.
- `CharArt.Height()` returns the unscaled height; `Entity` multiplies it by `baseScale` for bars.
- Bosses mutate the shared `AttackDef.Repeat` for rings; it is reset in `Boss.Setup`.
- `GameInput`: E and Space are both confirm and interact. Screens ignore input on the frame they open.
