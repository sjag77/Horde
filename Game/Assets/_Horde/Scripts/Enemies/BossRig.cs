using UnityEngine;

namespace Horde
{
    /// <summary>
    /// The boss is the one creature with a real rig: body, head, hinged jaw, two arms and two
    /// legs on their own transforms. It walks, breathes, roars, rears back before a charge and
    /// lunges through it - all driven from a handful of numbers, no animation clips.
    /// </summary>
    public sealed class BossRig
    {
        readonly Transform root;
        readonly Rig body, head, jaw, armL, armR, legL, legR;
        readonly Rig auraL, auraR;      // horn fire, lit while it winds up
        float stride, breathe, jawOpen, rear;

        public BossRig(Game g)
        {
            var go = new GameObject("BossRig");
            root = go.transform;
            body = Part(g, "Body", Models.DemonBody);
            head = Part(g, "Head", Models.DemonHead);
            jaw = Part(g, "Jaw", Models.DemonJaw);
            armL = Part(g, "ArmL", Models.DemonArm);
            armR = Part(g, "ArmR", Models.DemonArm);
            legL = Part(g, "LegL", Models.DemonLeg);
            legR = Part(g, "LegR", Models.DemonLeg);
            auraL = g.NewRig("HornFireL", Models.ShardMesh, new Color(1f, 0.5f, 0.15f), root, true);
            auraR = g.NewRig("HornFireR", Models.ShardMesh, new Color(1f, 0.5f, 0.15f), root, true);
            Show(false);
        }

        Rig Part(Game g, string name, Mesh m) => g.NewRig(name, m, Color.white, root);

        public void Show(bool on)
        {
            body.Enabled = head.Enabled = jaw.Enabled = on;
            armL.Enabled = armR.Enabled = legL.Enabled = legR.Enabled = on;
            if (!on) auraL.Enabled = auraR.Enabled = false;
        }

        /// <summary>
        /// phase: 0 idle/chasing, 1 winding up, 2 charging. speed drives the walk cycle.
        /// </summary>
        public void Place(Vector2 pos, float facingDeg, float scale, float dt, float speed,
                          int phase, float windK, Color tint, float glow)
        {
            stride += dt * (2.5f + speed * 2.4f);
            breathe += dt * 2.1f;
            float want = phase == 1 ? Mathf.Lerp(0.15f, 1f, windK) : phase == 2 ? 1f : 0.1f + 0.08f * Mathf.Sin(breathe);
            jawOpen = Mathf.Lerp(jawOpen, want, 1f - Mathf.Exp(-14f * dt));
            float wantRear = phase == 1 ? windK : phase == 2 ? -0.55f : 0f;
            rear = Mathf.Lerp(rear, wantRear, 1f - Mathf.Exp(-16f * dt));

            root.SetPositionAndRotation(Rig.At(pos), Rig.Facing(facingDeg));
            root.localScale = Vector3.one * scale;

            float swing = Mathf.Sin(stride) * (phase == 2 ? 10f : 26f);
            float bob = Mathf.Abs(Mathf.Cos(stride)) * 0.06f + Mathf.Sin(breathe) * 0.015f;
            float lean = -rear * 26f;      // rears back on the wind-up, pitches forward on the dash

            body.Tr.localPosition = new Vector3(0f, bob, -rear * 0.18f);
            body.Tr.localRotation = Quaternion.Euler(lean, 0f, Mathf.Sin(stride) * 3f);
            body.Tr.localScale = Vector3.one;

            var headPos = new Vector3(0f, 1.72f + bob, 0.18f + rear * 0.08f);
            head.Tr.localPosition = headPos;
            head.Tr.localRotation = Quaternion.Euler(lean * 0.6f - jawOpen * 12f, Mathf.Sin(breathe * 0.7f) * 4f, 0f);
            head.Tr.localScale = Vector3.one;

            jaw.Tr.localPosition = headPos + head.Tr.localRotation * new Vector3(0f, -0.14f, 0.06f);
            jaw.Tr.localRotation = head.Tr.localRotation * Quaternion.Euler(jawOpen * 34f, 0f, 0f);
            jaw.Tr.localScale = Vector3.one;

            ArmAt(armL, -0.66f, 1.34f + bob, swing, lean);
            ArmAt(armR, 0.66f, 1.34f + bob, -swing, lean);
            LegAt(legL, -0.26f, 0.62f + bob * 0.4f, -swing);
            LegAt(legR, 0.26f, 0.62f + bob * 0.4f, swing);

            // horns catch fire as the charge builds - the tell you can read across the screen
            bool fire = phase == 1 || phase == 2;
            auraL.Enabled = auraR.Enabled = fire;
            if (fire)
            {
                float f = phase == 2 ? 1f : windK;
                HornFire(auraL, -0.55f, 2.35f + bob, f);
                HornFire(auraR, 0.55f, 2.35f + bob, f);
            }

            body.SetTint(tint, glow);
            head.SetTint(tint, glow);
            jaw.SetTint(tint, glow);
            armL.SetTint(tint, glow);
            armR.SetTint(tint, glow);
            legL.SetTint(tint, glow);
            legR.SetTint(tint, glow);
        }

        void ArmAt(Rig r, float x, float y, float swing, float lean)
        {
            r.Tr.localPosition = new Vector3(x, y, 0f);
            r.Tr.localRotation = Quaternion.Euler(swing * 0.7f + lean * 0.4f, 0f, -Mathf.Sign(x) * 8f);
            r.Tr.localScale = Vector3.one;
        }

        void LegAt(Rig r, float x, float y, float swing)
        {
            r.Tr.localPosition = new Vector3(x, y, 0f);
            r.Tr.localRotation = Quaternion.Euler(swing, 0f, 0f);
            r.Tr.localScale = Vector3.one;
        }

        void HornFire(Rig r, float x, float y, float f)
        {
            r.Tr.localPosition = new Vector3(x, y, -0.1f);
            r.Tr.localRotation = Quaternion.Euler(-70f, 0f, 0f);
            r.Tr.localScale = Vector3.one * (0.6f + 1.5f * f) * (0.85f + 0.25f * Mathf.Sin(Time.time * 26f + x));
            r.SetTint(new Color(1f, 0.45f + 0.3f * f, 0.12f), 1.4f + f);
        }
    }
}
