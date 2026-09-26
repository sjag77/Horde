using System.Collections.Generic;
using UnityEngine;

namespace Horde
{
    /// <summary>
    /// A tiny procedural modelling kit. Every creature, weapon and prop in HORDE is built here
    /// at startup from primitives welded into ONE mesh with baked vertex colours, so a whole
    /// creature costs a single draw call and the game still ships with no imported assets.
    /// Models face +Z and stand on Y = 0.
    /// </summary>
    public sealed class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>(512);
        readonly List<Vector3> norms = new List<Vector3>(512);
        readonly List<Color> cols = new List<Color>(512);
        readonly List<int> tris = new List<int>(1024);

        public int VertexCount => verts.Count;

        public Mesh Build(string name, bool recalcNormals = false)
        {
            var m = new Mesh { name = name };
            if (verts.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(verts);
            m.SetColors(cols);
            m.SetTriangles(tris, 0);
            if (recalcNormals) m.RecalculateNormals();
            else m.SetNormals(norms);
            m.RecalculateBounds();
            m.UploadMeshData(true);
            return m;
        }

        // Colours are authored the way they should look on screen (sRGB). The project renders
        // in linear space, so they are converted once here instead of washing out in the shader.
        static readonly bool Linear = QualitySettings.activeColorSpace == ColorSpace.Linear;

        void Push(Vector3 p, Vector3 n, Color c)
        {
            verts.Add(p); norms.Add(n); cols.Add(Linear ? c.linear : c);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int i = verts.Count;
            Push(a, n, col); Push(b, n, col); Push(c, n, col); Push(d, n, col);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        void Tri(Vector3 a, Vector3 b, Vector3 c, Color col)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int i = verts.Count;
            Push(a, n, col); Push(b, n, col); Push(c, n, col);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        // ---- primitives ---------------------------------------------------------------

        /// <summary>Axis-aligned box, optionally tapered towards +Y (top scale) and sheared.</summary>
        public MeshBuilder Box(Vector3 center, Vector3 size, Color col, float topScale = 1f, Quaternion? rot = null)
        {
            Vector3 h = size * 0.5f;
            Quaternion q = rot ?? Quaternion.identity;
            Vector3 P(float sx, float sy, float sz)
            {
                float s = sy > 0f ? topScale : 1f;
                return center + q * new Vector3(sx * h.x * s, sy * h.y, sz * h.z * s);
            }
            Vector3 a = P(-1, -1, -1), b = P(1, -1, -1), c = P(1, -1, 1), d = P(-1, -1, 1);
            Vector3 e = P(-1, 1, -1), f = P(1, 1, -1), g = P(1, 1, 1), hh = P(-1, 1, 1);
            Quad(d, c, b, a, col);          // bottom
            Quad(e, f, g, hh, col);         // top
            Quad(a, b, f, e, col);          // -Z
            Quad(c, d, hh, g, col);         // +Z
            Quad(b, c, g, f, col);          // +X
            Quad(d, a, e, hh, col);         // -X
            return this;
        }

        /// <summary>UV sphere; squash with a non-uniform radius to get eggs, bellies and heads.</summary>
        public MeshBuilder Sphere(Vector3 center, Vector3 radius, Color col, int seg = 10, int rings = 7)
        {
            int i0 = verts.Count;
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings, phi = v * Mathf.PI;
                float sy = Mathf.Cos(phi), sr = Mathf.Sin(phi);
                for (int s = 0; s <= seg; s++)
                {
                    float u = s / (float)seg, th = u * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(th) * sr, sy, Mathf.Sin(th) * sr);
                    var p = new Vector3(dir.x * radius.x, dir.y * radius.y, dir.z * radius.z);
                    Push(center + p, new Vector3(dir.x / radius.x, dir.y / radius.y, dir.z / radius.z).normalized, col);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < seg; s++)
                {
                    int a = i0 + r * (seg + 1) + s, b = a + seg + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            return this;
        }

        public MeshBuilder Sphere(Vector3 center, float radius, Color col, int seg = 10, int rings = 7)
            => Sphere(center, Vector3.one * radius, col, seg, rings);

        /// <summary>Cone / cylinder / spike: radius can differ at each end.</summary>
        public MeshBuilder Tube(Vector3 from, Vector3 to, float r0, float r1, Color col, int seg = 8, bool caps = true)
        {
            Vector3 axis = to - from;
            float len = axis.magnitude;
            if (len < 1e-5f) return this;
            Vector3 dir = axis / len;
            Vector3 up = Mathf.Abs(dir.y) > 0.95f ? Vector3.forward : Vector3.up;
            Vector3 sx = Vector3.Cross(up, dir).normalized, sz = Vector3.Cross(dir, sx);
            int i0 = verts.Count;
            for (int s = 0; s <= seg; s++)
            {
                float th = s / (float)seg * Mathf.PI * 2f;
                Vector3 rad = sx * Mathf.Cos(th) + sz * Mathf.Sin(th);
                Push(from + rad * r0, rad, col);
                Push(to + rad * r1, rad, col);
            }
            for (int s = 0; s < seg; s++)
            {
                int a = i0 + s * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(a + 2);
                tris.Add(a + 2); tris.Add(a + 1); tris.Add(a + 3);
            }
            if (!caps) return this;
            if (r0 > 1e-4f) Fan(from, -dir, r0, sx, sz, col, seg);
            if (r1 > 1e-4f) Fan(to, dir, r1, sx, sz, col, seg);
            return this;
        }

        void Fan(Vector3 c, Vector3 n, float r, Vector3 sx, Vector3 sz, Color col, int seg)
        {
            int i0 = verts.Count;
            Push(c, n, col);
            for (int s = 0; s <= seg; s++)
            {
                float th = s / (float)seg * Mathf.PI * 2f;
                Push(c + (sx * Mathf.Cos(th) + sz * Mathf.Sin(th)) * r, n, col);
            }
            for (int s = 0; s < seg; s++)
            {
                if (Vector3.Dot(n, Vector3.up) >= 0f) { tris.Add(i0); tris.Add(i0 + s + 1); tris.Add(i0 + s + 2); }
                else { tris.Add(i0); tris.Add(i0 + s + 2); tris.Add(i0 + s + 1); }
            }
        }

        /// <summary>Tapered limb with a rounded end - arms, legs, tails, horns.</summary>
        public MeshBuilder Limb(Vector3 from, Vector3 to, float r0, float r1, Color col, int seg = 7)
        {
            Tube(from, to, r0, r1, col, seg, false);
            Sphere(from, r0, col, seg, 5);
            Sphere(to, Mathf.Max(r1, 0.012f), col, seg, 5);
            return this;
        }

        /// <summary>Curved horn or tusk swept through an arc.</summary>
        public MeshBuilder Horn(Vector3 root, Vector3 dir, Vector3 curl, float len, float r, Color col, int steps = 6)
        {
            Vector3 p = root;
            dir = dir.normalized;
            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)steps, t1 = (i + 1) / (float)steps;
                Vector3 d0 = (dir + curl * t).normalized, d1 = (dir + curl * t1).normalized;
                Vector3 a = p, b = p + d1 * (len / steps);
                Tube(a, b, r * (1f - t * 0.85f), r * (1f - t1 * 0.85f), col, 6, false);
                p = b;
            }
            return this;
        }

        public MeshBuilder Torus(Vector3 center, float radius, float thick, Color col, int seg = 24, int side = 6)
        {
            int i0 = verts.Count;
            for (int s = 0; s <= seg; s++)
            {
                float th = s / (float)seg * Mathf.PI * 2f;
                Vector3 c = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th));
                for (int k = 0; k <= side; k++)
                {
                    float ph = k / (float)side * Mathf.PI * 2f;
                    Vector3 n = (c * Mathf.Cos(ph) + Vector3.up * Mathf.Sin(ph));
                    Push(center + c * radius + n * thick, n, col);
                }
            }
            for (int s = 0; s < seg; s++)
                for (int k = 0; k < side; k++)
                {
                    int a = i0 + s * (side + 1) + k, b = a + side + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            return this;
        }

        /// <summary>Flat disc lying on the ground (decals, shadows, telegraphs).</summary>
        public MeshBuilder Disc(Vector3 center, float radius, Color col, int seg = 24)
        {
            int i0 = verts.Count;
            Push(center, Vector3.up, col);
            for (int s = 0; s <= seg; s++)
            {
                float th = s / (float)seg * Mathf.PI * 2f;
                Push(center + new Vector3(Mathf.Cos(th) * radius, 0f, Mathf.Sin(th) * radius), Vector3.up, col);
            }
            for (int s = 0; s < seg; s++) { tris.Add(i0); tris.Add(i0 + s + 1); tris.Add(i0 + s + 2); }
            return this;
        }

        /// <summary>Flat ring on the ground; fades from inner to outer colour.</summary>
        public MeshBuilder Ring(Vector3 center, float inner, float outer, Color a, Color b, int seg = 40)
        {
            int i0 = verts.Count;
            for (int s = 0; s <= seg; s++)
            {
                float th = s / (float)seg * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th));
                Push(center + d * inner, Vector3.up, a);
                Push(center + d * outer, Vector3.up, b);
            }
            for (int s = 0; s < seg; s++)
            {
                int i = i0 + s * 2;
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i + 2); tris.Add(i + 1); tris.Add(i + 3);
            }
            return this;
        }

        /// <summary>A blade: flat diamond cross-section swept to a point.</summary>
        public MeshBuilder Blade(Vector3 root, Vector3 tip, float width, float thick, Color edge, Color spine)
        {
            Vector3 dir = (tip - root).normalized;
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * width * 0.5f;
            Vector3 up = Vector3.up * thick * 0.5f;
            Vector3 a = root - side, b = root + side, t = tip;
            Quad(a + up, b + up, t, t, edge);
            Quad(b - up, a - up, t, t, spine);
            Quad(a - up, b - up, b + up, a + up, spine);
            Tri(a + up, t, a - up, edge);
            Tri(b - up, t, b + up, edge);
            return this;
        }

        public MeshBuilder Spikes(Vector3 center, float ringR, float len, float r, int n, Color col, float y = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float th = i / (float)n * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Cos(th), 0f, Mathf.Sin(th));
                Tube(center + d * ringR + Vector3.up * y, center + d * (ringR + len) + Vector3.up * y, r, 0.004f, col, 5);
            }
            return this;
        }
    }
}
