using UnityEngine;

namespace Horde
{
    /// <summary>
    /// One 3D object in the arena: a mesh, a tint and a place on the XY battlefield.
    /// The game logic still thinks in 2D - a Rig takes that 2D position plus a height and a
    /// facing angle and puts a real model there, standing up out of the ground towards the
    /// tilted camera. Colour is pushed through a MaterialPropertyBlock so every creature
    /// shares one material and the SRP batcher can keep the draw calls down.
    /// </summary>
    public sealed class Rig
    {
        static MaterialPropertyBlock block;
        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int EmissiveId = Shader.PropertyToID("_Emissive");
        static readonly int FlashId = Shader.PropertyToID("_Flash");

        public readonly Transform Tr;
        public readonly MeshFilter Filter;
        public readonly MeshRenderer Renderer;
        Color tint = Color.white;
        float emissive = -1f, flash;

        /// <summary>Models are authored Y-up facing +Z; this lays them onto the XY arena.</summary>
        public static readonly Quaternion Stand = Quaternion.Euler(-90f, 0f, 0f);

        public Rig(string name, Mesh mesh, Material mat, Transform parent, bool shadows = true)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            Tr = go.transform;
            Filter = go.AddComponent<MeshFilter>();
            Filter.sharedMesh = mesh;
            Renderer = go.AddComponent<MeshRenderer>();
            Renderer.sharedMaterial = mat;
            Renderer.shadowCastingMode = shadows
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            Renderer.receiveShadows = false;   // flat-lit look; shadows land on the floor only
            Renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            Renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            Renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        public bool Enabled
        {
            get => Renderer.enabled;
            set => Renderer.enabled = value;
        }

        public Mesh Mesh
        {
            get => Filter.sharedMesh;
            set { if (Filter.sharedMesh != value) Filter.sharedMesh = value; }
        }

        /// <summary>A world point for a 2D arena position at the given height above the floor.</summary>
        public static Vector3 At(Vector2 p, float height = 0f) => new Vector3(p.x, p.y, -height);

        /// <summary>Facing angle in the arena (degrees from +X) as a model rotation.</summary>
        public static Quaternion Facing(float degrees) => Quaternion.AngleAxis(degrees - 90f, Vector3.forward) * Stand;

        public void Place(Vector2 pos, float facingDeg, float scale, float height = 0f)
        {
            Tr.SetPositionAndRotation(At(pos, height), Facing(facingDeg));
            Tr.localScale = Vector3.one * scale;
        }

        public void Place(Vector2 pos, float facingDeg, Vector3 scale, float height = 0f)
        {
            Tr.SetPositionAndRotation(At(pos, height), Facing(facingDeg));
            Tr.localScale = scale;
        }

        /// <summary>Tilt a model forward/back in its own facing plane - leaning, rearing, tumbling.</summary>
        public void Place(Vector2 pos, float facingDeg, Vector3 scale, float height, float pitchDeg, float rollDeg = 0f)
        {
            Tr.SetPositionAndRotation(At(pos, height),
                Facing(facingDeg) * Quaternion.Euler(pitchDeg, 0f, rollDeg));
            Tr.localScale = scale;
        }

        public void SetTint(Color c, float glow = 0f) => SetLook(c, glow, flash);

        /// <summary>Tint shades the model's baked colours; flash whitens it when it is hit.</summary>
        public void SetLook(Color c, float glow, float hitFlash)
        {
            if (c == tint && Mathf.Approximately(glow, emissive) && Mathf.Approximately(hitFlash, flash)) return;
            tint = c;
            emissive = glow;
            flash = hitFlash;
            block ??= new MaterialPropertyBlock();
            Renderer.GetPropertyBlock(block);
            block.SetColor(TintId, c);
            block.SetFloat(EmissiveId, glow);
            block.SetFloat(FlashId, hitFlash);
            Renderer.SetPropertyBlock(block);
        }

        /// <summary>Forces the next SetTint through even if the colour matches (after pooling).</summary>
        public void Invalidate() => emissive = -1f;
    }
}
