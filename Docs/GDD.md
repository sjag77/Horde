# HORDE — Game Design Document (v1, Android first)

> **HORDE** is an internal codename. The store name is chosen before publishing and must be
> checked for conflicts. Every name, character and asset in this game is original —
> nothing from Dota 2 / Nest of Thorns (Valve IP) is used.

## 1. Pitch
A one-thumb 2D survivors-style roguelite for phones. Pick a hero, move with a floating
joystick, and let your abilities fire on their own while hundreds of enemies close in.
Level up, choose 1 of 3 upgrades, beat three mini-bosses and the final boss at 10:00.
Spend gold between runs to get permanently stronger.

## 2. Platform & tech
| | |
|---|---|
| Engine | Unity 6.3 LTS, Universal 2D (URP) template, C# |
| First target | Android (Google Play). iOS later |
| Orientation | **Portrait**, one-handed |
| Frame rate | 60 fps target, 30 fps floor on low-end phones |
| Input | Floating joystick (appears where the thumb lands). No other buttons during play |
| Session | ~10 minutes per run, pauses automatically when the app is backgrounded |

## 3. Core loop
1. Choose a hero → run starts.
2. Enemies spawn in rings just off-screen and walk in.
3. Kills drop **XP gems** → fill XP bar → **Level up**: pick 1 of 3 (new ability, ability upgrade, or passive).
4. **Mini-bosses** at 3:00, 6:00, 9:00. Each drops a chest that **Evolves** one maxed ability.
5. **Final boss** at 10:00. Win or die → gold earned → meta shop → next run.

## 4. Heroes (v1: 3)
| Hero | Role | Starting ability | Innate trait |
|---|---|---|---|
| **Vesper, the Starcaller** | Ranged mage | Spark Bolt | −20% max HP, **+1 projectile** on every ability |
| **Brann, the Warden** | Balanced | Frost Ring | Takes 15% less damage; frozen enemies take +25% damage |
| **Kai, the Duelist** | Fast melee | Crescent Slash | +20% move speed; every 4 s marks the toughest nearby enemy — next hit crits |

Loadout limits: **5 ability slots, 4 passive slots**. Abilities level 1–5, then Evolve from a boss chest.

## 5. Abilities (v1: 8) — all automatic
| Ability | Behaviour | Scales with |
|---|---|---|
| Spark Bolt | Projectile at nearest enemy, bounces 1× (+1 per 2 levels) | damage, projectiles |
| Crescent Slash | Wide melee arc toward the nearest enemy | damage, area |
| Frost Ring | Expanding ring, damages + slows 40% | area, duration |
| Orbit Blades | Blades circle the hero, hit on contact | count, speed |
| Chain Lightning | Hits one target, jumps to 3 more | jumps, damage |
| Toxic Pool | Drops a damage-over-time puddle at a crowd | area, duration |
| Boomerang | Thrown out and back, pierces everything | count, speed |
| Meteor | Marks a crowd, lands after 0.6 s for big AoE | damage, area |

## 6. Passives (v1: 6)
Max HP · Move Speed · Cooldown Reduction · Area · Damage · Pickup Radius

## 7. Enemies
| Enemy | Behaviour |
|---|---|
| Swarmer | Weak, fast, comes in packs |
| Brute | Slow, tanky, heavy contact damage |
| Spitter | Keeps distance, fires **clearly visible, telegraphed** projectiles |
| Charger | Winds up, then dashes in a straight line |
| **Mini-boss: Stone Colossus** | Splits on death 1 → 3 → 9 |
| **Mini-boss: Twice-Risen Wraith** | Revives twice, faster each time |
| **Mini-boss: Sentinel Orb** | Relentlessly follows the hero, huge damage on contact |
| **Final boss: The Hive Matriarch** | Arena fight at 10:00: summons swarms, radial volleys, dash attack |

## 8. Pickups
| Pickup | Rarity | Effect |
|---|---|---|
| XP gem | Every kill | Experience |
| **Cheese** | Rare | Heals 25% max HP; stays on the ground if HP is full |
| **Magnet** | Rare | Pulls every XP gem on the map to you within ~1 s |
| Gold coin | Common | Meta-currency |
| Boss chest | Mini-boss kill | Evolve one maxed ability |

Walk onto rare pickups to use them. They never auto-collect.

## 9. Meta progression (between runs)
Gold buys permanent ranks in: Max HP, HP Regen, Damage, Move Speed, Cooldown, Area,
Armor, XP Gain, Revive (extra life). Unlock harder **difficulty tiers** by beating the boss.

## 10. Monetization (after the game is fun)
- Rewarded ad: revive once per run
- Rewarded ad: double gold at the end of a run
- In-app purchase: remove interstitial ads
No pay-to-win purchases in v1.

## 11. Performance rules (non-negotiable for phones)
- **Object pooling** for enemies, projectiles, gems, damage numbers, effects. No `Instantiate`/`Destroy` during play.
- **No `Update()` per enemy.** One `EnemyManager` updates all enemies from arrays.
- **Spatial hash grid** for hits and separation — no per-enemy Physics2D colliders.
- One **sprite atlas**, SRP Batcher on, minimal overdraw, no per-frame allocations (GC spikes = stutter).
- Enemy cap ~400 on screen; LOD for effects when frame time is high.

## 12. Milestones
| # | Milestone | Done when |
|---|---|---|
| M0 | Setup | Unity 6.3 LTS + Android module installed, empty project runs on phone |
| M1 | Prototype | 1 hero, joystick, 2 enemies, 2 abilities, XP + level-up choice, 60 fps on phone |
| M2 | Core game | 3 heroes, 8 abilities, 6 passives, 4 enemies, 10-minute wave timeline |
| M3 | Bosses & meta | 3 mini-bosses, final boss, gold + meta shop, save data |
| M4 | Polish & ads | Real art, audio, rewarded ads, settings, Play Console internal test |
| M5 | Publish | Store listing, privacy policy, closed test → production on Google Play |

## 13. Art & audio
Placeholder shapes until M3. Then licensed 2D asset packs (commercial-use license) or commissioned art.
