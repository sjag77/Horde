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
        readonly SpriteRenderer[] glow = new SpriteRenderer[Cap], core = new SpriteRenderer[Cap];
        readonly Transform[] glowTr = new Transform[Cap], coreTr = new Transform[Cap];

        public BossBullets(Game g)
        {
            this.g = g;
            var root = new GameObject("BossBullets").transform;
            for (int i = 0; i < Cap; i++)
            {
                glow[i] = g.NewSprite("BulletGlow", Sprites.Glow, HotColor, 16, root);
                core[i] = g.NewSprite("Bullet", Sprites.Circle, CoreColor, 17, root);
                glowTr[i] = glow[i].transform;
                coreTr[i] = core[i].transform;
                glowTr[i].localScale = Vector3.one * 1.0f;
                coreTr[i].localScale = Vector3.one * 0.32f;
                glow[i].enabled = core[i].enabled = false;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < Cap; i++) glow[i].enabled = core[i].enabled = false;
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
            glow[i].enabled = core[i].enabled = true;
            glowTr[i].position = coreTr[i].position = at;
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
                glowTr[i].position = coreTr[i].position = pos[i];
            }
        }

        void Remove(int i)
        {
            glow[i].enabled = core[i].enabled = false;
            int last = --count;
            if (i == last) return;
            (pos[i], pos[last]) = (pos[last], pos[i]);
            (vel[i], vel[last]) = (vel[last], vel[i]);
            (life[i], life[last]) = (life[last], life[i]);
            (dmg[i], dmg[last]) = (dmg[last], dmg[i]);
            (glow[i], glow[last]) = (glow[last], glow[i]);
            (core[i], core[last]) = (core[last], core[i]);
            (glowTr[i], glowTr[last]) = (glowTr[last], glowTr[i]);
            (coreTr[i], coreTr[last]) = (coreTr[last], coreTr[i]);
        }
    }
}
