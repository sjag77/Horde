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

        static readonly Color Skin = new Color(1f, 1f, 1f);
        static readonly Color Dark = new Color(0.34f, 0.34f, 0.38f);
        static readonly Color Mid = new Color(0.62f, 0.62f, 0.66f);
        static readonly Color Bone = new Color(0.95f, 0.93f, 0.86f);
        static readonly Color Eye = new Color(1f, 0.97f, 0.9f);
        static readonly Color Metal = new Color(0.78f, 0.80f, 0.86f);

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
            PodStrut = new MeshBuilder().Box(Vector3.zero, new Vector3(0.06f, 0.06f, 1f), Mid).Build("Strut");
            Sphere1 = new MeshBuilder().Sphere(Vector3.zero, 0.5f, Color.white, 12, 9).Build("Sphere");
            Cube1 = new MeshBuilder().Box(Vector3.zero, Vector3.one, Color.white).Build("Cube");
        }

        static void Eyes(MeshBuilder b, float y, float z, float spread, float r)
        {
            b.Sphere(new Vector3(spread, y, z), r, Eye, 7, 5);
            b.Sphere(new Vector3(-spread, y, z), r, Eye, 7, 5);
            b.Sphere(new Vector3(spread, y, z + r * 0.55f), r * 0.45f, Dark, 6, 4);
            b.Sphere(new Vector3(-spread, y, z + r * 0.55f), r * 0.45f, Dark, 6, 4);
        }

        // ---- rank and file -------------------------------------------------------------

        // Hunched grunt: heavy shoulders, long arms, stubby legs. The shape you see most.
        static Mesh BuildGrunt()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.46f, 0f), new Vector3(0.26f, 0.30f, 0.24f), Skin);        // torso
            b.Sphere(new Vector3(0f, 0.70f, 0.10f), new Vector3(0.19f, 0.18f, 0.20f), Skin);     // head, thrust forward
            b.Sphere(new Vector3(0.20f, 0.62f, 0f), 0.13f, Mid);                                  // shoulders
            b.Sphere(new Vector3(-0.20f, 0.62f, 0f), 0.13f, Mid);
            b.Limb(new Vector3(0.22f, 0.60f, 0.02f), new Vector3(0.26f, 0.30f, 0.26f), 0.08f, 0.06f, Skin);
            b.Limb(new Vector3(-0.22f, 0.60f, 0.02f), new Vector3(-0.26f, 0.30f, 0.26f), 0.08f, 0.06f, Skin);
            b.Limb(new Vector3(0.24f, 0.30f, 0.26f), new Vector3(0.28f, 0.22f, 0.40f), 0.055f, 0.03f, Bone);   // claws
            b.Limb(new Vector3(-0.24f, 0.30f, 0.26f), new Vector3(-0.28f, 0.22f, 0.40f), 0.055f, 0.03f, Bone);
            b.Limb(new Vector3(0.12f, 0.24f, 0f), new Vector3(0.13f, 0.03f, 0.04f), 0.09f, 0.07f, Dark);       // legs
            b.Limb(new Vector3(-0.12f, 0.24f, 0f), new Vector3(-0.13f, 0.03f, 0.04f), 0.09f, 0.07f, Dark);
            Eyes(b, 0.74f, 0.24f, 0.075f, 0.045f);
            b.Tube(new Vector3(0f, 0.80f, 0.02f), new Vector3(0f, 0.95f, -0.10f), 0.035f, 0.004f, Bone, 5);    // crest
            return b.Build("Grunt");
        }

        // Lean sprinter: forward-pitched spine, one arm out, long shins.
        static Mesh BuildRunner()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.52f, 0.02f), new Vector3(0.17f, 0.24f, 0.19f), Skin);
            b.Sphere(new Vector3(0f, 0.76f, 0.14f), new Vector3(0.14f, 0.14f, 0.17f), Skin);
            b.Limb(new Vector3(0.16f, 0.64f, 0.04f), new Vector3(0.20f, 0.52f, 0.34f), 0.055f, 0.035f, Skin);
            b.Limb(new Vector3(-0.16f, 0.64f, 0.04f), new Vector3(-0.22f, 0.44f, -0.22f), 0.055f, 0.035f, Skin);
            b.Limb(new Vector3(0.10f, 0.32f, 0f), new Vector3(0.12f, 0.06f, 0.22f), 0.07f, 0.045f, Dark);
            b.Limb(new Vector3(-0.10f, 0.32f, 0f), new Vector3(-0.13f, 0.06f, -0.20f), 0.07f, 0.045f, Dark);
            Eyes(b, 0.79f, 0.26f, 0.06f, 0.038f);
            for (int i = 0; i < 4; i++)   // spined ridge down the back
                b.Tube(new Vector3(0f, 0.72f - i * 0.10f, -0.10f - i * 0.02f),
                       new Vector3(0f, 0.82f - i * 0.10f, -0.22f - i * 0.02f), 0.030f, 0.004f, Bone, 5);
            return b.Build("Runner");
        }

        // Four-legged beast: low slung, big jaw, whipping tail.
        static Mesh BuildHound()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.34f, 0f), new Vector3(0.20f, 0.19f, 0.34f), Skin);
            b.Sphere(new Vector3(0f, 0.42f, 0.32f), new Vector3(0.16f, 0.15f, 0.17f), Skin);      // head
            b.Tube(new Vector3(0f, 0.38f, 0.42f), new Vector3(0f, 0.34f, 0.60f), 0.10f, 0.07f, Skin, 8);   // snout
            b.Sphere(new Vector3(0f, 0.33f, 0.60f), 0.045f, Dark, 6, 4);                          // nose
            b.Tube(new Vector3(0.09f, 0.52f, 0.28f), new Vector3(0.15f, 0.70f, 0.22f), 0.05f, 0.006f, Dark, 5);  // ears
            b.Tube(new Vector3(-0.09f, 0.52f, 0.28f), new Vector3(-0.15f, 0.70f, 0.22f), 0.05f, 0.006f, Dark, 5);
            b.Limb(new Vector3(0.15f, 0.30f, 0.20f), new Vector3(0.17f, 0.04f, 0.24f), 0.06f, 0.045f, Dark);
            b.Limb(new Vector3(-0.15f, 0.30f, 0.20f), new Vector3(-0.17f, 0.04f, 0.24f), 0.06f, 0.045f, Dark);
            b.Limb(new Vector3(0.15f, 0.30f, -0.18f), new Vector3(0.17f, 0.04f, -0.24f), 0.065f, 0.05f, Dark);
            b.Limb(new Vector3(-0.15f, 0.30f, -0.18f), new Vector3(-0.17f, 0.04f, -0.24f), 0.065f, 0.05f, Dark);
            b.Horn(new Vector3(0f, 0.42f, -0.30f), new Vector3(0f, 0.25f, -1f), new Vector3(0f, 0.9f, 0f), 0.45f, 0.055f, Skin);
            Eyes(b, 0.47f, 0.42f, 0.085f, 0.04f);
            for (int i = 0; i < 5; i++)   // teeth
            {
                float x = -0.06f + i * 0.03f;
                b.Tube(new Vector3(x, 0.32f, 0.58f), new Vector3(x, 0.27f, 0.60f), 0.016f, 0.003f, Bone, 4);
            }
            return b.Build("Hound");
        }

        // Bloated acid sac on stubby legs with a mortar tube up front.
        static Mesh BuildSpitter()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.44f, -0.06f), new Vector3(0.34f, 0.34f, 0.32f), Skin);
            for (int i = 0; i < 6; i++)   // glands
            {
                float a = i * Mathf.PI * 2f / 6f;
                b.Sphere(new Vector3(Mathf.Cos(a) * 0.26f, 0.44f + Mathf.Sin(a) * 0.22f, -0.10f), 0.09f, Mid, 7, 5);
            }
            b.Tube(new Vector3(0f, 0.46f, 0.18f), new Vector3(0f, 0.52f, 0.46f), 0.12f, 0.10f, Dark, 9);   // barrel
            b.Torus(new Vector3(0f, 0.52f, 0.46f), 0.10f, 0.022f, Mid, 12, 5);
            b.Limb(new Vector3(0.18f, 0.20f, -0.06f), new Vector3(0.24f, 0.03f, -0.12f), 0.05f, 0.04f, Dark);
            b.Limb(new Vector3(-0.18f, 0.20f, -0.06f), new Vector3(-0.24f, 0.03f, -0.12f), 0.05f, 0.04f, Dark);
            b.Limb(new Vector3(0f, 0.18f, 0.14f), new Vector3(0f, 0.03f, 0.22f), 0.05f, 0.04f, Dark);
            Eyes(b, 0.66f, 0.18f, 0.10f, 0.05f);
            return b.Build("Spitter");
        }

        // Gelatinous colony: a big wobbling blob with buds already pushing out of it.
        static Mesh BuildSplitter()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.40f, 0f), new Vector3(0.40f, 0.38f, 0.38f), Skin, 12, 9);
            b.Sphere(new Vector3(0.24f, 0.58f, -0.14f), 0.17f, Mid, 9, 6);
            b.Sphere(new Vector3(-0.22f, 0.52f, -0.18f), 0.15f, Mid, 9, 6);
            b.Sphere(new Vector3(0.02f, 0.68f, 0.16f), 0.13f, Mid, 9, 6);
            b.Sphere(new Vector3(0f, 0.30f, 0.30f), 0.14f, Skin, 9, 6);
            Eyes(b, 0.46f, 0.34f, 0.12f, 0.055f);
            return b.Build("Splitter");
        }

        // What a Splitter leaves behind: tiny, fast, and there are always more.
        static Mesh BuildMite()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.20f, 0f), new Vector3(0.20f, 0.17f, 0.20f), Skin, 9, 6);
            b.Sphere(new Vector3(0f, 0.26f, 0.14f), 0.075f, Eye, 7, 5);
            for (int i = 0; i < 3; i++)
            {
                float a = -0.7f + i * 0.7f;
                b.Limb(new Vector3(Mathf.Sin(a) * 0.16f, 0.16f, Mathf.Cos(a) * 0.12f),
                       new Vector3(Mathf.Sin(a) * 0.28f, 0.02f, Mathf.Cos(a) * 0.20f), 0.035f, 0.02f, Dark, 5);
                b.Limb(new Vector3(-Mathf.Sin(a) * 0.16f, 0.16f, Mathf.Cos(a) * 0.12f),
                       new Vector3(-Mathf.Sin(a) * 0.28f, 0.02f, Mathf.Cos(a) * 0.20f), 0.035f, 0.02f, Dark, 5);
            }
            return b.Build("Mite");
        }

        // Armoured ogre: plated shoulders, a helm with a glowing visor, two wrecking-ball fists.
        static Mesh BuildOgre()
        {
            var b = new MeshBuilder();
            b.Box(new Vector3(0f, 0.62f, 0f), new Vector3(0.52f, 0.56f, 0.40f), Skin, 0.8f);       // chest block
            b.Box(new Vector3(0f, 0.34f, 0.02f), new Vector3(0.40f, 0.22f, 0.34f), Mid);           // belt
            b.Sphere(new Vector3(0.38f, 0.86f, 0f), 0.22f, Metal, 9, 7);                            // pauldrons
            b.Sphere(new Vector3(-0.38f, 0.86f, 0f), 0.22f, Metal, 9, 7);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 3; i++)
                    b.Tube(new Vector3(s * 0.38f, 0.94f, -0.14f + i * 0.14f),
                           new Vector3(s * 0.46f, 1.16f, -0.18f + i * 0.16f), 0.05f, 0.005f, Bone, 5);
            b.Sphere(new Vector3(0f, 0.98f, 0.08f), new Vector3(0.19f, 0.18f, 0.20f), Metal);       // helm
            b.Box(new Vector3(0f, 0.98f, 0.24f), new Vector3(0.26f, 0.055f, 0.06f), Eye);           // visor slit
            b.Limb(new Vector3(0.40f, 0.80f, 0.02f), new Vector3(0.48f, 0.40f, 0.20f), 0.12f, 0.10f, Skin);
            b.Limb(new Vector3(-0.40f, 0.80f, 0.02f), new Vector3(-0.48f, 0.40f, 0.20f), 0.12f, 0.10f, Skin);
            b.Sphere(new Vector3(0.50f, 0.34f, 0.24f), 0.16f, Metal, 9, 6);                         // fists
            b.Sphere(new Vector3(-0.50f, 0.34f, 0.24f), 0.16f, Metal, 9, 6);
            b.Limb(new Vector3(0.18f, 0.30f, 0f), new Vector3(0.20f, 0.04f, 0.04f), 0.13f, 0.11f, Dark);
            b.Limb(new Vector3(-0.18f, 0.30f, 0f), new Vector3(-0.20f, 0.04f, 0.04f), 0.13f, 0.11f, Dark);
            return b.Build("Ogre");
        }

        // ---- boss rig ------------------------------------------------------------------

        static Mesh BuildDemonBody()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.95f, -0.02f), new Vector3(0.62f, 0.68f, 0.52f), Skin, 14, 10);
            b.Box(new Vector3(0f, 0.95f, 0.42f), new Vector3(0.62f, 0.72f, 0.16f), Metal, 0.75f);        // breastplate
            b.Torus(new Vector3(0f, 1.05f, 0.30f), 0.30f, 0.05f, Metal, 20, 6);
            b.Sphere(new Vector3(0.66f, 1.34f, 0f), 0.30f, Metal, 11, 8);                                 // pauldrons
            b.Sphere(new Vector3(-0.66f, 1.34f, 0f), 0.30f, Metal, 11, 8);
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                    b.Tube(new Vector3(s * 0.66f, 1.46f, -0.26f + i * 0.16f),
                           new Vector3(s * 0.80f, 1.86f, -0.34f + i * 0.20f), 0.07f, 0.006f, Bone, 6);
                b.Sphere(new Vector3(0f, 0.52f, 0f), new Vector3(0.44f, 0.30f, 0.40f), Dark, 11, 7);      // hips
            return b.Build("DemonBody");
        }

        static Mesh BuildDemonHead()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0f, 0.02f), new Vector3(0.34f, 0.32f, 0.34f), Skin, 12, 9);
            b.Horn(new Vector3(0.22f, 0.22f, -0.02f), new Vector3(0.45f, 1f, -0.25f), new Vector3(0.1f, -1.3f, 1.1f), 0.78f, 0.10f, Bone, 7);
            b.Horn(new Vector3(-0.22f, 0.22f, -0.02f), new Vector3(-0.45f, 1f, -0.25f), new Vector3(-0.1f, -1.3f, 1.1f), 0.78f, 0.10f, Bone, 7);
            b.Sphere(new Vector3(0.15f, 0.04f, 0.26f), 0.085f, Eye, 8, 6);                                 // burning eyes
            b.Sphere(new Vector3(-0.15f, 0.04f, 0.26f), 0.085f, Eye, 8, 6);
            b.Sphere(new Vector3(0f, 0.20f, 0.24f), 0.05f, Eye, 7, 5);                                     // third eye
            b.Box(new Vector3(0f, -0.10f, 0.30f), new Vector3(0.30f, 0.10f, 0.14f), Dark);                 // upper jaw
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
            b.Box(new Vector3(0f, -0.05f, 0.18f), new Vector3(0.30f, 0.11f, 0.30f), Dark, 0.9f);
            for (int i = 0; i < 6; i++)
            {
                float x = -0.115f + i * 0.046f;
                b.Tube(new Vector3(x, 0.02f, 0.30f), new Vector3(x, 0.14f, 0.31f), 0.022f, 0.004f, Bone, 4);
            }
            return b.Build("DemonJaw");
        }

        // Arms and legs are separate so the boss can wind up, swing and stomp.
        static Mesh BuildDemonArm()
        {
            var b = new MeshBuilder();
            b.Limb(Vector3.zero, new Vector3(0f, -0.62f, 0.10f), 0.17f, 0.14f, Skin, 9);
            b.Sphere(new Vector3(0f, -0.70f, 0.14f), 0.21f, Mid, 10, 7);                                   // fist
            for (int i = 0; i < 3; i++)
                b.Tube(new Vector3(-0.10f + i * 0.10f, -0.80f, 0.26f), new Vector3(-0.12f + i * 0.12f, -0.92f, 0.38f), 0.045f, 0.006f, Bone, 5);
            return b.Build("DemonArm");
        }

        static Mesh BuildDemonLeg()
        {
            var b = new MeshBuilder();
            b.Limb(Vector3.zero, new Vector3(0f, -0.40f, -0.08f), 0.19f, 0.15f, Skin, 9);
            b.Limb(new Vector3(0f, -0.40f, -0.08f), new Vector3(0f, -0.78f, 0.06f), 0.15f, 0.12f, Skin, 9);
            b.Box(new Vector3(0f, -0.84f, 0.12f), new Vector3(0.30f, 0.12f, 0.42f), Dark);                 // hoof
            return b.Build("DemonLeg");
        }

        // ---- the hero ------------------------------------------------------------------

        static Mesh BuildShipHull()
        {
            var b = new MeshBuilder();
            b.Sphere(new Vector3(0f, 0.30f, 0.06f), new Vector3(0.24f, 0.16f, 0.46f), Skin, 12, 8);
            b.Tube(new Vector3(0f, 0.30f, 0.38f), new Vector3(0f, 0.30f, 0.72f), 0.14f, 0.02f, Metal, 10);  // nose
            b.Sphere(new Vector3(0f, 0.42f, 0.10f), new Vector3(0.14f, 0.11f, 0.22f), Dark, 10, 7);         // canopy
            b.Sphere(new Vector3(0f, 0.46f, 0.16f), new Vector3(0.09f, 0.06f, 0.12f), Eye, 8, 6);           // cockpit glow
            b.Box(new Vector3(0f, 0.20f, -0.22f), new Vector3(0.30f, 0.10f, 0.34f), Mid, 0.7f);
            return b.Build("ShipHull");
        }

        static Mesh BuildShipWing()
        {
            var b = new MeshBuilder();
            b.Blade(new Vector3(0.06f, 0f, 0.10f), new Vector3(0.62f, 0f, -0.30f), 0.30f, 0.07f, Skin, Mid);
            b.Tube(new Vector3(0.44f, 0f, -0.12f), new Vector3(0.44f, 0f, 0.22f), 0.05f, 0.03f, Metal, 7);  // wingtip pylon
            return b.Build("ShipWing");
        }

        static Mesh BuildShipEngine()
        {
            var b = new MeshBuilder();
            b.Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -0.26f), 0.11f, 0.13f, Mid, 9);
            b.Tube(new Vector3(0f, 0f, -0.26f), new Vector3(0f, 0f, -0.52f), 0.11f, 0.02f, Eye, 9);         // thrust plume
            return b.Build("ShipEngine");
        }

        static Mesh BuildShipPod()
        {
            var b = new MeshBuilder();
            b.Sphere(Vector3.zero, new Vector3(0.16f, 0.14f, 0.20f), Metal, 9, 7);
            b.Torus(new Vector3(0f, 0f, 0.04f), 0.14f, 0.03f, Mid, 14, 5);
            b.Sphere(new Vector3(0f, 0f, 0.16f), 0.08f, Eye, 8, 6);
            return b.Build("ShipPod");
        }

        // ---- spells and props ------------------------------------------------------------

        static Mesh BuildBlade()
        {
            var b = new MeshBuilder();
            b.Blade(new Vector3(-0.18f, 0f, 0f), new Vector3(0.52f, 0f, 0.16f), 0.20f, 0.05f, Color.white, Mid);
            b.Sphere(new Vector3(-0.20f, 0f, -0.02f), 0.10f, Mid, 8, 6);
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
                        new Vector3(Mathf.Cos(a) * 0.20f, Mathf.Sin(a) * 0.20f, -0.42f), 0.05f, 0.02f, Mid, Dark);
            }
            return b.Build("Bolt");
        }

        static Mesh BuildRock()
        {
            var b = new MeshBuilder();
            var rnd = new System.Random(7);
            b.Sphere(Vector3.zero, 0.5f, Mid, 12, 9);
            for (int i = 0; i < 9; i++)   // knock chunks out of the silhouette
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, e = (float)rnd.NextDouble() * Mathf.PI - Mathf.PI / 2f;
                var d = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e));
                b.Sphere(d * 0.42f, 0.16f + (float)rnd.NextDouble() * 0.12f, i % 3 == 0 ? Color.white : Dark, 7, 5);
            }
            return b.Build("Rock", true);
        }

        static Mesh BuildMine()
        {
            var b = new MeshBuilder();
            b.Sphere(Vector3.zero, 0.30f, Dark, 12, 9);
            b.Torus(Vector3.zero, 0.31f, 0.035f, Mid, 18, 5);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                var d = new Vector3(Mathf.Cos(a), 0.25f, Mathf.Sin(a)).normalized;
                b.Tube(d * 0.26f, d * 0.52f, 0.06f, 0.008f, Mid, 6);
            }
            b.Tube(new Vector3(0f, 0.26f, 0f), new Vector3(0f, 0.50f, 0f), 0.06f, 0.01f, Mid, 6);
            b.Sphere(new Vector3(0f, 0.30f, 0f), 0.13f, Color.white, 9, 7);   // lamp
            return b.Build("Mine");
        }

        static Mesh BuildGem()
        {
            var b = new MeshBuilder();
            b.Tube(new Vector3(0f, -0.28f, 0f), new Vector3(0f, 0f, 0f), 0.004f, 0.22f, Color.white, 6);
            b.Tube(new Vector3(0f, 0f, 0f), new Vector3(0f, 0.34f, 0f), 0.22f, 0.004f, Color.white, 6);
            return b.Build("Gem", true);
        }

        // The Void Vortex funnel: a twisted cone that spins in place.
        static Mesh BuildFunnel()
        {
            var b = new MeshBuilder();
            for (int i = 0; i < 4; i++)
            {
                float a0 = i * Mathf.PI * 2f / 4f;
                Vector3 prev = Vector3.zero;
                for (int s = 0; s < 10; s++)
                {
                    float t0 = s / 10f, t1 = (s + 1) / 10f;
                    float r0 = t0 * 0.5f, r1 = t1 * 0.5f;
                    float y0 = t0 * t0 * 0.9f, y1 = t1 * t1 * 0.9f;
                    float s0 = a0 + t0 * 4.2f, s1 = a0 + t1 * 4.2f;
                    var p0 = new Vector3(Mathf.Cos(s0) * r0, y0, Mathf.Sin(s0) * r0);
                    var p1 = new Vector3(Mathf.Cos(s1) * r1, y1, Mathf.Sin(s1) * r1);
                    b.Tube(p0, p1, 0.02f + t0 * 0.06f, 0.02f + t1 * 0.06f, s % 2 == 0 ? Color.white : Mid, 5, false);
                    prev = p1;
                }
            }
            b.Sphere(Vector3.zero, 0.1f, Color.white, 9, 7);
            return b.Build("Funnel");
        }

        // An ice shard thrown out by Frost Nova.
        static Mesh BuildShard()
        {
            var b = new MeshBuilder();
            b.Tube(new Vector3(0f, 0f, -0.2f), new Vector3(0f, 0f, 0.1f), 0.02f, 0.09f, Color.white, 5);
            b.Tube(new Vector3(0f, 0f, 0.1f), new Vector3(0f, 0f, 0.5f), 0.09f, 0.004f, Color.white, 5);
            return b.Build("Shard");
        }
    }
}
