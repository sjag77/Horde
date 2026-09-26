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
        readonly Rig[] rig;

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
            rig = new Rig[capacity];

            var root = new GameObject("Projectiles").transform;
            for (int i = 0; i < capacity; i++)
            {
                rig[i] = g.NewRig("Bolt", Models.BoltMesh, BoltColor, root, true);
                rig[i].Enabled = false;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < capacity; i++) rig[i].Enabled = false;
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
            rig[i].Enabled = true;
            rig[i].Invalidate();
            rig[i].SetTint(BoltColor, 1.8f);
            Draw(i);
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
                Draw(i);
                if (Random.value < 0.35f) g.Fx.Burst(pos[i], BoltColor, 1, 1.2f);   // glowing trail
            }
        }

        // Bolts fly nose-first at chest height, stretched along their travel.
        void Draw(int i)
        {
            float deg = Mathf.Atan2(vel[i].y, vel[i].x) * Mathf.Rad2Deg;
            float s = radius[i] * 3.4f;
            rig[i].Place(pos[i], deg, new Vector3(s, s, s * 1.7f), 0.42f);
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
            rig[i].Enabled = false;
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
                (rig[i], rig[last]) = (rig[last], rig[i]);
            }
            count--;
        }
    }
}
