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
