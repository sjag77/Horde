using UnityEngine;

namespace Horde
{
    /// <summary>
    /// The hero's gunship in 3D: hull, two swept wings, twin engines with live thrust plumes,
    /// and a hover bob. It banks into turns and pitches when it accelerates, so movement has
    /// weight even though the ship never leaves the plane.
    /// </summary>
    public sealed class HeroRig
    {
        readonly Transform root;
        readonly Rig hull, wingL, wingR, engL, engR, plumeL, plumeR;
        readonly Rig[] ghosts;
        readonly float[] ghostLife;
        float bank, pitch, hover, thrust;
        Color tint = Color.white;

        public const int GhostCap = 6;

        public HeroRig(Game g)
        {
            var go = new GameObject("HeroRig");
            root = go.transform;
            hull = g.NewRig("Hull", Models.ShipHull, Color.white, root);
            wingL = g.NewRig("WingL", Models.ShipWing, Color.white, root);
            wingR = g.NewRig("WingR", Models.ShipWing, Color.white, root);
            engL = g.NewRig("EngL", Models.ShipEngine, Color.white, root);
            engR = g.NewRig("EngR", Models.ShipEngine, Color.white, root);
            plumeL = g.NewRig("PlumeL", Models.ShipEngine, Color.white, root, true);
            plumeR = g.NewRig("PlumeR", Models.ShipEngine, Color.white, root, true);

            ghosts = new Rig[GhostCap];
            ghostLife = new float[GhostCap];
            for (int i = 0; i < GhostCap; i++)
            {
                ghosts[i] = g.NewRig("DashGhost", Models.ShipHull, Color.white, null, true);
                ghosts[i].Enabled = false;
            }
            Layout();
        }

        void Layout()
        {
            hull.Tr.localPosition = Vector3.zero;
            wingL.Tr.localPosition = new Vector3(0f, 0.28f, 0.02f);
            wingL.Tr.localScale = new Vector3(-1f, 1f, 1f);      // mirrored
            wingR.Tr.localPosition = new Vector3(0f, 0.28f, 0.02f);
            engL.Tr.localPosition = new Vector3(-0.26f, 0.28f, -0.30f);
            engR.Tr.localPosition = new Vector3(0.26f, 0.28f, -0.30f);
            plumeL.Tr.localPosition = new Vector3(-0.26f, 0.28f, -0.42f);
            plumeR.Tr.localPosition = new Vector3(0.26f, 0.28f, -0.42f);
        }

        public bool Visible
        {
            set
            {
                hull.Enabled = wingL.Enabled = wingR.Enabled = value;
                engL.Enabled = engR.Enabled = plumeL.Enabled = plumeR.Enabled = value;
            }
        }

        public void SetColor(Color c)
        {
            tint = c;
            hull.SetTint(c);
            wingL.SetTint(c);
            wingR.SetTint(c);
            engL.SetTint(c * 0.8f);
            engR.SetTint(c * 0.8f);
        }

        public void Tick(float dt, Vector2 pos, Vector2 facing, Vector2 move, float hurt, bool blinking)
        {
            float deg = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            // sideways component of the input decides the bank; forward component the pitch
            float side = move.sqrMagnitude > 0.0001f ? Vector2.Dot(move, new Vector2(-facing.y, facing.x)) : 0f;
            float fwd = move.sqrMagnitude > 0.0001f ? Vector2.Dot(move, facing) : 0f;
            bank = Mathf.Lerp(bank, Mathf.Clamp(-side, -1f, 1f) * 26f, 1f - Mathf.Exp(-8f * dt));
            pitch = Mathf.Lerp(pitch, -Mathf.Clamp(fwd, -1f, 1f) * 9f, 1f - Mathf.Exp(-7f * dt));
            thrust = Mathf.Lerp(thrust, move.magnitude, 1f - Mathf.Exp(-10f * dt));
            hover += dt * 3.1f;

            float h = 0.30f + Mathf.Sin(hover) * 0.05f;
            root.SetPositionAndRotation(Rig.At(pos, h), Rig.Facing(deg) * Quaternion.Euler(pitch, 0f, bank));
            root.localScale = Vector3.one * 1.15f;

            float flare = (0.55f + thrust * 1.1f) * (0.9f + 0.1f * Mathf.Sin(Time.time * 40f));
            plumeL.Tr.localScale = plumeR.Tr.localScale = new Vector3(1f, 1f, flare);
            var hot = Color.Lerp(new Color(0.4f, 0.85f, 1f), new Color(1f, 0.95f, 0.8f), thrust * 0.6f);
            plumeL.SetTint(hot, 1.6f + thrust);
            plumeR.SetTint(hot, 1.6f + thrust);

            if (hurt > 0f)
            {
                var flash = Color.Lerp(tint, Color.white, Mathf.Clamp01(hurt));
                hull.SetTint(flash, Mathf.Clamp01(hurt));
                wingL.SetTint(flash, Mathf.Clamp01(hurt));
                wingR.SetTint(flash, Mathf.Clamp01(hurt));
            }
            else if (blinking)
            {
                hull.SetTint(tint, 0.6f);
            }

            for (int i = 0; i < GhostCap; i++)
            {
                if (ghostLife[i] <= 0f) continue;
                ghostLife[i] -= dt;
                if (ghostLife[i] <= 0f) { ghosts[i].Enabled = false; continue; }
                float k = ghostLife[i] / 0.5f;
                ghosts[i].SetTint(new Color(tint.r, tint.g, tint.b, 0.6f * k), 1.2f * k);
                ghosts[i].Tr.localScale = Vector3.one * (1.15f * (1.35f - 0.35f * k));
            }
        }

        /// <summary>Leaves a trail of glowing after-images along a dash.</summary>
        public void Dash(Vector2 from, Vector2 to, float deg)
        {
            for (int i = 0; i < GhostCap; i++)
            {
                float t = i / (float)(GhostCap - 1);
                ghosts[i].Tr.SetPositionAndRotation(Rig.At(Vector2.Lerp(from, to, t), 0.30f), Rig.Facing(deg));
                ghosts[i].Tr.localScale = Vector3.one * 1.15f;
                ghostLife[i] = 0.2f + 0.3f * t;
                ghosts[i].Enabled = true;
            }
        }

        public void ClearGhosts()
        {
            for (int i = 0; i < GhostCap; i++) { ghostLife[i] = 0f; ghosts[i].Enabled = false; }
        }
    }
}
