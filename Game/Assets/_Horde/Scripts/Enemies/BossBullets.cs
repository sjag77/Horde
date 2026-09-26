using UnityEngine;

namespace Horde
{
    /// <summary>Pooled boss projectiles: glowing orbs that damage the hero on contact.</summary>
    public sealed class BossBullets
    {
        const int Cap = 256;
        static readonly Color CoreColor = new Color(1f, 0.93f, 0.8f);
        static readonly Color HotColor = new Color(1f, 0.35f, 0.18f, 0.85f);

        readonly Game g;
        int count;
        readonly Vector2[] pos = new Vector2[Cap], vel = new Vector2[Cap];
        readonly float[] life = new float[Cap], dmg = new float[Cap];
        readonly Rig[] glow = new Rig[Cap], core = new Rig[Cap];
        readonly float[] spin = new float[Cap];

        public BossBullets(Game g)
        {
            this.g = g;
            var root = new GameObject("BossBullets").transform;
            for (int i = 0; i < Cap; i++)
            {
                glow[i] = g.NewRig("BulletGlow", Models.Sphere1, HotColor, root, true);
                core[i] = g.NewRig("Bullet", Models.Sphere1, CoreColor, root, true);
                glow[i].Enabled = core[i].Enabled = false;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < Cap; i++) glow[i].Enabled = core[i].Enabled = false;
            count = 0;
        }

        public void Fire(Vector2 at, Vector2 dir, float speed, float damage)
        {
            if (count >= Cap) return;
            int i = count++;
            pos[i] = at;
            vel[i] = dir.normalized * speed;
            life[i] = 4.5f;
            dmg[i] = damage;
            spin[i] = Random.value * 360f;
            glow[i].Enabled = core[i].Enabled = true;
            glow[i].Invalidate(); core[i].Invalidate();
            glow[i].SetTint(HotColor, 1.6f);
            core[i].SetTint(CoreColor, 2.2f);
            Draw(i);
        }

        public void Tick(float dt)
        {
            var p = g.Player;
            float hitR = Player.Radius + 0.16f;
            float limX = Game.ArenaHalfW + 1f, limY = Game.ArenaHalfH + 1f;
            for (int i = 0; i < count; i++)
            {
                life[i] -= dt;
                pos[i] += vel[i] * dt;
                bool dead = life[i] <= 0f || Mathf.Abs(pos[i].x) > limX || Mathf.Abs(pos[i].y) > limY;
                if (!dead && (pos[i] - p.Pos).sqrMagnitude < hitR * hitR)
                {
                    p.Damage(dmg[i]);
                    g.Fx.Burst(pos[i], HotColor, 6, 4f);
                    dead = true;
                }
                if (dead) { Remove(i); i--; continue; }
                spin[i] += dt * 260f;
                Draw(i);
            }
        }

        // A spinning hot core inside a soft shell, flying at chest height.
        void Draw(int i)
        {
            float pulse = 1f + 0.12f * Mathf.Sin(spin[i] * 0.2f);
            glow[i].Place(pos[i], spin[i], Vector3.one * (0.52f * pulse), 0.45f);
            core[i].Place(pos[i], -spin[i] * 1.6f, Vector3.one * 0.26f, 0.45f);
        }

        void Remove(int i)
        {
            glow[i].Enabled = core[i].Enabled = false;
            int last = --count;
            if (i == last) return;
            (pos[i], pos[last]) = (pos[last], pos[i]);
            (vel[i], vel[last]) = (vel[last], vel[i]);
            (life[i], life[last]) = (life[last], life[i]);
            (dmg[i], dmg[last]) = (dmg[last], dmg[i]);
            (glow[i], glow[last]) = (glow[last], glow[i]);
            (core[i], core[last]) = (core[last], core[i]);
            (spin[i], spin[last]) = (spin[last], spin[i]);
        }
    }
}
