# HORDE

A one-thumb survivors-style arcade game for Android, built in Unity 6.3 LTS (C#).

Move with the joystick; every weapon fires on its own. Level up, beat 4 bosses,
then survive an endless swarm for as long as you can.

## Play

Install `HORDE-vX.Y.Z.apk` on an Android phone (Android 7.1 / API 25 or newer, 64-bit ARM).
Android will warn that the app isn't from the Play Store — choose "Install anyway".

## Heroes

| Hero | Starting ability | Innate |
|---|---|---|
| EMBER | Frost Nova | A ring of flame burns nearby enemies |
| VENOM | Spark Bolt | Spark, Chain Lightning and Orbit Blades poison; Spark bounces from Lv2 |
| NAPALM | Meteor | Oil coats enemies; Bombs and Meteors set them alight |

## Abilities

Spark Bolt · Orbit Blades · Frost Nova · Chain Lightning · Meteor · Bomb Mines —
up to 4 at once, raised to 6 by boss rewards. Each goes to level 8.

## Build it yourself

1. Install Unity **6.3 LTS (6000.3.24f1)** with **Android Build Support** (OpenJDK + SDK & NDK).
2. Open `Game/` in Unity.
3. Menu: **HORDE → 1. Set Up Project**, then **HORDE → 2. Build Android APK**.

Or from the command line:

```bash
"/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath Game -buildTarget Android \
  -executeMethod HordeSetup.BuildAndroid -logFile build.log
```

The APK lands in `Game/Builds/Android/HORDE.apk`.

## Layout

```
Game/Assets/_Horde/Scripts/   all gameplay code
Game/Assets/_Horde/Editor/    one-click setup + build
Docs/GDD.md                   design document
Releases/                     per-version patch notes and checksums
```

All art, sound and effects are generated in code at startup — there are no imported assets.
