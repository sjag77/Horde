using UnityEngine;

namespace Horde
{
    /// <summary>Pooled projectiles in parallel arrays; hits are found through the enemy grid.</summary>
    public sealed class ProjectileSystem
    {
        static readonly Color BoltColor = new Color(0.7f, 0.97f, 1f);

        readonly Game g;
        readonly int capacity;
        int count;
        readonly Vector2[] pos, vel;
        readonly float[] life, damage, radius;
        readonly int[] bounces, lastHit;
        readonly SpriteRenderer[] sr;
        readonly Transform[] tr;

        public ProjectileSystem(Game g, int capacity)
        {
            this.g = g;
            this.capacity = capacity;
            pos = new Vector2[capacity];
            vel = new Vector2[capacity];
            life = new float[capacity];
            damage = new float[capacity];
            radius = new float[capacity];
            bounces = new int[capacity];
            lastHit = new int[capacity];
            sr = new SpriteRenderer[capacity];
            tr = new Transform[capacity];

            var root = new GameObject("Projectiles").transform;
            for (int i = 0; i < capacity; i++)
            {
                sr[i] = g.NewSprite("Bolt", Sprites.Circle, BoltColor, 15, root);
                tr[i] = sr[i].transform;
                sr[i].enabled = false;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < capacity; i++) sr[i].enabled = false;
            count = 0;
        }

        public void Fire(Vector2 from, Vector2 dir, float speed, float dmg, float r, int bounce)
        {
            if (count >= capacity || dir.sqrMagnitude < 1e-6f) return;
            int i = count++;
            pos[i] = from;
            vel[i] = dir.normalized * speed;
            life[i] = 2.2f;
            damage[i] = dmg;
            radius[i] = r;
            bounces[i] = bounce;
            lastHit[i] = -1;
            sr[i].enabled = true;
            tr[i].localScale = Vector3.one * (r * 2.4f);
            tr[i].position = from;
        }

        public void Tick(float dt)
        {
            var enemies = g.Enemies;
            for (int i = 0; i < count; i++)
            {
                life[i] -= dt;
                pos[i] += vel[i] * dt;
                bool dead = life[i] <= 0f;

                if (!dead)
                {
                    int hit = FindHit(i, enemies);
                    if (hit >= 0)
                    {
                        enemies.Hit(hit, damage[i], pos[i], 9f);   // strong push-back on survivors
                        g.Heroes.OnDirectHit(hit);
                        int next = bounces[i] > 0 ? enemies.Nearest(pos[i], 5f, hit) : -1;
                        if (next >= 0)
                        {
                            bounces[i]--;
                            lastHit[i] = hit;
                            vel[i] = (enemies.Pos[next] - pos[i]).normalized * vel[i].magnitude;
                            life[i] = 1.2f;
                        }
                        else dead = true;
                    }
                }

                if (dead)
                {
                    Remove(i);
                    i--;               // the swapped-in projectile still needs this frame's update
                    continue;
                }
                tr[i].position = pos[i];
            }
        }

        int FindHit(int i, EnemySystem enemies)
        {
            var grid = enemies.Grid;
            Vector2 p = pos[i];
            float reach = radius[i] + 1.6f;   // covers the mini-boss radius
            int x0 = grid.CellX(p.x - reach), x1 = grid.CellX(p.x + reach);
            int y0 = grid.CellY(p.y - reach), y1 = grid.CellY(p.y + reach);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int j = grid.Head(x, y); j != -1; j = grid.Next(j))
                    {
                        if (!enemies.Alive[j] || j == lastHit[i]) continue;
                        float rr = radius[i] + enemies.Radius[j];
                        if ((enemies.Pos[j] - p).sqrMagnitude < rr * rr) return j;
                    }
            return -1;
        }

        void Remove(int i)
        {
            sr[i].enabled = false;
            int last = count - 1;
            if (i != last)
            {
                (pos[i], pos[last]) = (pos[last], pos[i]);
                (vel[i], vel[last]) = (vel[last], vel[i]);
                (life[i], life[last]) = (life[last], life[i]);
                (damage[i], damage[last]) = (damage[last], damage[i]);
                (radius[i], radius[last]) = (radius[last], radius[i]);
                (bounces[i], bounces[last]) = (bounces[last], bounces[i]);
                (lastHit[i], lastHit[last]) = (lastHit[last], lastHit[i]);
                (sr[i], sr[last]) = (sr[last], sr[i]);
                (tr[i], tr[last]) = (tr[last], tr[i]);
            }
            count--;
        }
    }
}
