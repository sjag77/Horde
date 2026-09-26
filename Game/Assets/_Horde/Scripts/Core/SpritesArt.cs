using UnityEngine;

namespace Horde
{
    /// <summary>
    /// v0.0.2 art pass: real creatures instead of blobs, and a proper sprite for every spell.
    /// Everything is still generated at runtime as a signed-distance field, so there are no
    /// imported assets and the whole set costs a few milliseconds at startup.
    /// Characters face +X so a transform's rotation reads as their facing.
    /// </summary>
    public static partial class Sprites
    {
        // Creatures
        public static Sprite Grunt { get; private set; }
        public static Sprite Runner { get; private set; }
        public static Sprite Hound { get; private set; }
        public static Sprite Spitter { get; private set; }
        public static Sprite Splitter { get; private set; }
        public static Sprite Mite { get; private set; }
        public static Sprite Ogre { get; private set; }
        public static Sprite Demon { get; private set; }

        // Spell art
        public static Sprite Bolt { get; private set; }
        public static Sprite Blade { get; private set; }
        public static Sprite Flake { get; private set; }
        public static Sprite Rock { get; private set; }
        public static Sprite MineArt { get; private set; }
        public static Sprite VortexArt { get; private set; }
        public static Sprite WaveArt { get; private set; }
        public static Sprite Shock { get; private set; }
        public static Sprite IconVortex { get; private set; }
        public static Sprite IconWave { get; private set; }

        static void InitArt()
        {
            Grunt = MakeRGBA(128, GruntPx);
            Runner = MakeRGBA(128, RunnerPx);
            Hound = MakeRGBA(128, HoundPx);
            Spitter = MakeRGBA(128, SpitterPx);
            Splitter = MakeRGBA(128, SplitterPx);
            Mite = MakeRGBA(96, MitePx);
            Ogre = MakeRGBA(160, OgrePx);
            Demon = MakeRGBA(224, DemonPx);

            Bolt = MakeRGBA(96, BoltPx);
            Blade = MakeRGBA(128, BladePx);
            Flake = MakeRGBA(160, FlakePx);
            Rock = MakeRGBA(128, RockPx);
            MineArt = MakeRGBA(128, MinePx);
            VortexArt = MakeRGBA(192, VortexPx);
            WaveArt = MakeRGBA(192, WavePx);
            Shock = Make(128, ShockA);
            IconVortex = Make(96, IconVortexA);
            IconWave = Make(96, IconWaveA);
        }

        // ---- small SDF helpers (positive inside) -------------------------------------
        static float Cap(Vector2 p, Vector2 a, Vector2 b, float r) => r - Seg(p, a, b);
        static float Disc(Vector2 p, float cx, float cy, float r) => r - Vector2.Distance(p, new Vector2(cx, cy));
        static float Oval(Vector2 p, float cx, float cy, float rx, float ry)
        {
            var q = new Vector2((p.x - cx) / rx, (p.y - cy) / ry);
            return (1f - q.magnitude) * Mathf.Min(rx, ry);
        }
        static float U(float a, float b) => Mathf.Max(a, b);
        // Signed tapered stroke (positive inside): wide at a, a point at b.
        static float SdTaper(Vector2 p, Vector2 a, Vector2 b, float w)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Mathf.Lerp(w, 0.01f, t) - Vector2.Distance(p, a + ab * t);
        }
        static float U(float a, float b, float c) => Mathf.Max(a, Mathf.Max(b, c));
        static Vector2 Mir(Vector2 p) => new Vector2(p.x, -p.y);

        // Two-sided limbs: the same capsule mirrored across the body axis.
        static float Limb(Vector2 p, Vector2 a, Vector2 b, float r) => U(Cap(p, a, b, r), Cap(p, Mir(a), Mir(b), r));

        // Dark ink used for eyes, seams and mouths; bright for glints and glows.
        static float Ink(float v, float mask, float to) => Mathf.Lerp(v, to, Mathf.Clamp01(mask));

        // ---- creatures ---------------------------------------------------------------

        // Hunched grunt mid-stride: torso, head, two reaching arms, two trailing legs.
        static Color GruntPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float legs = Limb(p, new Vector2(-0.08f, 0.11f), new Vector2(-0.33f, 0.19f), 0.055f);
            float arms = Limb(p, new Vector2(0.03f, 0.15f), new Vector2(0.27f, 0.25f), 0.05f);
            float torso = Oval(p, -0.02f, 0f, 0.19f, 0.22f);
            float head = Disc(p, 0.18f, 0f, 0.125f);
            float s = U(U(torso, head), U(arms, legs));
            float v = Body(s) * (0.74f + 0.3f * Mathf.Clamp01(y * 1.8f + 0.5f));
            v *= head > 0f ? 1.12f : 1f;                                   // head catches the light
            v = Ink(v, Edge(0.02f - Mathf.Abs(Seg(p, new Vector2(-0.16f, 0f), new Vector2(0.06f, 0f)))) * 0.5f, v * 0.62f);
            float eyes = U(Disc(p, 0.22f, 0.055f, 0.032f), Disc(p, 0.22f, -0.055f, 0.032f));
            v = Ink(v, Edge(eyes), 0.05f);
            return Gray(v, Edge(s));
        }

        // Lean sprinter: one arm forward, one back, legs scissored - reads as speed.
        static Color RunnerPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float armF = Cap(p, new Vector2(0.05f, 0.09f), new Vector2(0.34f, 0.05f), 0.042f);
            float armB = Cap(p, new Vector2(-0.05f, -0.09f), new Vector2(-0.29f, -0.15f), 0.042f);
            float legF = Cap(p, new Vector2(-0.02f, 0.07f), new Vector2(0.19f, 0.27f), 0.05f);
            float legB = Cap(p, new Vector2(-0.05f, -0.06f), new Vector2(-0.37f, -0.13f), 0.05f);
            float torso = Oval(p, -0.03f, 0f, 0.15f, 0.16f);
            float head = Disc(p, 0.17f, 0f, 0.105f);
            float crest = Cap(p, new Vector2(0.13f, 0f), new Vector2(-0.04f, 0f), 0.035f);   // spined ridge
            float s = U(U(torso, head), U(U(armF, armB), U(U(legF, legB), crest)));
            float v = Body(s) * (0.72f + 0.34f * Mathf.Clamp01(y * 1.8f + 0.5f));
            v = Ink(v, Edge(crest) * 0.55f, v * 1.25f);
            float eyes = U(Disc(p, 0.20f, 0.045f, 0.028f), Disc(p, 0.20f, -0.045f, 0.028f));
            v = Ink(v, Edge(eyes), 0.06f);
            return Gray(v, Edge(s));
        }

        // Four-legged beast: long body, snout, ears, whipping tail.
        static Color HoundPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float body = Oval(p, -0.04f, 0f, 0.26f, 0.145f);
            float head = Disc(p, 0.24f, 0f, 0.115f);
            float snout = Cap(p, new Vector2(0.29f, 0f), new Vector2(0.44f, 0f), 0.055f);
            float ears = Limb(p, new Vector2(0.22f, 0.10f), new Vector2(0.27f, 0.21f), 0.032f);
            float front = Limb(p, new Vector2(0.10f, 0.11f), new Vector2(0.17f, 0.25f), 0.042f);
            float back = Limb(p, new Vector2(-0.17f, 0.11f), new Vector2(-0.25f, 0.25f), 0.045f);
            float tail = SdTaper(p, new Vector2(-0.26f, 0.02f), new Vector2(-0.47f, 0.16f), 0.05f);
            float s = U(U(body, head), U(U(snout, ears), U(U(front, back), tail)));
            float v = Body(s) * (0.7f + 0.36f * Mathf.Clamp01(y * 1.9f + 0.5f));
            v = Ink(v, Edge(Disc(p, -0.02f, 0f, 0.1f)) * 0.35f, v * 0.7f);          // dark saddle
            float eyes = U(Disc(p, 0.27f, 0.055f, 0.03f), Disc(p, 0.27f, -0.055f, 0.03f));
            v = Ink(v, Edge(eyes), 1f);                                              // pale glowing eyes
            v = Ink(v, Edge(Disc(p, 0.44f, 0f, 0.028f)), 0.08f);                     // nose
            return Gray(v, Edge(s));
        }

        // Bloated sac with a spitting tube up front and stubby legs.
        static Color SpitterPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float sac = Oval(p, -0.09f, 0f, 0.26f, 0.25f);
            float tube = Cap(p, new Vector2(0.06f, 0f), new Vector2(0.36f, 0f), 0.08f);
            float legs = U(Limb(p, new Vector2(-0.12f, 0.19f), new Vector2(-0.24f, 0.30f), 0.036f),
                           Limb(p, new Vector2(-0.22f, 0.10f), new Vector2(-0.38f, 0.16f), 0.036f));
            float s = U(U(sac, tube), legs);
            float v = Body(s) * (0.7f + 0.36f * Mathf.Clamp01(y * 1.6f + 0.5f));
            for (int k = 0; k < 4; k++)                                              // pulsing glands
            {
                float a = k * 1.7f;
                v = Ink(v, Edge(Disc(p, -0.10f + Mathf.Cos(a) * 0.13f, Mathf.Sin(a) * 0.13f, 0.045f)) * 0.8f, 1f);
            }
            v = Ink(v, Edge(Disc(p, 0.36f, 0f, 0.058f)), 0.05f);                     // muzzle hole
            return Gray(v, Edge(s));
        }

        // Gelatinous colony blob: cracked seam and two buds ready to break off.
        static Color SplitterPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float d = p.magnitude, a = Mathf.Atan2(y, x);
            float blob = 0.30f + 0.035f * Mathf.Sin(a * 5f) + 0.02f * Mathf.Sin(a * 3f + 1.2f) - d;
            float buds = U(Disc(p, -0.18f, 0.26f, 0.115f), Disc(p, -0.16f, -0.28f, 0.10f));
            float s = U(blob, buds);
            float v = Body(s) * (0.68f + 0.4f * Mathf.Clamp01(y * 1.6f + 0.5f));
            v = Ink(v, Edge(0.022f - Seg(p, new Vector2(-0.24f, 0.06f), new Vector2(0.22f, -0.04f))) * 0.8f, v * 0.45f);
            v = Ink(v, Edge(Disc(p, -0.09f, 0.1f, 0.07f)) * 0.5f, v * 1.3f);         // inner gloss
            float eyes = U(Disc(p, 0.16f, 0.09f, 0.035f), Disc(p, 0.18f, -0.06f, 0.035f));
            v = Ink(v, Edge(eyes), 0.06f);
            return Gray(v, Edge(s));
        }

        // Tiny three-legged crawler spat out when a Splitter dies.
        static Color MitePx(float x, float y)
        {
            var p = new Vector2(x, y);
            float body = Oval(p, 0f, 0f, 0.21f, 0.18f);
            float legs = U(Limb(p, new Vector2(0.04f, 0.13f), new Vector2(0.16f, 0.30f), 0.035f),
                           Cap(p, new Vector2(-0.14f, 0f), new Vector2(-0.34f, 0f), 0.035f));
            float s = U(body, legs);
            float v = Body(s) * (0.72f + 0.34f * Mathf.Clamp01(y * 2f + 0.5f));
            v = Ink(v, Edge(Disc(p, 0.09f, 0f, 0.06f)), 0.05f);                      // single eye
            v = Ink(v, Edge(Disc(p, 0.11f, 0.02f, 0.02f)), 1f);
            return Gray(v, Edge(s));
        }

        // Armoured ogre: spiked pauldrons, helm with a visor slit, two heavy fists.
        static Color OgrePx(float x, float y)
        {
            var p = new Vector2(x, y);
            float torso = Oval(p, -0.06f, 0f, 0.27f, 0.30f);
            float pauld = Limb(p, new Vector2(0.01f, 0.30f), new Vector2(0.01f, 0.30f), 0.145f);
            float arms = Limb(p, new Vector2(0.05f, 0.30f), new Vector2(0.30f, 0.25f), 0.085f);
            float head = Disc(p, 0.24f, 0f, 0.125f);
            float spikes = 0f;
            for (int k = -1; k <= 1; k += 2)
                for (int j = 0; j < 3; j++)
                {
                    var b = new Vector2(0.01f + (j - 1) * 0.12f, k * 0.30f);
                    spikes = U(spikes, SdTaper(p, b, b + new Vector2((j - 1) * 0.06f, k * 0.20f), 0.05f));
                }
            float s = U(U(torso, head), U(U(pauld, arms), spikes));
            float v = Body(s) * (0.7f + 0.36f * Mathf.Clamp01(y * 1.4f + 0.5f));
            if (s > 0.05f && Mathf.Abs(Mathf.Repeat(x + 0.5f, 0.14f) - 0.07f) < 0.014f) v *= 0.7f;   // armour plating
            float visor = Mathf.Abs(x - 0.27f) < 0.035f && Mathf.Abs(y) < 0.095f ? 1f : 0f;
            v = Ink(v, visor, 0.04f);
            v = Ink(v, visor * (Mathf.Abs(y) < 0.055f ? 1f : 0f) * 0.9f, 1f);        // glowing eye slit
            return Gray(v, Edge(s));
        }

        // Boss: horned demon with a fanged jaw, three eyes and spiked shoulder plates.
        static Color DemonPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float torso = Oval(p, -0.06f, 0f, 0.28f, 0.30f);
            float pauld = Limb(p, new Vector2(-0.02f, 0.30f), new Vector2(-0.02f, 0.30f), 0.155f);
            float arms = Limb(p, new Vector2(0.04f, 0.31f), new Vector2(0.27f, 0.30f), 0.085f);
            float head = Disc(p, 0.20f, 0f, 0.175f);
            float horns = U(SdTaper(p, new Vector2(0.24f, 0.11f), new Vector2(0.44f, 0.36f), 0.065f),
                            SdTaper(p, new Vector2(0.24f, -0.11f), new Vector2(0.44f, -0.36f), 0.065f));
            float s = U(U(torso, head), U(U(pauld, arms), horns));
            float v = Body(s) * (0.66f + 0.4f * Mathf.Clamp01(y * 1.3f + 0.5f));
            if (Mathf.Abs(Vector2.Distance(p, new Vector2(-0.06f, 0f)) - 0.19f) < 0.018f) v *= 0.62f;   // chest ring
            float eyes = U(Disc(p, 0.24f, 0.075f, 0.045f), Disc(p, 0.24f, -0.075f, 0.045f), Disc(p, 0.12f, 0f, 0.035f));
            float glint = U(Disc(p, 0.25f, 0.08f, 0.018f), Disc(p, 0.25f, -0.08f, 0.018f), Disc(p, 0.13f, 0.01f, 0.014f));
            v = Ink(v, Edge(eyes), 0.03f);
            v = Ink(v, Edge(glint), 1f);
            float jaw = Mathf.Abs(x - 0.33f) < 0.05f && Mathf.Abs(y) < 0.11f ? 1f : 0f;
            float teeth = jaw > 0f && Mathf.Repeat(y + 0.5f, 0.045f) < 0.022f ? 1f : 0f;
            v = Ink(v, jaw, 0.03f);
            v = Ink(v, teeth * jaw, 0.95f);
            return Gray(v, Edge(s));
        }

        // ---- spell art ----------------------------------------------------------------

        // Arrowhead bolt with a hot core and swept fins (points +X).
        static Color BoltPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float head = 0.20f - (Mathf.Abs(p.x - 0.20f) + Mathf.Abs(p.y) * 1.9f);
            float shaft = SdTaper(p, new Vector2(-0.42f, 0f), new Vector2(0.24f, 0f), 0.085f);
            float fins = U(SdTaper(p, new Vector2(-0.18f, 0f), new Vector2(-0.40f, 0.22f), 0.05f),
                           SdTaper(p, new Vector2(-0.18f, 0f), new Vector2(-0.40f, -0.22f), 0.05f));
            float s = U(head, U(shaft, fins));
            float core = Edge(0.045f - Mathf.Abs(y)) * Edge(0.30f - Mathf.Abs(x));
            float v = Mathf.Lerp(0.72f, 1f, Mathf.Clamp01(core + Edge(Disc(p, 0.16f, 0f, 0.07f))));
            return Gray(v, Edge(s));
        }

        // Curved scimitar: a crescent with a bright edge and a dark spine.
        static Color BladePx(float x, float y)
        {
            var p = new Vector2(x, y);
            float outer = 0.46f - Vector2.Distance(p, new Vector2(-0.10f, -0.16f));
            float inner = Vector2.Distance(p, new Vector2(-0.22f, -0.34f)) - 0.46f;
            float crescent = Mathf.Min(outer, inner);
            float hilt = Cap(p, new Vector2(-0.26f, -0.12f), new Vector2(-0.38f, -0.02f), 0.06f);
            float s = U(crescent, hilt);
            float v = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((0.055f - outer) / 0.05f));   // lit cutting edge
            v = Ink(v, Edge(hilt) * 0.9f, 0.35f);
            return Gray(v, Edge(s));
        }

        // Six-armed snowflake with branches and a bright core.
        static Color FlakePx(float x, float y)
        {
            var p = new Vector2(x, y);
            float s = Disc(p, 0f, 0f, 0.075f);
            for (int k = 0; k < 6; k++)
            {
                var q = Rot(p, -k * 60f);
                float arm = Cap(q, Vector2.zero, new Vector2(0.46f, 0f), 0.028f);
                float b1 = Cap(q, new Vector2(0.20f, 0f), new Vector2(0.31f, 0.12f), 0.02f);
                float b2 = Cap(q, new Vector2(0.20f, 0f), new Vector2(0.31f, -0.12f), 0.02f);
                float b3 = Cap(q, new Vector2(0.33f, 0f), new Vector2(0.41f, 0.08f), 0.016f);
                float b4 = Cap(q, new Vector2(0.33f, 0f), new Vector2(0.41f, -0.08f), 0.016f);
                s = U(s, U(U(arm, b1), U(U(b2, b3), b4)));
            }
            float v = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(Edge(Disc(p, 0f, 0f, 0.09f)) + 0.35f * (1f - p.magnitude * 2f)));
            return Gray(v, Edge(s));
        }

        // Jagged burning rock: dark stone with molten cracks.
        static Color RockPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float a = Mathf.Atan2(y, x), d = p.magnitude;
            float s = 0.40f + 0.05f * Mathf.Sin(a * 7f + 0.6f) + 0.035f * Mathf.Sin(a * 4f - 1.1f) - d;
            float v = Mathf.Lerp(0.28f, 0.6f, Mathf.Clamp01(0.5f + y)) * Body(s);
            float crack = Mathf.Max(Edge(0.028f - Seg(p, new Vector2(-0.26f, 0.1f), new Vector2(0.05f, -0.05f))),
                          Mathf.Max(Edge(0.022f - Seg(p, new Vector2(0.05f, -0.05f), new Vector2(0.24f, 0.14f))),
                                    Edge(0.02f - Seg(p, new Vector2(0.03f, -0.04f), new Vector2(-0.02f, -0.3f)))));
            v = Ink(v, crack, 1f);
            v = Ink(v, Edge(Disc(p, -0.16f, 0.2f, 0.06f)) * 0.5f, 0.85f);
            return Gray(v, Edge(s));
        }

        // Spiked sea-mine, read at a glance: dark shell, 8 spikes, bright lamp on top.
        static Color MinePx(float x, float y)
        {
            var p = new Vector2(x, y);
            float shell = Disc(p, 0f, 0f, 0.30f);
            float spikes = 0f;
            for (int k = 0; k < 8; k++)
            {
                var q = Rot(p, -k * 45f);
                spikes = U(spikes, SdTaper(q, new Vector2(0.24f, 0f), new Vector2(0.47f, 0f), 0.062f));
            }
            float s = U(shell, spikes);
            float v = Mathf.Lerp(0.22f, 0.55f, Mathf.Clamp01(0.5f + y * 1.2f)) * Body(s);
            if (Mathf.Abs(Vector2.Distance(p, Vector2.zero) - 0.22f) < 0.016f) v *= 0.6f;
            v = Ink(v, Edge(Disc(p, 0f, 0f, 0.13f)), 1f);        // lamp (tinted by the renderer)
            v = Ink(v, Edge(Disc(p, -0.09f, 0.14f, 0.05f)) * 0.5f, 0.8f);
            return Gray(v, Edge(s));
        }

        // Spiral singularity: four log-spiral arms swirling into a bright core.
        static Color VortexPx(float x, float y)
        {
            var p = new Vector2(x, y);
            float d = p.magnitude;
            if (d > 0.49f) return Gray(1f, 0f);
            float a = Mathf.Atan2(y, x);
            float spin = a - Mathf.Log(Mathf.Max(d, 0.02f)) * 2.4f;
            float arms = Mathf.Pow(Mathf.Abs(Mathf.Cos(spin * 2f)), 3f);
            float fade = Mathf.Clamp01((0.49f - d) / 0.2f) * Mathf.Clamp01(d / 0.06f);
            float core = Mathf.Clamp01(1f - d * 5f);
            float alpha = Mathf.Clamp01(arms * fade + core);
            float v = Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(core * 1.4f + arms * 0.3f));
            return Gray(v, alpha);
        }

        // Crescent flame wave, travelling along +X.
        static Color WavePx(float x, float y)
        {
            var p = new Vector2(x, y);
            float d = p.magnitude;
            float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.36f) / 0.11f);
            float arc = Mathf.Clamp01((Mathf.Cos(Mathf.Atan2(y, x)) - 0.12f) / 0.55f);
            float tongues = 0.75f + 0.25f * Mathf.Sin(Mathf.Atan2(y, x) * 14f);
            float alpha = Mathf.Clamp01(band * arc * tongues);
            float v = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(1f - Mathf.Abs(d - 0.33f) / 0.07f));
            return Gray(v, alpha);
        }

        // Soft shockwave disc used for blast telegraphs (alpha only).
        static float ShockA(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y);
            if (d > 0.5f) return 0f;
            float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.44f) / 0.06f);
            return Mathf.Max(rim, Mathf.Clamp01(1f - d * 2f) * 0.22f);
        }

        static float IconVortexA(float x, float y)   // spiral pulled into a core
        {
            float d = Mathf.Sqrt(x * x + y * y);
            if (d > 0.47f) return 0f;
            float a = Mathf.Atan2(y, x) - Mathf.Log(Mathf.Max(d, 0.03f)) * 2.2f;
            float arms = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 1.5f)), 4f);
            return Mathf.Clamp01(arms * Mathf.Clamp01((0.47f - d) / 0.12f) + Mathf.Clamp01(1f - d * 6f));
        }

        static float IconWaveA(float x, float y)     // three rising flame tongues
        {
            var p = new Vector2(x, y);
            float best = 0f;
            for (int k = -1; k <= 1; k++)
            {
                float ox = k * 0.22f, h = k == 0 ? 0.44f : 0.30f;
                var baseP = new Vector2(ox, -0.36f);
                var tip = new Vector2(ox + k * 0.07f, h);
                best = Mathf.Max(best, Taper(p, baseP, tip, k == 0 ? 0.14f : 0.10f));
            }
            return Mathf.Max(best, Edge(0.05f - Mathf.Abs(y + 0.4f)) * Edge(0.34f - Mathf.Abs(x)));
        }
    }
}
