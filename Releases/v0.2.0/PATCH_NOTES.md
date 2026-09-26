# HORDE v0.2.0 — "Right Way Up"

Straight from your v0.1.0 feedback.

## The camera angle is fixed

v0.1.0 had the perspective inverted: the horizon sat at the **bottom** of the screen, which is
why it only looked right with the phone upside down. The camera now sits south of you looking
north and down — distance at the top, models standing up toward you. It reads correctly held
normally.

## No more solid colours — real materials

Every creature used to be one flat tinted colour. Now each model carries its **own painted
materials**, baked per part, and the tint is only used for status effects (frost, poison, oil,
fire) and the white flash when something is hit.

- **Grunts** — leather-wrapped goblins with a hood, tusks, bone claws, amber eyes and a **rusty
  iron cleaver** in hand
- **Runners** — war-painted stalkers with a head wrap and bone spurs down the spine
- **Hounds** — shaggy dark fur, scarred muzzle, **spiked iron collar** with brass studs, ivory fangs
- **Spitters** — sickly green sacs of glowing glands with a chitin mortar
- **Splitters / Mites** — translucent ooze with a darker nucleus showing through
- **Ogres** — grey-purple skin under riveted **iron plate**, a brass belt buckle, glowing visor
  slit and a two-handed **maul**
- **The Demon boss** — dark red hide, blackened armour, a brass chest ring with a burning core
  stone, bone horns and hooves

The hero's gunship keeps its own panelling, dark glass canopy and metal nose; your hero colour
now washes over it instead of replacing it.

## A real arena

- The flat tiled floor is gone. The ground is **mortared flagstone** — every slab a slightly
  different worn, mossy or sunken stone — and it **receives the horde's shadows**.
- Scattered across it: rubble, bones half-buried in the dirt, broken pillars, **skulls on
  stakes** and burning braziers.
- The boundary is now a **stone rampart** with courses, battlements, a lit coping and torches
  burning along the walls.

## Lighting

Proper gamma handling (colours are authored the way they should look and converted once),
softer ambient, a tighter specular highlight so leather, bone and iron don't all read as the
same matte plastic.

Gameplay is unchanged from v0.1.0 — same heroes, same 4 bosses then endless, same secret.

— SJ
