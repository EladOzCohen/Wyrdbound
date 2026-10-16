# WYRDBOUND — Roadmap

## Saga I — The Bound Son (this project)

### Done (v0.1)
- [x] Core combat: dash with i-frames, dash-strike, hit-stop, knockback and wall-slams, telegraphed enemy attacks
- [x] 4 weapons (Axe, Spear, Bow, Twin Seaxes), each with a combo and a special
- [x] Rune-cast with lodging stones; Fury meter and god Calls
- [x] 58 blessings, including 8 Duos; rarities, Iðunn's apples, Fate-Thread rerolls
- [x] 4 realms with procedural chambers; 20 foes, armored elites, 3 mini-bosses, 4 bosses
- [x] Door choices with reward previews; shop, springs, story encounters, passages
- [x] Hel's hall hub: residents, gifting, keepsakes, armory, Rune-Stone, Weave of Wyrd, Codex
- [x] ~180 conversations; branching two-path ending
- [x] All art, SFX and music generated in code
- [x] Keyboard, mouse and gamepad; settings and accessibility (Odin's Grace)
- [x] Automated end-to-end play-test (`-wyrdtest`)

### Next (v0.2): feel and balance
- [ ] Play-test with real players; tune enemy HP and damage curves, boon values and wave budgets
- [ ] Weapon aspects (Hades-style variants) unlocked with a new currency
- [ ] More chamber layouts and set-piece rooms per realm (bridges, collapsing floors, traps)
- [ ] Hazards: rune traps, blade-river tiles, venom pools that spread
- [ ] A short tutorial room for the first escape
- [ ] Controller rumble; key rebinding

### v0.3: depth
- [ ] Blessing "Legendaries" and more Duos (every god pair)
- [ ] Prophecies / quest log (Fated List analog) for long-term goals
- [ ] Hall decorations bought with a new currency, which change the hub
- [ ] More dialogue: second and third tiers for each resident, keyed to keepsakes and Weave level

### v1.0: ship
- [ ] Commissioned art pass (the `CharArt`, `PropArt` and `SpriteLib` entry points make this a drop-in)
- [ ] Commissioned or licensed Norse fonts in `Resources/Fonts`
- [ ] Composer pass on the music, or keep the generative score and extend it
- [ ] Localization (Hebrew first)
- [ ] Steam build pipeline, achievements and cloud saves
- [ ] Performance pass: precompute floor textures off the main thread

## Saga II
- Starts from the save's `ending` flag ("cut" or "hold").
- *Twilight*: Naglfar, the field of Vígríðr, Fenrir unbound, Thor against the serpent.
- *The Other Hands*: Váli and Hel hunt a way to unpick the knot; Odin as an open antagonist.

## Saga III
- *The Green World*: after the end, Baldr's new age, and what the survivors owe the dead.
