# WYRDBOUND — Handoff / Progress Notes

> **בעברית בקצרה:** Saga I שלם ועובד. הוא נבנה ב-Unity 6.6, מתקמפל בלי שגיאות, ובוט בדיקה אוטומטי שיחק בו מקצה לקצה.
> בפעם הבאה: איזון (קושי), ליטוש, והמשך לפי `docs/ROADMAP.md`. הסיפור המלא נמצא ב-`docs/SAGA_BIBLE.md`.
> כדי להמשיך, כתוב: "תמשיך את Wyrdbound לפי docs/HANDOFF.md".

## Status (2026-10-16)

- **All planned Saga I systems are implemented.** That's about 14,300 lines of C# across 60+ files and about 180 conversations.
- **Compiles with 0 errors** on Unity 6000.6.6f1. Packages: `ugui 2.6.0`, `inputsystem 1.20.0`. The project uses the Input System backend (`activeInputHandler: 1`).
- **Windows build works.** Build with `Wyrdbound → Build Windows` or the batch command in the README. Output: `Builds/Windows/` (gitignored).
- **Automated play-test** (`-wyrdtest`; see the README).
  - Run 1 went through title, intro, hall, every menu, and then Niflhel → Váli → Náströnd (mini-boss, story room, shop) → Níðhöggr → Gjöll… with **0 exceptions**.
  - Screenshots confirmed the art, UI, HUD, dialogue, shop and blessings render correctly.
- Backed up at https://github.com/EladOzCohen/Wyrdbound (public).

## Fixes and tuning after play-test 1 (in source; not yet re-verified by a second test run)
- Empty dialogue portraits showed as white squares. They are now hidden until a sprite is set.
- Toasts drew over menus. They now render on the HUD layer, beneath screens.
- Höðr's blindfold was misplaced.
- The NPC talk marker was washed out by the additive material.
- **Balance:** foes and bosses died in 1–2 hits (Váli in ~16 s, Níðhöggr in ~11 s, against a dumb bot).
  - Added `EnemyDefs.HpScale = 1.6` and `BossHpScale = 2.2`.
  - Raised wave budgets in `Encounter`.
  - **Next session: play-test by hand and keep tuning.**
- Bot screenshots were captured one frame late. Fixed with `Snap()`.

## Graphics upgrade (2026-10-16, second session)
- **Skeletal cut-out animation.** Characters are built from separate jointed parts and animated procedurally.
  - `Art/CharSpecs.cs` is the one table of every character's look.
  - `Art/RigArt.cs` paints each part at 2× resolution, with a rim light and core shadow.
  - `Fx/Rig.cs` builds the bone hierarchy and animates it. Rig kinds: Humanoid, Quad, Bird, Serpent, Ghost, Blob, Bell, and Static (whole sprite).
  - Animations cover: gait cycles, breathing, attack windup/strike/recovery, slams, casting, shooting, lunges and dashes, hurt recoil, death collapse, emerging from the ground, and spring physics for capes, hair, tails and tatters.
  - `Entity.FillRig()` maps game state to poses: Player handles the weapon arm, dash and throw; Enemy handles its AI states.
- **2D lighting.** `Fx/Lighting.cs` uses light sprites on layer 30. A light camera renders them into a 1/3-resolution HDR buffer, and `Wyrd/Sprite` multiplies that buffer in, with each realm's `Biome.Ambient` as the base. `Lights.Attach/At/ToLayer`, plus `Vfx.Light`, now emit real light.
- **Post-processing.** `Fx/PostFx.cs` + `Resources/Shaders/WyrdPost.shader` add bloom (threshold 1.25), a colour grade, vignette and an ACES tonemap. Both lighting and bloom can be toggled in Settings.
- Weapon trails (`Fx/WeaponTrail.cs`), a parallax void backdrop (`World/Backdrop.cs`), a closer camera (ortho size 7.2), and 4× portraits.
- Play-test 3 ran a full escape with **0 errors**. Bot options: `-wyrdrooms N` for a quick run; close-up screenshots are captured automatically.

## Architecture (quick reference)
- **Bootstrap.** `Boot` creates `Game` in any scene. `Game` owns:
  - Input, Audio, Camera, UI, `RunDirector`, `Hub` and the persistent `Player`
  - Time: hitstop, slow-mo, `TestSpeed`
  - Timers: `After`, `AfterReal`
  - Fades: `Transition`
- **Chambers.** `RunDirector.BuildRoom` → `RoomGen` → `LevelBuilder` (floor via `RoomArt`, props via `PropArt`, `Ambient`) → `Door`, `Pickup` and `Encounter`.
- **Combat.** `Combat.Deal` → `BoonSystem.ModifyOutgoing` → `Entity.TakeHit` → `BoonSystem.AfterHit`. Statuses live in `Status` and tick in `Entity.TickStatus`.
- **Story.** Context strings: `hel, ganglot, return, baldr, nanna, mimir, modgud, norns, vali, hodr, hermod, surface, ending, god_<god>, boss_<id>, bossdown_<id>, gift_<npc>`. `Story.Pick` returns the highest-priority unseen convo, or else a random repeatable one.
- **Saves.** JSON in `Application.persistentDataPath` (`wyrdbound_save.json` and `wyrdbound_settings.json`). The bot uses the suffix `_autotest`.
- **Art swap points:** `CharArt.Get`, `PropArt.Get`, `SpriteLib`. **Font swap point:** `Resources/Fonts/Main`.

## Next steps (in order)
1. Rebuild and re-run the play-test. Read the screenshots and confirm the fixes above.
2. Hand play-test with the user. Tune HP, damage, wave budgets, boss patterns and the boon values in `BoonDefs`.
3. The bot never uses the god **Call** and rarely buys in the shop. Extend it to cover those, and the victory/surface path.
4. Floor painting costs about 0.6 M pixels per room on the main thread. Measure it; if transitions hitch, move it to a worker thread.
5. Continue with `docs/ROADMAP.md` v0.2: aspects, hazards, tutorial room, rebinding.

## Useful commands

Compile check:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "C:/Users/elado/Desktop/Wyrdbound" -logFile compile.log
```

Then search `compile.log` for `error CS`.

Build:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Unity.exe" -batchmode -nographics -projectPath "C:/Users/elado/Desktop/Wyrdbound" -executeMethod Wyrdbound.EditorTools.WyrdboundSetup.BuildWindows -logFile build.log
```

Play-test (opens a window; it quits by itself):

```bash
Builds/Windows/Wyrdbound.exe -wyrdtest -wyrdout <dir> -screen-fullscreen 0 -screen-width 1600 -screen-height 900
```

## 3D conversion (2026-10-17, branch `3d`)
The user wanted real 3D instead of "almost pure 2D", at zero cost. Approach: **keep every line of gameplay on the 2D
floor plane (XY) and replace only the view.** Height above the floor is **-Z**.
- **Free art:** KayKit (CC0) Adventurers + Skeletons + Dungeon Remastered, downloaded from the official
  `KayKit-Game-Assets` GitHub into `Assets/ThirdParty/Resources/KayKit/` (CC0 → fine in the public repo).
  `Editor/KayKitImport.cs` imports characters as **Legacy** animation (each FBX carries ~75-90 clips) and has a
  `Wyrdbound → KayKit Report` menu that lists clips/bones/sizes into `KayKitReport.txt`.
- **`Fx/View3D.cs`**: pitch 52°, FOV 30°; `At`, `Flatten` (3D point → equivalent floor point), `Face(dir)` (stand a
  Y-up model on the floor), `TowardCam`, `Billboard` (upright sprites face the camera), `LightSync` (every 2D light
  sprite also drives a real point light).
- **`Fx/CameraRig.cs`**: perspective, deferred rendering; `HalfH/HalfW/Center` replace orthographicSize;
  `ScreenToWorld` ray-casts the floor plane.
- **`Fx/Model3D.cs`**: `ModelSpecs` = cast list (which art id → which KayKit body, tint, rim colour, visible items,
  extra weapon models). `Model3D` plays clips from `RigInput`: idle/walk/run/shamble, attacks **scrubbed** in sync
  with AI windup/strike, slams, stabs, casts, aim/shoot, dodge, hit, death (skeletons fall apart), skeleton
  spawn-from-ground; baked-mesh additive afterimages (`Wyrd/Ghost`). Player swings use `Player.OnSwingBegin` →
  `SwingTimed` so the clip's contact frame lands when the gameplay windup ends.
- **Not modelled yet (still 2D rigs as billboards):** helhound, garmr, bone_rat, raven, serpent, larva, Níðhöggr,
  wraith, wight, bloat, bell, mimir, norns. Candidates: Quaternius CC0 packs (Yeti for Hrímþurs/frost giants, animals).
- **`World/Room3D.cs`**: floor slab mesh (painted floor texture) + 3-row cliff skirts with a procedural tiling rock
  texture; `Prop()` maps prop kinds to Dungeon models (pillar, stone, chest, table) — others remain billboards.
- **`Fx/Atmosphere.cs`**: realm ambient + a shadow-casting "moon" directional light; shadow quality settings.
- **Shaders:** `Wyrd/Lit` (surface shader: tint-recolour, rim light, emission, hit flash); sprite shaders gained a
  `_ZTest` property — `Mats.Sprite/Additive` are depth-tested (decals, billboards), `Mats.SpriteTop/AdditiveTop`
  draw over everything (particles, projectiles, trails, bars) like the 2D game did.
- Verified: compiles clean, Windows build OK, bot runs with 0 errors / 0 exceptions (4-room run + death + hall).
- **Next (3D):** models for the creatures above; KayKit Dungeon walls/banners/torches dressing chamber edges;
  3D doors (`wall_doorway`); camera occlusion fade for tall pillars; tune tints/heights per character; higher-res
  floor painting (RoomArt.PPU 32 → 48) since the camera is closer now; FXAA (deferred has no MSAA).

### 3D polish pass 2 (2026-10-17)
- **Back walls** (`Room3D.BackWalls`): KayKit wall / cracked / broken segments along each chamber's far (north) rim,
  never between camera and fight; mounted torches (real flame light via `Ambient.AddFlame`) and realm banners.
- **3D doors** (`Room3D.Doorway` + `Door.cs`): `wall_doorway` arches; the portal glow stands in the opening.
- **Creatures built in code** (`Fx/ProcModels.cs`, `ModelSpec.Proc`): `serpent`/`larva` (head + tapering body
  following its own trail, jaw, rearing strike), `bell` (lathed bronze bell that swings to toll), `dragon`
  (Níðhöggr's horned head with jaw). Níðhöggr's `BossSegment`s are 3D spined coils.
- **Occlusion fade:** `Occluder` + `_Dither` (Bayer screen-door in `Wyrd/Lit`) fades tall props hiding Narfi.
  `Wyrd/Lit` is `#pragma target 3.5` (needs >10 interpolators).
- **Still 2D billboards:** helhound, garmr, bone_rat, raven, wraith, wight, bloat, mimir, norns. Free CC0 candidates
  found (Quaternius, Google Drive; awaiting the user's OK to download): Wolf.fbx (Ultimate Animated Animals) for
  helhound/Garmr, Ghost_Skull.fbx for wraith/wight, Pigeon.fbx (tinted black) for raven, GreenSpikyBlob.fbx for
  bloat. Direct download: `https://drive.google.com/uc?export=download&id=<id>`; ids: Wolf `128WmNfZthQqZ1OZpinWCAJ8IgvjNcoSm`, Ghost_Skull `1pQDfJwnc4r3PiVL73h4waXkqFuFngl6Y`, Pigeon `1HV4WM2FRJXlq3Wz6awceZExVnRD_lPAp`, GreenSpikyBlob `1i8XpBxwTqM_4JRN0D986NfLT4_QPFAC6`, Dragon_Evolved `1_ColYvoIS7vj1N4qWrLuTmZmVK_lsu7j`.
