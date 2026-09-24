using UnityEngine;

namespace Horde
{
    /// <summary>
    /// Placeholder art generated at startup, so the prototype needs no imported assets.
    /// Every sprite is exactly 1 world unit wide; scale transforms to size them.
    /// </summary>
    public static class Sprites
    {
        public static Sprite Circle { get; private set; }
        public static Sprite Ring { get; private set; }
        public static Sprite Diamond { get; private set; }
        public static Sprite Triangle { get; private set; }
        public static Sprite Square { get; private set; }
        public static Sprite Glow { get; private set; }
        public static Sprite Cheese { get; private set; }
        public static Sprite Magnet { get; private set; }
        public static Sprite Hero { get; private set; }
        public static Sprite Swarmer { get; private set; }
        public static Sprite Brute { get; private set; }
        public static Sprite Boss { get; private set; }
        public static Sprite Gem { get; private set; }
        public static Sprite Floor { get; private set; }
        public static Sprite IconSpark { get; private set; }
        public static Sprite IconBlades { get; private set; }
        public static Sprite IconNova { get; private set; }
        public static Sprite IconChain { get; private set; }
        public static Sprite IconMeteor { get; private set; }
        public static Sprite IconBomb { get; private set; }
        public static Sprite JoyBase { get; private set; }
        public static Sprite JoyKnob { get; private set; }

        public static void Init()
        {
            if (Circle != null) return;
            Circle = Make(96, (x, y) => Edge(0.46f - Mathf.Sqrt(x * x + y * y)));
            Ring = Make(128, (x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                return Mathf.Min(Edge(0.47f - d), Edge(d - 0.40f));
            });
            Diamond = Make(96, (x, y) => Edge(0.46f - (Mathf.Abs(x) + Mathf.Abs(y))));
            Triangle = Make(96, TriangleAlpha);
            Square = Make(4, (x, y) => 1f);
            Glow = Make(96, (x, y) =>
            {
                float d = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 2f);
                return d * d;
            });
            Cheese = Make(96, CheeseAlpha);
            Magnet = Make(96, MagnetAlpha);
            Hero = MakeRGBA(128, HeroPx);
            Swarmer = MakeRGBA(96, SwarmerPx);
            Brute = MakeRGBA(128, BrutePx);
            Boss = MakeRGBA(192, BossPx);
            Gem = MakeRGBA(64, GemPx);
            Floor = MakeFloor();
            IconSpark = Make(96, IconSparkA);
            IconBlades = Make(96, IconBladesA);
            IconNova = Make(96, IconNovaA);
            IconChain = Make(96, IconChainA);
            IconMeteor = Make(96, IconMeteorA);
            IconBomb = Make(96, IconBombA);
            JoyBase = Make(160, JoyBaseA);
            JoyKnob = MakeRGBA(128, JoyKnobPx);
        }

        static float Edge(float signedDistance) => Mathf.Clamp01(signedDistance / 0.02f + 0.5f);

        // Points along +X so a transform's rotation reads as the facing direction.
        static float TriangleAlpha(float x, float y)
        {
            var p = new Vector2(x, y);
            var a = new Vector2(0.46f, 0f);
            var b = new Vector2(-0.34f, 0.40f);
            var c = new Vector2(-0.34f, -0.40f);
            return Edge(Mathf.Min(Side(a, b, p), Mathf.Min(Side(b, c, p), Side(c, a, p))));
        }

        static float Side(Vector2 from, Vector2 to, Vector2 p)
        {
            Vector2 e = to - from;
            return (e.x * (p.y - from.y) - e.y * (p.x - from.x)) / e.magnitude;
        }

        // A wedge of cheese with three holes punched through it.
        static float CheeseAlpha(float x, float y)
        {
            var p = new Vector2(x, y);
            var a = new Vector2(-0.42f, -0.30f);
            var b = new Vector2(0.42f, -0.30f);
            var c = new Vector2(-0.42f, 0.34f);
            float wedge = Mathf.Min(Side(a, b, p), Mathf.Min(Side(b, c, p), Side(c, a, p)));
            float holes = Mathf.Max(Hole(p, -0.22f, -0.12f, 0.07f), Mathf.Max(Hole(p, 0.06f, -0.18f, 0.05f), Hole(p, -0.29f, 0.10f, 0.045f)));
            return Mathf.Min(Edge(wedge), 1f - holes);
        }

        static float Hole(Vector2 p, float cx, float cy, float r) => Edge(r - Vector2.Distance(p, new Vector2(cx, cy)));

        // A horseshoe magnet: the top half of a thick ring plus two legs.
        static float MagnetAlpha(float x, float y)
        {
            const float cy = 0.02f;
            float d = Mathf.Sqrt(x * x + (y - cy) * (y - cy));
            float arch = y >= cy ? Mathf.Min(Edge(0.40f - d), Edge(d - 0.20f)) : 0f;
            float legs = Mathf.Min(Mathf.Min(Edge(0.40f - Mathf.Abs(x)), Edge(Mathf.Abs(x) - 0.20f)),
                                   Mathf.Min(Edge(cy - y + 0.01f), Edge(y + 0.42f)));
            return Mathf.Max(arch, legs);
        }

        // Character art bakes grayscale shading into RGB: a tint colours the body while
        // outlines, eyes and highlights keep their contrast.
        static float Body(float s) => Mathf.Lerp(0.42f, 1f, Mathf.Clamp01((s - 0.03f) / 0.025f));
        static float Blob(float cx, float cy, float r, float x, float y) => Edge(r - Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)));
        static Color Gray(float v, float a) => new Color(v, v, v, a);

        // Swept-wing ship pointing along +X with a dark cockpit and glint.
        static Color HeroPx(float x, float y)
        {
            var p = new Vector2(x, y);
            var nose = new Vector2(0.46f, 0f);
            var wingL = new Vector2(-0.32f, 0.38f);
            var notch = new Vector2(-0.14f, 0f);
            var wingR = new Vector2(-0.32f, -0.38f);
            float s = Mathf.Min(Mathf.Min(Side(nose, wingL, p), Side(wingR, nose, p)),
                                Mathf.Max(Side(wingL, notch, p), Side(notch, wingR, p)));
            float v = Body(s) * (y > 0f ? 1f : 0.8f);
            v = Mathf.Lerp(v, 0.2f, Blob(0.1f, 0f, 0.075f, x, y));
            v = Mathf.Lerp(v, 1f, Blob(0.12f, 0.025f, 0.022f, x, y));
            return Gray(v, Edge(s));
        }

        // Spiky critter with two big eyes.
        static Color SwarmerPx(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
            float s = 0.34f + 0.06f * Mathf.Sin(a * 7f) - d;
            float v = Body(s) * (0.82f + 0.18f * Mathf.Clamp01(y * 2f + 0.5f));
            v += 0.18f * Blob(-0.1f, 0.15f, 0.06f, x, y);
            float eyes = Mathf.Max(Blob(-0.1f, 0f, 0.075f, x, y), Blob(0.1f, 0f, 0.075f, x, y));
            float pupils = Mathf.Max(Blob(-0.09f, -0.01f, 0.035f, x, y), Blob(0.11f, -0.01f, 0.035f, x, y));
            v = Mathf.Lerp(v, 1f, eyes);
            v = Mathf.Lerp(v, 0.04f, pupils);
            return Gray(v, Edge(s));
        }

        // Armoured block with seams, a heavy brow and slit eyes.
        static Color BrutePx(float x, float y)
        {
            float k = Mathf.Pow(Mathf.Pow(Mathf.Abs(x), 3f) + Mathf.Pow(Mathf.Abs(y), 3f), 1f / 3f);
            float s = 0.40f - k;
            float v = Body(s) * (0.78f + 0.22f * Mathf.Clamp01(y * 1.5f + 0.5f));
            if (s > 0.05f && (Mathf.Abs(y + 0.1f) < 0.022f || (Mathf.Abs(x) < 0.02f && y < -0.1f))) v *= 0.6f;
            float brow = Mathf.Abs(x) < 0.25f ? Mathf.Clamp01(1f - Mathf.Abs(y - 0.12f + Mathf.Abs(x) * 0.35f) / 0.03f) : 0f;
            float eyes = Mathf.Max(Blob(-0.12f, 0.04f, 0.05f, x, y), Blob(0.12f, 0.04f, 0.05f, x, y));
            v = Mathf.Lerp(v, 0.4f, brow * 0.85f);
            v = Mathf.Lerp(v, 0.03f, eyes);
            return Gray(v, Edge(s));
        }

        // Crowned horror: spiked rim, plate ring, three glinting eyes and a grin.
        static Color BossPx(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
            float s = 0.36f + Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 5f)), 8f) * 0.08f - d;
            float v = Body(s) * (0.75f + 0.25f * Mathf.Clamp01(y * 1.5f + 0.5f));
            if (Mathf.Abs(d - 0.25f) < 0.016f) v *= 0.65f;
            float eyes = Mathf.Max(Blob(-0.11f, 0.05f, 0.06f, x, y), Mathf.Max(Blob(0.11f, 0.05f, 0.06f, x, y), Blob(0f, 0.16f, 0.045f, x, y)));
            float glint = Mathf.Max(Blob(-0.11f, 0.05f, 0.022f, x, y), Mathf.Max(Blob(0.11f, 0.05f, 0.022f, x, y), Blob(0f, 0.16f, 0.017f, x, y)));
            float mouth = Mathf.Abs(x) < 0.14f && Mathf.Abs(y + 0.1f - x * x * 1.6f) < 0.02f ? 1f : 0f;
            v = Mathf.Lerp(v, 0.03f, Mathf.Max(eyes, mouth));
            v = Mathf.Lerp(v, 1f, glint);
            return Gray(v, Edge(s));
        }

        // Faceted crystal: lit left face, shaded right face, a highlight.
        static Color GemPx(float x, float y)
        {
            float s = 0.44f - (Mathf.Abs(x) + Mathf.Abs(y));
            float v = Body(s * 0.8f) * (x < 0f ? 1f : 0.72f) * (y > 0f ? 1f : 0.85f);
            v = Mathf.Lerp(v, 1f, Blob(-0.1f, 0.12f, 0.06f, x, y) * 0.9f);
            return Gray(v, Edge(s));
        }

        // Seamless dark stone tiles (128 px = 4 world units) for a tiled floor.
        static Sprite MakeFloor()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[size * size];
            uint seed = 99991;
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float u = i / (float)size, w = j / (float)size;
                    float blot = 0.5f + 0.25f * Mathf.Sin(u * Mathf.PI * 4f) * Mathf.Sin(w * Mathf.PI * 6f) + 0.25f * Mathf.Sin((u + w) * Mathf.PI * 2f);
                    seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                    float b = 0.85f + blot * 0.25f + ((seed & 1023) / 1023f - 0.5f) * 0.08f;
                    if (i % 64 < 2 || j % 64 < 2) b *= 0.55f;
                    px[j * size + i] = new Color32((byte)(18 * b), (byte)(22 * b), (byte)(34 * b), 255);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        static Vector2 Rot(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // Tapered stroke from a (width w) to b (a point), for comet tails and blades.
        static float Taper(Vector2 p, Vector2 a, Vector2 b, float w)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Edge(Mathf.Lerp(w, 0.012f, t) - Vector2.Distance(p, a + ab * t));
        }

        // Ability icons -----------------------------------------------------------------
        static float IconSparkA(float x, float y)   // four-point sparkle with smaller diagonal rays
        {
            float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
            float big = 0.09f + 0.35f * Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * a)), 8f);
            float small = 0.07f + 0.17f * Mathf.Pow(Mathf.Abs(Mathf.Sin(2f * a)), 10f);
            return Edge(Mathf.Max(big, small) - r);
        }

        static float IconBladesA(float x, float y)  // three curved blades around a hub
        {
            var p = new Vector2(x, y);
            float best = Edge(0.1f - p.magnitude);
            for (int k = 0; k < 3; k++)
            {
                var q = Rot(p, -k * 120f);
                best = Mathf.Max(best, Mathf.Max(Taper(q, new Vector2(0.05f, 0f), new Vector2(0.28f, 0.12f), 0.1f),
                                                 Taper(q, new Vector2(0.26f, 0.11f), new Vector2(0.42f, 0.3f), 0.06f)));
            }
            return best;
        }

        static float IconNovaA(float x, float y)    // six-armed snowflake
        {
            var p = new Vector2(x, y);
            float best = Edge(0.08f - p.magnitude);
            for (int k = 0; k < 6; k++)
            {
                var q = Rot(p, -k * 60f);
                float d = Mathf.Min(Seg(q, Vector2.zero, new Vector2(0.42f, 0f)),
                          Mathf.Min(Seg(q, new Vector2(0.22f, 0f), new Vector2(0.33f, 0.11f)), Seg(q, new Vector2(0.22f, 0f), new Vector2(0.33f, -0.11f))));
                best = Mathf.Max(best, Edge(0.036f - d));
            }
            return best;
        }

        static float IconChainA(float x, float y)   // zig-zag lightning bolt
        {
            var p = new Vector2(x, y);
            var a = new Vector2(0.17f, 0.44f);
            var b = new Vector2(-0.13f, 0.03f);
            var c = new Vector2(0.11f, 0.03f);
            var d = new Vector2(-0.17f, -0.44f);
            return Mathf.Max(Taper(p, b, a, 0.08f), Mathf.Max(Edge(0.065f - Seg(p, b, c)), Taper(p, c, d, 0.08f)));
        }

        static float IconMeteorA(float x, float y)  // fireball with three trailing streaks
        {
            var p = new Vector2(x, y);
            var head = new Vector2(0.15f, -0.15f);
            float best = Edge(0.17f - Vector2.Distance(p, head));
            best = Mathf.Max(best, Taper(p, head, new Vector2(-0.4f, 0.4f), 0.13f));
            best = Mathf.Max(best, Taper(p, head + new Vector2(-0.05f, 0.1f), new Vector2(-0.12f, 0.44f), 0.07f));
            best = Mathf.Max(best, Taper(p, head + new Vector2(-0.1f, 0.05f), new Vector2(-0.44f, 0.12f), 0.07f));
            return best;
        }

        static float IconLaserA(float x, float y)   // diagonal lance: glow, shaft, diamond tip
        {
            var p = Rot(new Vector2(x, y), -45f);
            float glow = 0.35f * Edge(0.1f - Seg(p, new Vector2(-0.44f, 0f), new Vector2(0.4f, 0f)));
            float shaft = Edge(0.045f - Seg(p, new Vector2(-0.44f, 0f), new Vector2(0.2f, 0f)));
            float tip = Edge(0.17f - (Mathf.Abs(p.x - 0.28f) + Mathf.Abs(p.y) * 1.6f));
            return Mathf.Max(glow, Mathf.Max(shaft, tip));
        }

        static float IconBombA(float x, float y)   // round bomb with a lit fuse
        {
            var p = new Vector2(x, y);
            float body = Edge(0.29f - Vector2.Distance(p, new Vector2(-0.06f, -0.08f)));
            float cap = Edge(0.07f - Seg(p, new Vector2(0.1f, 0.1f), new Vector2(0.17f, 0.17f)));
            float fuse = Edge(0.03f - Seg(p, new Vector2(0.16f, 0.16f), new Vector2(0.3f, 0.32f)));
            float r = Vector2.Distance(p, new Vector2(0.34f, 0.37f)), a = Mathf.Atan2(y - 0.37f, x - 0.34f);
            float spark = Edge(0.03f + 0.07f * Mathf.Pow(Mathf.Abs(Mathf.Cos(2f * a)), 6f) - r);
            return Mathf.Max(body, Mathf.Max(cap, Mathf.Max(fuse, spark)));
        }

        // Joystick: notched ring base and a shaded knob with a direction chevron (points +X).
        static float JoyBaseA(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            float ring = Mathf.Min(Edge(0.47f - r), Edge(r - 0.435f));
            float seg = Mathf.Repeat(a + 22.5f, 45f) - 22.5f;
            float notch = Mathf.Abs(seg) < 4f ? Mathf.Min(Edge(0.41f - r), Edge(r - 0.34f)) : 0f;
            float fill = Edge(0.435f - r) * 0.12f;
            return Mathf.Max(ring, Mathf.Max(notch, fill));
        }

        static Color JoyKnobPx(float x, float y)
        {
            float s = 0.44f - Mathf.Sqrt(x * x + y * y);
            float v = Body(s) * (0.85f + 0.15f * Mathf.Clamp01(y * 2f + 0.5f));
            var p = new Vector2(x, y);
            float chev = Mathf.Min(Seg(p, new Vector2(-0.08f, 0.16f), new Vector2(0.12f, 0f)), Seg(p, new Vector2(0.12f, 0f), new Vector2(-0.08f, -0.16f)));
            v = Mathf.Lerp(v, 0.15f, Edge(0.045f - chev));
            return Gray(v, Edge(s));
        }

        static Sprite MakeRGBA(int size, System.Func<float, float, Color> shade)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = (i + 0.5f) / size - 0.5f, y = (j + 0.5f) / size - 0.5f;
                    Color c = shade(x, y);
                    c.a = Mathf.Clamp01(c.a);
                    pixels[j * size + i] = c;
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        }

        static Sprite Make(int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = size <= 4 ? FilterMode.Point : FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = (i + 0.5f) / size - 0.5f, y = (j + 0.5f) / size - 0.5f;
                    pixels[j * size + i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x, y)) * 255f));
                }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        }
    }
}
