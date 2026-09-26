using UnityEngine;

namespace Horde
{
    /// <summary>
    /// Every 3D object in the game, modelled once at startup. Models are built Y-up facing +Z;
    /// the renderer tilts them onto the arena's XY plane, so all the gameplay code keeps working
    /// in 2D while the game draws in 3D.
    /// Vertex colours are greyscale-ish on purpose: the per-creature tint multiplies them, so one
    /// mesh serves every colour variant while plating, eyes and claws keep their contrast.
    /// </summary>
    public static class Models
    {
        // Creatures
        public static Mesh Grunt, Runner, Hound, Spitter, Splitter, Mite, Ogre;
        // Boss parts (a real rig: the body turns, the jaw opens, the horns glow)
        public static Mesh DemonBody, DemonHead, DemonJaw, DemonArm, DemonLeg;
        // Hero parts
        public static Mesh ShipHull, ShipWing, ShipEngine, ShipPod;
        // Props and spells
        public static Mesh BladeMesh, BoltMesh, RockMesh, MineMesh, GemMesh, FunnelMesh, ShardMesh, PodStrut;
        public static Mesh Sphere1, Cube1;   // unit primitives for glows and bullets

        // A fantasy palette baked straight into the models. Each creature carries its own
        // skin, leather, iron, bone and eye colours, so nothing is a flat single-colour blob
        // any more - the per-enemy tint now only shades them (poison, frost, burning).
        static readonly Color Bone = new Color(0.90f, 0.86f, 0.74f);
        static readonly Color BoneDark = new Color(0.62f, 0.57f, 0.46f);
        static readonly Color Iron = new Color(0.42f, 0.45f, 0.52f);
        static readonly Color IronDark = new Color(0.24f, 0.26f, 0.31f);
        static readonly Color Rust = new Color(0.46f, 0.26f, 0.16f);
        static readonly Color Brass = new Color(0.72f, 0.55f, 0.24f);
        static readonly Color Leather = new Color(0.34f, 0.22f, 0.14f);
        static readonly Color LeatherDark = new Color(0.20f, 0.13f, 0.09f);
        static readonly Color Cloth = new Color(0.45f, 0.17f, 0.15f);
        static readonly Color Claw = new Color(0.16f, 0.14f, 0.15f);
        static readonly Color EyeAmber = new Color(1f, 0.72f, 0.22f);
        static readonly Color EyeGreen = new Color(0.62f, 1f, 0.45f);
        static readonly Color EyeRed = new Color(1f, 0.34f, 0.22f);

        // Creature skins
        static readonly Color GruntSkin = new Color(0.52f, 0.43f, 0.36f);
        static readonly Color GruntSkinDark = new Color(0.34f, 0.28f, 0.24f);
        static readonly Color RunnerSkin = new Color(0.66f, 0.50f, 0.38f);
        static readonly Color RunnerSkinDark = new Color(0.44f, 0.31f, 0.24f);
        static readonly Color HoundFur = new Color(0.33f, 0.24f, 0.20f);
        static readonly Color HoundFurDark = new Color(0.19f, 0.14f, 0.12f);
        static readonly Color SpitterFlesh = new Color(0.55f, 0.62f, 0.34f);
        static readonly Color SpitterGland = new Color(0.78f, 0.88f, 0.30f);
        static readonly Color SlimeBody = new Color(0.38f, 0.66f, 0.34f);
        static readonly Color SlimeLight = new Color(0.58f, 0.86f, 0.44f);
        static readonly Color OgreSkin = new Color(0.44f, 0.40f, 0.48f);
        static readonly Color OgreSkinDark = new Color(0.29f, 0.26f, 0.33f);
        static readonly Color DemonSkin = new Color(0.52f, 0.17f, 0.15f);
        static readonly Color DemonSkinDark = new Color(0.31f, 0.10f, 0.10f);

        // Hero
        static readonly Color HullLight = new Color(0.82f, 0.86f, 0.92f);
        static readonly Color HullDark = new Color(0.38f, 0.42f, 0.50f);
        static readonly Color Glass = new Color(0.16f, 0.36f, 0.50f);
        static readonly Color Metal = new Color(0.62f, 0.66f, 0.74f);

        public static void Init()
        {
            if (Grunt != null) return;
            Grunt = BuildGrunt();
            Runner = BuildRunner();
            Hound = BuildHound();
            Spitter = BuildSpitter();
            Splitter = BuildSplitter();
            Mite = BuildMite();
            Ogre = BuildOgre();

            DemonBody = BuildDemonBody();
            DemonHead = BuildDemonHead();
            DemonJaw = BuildDemonJaw();
            DemonArm = BuildDemonArm();
            DemonLeg = BuildDemonLeg();

            ShipHull = BuildShipHull();
            ShipWing = BuildShipWing();
            ShipEngine = BuildShipEngine();
            ShipPod = BuildShipPod();

            BladeMesh = BuildBlade();
            BoltMesh = BuildBolt();
            RockMesh = BuildRock();
            MineMesh = BuildMine();
            GemMesh = BuildGem();
            FunnelMesh = BuildFunnel();
            ShardMesh = BuildShard();
            PodStrut = new MeshBuilder().Box(Vector3.zero, new Vector3(0.06f, 0.06f, 1f), Iron).Build("Strut");
            Sphere1 = new MeshBuilder().Sphere(Vector3.zero, 0.5f, Color.white, 12, 9).Build("Sphere");
            Cube1 = new MeshBuilder().Box(Vector3.zero, Vector3.one, Color.white).Build("Cube");
        }

        // Eyes with a dark pupil and a bright iris, so every creature has a gaze.
        static void Eyes(MeshBuilder b, float y, float z, float spread, float r, Color iris)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                b.Sphere(new Vector3(s * spread, y, z), r, iris, 7, 5);
                b.Sphere(new Vector3(s * spread, y, z + r * 0.6f), r * 0.42f, Claw, 6, 4);
            }
        }

        // ---- rank and file -------------------------------------------------------------

        // Hunched goblin grunt: hide-wrapped torso, a rusty cleaver, bone claws, amber eyes.
        static Mesh BuildGrunt()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.46f, 0f), new Vector3(0.26f, 0.30f, 0.24f), GruntSkin);
            b.Sphere(new Vector3(0f, 0.40f, 0.16f), new Vector3(0.21f, 0.18f, 0.12f), Leather);        // belly wrap
            b.Box(new Vector3(0f, 0.52f, 0.02f), new Vector3(0.44f, 0.09f, 0.42f), LeatherDark, 1f,
                  Quaternion.Euler(0f, 0f, 26f));                                                      // strap across the chest
            b.Sphere(new Vector3(0f, 0.70f, 0.10f), new Vector3(0.19f, 0.18f, 0.20f), GruntSkin);      // head
            b.Sphere(new Vector3(0f, 0.74f, -0.04f), new Vector3(0.17f, 0.13f, 0.14f), LeatherDark);   // hood
            b.Tube(new Vector3(0.14f, 0.74f, 0.04f), new Vector3(0.30f, 0.86f, -0.04f), 0.035f, 0.006f, GruntSkinDark, 5);
            b.Tube(new Vector3(-0.14f, 0.74f, 0.04f), new Vector3(-0.30f, 0.86f, -0.04f), 0.035f, 0.006f, GruntSkinDark, 5);
            b.Sphere(new Vector3(0.20f, 0.62f, 0f), 0.13f, GruntSkinDark);                             // shoulders
            b.Sphere(new Vector3(-0.20f, 0.62f, 0f), 0.13f, GruntSkinDark);
            b.Limb(new Vector3(0.22f, 0.60f, 0.02f), new Vector3(0.26f, 0.30f, 0.26f), 0.08f, 0.06f, GruntSkin);
            b.Limb(new Vector3(-0.22f, 0.60f, 0.02f), new Vector3(-0.26f, 0.30f, 0.26f), 0.08f, 0.06f, GruntSkin);
            for (int k = 0; k < 3; k++)                                                                // claws
            {
                float o = (k - 1) * 0.05f;
                b.Tube(new Vector3(-0.26f + o, 0.28f, 0.30f), new Vector3(-0.28f + o * 1.4f, 0.22f, 0.42f), 0.022f, 0.004f, Claw, 4);
            }
            b.Limb(new Vector3(0.24f, 0.30f, 0.26f), new Vector3(0.30f, 0.26f, 0.34f), 0.055f, 0.05f, GruntSkin);
            b.Blade(new Vector3(0.30f, 0.26f, 0.34f), new Vector3(0.34f, 0.24f, 0.78f), 0.17f, 0.04f, Iron, Rust);   // cleaver
            b.Tube(new Vector3(0.28f, 0.27f, 0.26f), new Vector3(0.31f, 0.25f, 0.40f), 0.035f, 0.03f, Leather, 5);
            b.Limb(new Vector3(0.12f, 0.24f, 0f), new Vector3(0.13f, 0.03f, 0.04f), 0.09f, 0.07f, GruntSkinDark);
            b.Limb(new Vector3(-0.12f, 0.24f, 0f), new Vector3(-0.13f, 0.03f, 0.04f), 0.09f, 0.07f, GruntSkinDark);
            Eyes(b, 0.74f, 0.24f, 0.075f, 0.042f, EyeAmber);
            b.Tube(new Vector3(0f, 0.68f, 0.26f), new Vector3(0f, 0.66f, 0.30f), 0.03f, 0.02f, GruntSkinDark, 5);   // snout
            for (int k = -1; k <= 1; k += 2)
                b.Tube(new Vector3(k * 0.05f, 0.63f, 0.26f), new Vector3(k * 0.05f, 0.69f, 0.27f), 0.015f, 0.003f, Bone, 4);  // tusks
            return b.Build("Grunt");
        }

        // Lean stalker: war-painted, hollow-cheeked, sprinting with bone spurs down its back.
        static Mesh BuildRunner()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.52f, 0.02f), new Vector3(0.17f, 0.24f, 0.19f), RunnerSkin);
            b.Box(new Vector3(0f, 0.50f, 0.14f), new Vector3(0.26f, 0.22f, 0.06f), Cloth);            // war paint / rag
            b.Sphere(new Vector3(0f, 0.76f, 0.14f), new Vector3(0.14f, 0.14f, 0.17f), RunnerSkin);
            b.Sphere(new Vector3(0f, 0.80f, 0.02f), new Vector3(0.13f, 0.11f, 0.12f), Cloth);          // head wrap
            b.Limb(new Vector3(0.16f, 0.64f, 0.04f), new Vector3(0.20f, 0.52f, 0.34f), 0.055f, 0.032f, RunnerSkin);
            b.Limb(new Vector3(-0.16f, 0.64f, 0.04f), new Vector3(-0.22f, 0.44f, -0.22f), 0.055f, 0.032f, RunnerSkin);
            b.Limb(new Vector3(0.10f, 0.32f, 0f), new Vector3(0.12f, 0.06f, 0.22f), 0.07f, 0.045f, RunnerSkinDark);
            b.Limb(new Vector3(-0.10f, 0.32f, 0f), new Vector3(-0.13f, 0.06f, -0.20f), 0.07f, 0.045f, RunnerSkinDark);
            b.Tube(new Vector3(0.20f, 0.52f, 0.34f), new Vector3(0.26f, 0.50f, 0.50f), 0.03f, 0.004f, Claw, 4);
            Eyes(b, 0.79f, 0.26f, 0.06f, 0.034f, EyeRed);
            for (int i = 0; i < 4; i++)
                b.Tube(new Vector3(0f, 0.72f - i * 0.10f, -0.10f - i * 0.02f),
                       new Vector3(0f, 0.82f - i * 0.10f, -0.22f - i * 0.02f), 0.028f, 0.004f, Bone, 5);
            return b.Build("Runner");
        }

        // Shaggy warhound: dark fur, a scarred muzzle, iron collar, ivory fangs.
        static Mesh BuildHound()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.34f, 0f), new Vector3(0.20f, 0.19f, 0.34f), HoundFur);
            b.Sphere(new Vector3(0f, 0.44f, -0.08f), new Vector3(0.17f, 0.14f, 0.22f), HoundFurDark);   // shaggy back
            b.Sphere(new Vector3(0f, 0.42f, 0.32f), new Vector3(0.16f, 0.15f, 0.17f), HoundFur);
            b.Torus(new Vector3(0f, 0.40f, 0.20f), 0.17f, 0.035f, Iron, 14, 5);                         // collar
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI * 2f / 6f;
                b.Tube(new Vector3(Mathf.Cos(a) * 0.17f, 0.40f + Mathf.Sin(a) * 0.17f, 0.20f),
                       new Vector3(Mathf.Cos(a) * 0.26f, 0.40f + Mathf.Sin(a) * 0.26f, 0.20f), 0.03f, 0.004f, Brass, 4);
            }
            b.Tube(new Vector3(0f, 0.38f, 0.42f), new Vector3(0f, 0.34f, 0.60f), 0.10f, 0.07f, HoundFurDark, 8);
            b.Sphere(new Vector3(0f, 0.33f, 0.60f), 0.045f, Claw, 6, 4);
            b.Tube(new Vector3(0.09f, 0.52f, 0.28f), new Vector3(0.15f, 0.70f, 0.22f), 0.05f, 0.006f, HoundFurDark, 5);
            b.Tube(new Vector3(-0.09f, 0.52f, 0.28f), new Vector3(-0.15f, 0.70f, 0.22f), 0.05f, 0.006f, HoundFurDark, 5);
            b.Limb(new Vector3(0.15f, 0.30f, 0.20f), new Vector3(0.17f, 0.04f, 0.24f), 0.06f, 0.042f, HoundFurDark);
            b.Limb(new Vector3(-0.15f, 0.30f, 0.20f), new Vector3(-0.17f, 0.04f, 0.24f), 0.06f, 0.042f, HoundFurDark);
            b.Limb(new Vector3(0.15f, 0.30f, -0.18f), new Vector3(0.17f, 0.04f, -0.24f), 0.065f, 0.045f, HoundFurDark);
            b.Limb(new Vector3(-0.15f, 0.30f, -0.18f), new Vector3(-0.17f, 0.04f, -0.24f), 0.065f, 0.045f, HoundFurDark);
            b.Horn(new Vector3(0f, 0.42f, -0.30f), new Vector3(0f, 0.25f, -1f), new Vector3(0f, 0.9f, 0f), 0.45f, 0.055f, HoundFur);
            Eyes(b, 0.47f, 0.42f, 0.085f, 0.036f, EyeAmber);
            for (int i = 0; i < 5; i++)
            {
                float x = -0.06f + i * 0.03f;
                b.Tube(new Vector3(x, 0.32f, 0.58f), new Vector3(x, 0.27f, 0.60f), 0.016f, 0.003f, Bone, 4);
            }
            return b.Build("Hound");
        }

        // Bloated spore-thrower: a sickly sac of glands with a chitin mortar.
        static Mesh BuildSpitter()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.44f, -0.06f), new Vector3(0.34f, 0.34f, 0.32f), SpitterFlesh, 12, 9);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                b.Sphere(new Vector3(Mathf.Cos(a) * 0.26f, 0.44f + Mathf.Sin(a) * 0.22f, -0.10f), 0.09f, SpitterGland, 8, 6);
            }
            b.Sphere(new Vector3(0f, 0.66f, -0.14f), 0.13f, SpitterGland, 9, 6);
            b.Tube(new Vector3(0f, 0.46f, 0.18f), new Vector3(0f, 0.52f, 0.46f), 0.12f, 0.10f, Claw, 9);
            b.Torus(new Vector3(0f, 0.52f, 0.46f), 0.10f, 0.022f, IronDark, 12, 5);
            b.Limb(new Vector3(0.18f, 0.20f, -0.06f), new Vector3(0.24f, 0.03f, -0.12f), 0.05f, 0.038f, Claw);
            b.Limb(new Vector3(-0.18f, 0.20f, -0.06f), new Vector3(-0.24f, 0.03f, -0.12f), 0.05f, 0.038f, Claw);
            b.Limb(new Vector3(0f, 0.18f, 0.14f), new Vector3(0f, 0.03f, 0.22f), 0.05f, 0.038f, Claw);
            Eyes(b, 0.62f, 0.16f, 0.10f, 0.045f, EyeGreen);
            return b.Build("Spitter");
        }

        // Ooze colony: a translucent green mass with a darker nucleus and buds breaking off.
        static Mesh BuildSplitter()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.40f, 0f), new Vector3(0.40f, 0.38f, 0.38f), SlimeBody, 13, 10);
            b.Sphere(new Vector3(0f, 0.36f, -0.02f), new Vector3(0.22f, 0.20f, 0.20f), SlimeLight, 9, 7);   // nucleus
            b.Sphere(new Vector3(0.24f, 0.58f, -0.14f), 0.17f, SlimeLight, 9, 6);
            b.Sphere(new Vector3(-0.22f, 0.52f, -0.18f), 0.15f, SlimeLight, 9, 6);
            b.Sphere(new Vector3(0.02f, 0.68f, 0.16f), 0.13f, SlimeBody, 9, 6);
            b.Sphere(new Vector3(0f, 0.30f, 0.30f), 0.14f, SlimeBody, 9, 6);
            Eyes(b, 0.46f, 0.34f, 0.12f, 0.05f, EyeAmber);
            return b.Build("Splitter");
        }

        static Mesh BuildMite()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.20f, 0f), new Vector3(0.20f, 0.17f, 0.20f), SlimeLight, 9, 6);
            b.Sphere(new Vector3(0f, 0.26f, 0.14f), 0.075f, EyeAmber, 7, 5);
            b.Sphere(new Vector3(0f, 0.26f, 0.19f), 0.035f, Claw, 6, 4);
            for (int i = 0; i < 3; i++)
            {
                float a = -0.7f + i * 0.7f;
                b.Limb(new Vector3(Mathf.Sin(a) * 0.16f, 0.16f, Mathf.Cos(a) * 0.12f),
                       new Vector3(Mathf.Sin(a) * 0.28f, 0.02f, Mathf.Cos(a) * 0.20f), 0.035f, 0.018f, SlimeBody, 5);
                b.Limb(new Vector3(-Mathf.Sin(a) * 0.16f, 0.16f, Mathf.Cos(a) * 0.12f),
                       new Vector3(-Mathf.Sin(a) * 0.28f, 0.02f, Mathf.Cos(a) * 0.20f), 0.035f, 0.018f, SlimeBody, 5);
            }
            return b.Build("Mite");
        }

        // Plate-armoured ogre with a riveted helm and a two-handed maul.
        static Mesh BuildOgre()
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, 0.62f, 0f), new Vector3(0.52f, 0.56f, 0.40f), OgreSkin, 0.8f);
            b.Box(new Vector3(0f, 0.68f, 0.21f), new Vector3(0.46f, 0.44f, 0.05f), Iron, 0.85f);        // breastplate
            b.Box(new Vector3(0f, 0.34f, 0.02f), new Vector3(0.44f, 0.16f, 0.36f), Leather);            // belt
            b.Box(new Vector3(0f, 0.34f, 0.21f), new Vector3(0.13f, 0.13f, 0.05f), Brass);              // buckle
            for (int i = -1; i <= 1; i++)
                b.Sphere(new Vector3(i * 0.15f, 0.84f, 0.23f), 0.03f, Brass, 6, 4);                     // rivets
            b.Sphere(new Vector3(0.38f, 0.86f, 0f), 0.22f, Iron, 10, 7);
            b.Sphere(new Vector3(-0.38f, 0.86f, 0f), 0.22f, Iron, 10, 7);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 3; i++)
                    b.Tube(new Vector3(s * 0.38f, 0.94f, -0.14f + i * 0.14f),
                           new Vector3(s * 0.46f, 1.16f, -0.18f + i * 0.16f), 0.05f, 0.005f, Rust, 5);
            b.Sphere(new Vector3(0f, 0.98f, 0.08f), new Vector3(0.19f, 0.18f, 0.20f), IronDark);        // helm
            b.Box(new Vector3(0f, 0.98f, 0.24f), new Vector3(0.26f, 0.05f, 0.06f), EyeRed);             // visor glow
            b.Tube(new Vector3(0f, 1.14f, 0.04f), new Vector3(0f, 1.34f, -0.04f), 0.04f, 0.005f, Rust, 5);
            b.Limb(new Vector3(0.40f, 0.80f, 0.02f), new Vector3(0.48f, 0.40f, 0.20f), 0.12f, 0.10f, OgreSkin);
            b.Limb(new Vector3(-0.40f, 0.80f, 0.02f), new Vector3(-0.48f, 0.40f, 0.20f), 0.12f, 0.10f, OgreSkin);
            b.Sphere(new Vector3(0.50f, 0.34f, 0.24f), 0.16f, OgreSkinDark, 9, 6);
            b.Sphere(new Vector3(-0.50f, 0.34f, 0.24f), 0.16f, OgreSkinDark, 9, 6);
            b.Tube(new Vector3(0.52f, 0.30f, 0.20f), new Vector3(0.52f, 0.26f, 0.86f), 0.045f, 0.045f, Leather, 6);   // maul haft
            b.Box(new Vector3(0.52f, 0.26f, 0.90f), new Vector3(0.26f, 0.24f, 0.26f), Iron);                          // maul head
            b.Box(new Vector3(0.52f, 0.26f, 0.90f), new Vector3(0.29f, 0.10f, 0.10f), IronDark);
            b.Limb(new Vector3(0.18f, 0.30f, 0f), new Vector3(0.20f, 0.04f, 0.04f), 0.13f, 0.11f, OgreSkinDark);
            b.Limb(new Vector3(-0.18f, 0.30f, 0f), new Vector3(-0.20f, 0.04f, 0.04f), 0.13f, 0.11f, OgreSkinDark);
            return b.Build("Ogre");
        }

        // ---- boss rig ------------------------------------------------------------------

        static Mesh BuildDemonBody()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.95f, -0.02f), new Vector3(0.62f, 0.68f, 0.52f), DemonSkin, 14, 10);
            b.Box(new Vector3(0f, 0.95f, 0.42f), new Vector3(0.62f, 0.72f, 0.16f), IronDark, 0.75f);
            b.Torus(new Vector3(0f, 1.05f, 0.30f), 0.30f, 0.05f, Brass, 20, 6);
            b.Sphere(new Vector3(0f, 1.05f, 0.44f), 0.11f, EyeRed, 9, 6);                                  // core stone
            b.Sphere(new Vector3(0.66f, 1.34f, 0f), 0.30f, IronDark, 11, 8);
            b.Sphere(new Vector3(-0.66f, 1.34f, 0f), 0.30f, IronDark, 11, 8);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                    b.Tube(new Vector3(s * 0.66f, 1.46f, -0.26f + i * 0.16f),
                           new Vector3(s * 0.80f, 1.86f, -0.34f + i * 0.20f), 0.07f, 0.006f, BoneDark, 6);
            b.Sphere(new Vector3(0f, 0.52f, 0f), new Vector3(0.44f, 0.30f, 0.40f), DemonSkinDark, 11, 7);
            b.Box(new Vector3(0f, 0.52f, 0.30f), new Vector3(0.40f, 0.34f, 0.10f), Leather, 0.9f);         // loin plate
            return b.Build("DemonBody");
        }

        static Mesh BuildDemonHead()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0f, 0.02f), new Vector3(0.34f, 0.32f, 0.34f), DemonSkin, 12, 9);
            b.Sphere(new Vector3(0f, 0.12f, -0.06f), new Vector3(0.30f, 0.22f, 0.26f), DemonSkinDark, 10, 7);
            b.Horn(new Vector3(0.22f, 0.22f, -0.02f), new Vector3(0.45f, 1f, -0.25f), new Vector3(0.1f, -1.3f, 1.1f), 0.78f, 0.10f, Bone, 7);
            b.Horn(new Vector3(-0.22f, 0.22f, -0.02f), new Vector3(-0.45f, 1f, -0.25f), new Vector3(-0.1f, -1.3f, 1.1f), 0.78f, 0.10f, Bone, 7);
            b.Sphere(new Vector3(0.15f, 0.04f, 0.26f), 0.085f, EyeRed, 8, 6);
            b.Sphere(new Vector3(-0.15f, 0.04f, 0.26f), 0.085f, EyeRed, 8, 6);
            b.Sphere(new Vector3(0f, 0.20f, 0.24f), 0.05f, EyeAmber, 7, 5);
            b.Box(new Vector3(0f, -0.10f, 0.30f), new Vector3(0.30f, 0.10f, 0.14f), DemonSkinDark);
            for (int i = 0; i < 6; i++)
            {
                float x = -0.115f + i * 0.046f;
                b.Tube(new Vector3(x, -0.14f, 0.34f), new Vector3(x, -0.24f, 0.34f), 0.022f, 0.004f, Bone, 4);
            }
            return b.Build("DemonHead");
        }

        static Mesh BuildDemonJaw()
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, -0.05f, 0.18f), new Vector3(0.30f, 0.11f, 0.30f), DemonSkinDark, 0.9f);
            for (int i = 0; i < 6; i++)
            {
                float x = -0.115f + i * 0.046f;
                b.Tube(new Vector3(x, 0.02f, 0.30f), new Vector3(x, 0.14f, 0.31f), 0.022f, 0.004f, Bone, 4);
            }
            return b.Build("DemonJaw");
        }

        static Mesh BuildDemonArm()
        {
            var b = new MeshBuilder();
            b.Limb(Vector3.zero, new Vector3(0f, -0.62f, 0.10f), 0.17f, 0.14f, DemonSkin, 9);
            b.Torus(new Vector3(0f, -0.30f, 0.05f), 0.16f, 0.035f, Brass, 14, 5);                          // bracer
            b.Sphere(new Vector3(0f, -0.70f, 0.14f), 0.21f, DemonSkinDark, 10, 7);
            for (int i = 0; i < 3; i++)
                b.Tube(new Vector3(-0.10f + i * 0.10f, -0.80f, 0.26f), new Vector3(-0.12f + i * 0.12f, -0.92f, 0.38f), 0.045f, 0.006f, Bone, 5);
            return b.Build("DemonArm");
        }

        static Mesh BuildDemonLeg()
        {
            var b = new MeshBuilder();
            b.Limb(Vector3.zero, new Vector3(0f, -0.40f, -0.08f), 0.19f, 0.15f, DemonSkin, 9);
            b.Limb(new Vector3(0f, -0.40f, -0.08f), new Vector3(0f, -0.78f, 0.06f), 0.15f, 0.12f, DemonSkinDark, 9);
            b.Box(new Vector3(0f, -0.84f, 0.12f), new Vector3(0.30f, 0.12f, 0.42f), BoneDark);             // hoof
            return b.Build("DemonLeg");
        }

        // ---- the hero ------------------------------------------------------------------

        static Mesh BuildShipHull()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.30f, 0.06f), new Vector3(0.24f, 0.16f, 0.46f), HullLight, 12, 8);
            b.Sphere(new Vector3(0f, 0.24f, 0.02f), new Vector3(0.22f, 0.12f, 0.40f), HullDark, 12, 8);    // shaded underside
            b.Tube(new Vector3(0f, 0.30f, 0.38f), new Vector3(0f, 0.30f, 0.72f), 0.14f, 0.02f, Metal, 10);
            b.Sphere(new Vector3(0f, 0.42f, 0.10f), new Vector3(0.14f, 0.11f, 0.22f), HullDark, 10, 7);
            b.Sphere(new Vector3(0f, 0.45f, 0.14f), new Vector3(0.11f, 0.08f, 0.17f), Glass, 10, 7);       // canopy
            b.Box(new Vector3(0f, 0.20f, -0.22f), new Vector3(0.30f, 0.10f, 0.34f), Metal, 0.7f);
            b.Box(new Vector3(0f, 0.40f, -0.26f), new Vector3(0.05f, 0.18f, 0.22f), HullLight, 0.5f);      // tail fin
            return b.Build("ShipHull");
        }

        static Mesh BuildShipWing()
        {
            var b = new MeshBuilder();
            b.Blade(new Vector3(0.06f, 0f, 0.10f), new Vector3(0.62f, 0f, -0.30f), 0.30f, 0.07f, HullLight, HullDark);
            b.Tube(new Vector3(0.44f, 0f, -0.12f), new Vector3(0.44f, 0f, 0.22f), 0.05f, 0.03f, Metal, 7);
            b.Sphere(new Vector3(0.44f, 0f, 0.24f), 0.035f, EyeRed, 6, 4);                                  // nav light
            return b.Build("ShipWing");
        }

        static Mesh BuildShipEngine()
        {
            var b = new MeshBuilder();
            b.Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -0.26f), 0.11f, 0.13f, Metal, 9);
            b.Torus(new Vector3(0f, 0f, -0.26f), 0.12f, 0.02f, HullDark, 12, 5);
            b.Tube(new Vector3(0f, 0f, -0.26f), new Vector3(0f, 0f, -0.52f), 0.11f, 0.02f, Color.white, 9);
            return b.Build("ShipEngine");
        }

        static Mesh BuildShipPod()
        {
            var b = new MeshBuilder();
            b.Sphere(Vector3.zero, new Vector3(0.16f, 0.14f, 0.20f), Metal, 9, 7);
            b.Torus(new Vector3(0f, 0f, 0.04f), 0.14f, 0.03f, HullDark, 14, 5);
            b.Sphere(new Vector3(0f, 0f, 0.16f), 0.08f, Color.white, 8, 6);
            return b.Build("ShipPod");
        }

        // ---- spells and props ------------------------------------------------------------

        static Mesh BuildBlade()
        {
            var b = new MeshBuilder();
            b.Blade(new Vector3(-0.18f, 0f, 0f), new Vector3(0.52f, 0f, 0.16f), 0.20f, 0.05f, Color.white, Iron);
            b.Sphere(new Vector3(-0.20f, 0f, -0.02f), 0.10f, Leather, 8, 6);
            b.Torus(new Vector3(-0.14f, 0f, 0.01f), 0.09f, 0.022f, Brass, 10, 5);
            return b.Build("Blade");
        }

        static Mesh BuildBolt()
        {
            var b = new MeshBuilder();
            b.Tube(new Vector3(0f, 0f, -0.34f), new Vector3(0f, 0f, 0.14f), 0.055f, 0.10f, Color.white, 8);
            b.Tube(new Vector3(0f, 0f, 0.14f), new Vector3(0f, 0f, 0.42f), 0.10f, 0.005f, Color.white, 8);
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                b.Blade(new Vector3(Mathf.Cos(a) * 0.04f, Mathf.Sin(a) * 0.04f, -0.30f),
                        new Vector3(Mathf.Cos(a) * 0.20f, Mathf.Sin(a) * 0.20f, -0.42f), 0.05f, 0.02f, Color.white, Color.grey);
            }
            return b.Build("Bolt");
        }

        static Mesh BuildRock()
        {
            var b = new MeshBuilder();
            var rnd = new System.Random(7);
            var stone = new Color(0.30f, 0.26f, 0.24f);
            var molten = new Color(1f, 0.45f, 0.12f);
            b.Sphere(Vector3.zero, 0.5f, stone, 12, 9);
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, e = (float)rnd.NextDouble() * Mathf.PI - Mathf.PI / 2f;
                var d = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
                b.Sphere(d * 0.42f, 0.16f + (float)rnd.NextDouble() * 0.12f, i % 3 == 0 ? molten : stone, 7, 5);
            }
            return b.Build("Rock", true);
        }

        static Mesh BuildMine()
        {
            var b = new MeshBuilder();
            b.Sphere(Vector3.zero, 0.30f, IronDark, 12, 9);
            b.Torus(Vector3.zero, 0.31f, 0.035f, Iron, 18, 5);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                var d = new Vector3(Mathf.Cos(a), 0.25f, Mathf.Sin(a)).normalized;
                b.Tube(d * 0.26f, d * 0.52f, 0.06f, 0.008f, Rust, 6);
            }
            b.Tube(new Vector3(0f, 0.26f, 0f), new Vector3(0f, 0.50f, 0f), 0.06f, 0.01f, Iron, 6);
            b.Sphere(new Vector3(0f, 0.30f, 0f), 0.13f, Color.white, 9, 7);
            return b.Build("Mine");
        }

        static Mesh BuildGem()
        {
            var b = new MeshBuilder();
            b.Tube(new Vector3(0f, -0.28f, 0f), new Vector3(0f, 0f, 0f), 0.004f, 0.22f, Color.white, 6);
            b.Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.34f, 0f), 0.22f, 0.004f, Color.white, 6);
            return b.Build("Gem", true);
        }

        static Mesh BuildFunnel()
        {
            var b = new MeshBuilder();
            for (int i = 0; i < 4; i++)
            {
                float a0 = i * Mathf.PI * 2f / 4f;
                for (int s = 0; s < 10; s++)
                {
                    float t0 = s / 10f, t1 = (s + 1) / 10f;
                    float r0 = t0 * 0.5f, r1 = t1 * 0.5f;
                    float y0 = t0 * t0 * 0.9f, y1 = t1 * t1 * 0.9f;
                    float s0 = a0 + t0 * 4.2f, s1 = a0 + t1 * 4.2f;
                    var p0 = new Vector3(Mathf.Cos(s0) * r0, y0, Mathf.Sin(s0) * r0);
                    var p1 = new Vector3(Mathf.Cos(s1) * r1, y1, Mathf.Sin(s1) * r1);
                    b.Tube(p0, p1, 0.02f + t0 * 0.06f, 0.02f + t1 * 0.06f, s % 2 == 0 ? Color.white : new Color(0.7f, 0.5f, 1f), 5, false);
                }
            }
            b.Sphere(Vector3.zero, 0.1f, Color.white, 9, 7);
            return b.Build("Funnel");
        }

        static Mesh BuildShard()
        {
            var b = new MeshBuilder();
            b.Tube(new Vector3(0f, 0f, -0.2f), new Vector3(0f, 0f, 0.1f), 0.02f, 0.09f, Color.white, 5);
            b.Tube(new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, 0.5f), 0.09f, 0.004f, Color.white, 5);
            return b.Build("Shard");
        }
    }
}
