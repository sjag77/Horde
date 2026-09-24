# HORDE — Patch 0.0.1

First numbered patch. Android (arm64), versionName 0.0.1, versionCode 2.

## New: Heroes
Pick a hero before every run. Each hero has an **innate** — an always-on passive, weaker than an ability.

| Hero | Title | Starts with | Innate |
|---|---|---|---|
| **EMBER** | The Fire Walker | Frost Nova | **Burning Aura** — enemies close to you catch fire (3 dmg/s). Ring of licking flames around the ship. |
| **VENOM** | The Plague Caster | Spark Bolt | **Toxic Touch** — Spark Bolt, Chain Lightning and Orbit Blades poison (4 dmg/s). Spark bounces from level 2 and deals +12%. |
| **NAPALM** | The Oil Burner | Meteor | **Oil Slick** — enemies touching your oil ring get coated; Bombs and Meteors ignite oiled enemies (4 dmg/s). |

Burning and poisoned enemies flicker orange / pulse green and throw off flames or toxic bubbles.

## Controls
- Joystick base now **stays where your thumb lands** (knob clamps to the ring).
- New **DASH button** (fires on touch-down). Double-tap dash still available.
- Dash: longer invulnerability (0.4 s), afterimage trail, shockwave.

## Settings (new)
- Sound volume
- Joystick size (small / medium / large)
- Dash button side (left / right)
- Double-tap dash on / off
- Difficulty: HARD. (There is no easy mode. Try it.)

## Bosses
- A boss charge now **locks its destination when the wind-up starts** and a red ring marks where it will land — dash out of it, even point-blank.

## Balance
- Frost Nova: freeze/slow 1.6 s → 2.2 s, cooldown slightly shorter (3.6 − 0.2·lvl, min 1.4 → 3.3 − 0.2·lvl, min 1.2).

## UI
- How to Play: two pages (rules + heroes, then abilities), updated for heroes, dash button and settings.
- Menu: PLAY → hero select, HOW TO PLAY, SETTINGS, version label.
- Pause menu: SETTINGS button.
- Launch warning: signature line removed.

## Files
- `HORDE-v0.0.1.apk` — installable build
- `HORDE-v0.0.1.apk.sha256` — checksum
- `HORDE-v0.0.1-source.tar.gz` — game source (Assets/_Horde, ProjectSettings, package manifest)
