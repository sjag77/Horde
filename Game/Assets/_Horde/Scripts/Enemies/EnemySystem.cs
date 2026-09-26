using UnityEngine;

namespace Horde
{
    public enum EnemyKind : byte { Grunt, Runner, Hound, Spitter, Splitter, Mite, Brute, Boss }

    /// <summary>
    /// Every enemy lives in parallel arrays updated in one loop. Dead enemies are only flagged
    /// during a frame and compacted at the start of the next, so the indices stored in the
    /// spatial grid stay valid for everything that queries it this frame.
    /// </summary>
    public sealed class EnemySystem
    {
        static readonly Color SwarmerColor = new Color(1f, 0.27f, 0.36f);   // Grunt
        static readonly Color RunnerColor = new Color(1f, 0.55f, 0.25f);
        static readonly Color HoundColor = new Color(0.95f, 0.78f, 0.35f);
        static readonly Color SpitterColor = new Color(0.35f, 0.9f, 0.75f);
        static readonly Color SplitterColor = new Color(0.6f, 1f, 0.35f);
        static readonly Color MiteColor = new Color(0.8f, 1f, 0.6f);
        static readonly Color BruteColor = new Color(0.68f, 0.46f, 1f);
        static readonly Color SlowTint = new Color(0.55f, 0.8f, 1f);
        const float SlowFactor = 0.4f;
        static readonly Color BurnTint = new Color(1f, 0.45f, 0.1f);
        static readonly Color BurnHot = new Color(1f, 0.85f, 0.3f);
        static readonly Color PoisonTint = new Color(0.45f, 1f, 0.3f);
        static readonly Color OilTint = new Color(0.25f, 0.17f, 0.08f);
        static readonly Color BossColor = new Color(1f, 0.72f, 0.18f);
        static readonly float[] BossTimes = { 90f, 240f, 390f, 540f };   // 1:30, 4:00, 6:30, 9:00 — then endless

        readonly Game g;
        public readonly int Capacity;
        public int Count { get; private set; }

        public readonly Vector2[] Pos;
        public readonly float[] Radius;
        public readonly float[] OrbitCooldown;
        public readonly float[] WaveCooldown;
        public readonly bool[] Alive;
        readonly Vector2[] knock;
        readonly float[] hp, speed, contact, flash, slow;
        readonly float[] burn, burnDps, poison, poisonDps, oil, dotAcc, dotTick;
        readonly float[] atkCd, face;   // spitter reload, drawn facing
        readonly EnemyKind[] kind;
        readonly Rig[] rig;              // one 3D model per enemy, one draw call each
        readonly float[] anim;           // walk-cycle phase
        BossRig bossRig;
        public readonly SpatialGrid Grid;
        float spawnBudget;

        // One mini-boss at a time: chase, wind up (slow, flashing), then dash in a straight line.
        int bossesSpawned;
        byte bossPhase;
        float bossTimer, bossMaxHp;
        bool bossNeedsDir;
        int bossTier, bossDashesLeft;
        float bossAge, bossBaseContact, bossBaseSpeed;
        bool bossRageWarned;
        const float BossWarnTime = 10f;
        bool bossPointChosen;
        SpriteRenderer bossMarker;
        public Vector2 NextBossPoint { get; private set; }
        public bool BossPointKnown => bossPointChosen;
        public Vector2 BossPos { get; private set; }
        public int BossesSpawned => bossesSpawned;
        public int BossesKilled { get; private set; }
        public int BossTotal => BossTimes.Length;
        public float BossTime(int i) => BossTimes[i];
        Vector2 bossDashDir, bossDashTarget, bossAimDir;
        float bossDashSpeed, bossWind, bossWindMax;
        SpriteRenderer dashMarker, chargeBeam, chargeGlow;
        public bool BossAlive { get; private set; }
        public float BossHpFrac { get; private set; }

        public EnemySystem(Game g, int capacity)
        {
            this.g = g;
            Capacity = capacity;
            Pos = new Vector2[capacity];
            Radius = new float[capacity];
            OrbitCooldown = new float[capacity];
            WaveCooldown = new float[capacity];
            Alive = new bool[capacity];
            knock = new Vector2[capacity];
            hp = new float[capacity];
            speed = new float[capacity];
            contact = new float[capacity];
            flash = new float[capacity];
            slow = new float[capacity];
            burn = new float[capacity];
            burnDps = new float[capacity];
            poison = new float[capacity];
            poisonDps = new float[capacity];
            oil = new float[capacity];
            dotAcc = new float[capacity];
            dotTick = new float[capacity];
            atkCd = new float[capacity];
            face = new float[capacity];
            kind = new EnemyKind[capacity];
            rig = new Rig[capacity];
            anim = new float[capacity];

            var root = new GameObject("Enemies").transform;
            for (int i = 0; i < capacity; i++)
            {
                rig[i] = g.NewRig("Enemy", Models.Grunt, SwarmerColor, root);
                rig[i].Enabled = false;
            }
            bossRig = new BossRig(g);
            Grid = new SpatialGrid(Game.ArenaHalfW, Game.ArenaHalfH, 3f, 1.2f, capacity);
            bossMarker = g.NewSprite("BossLanding", Sprites.Ring, BossColor, 4, root);
            bossMarker.enabled = false;
            // The charge is told by the boss itself: a collapsing ring, a swelling glow and a
            // spear of light along the direction it is about to hurl itself. No ground marker.
            dashMarker = g.NewSprite("BossChargeRing", Sprites.Ring, new Color(1f, 0.25f, 0.2f), 14, root);
            chargeBeam = g.NewSprite("BossChargeBeam", Sprites.Square, new Color(1f, 0.3f, 0.15f), 6, root);
            chargeGlow = g.NewSprite("BossChargeGlow", Sprites.Glow, new Color(1f, 0.35f, 0.1f), 9, root);
            dashMarker.enabled = chargeBeam.enabled = chargeGlow.enabled = false;
        }

        public void Reset()
        {
            for (int i = 0; i < Capacity; i++)
            {
                rig[i].Enabled = false;
                Alive[i] = false;
            }
            bossRig.Show(false);
            Count = 0;
            spawnBudget = 0f;
            bossesSpawned = 0;
            bossPhase = 0;
            bossTimer = 3f;
            BossAlive = false;
            BossHpFrac = 0f;
            bossPointChosen = false;
            bossMarker.enabled = false;
            BossesKilled = 0;
            dashMarker.enabled = chargeBeam.enabled = chargeGlow.enabled = false;
            Grid.Clear();
        }

        public void Tick(float dt)
        {
            Compact();

            float t = g.RunTime;
            spawnBudget += (Mathf.Min(1.3f + t * 0.05f, 16f) + Mathf.Max(0f, t - 540f) * 0.04f) * dt;   // enemies per second, ramping
            while (spawnBudget >= 1f)
            {
                spawnBudget -= 1f;
                Spawn(t);
            }

            if (bossesSpawned < BossTimes.Length)
            {
                float until = BossTimes[bossesSpawned] - t;
                if (until <= BossWarnTime && !bossPointChosen)
                {
                    // Pick the landing spot early so it shows on the map (and the ground) before the boss arrives.
                    bossPointChosen = true;
                    NextBossPoint = PickBossPoint();
                    bossMarker.transform.position = NextBossPoint;
                    bossMarker.enabled = true;
                    g.Hud.Banner("BOSS " + (bossesSpawned + 1) + " LANDING SOON", BossColor);
                    g.Sfx.Play(Sound.BossWarn, 0f);
                }
                if (bossPointChosen)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * 10f);
                    bossMarker.transform.localScale = Vector3.one * (3.2f + pulse * 0.6f);
                    var mc = BossColor;
                    mc.a = 0.35f + 0.5f * pulse;
                    bossMarker.color = mc;
                }
                if (until <= 0f)
                {
                    if (Count >= Capacity) { Alive[Count - 1] = false; rig[Count - 1].Enabled = false; Count--; }
                    Spawn(t, true, NextBossPoint);
                    bossesSpawned++;
                    bossPointChosen = false;
                    bossMarker.enabled = false;
                    g.Hud.Banner("BOSS " + bossesSpawned + " HAS LANDED", BossColor);
                    g.Sfx.Play(Sound.Boom, 0f);
                    g.Shake(0.5f);
                }
            }

            BossBrain(dt, t);

            Vector2 target = g.Player.Pos;
            float knockDecay = Mathf.Exp(-8f * dt);
            for (int i = 0; i < Count; i++)
            {
                Vector2 d = target - Pos[i];
                float len = d.magnitude;
                float mul = slow[i] > 0f ? SlowFactor : 1f;
                if (kind[i] == EnemyKind.Boss)
                {
                    if (bossPhase == 1 || bossPhase == 3) { mul = 0.1f; flash[i] = Mathf.Max(flash[i], 0.5f); }
                    else if (bossPhase == 2)
                    {
                        // the charge was aimed when the wind-up began: it goes there, not after you
                        Vector2 to = bossDashTarget - Pos[i];
                        float dist = to.magnitude;
                        if (dist > 0.05f) Pos[i] += to / dist * Mathf.Min(bossDashSpeed * dt, dist);
                        mul = 0f;
                    }
                }
                if (len > 0.001f) Pos[i] += d * (speed[i] * mul * dt / len);
                Pos[i] += knock[i] * dt;
                knock[i] *= knockDecay;
            }

            Grid.Clear();
            for (int i = 0; i < Count; i++) Grid.Insert(i, Pos[i]);
            Separate();

            bool bossSeen = false;
            for (int i = 0; i < Count; i++)
            {
                if (kind[i] == EnemyKind.Boss && Alive[i]) { bossSeen = true; BossHpFrac = Mathf.Clamp01(hp[i] / bossMaxHp); BossPos = Pos[i]; }
                float rr = Radius[i] + Player.Radius;
                if ((Pos[i] - target).sqrMagnitude < rr * rr && g.Player.Damage(contact[i]))
                    SlowAround(target, 2.4f, 1.8f);   // getting hit opens an escape route
                // creatures turn to face where they are heading; the boss always faces the hero
                Vector2 look = target - Pos[i];
                if (look.sqrMagnitude > 0.0004f)
                {
                    float want = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
                    face[i] = Mathf.LerpAngle(face[i], want, 1f - Mathf.Exp(-9f * dt));
                }
                if (kind[i] == EnemyKind.Spitter) TickSpitter(i, dt, target);
                if (kind[i] != EnemyKind.Boss) Animate(i, dt);
                bool hadStatus = slow[i] > 0f || burn[i] > 0f || poison[i] > 0f || oil[i] > 0f;
                if (WaveCooldown[i] > 0f) WaveCooldown[i] -= dt;
                if (slow[i] > 0f) slow[i] -= dt;
                if (oil[i] > 0f) oil[i] -= dt;
                if (burn[i] > 0f || poison[i] > 0f) TickDot(i, dt);
                if (!Alive[i]) continue;
                if (flash[i] > 0f) flash[i] -= dt * 6f;
                if (kind[i] != EnemyKind.Boss)
                    rig[i].SetLook(RenderTint(i), 0f, Mathf.Clamp01(flash[i]) * 0.85f);
            }
            BossAlive = bossSeen;
        }

        // Every creature walks: a bob on each footfall, a forward lean, a roll into the turn,
        // and a squash when something hits it. One transform, no animation clips.
        void Animate(int i, float dt)
        {
            float sp = speed[i] * (slow[i] > 0f ? SlowFactor : 1f);
            anim[i] += dt * (2.2f + sp * 3.4f);
            float step = Mathf.Sin(anim[i]);
            float hit = Mathf.Clamp01(flash[i]);
            float size = Radius[i] * VisualScale(kind[i]);

            float bob = Mathf.Abs(Mathf.Cos(anim[i])) * 0.09f * size;
            float lean = 9f + step * 7f + hit * -14f;          // recoils when struck
            float roll = step * (kind[i] == EnemyKind.Hound ? 4f : 8f);
            var scale = new Vector3(size * (1f + hit * 0.22f), size * (1f - hit * 0.16f), size * (1f + hit * 0.22f));
            rig[i].Place(Pos[i], face[i], scale, bob, lean, roll);
        }

        // Spitters keep their distance and lob acid at you.
        void TickSpitter(int i, float dt, Vector2 target)
        {
            atkCd[i] -= dt * (slow[i] > 0f ? 0.4f : 1f);
            if (atkCd[i] > 0f) return;
            Vector2 d = target - Pos[i];
            float dist = d.magnitude;
            if (dist > 13f || dist < 1.2f) { atkCd[i] = 0.4f; return; }
            atkCd[i] = 2.4f;
            Vector2 dir = d / dist;
            g.BossBullets.Fire(Pos[i] + dir * (Radius[i] + 0.2f), dir, 7.5f, 9f * (1f + g.RunTime / 240f));
            g.Fx.Burst(Pos[i] + dir * 0.4f, SpitterColor, 4, 3f);
            g.Sfx.Play(Sound.Shoot, 0.25f);
        }

        public void Hit(int i, float damage, Vector2 from, float knockback, bool showNumber = true)
        {
            if (!Alive[i]) return;
            hp[i] -= damage;
            flash[i] = 1f;
            Vector2 d = Pos[i] - from;
            float len = d.magnitude;
            if (len > 0.001f) knock[i] += d / len * KnockMul(i) * knockback;
            if (showNumber) g.Numbers.Damage(Pos[i], damage);
            g.Fx.Burst(Pos[i], BaseColor(i), 3, 4f);
            g.Sfx.Play(Sound.Hit);
            if (hp[i] <= 0f) Kill(i);
        }

        /// <summary>Continuous damage with no number spam - used by Void Vortex.</summary>
        public void Grind(int i, float damage)
        {
            if (!Alive[i]) return;
            hp[i] -= damage;
            flash[i] = Mathf.Max(flash[i], 0.3f);
            dotAcc[i] += damage;
            if (dotAcc[i] > 0f && Random.value < 0.04f) { g.Numbers.Damage(Pos[i], dotAcc[i]); dotAcc[i] = 0f; }
            if (hp[i] <= 0f) Kill(i);
        }

        /// <summary>Shove an enemy directly (Void Vortex's pull); bosses barely budge.</summary>
        public void Drag(int i, Vector2 delta)
        {
            if (Alive[i]) Pos[i] += delta * KnockMul(i);
        }

        public int Nearest(Vector2 p, float maxDist, int exclude = -1)
        {
            int best = -1;
            float bestD = maxDist * maxDist;
            for (int i = 0; i < Count; i++)
            {
                if (!Alive[i] || i == exclude) continue;
                float d = (Pos[i] - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void Kill(int i)
        {
            Alive[i] = false;
            rig[i].Enabled = false;
            g.Kills++;
            if (kind[i] == EnemyKind.Boss) { bossRig.Show(false); KillBoss(i); return; }
            EnemyKind k = kind[i];
            bool big = k == EnemyKind.Brute || k == EnemyKind.Splitter;
            float xp = k switch
            {
                EnemyKind.Brute => 10f,
                EnemyKind.Splitter => 5f,
                EnemyKind.Spitter => 4f,
                EnemyKind.Hound => 3f,
                EnemyKind.Runner => 2.5f,
                EnemyKind.Mite => 1f,
                _ => 2f
            };
            Vector2 at = Pos[i];
            g.Xp.Drop(at, xp);   // fewer, harder kills still level you up
            g.Fx.Burst(at, BaseColor(i), big ? 16 : 8, 6f);
            g.Fx.Pop(at, BaseColor(i), Radius[i] * 4f);
            g.Sfx.Play(Sound.Kill);
            if (big) g.Shake(0.18f);
            g.Pickups.MaybeDrop(at, k == EnemyKind.Brute);
            if (k == EnemyKind.Splitter) SplitInto(at, 3, EnemyKind.Mite);
            else if (k == EnemyKind.Mite && Random.value < 0.22f) SplitInto(at, 2, EnemyKind.Mite);   // a colony that fights back
        }

        // A popped Splitter scatters a colony: clear them fast or they swarm you.
        void SplitInto(Vector2 at, int n, EnemyKind k)
        {
            float t = g.RunTime;
            for (int c = 0; c < n && Count < Capacity; c++)
            {
                float a = Random.value * Mathf.PI * 2f;
                Spawn(t, false, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.45f + Random.value * 0.3f), k);
            }
            g.Fx.Pop(at, SplitterColor, 2.2f);
        }

        void Spawn(float t, bool boss = false, Vector2? at = null, EnemyKind? force = null)
        {
            if (Count >= Capacity) return;

            float halfH = g.ViewHalfH, halfW = g.ViewHalfW;
            Vector2 cam = g.CamFocus;
            float ring = Mathf.Sqrt(halfW * halfW + halfH * halfH) + 1.2f;
            Vector2 p = g.Player.Pos;
            for (int tries = 0; tries < 8; tries++)
            {
                float a = Random.value * Mathf.PI * 2f;
                p = g.Player.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ring;
                p.x = Mathf.Clamp(p.x, -Game.ArenaHalfW + 0.5f, Game.ArenaHalfW - 0.5f);
                p.y = Mathf.Clamp(p.y, -Game.ArenaHalfH + 0.5f, Game.ArenaHalfH - 0.5f);
                if (Mathf.Abs(p.x - cam.x) > halfW + 0.6f || Mathf.Abs(p.y - cam.y) > halfH + 0.6f) break;
            }
            if (at.HasValue) p = at.Value;

            float scale = 1f + t / 60f * 0.2f + Mathf.Max(0f, t - 90f) / 60f * 0.7f + Mathf.Max(0f, t - 540f) / 60f * 1.2f;
            EnemyKind k = boss ? EnemyKind.Boss : force ?? PickKind(t);

            int i = Count++;
            kind[i] = k;
            Pos[i] = p;
            knock[i] = Vector2.zero;
            // Grunt HP is the yardstick the whole kit is balanced against: Spark Lv1 can't
            // one-shot one, Lv2 can - until run time outgrows it.
            switch (k)
            {
                case EnemyKind.Runner:   Radius[i] = 0.26f; hp[i] = 16f;  speed[i] = 3.5f + Random.value * 0.4f; contact[i] = 7f;  break;
                case EnemyKind.Hound:    Radius[i] = 0.33f; hp[i] = 26f;  speed[i] = 3.0f + Random.value * 0.3f; contact[i] = 10f; break;
                case EnemyKind.Spitter:  Radius[i] = 0.36f; hp[i] = 34f;  speed[i] = 1.5f;                       contact[i] = 9f;  break;
                case EnemyKind.Splitter: Radius[i] = 0.46f; hp[i] = 58f;  speed[i] = 1.7f;                       contact[i] = 11f; break;
                case EnemyKind.Mite:     Radius[i] = 0.19f; hp[i] = 11f;  speed[i] = 3.3f + Random.value * 0.5f; contact[i] = 5f;  break;
                case EnemyKind.Brute:    Radius[i] = 0.55f; hp[i] = 160f; speed[i] = 1.2f;                       contact[i] = 18f; break;
                default:                 Radius[i] = 0.30f; hp[i] = 23f;  speed[i] = 2.1f + Random.value * 0.35f; contact[i] = 8f; break;
            }
            hp[i] *= scale;
            contact[i] *= 1f + t / 60f * 0.12f;
            atkCd[i] = 1.2f + Random.value;
            face[i] = 0f;
            flash[i] = 0f;
            slow[i] = 0f;
            burn[i] = burnDps[i] = poison[i] = poisonDps[i] = oil[i] = dotAcc[i] = 0f;
            dotTick[i] = 0.5f;
            OrbitCooldown[i] = WaveCooldown[i] = 0f;
            Alive[i] = true;

            if (boss)
            {
                bossTier = bossesSpawned;                        // every boss is bigger, tougher and faster
                Radius[i] = 1.05f + 0.1f * bossTier;
                hp[i] = bossMaxHp = 2000f * scale * (1f + 0.9f * bossTier);
                speed[i] = 1.75f + 0.15f * bossTier;
                contact[i] = 30f + 12f * bossTier;
                bossPhase = 0;
                bossTimer = 2.5f;
                bossAge = 0f;
                bossBaseContact = contact[i];
                bossBaseSpeed = speed[i];
                bossRageWarned = false;
            }

            anim[i] = Random.value * 10f;
            bool isBoss = kind[i] == EnemyKind.Boss;
            rig[i].Enabled = !isBoss;                       // the boss is drawn by its own rig
            rig[i].Mesh = KindMesh(kind[i]);
            rig[i].Invalidate();
            rig[i].SetLook(Color.white, 0f, 0f);
            rig[i].Place(p, 90f, Radius[i] * VisualScale(kind[i]));
            if (isBoss) bossRig.Show(true);
        }

        // The horde changes shape as the run goes on: grunts, then runners and hounds, then
        // spitters that shoot and splitters that leave a colony behind when they pop.
        EnemyKind PickKind(float t)
        {
            float runner = t > 25f ? Mathf.Min(0.30f, 0.08f + (t - 25f) / 900f) : 0f;
            float hound = t > 60f ? Mathf.Min(0.24f, 0.06f + (t - 60f) / 1100f) : 0f;
            float brute = t > 75f ? Mathf.Min(0.18f, 0.04f + (t - 75f) / 1400f) : 0f;
            float splitter = t > 110f ? Mathf.Min(0.18f, 0.05f + (t - 110f) / 1300f) : 0f;
            float spitter = t > 150f ? Mathf.Min(0.14f, 0.04f + (t - 150f) / 1600f) : 0f;
            float r = Random.value;
            if ((r -= runner) < 0f) return EnemyKind.Runner;
            if ((r -= hound) < 0f) return EnemyKind.Hound;
            if ((r -= brute) < 0f) return EnemyKind.Brute;
            if ((r -= splitter) < 0f) return EnemyKind.Splitter;
            if ((r -= spitter) < 0f) return EnemyKind.Spitter;
            return EnemyKind.Grunt;
        }

        // World size of one model unit, per creature: models are roughly 1 unit tall.
        static float VisualScale(EnemyKind k) => k switch
        {
            EnemyKind.Boss => 2.3f,
            EnemyKind.Brute => 2.6f,
            EnemyKind.Hound => 3.8f,
            EnemyKind.Runner => 4.3f,
            EnemyKind.Spitter => 3.2f,
            EnemyKind.Splitter => 3.0f,
            EnemyKind.Mite => 4.2f,
            _ => 4.0f
        };

        // Push overlapping enemies apart so the horde reads as a crowd instead of a single stack.
        void Separate()
        {
            for (int i = 0; i < Count; i++)
            {
                int cx = Grid.CellX(Pos[i].x), cy = Grid.CellY(Pos[i].y);
                int y0 = Mathf.Max(0, cy - 1), y1 = Mathf.Min(Grid.Rows - 1, cy + 1);
                int x0 = Mathf.Max(0, cx - 1), x1 = Mathf.Min(Grid.Cols - 1, cx + 1);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        for (int j = Grid.Head(x, y); j != -1; j = Grid.Next(j))
                        {
                            if (j <= i) continue;
                            Vector2 d = Pos[j] - Pos[i];
                            float min = Radius[i] + Radius[j];
                            float d2 = d.sqrMagnitude;
                            if (d2 >= min * min || d2 < 1e-6f) continue;
                            float dist = Mathf.Sqrt(d2);
                            Vector2 push = d / dist * ((min - dist) * 0.5f);
                            Pos[i] -= push;
                            Pos[j] += push;
                        }
            }
        }

        void Compact()
        {
            int i = 0;
            while (i < Count)
            {
                if (Alive[i]) { i++; continue; }
                rig[i].Enabled = false;
                int last = Count - 1;
                if (i != last) Swap(i, last);
                Count--;
            }
        }

        void Swap(int a, int b)
        {
            (Pos[a], Pos[b]) = (Pos[b], Pos[a]);
            (Radius[a], Radius[b]) = (Radius[b], Radius[a]);
            (OrbitCooldown[a], OrbitCooldown[b]) = (OrbitCooldown[b], OrbitCooldown[a]);
            (WaveCooldown[a], WaveCooldown[b]) = (WaveCooldown[b], WaveCooldown[a]);
            (Alive[a], Alive[b]) = (Alive[b], Alive[a]);
            (knock[a], knock[b]) = (knock[b], knock[a]);
            (hp[a], hp[b]) = (hp[b], hp[a]);
            (speed[a], speed[b]) = (speed[b], speed[a]);
            (contact[a], contact[b]) = (contact[b], contact[a]);
            (flash[a], flash[b]) = (flash[b], flash[a]);
            (slow[a], slow[b]) = (slow[b], slow[a]);
            (burn[a], burn[b]) = (burn[b], burn[a]);
            (burnDps[a], burnDps[b]) = (burnDps[b], burnDps[a]);
            (poison[a], poison[b]) = (poison[b], poison[a]);
            (poisonDps[a], poisonDps[b]) = (poisonDps[b], poisonDps[a]);
            (oil[a], oil[b]) = (oil[b], oil[a]);
            (dotAcc[a], dotAcc[b]) = (dotAcc[b], dotAcc[a]);
            (dotTick[a], dotTick[b]) = (dotTick[b], dotTick[a]);
            (atkCd[a], atkCd[b]) = (atkCd[b], atkCd[a]);
            (face[a], face[b]) = (face[b], face[a]);
            (kind[a], kind[b]) = (kind[b], kind[a]);
            (rig[a], rig[b]) = (rig[b], rig[a]);
            (anim[a], anim[b]) = (anim[b], anim[a]);
        }

        static Color KindColor(EnemyKind k) => k switch
        {
            EnemyKind.Boss => BossColor,
            EnemyKind.Brute => BruteColor,
            EnemyKind.Runner => RunnerColor,
            EnemyKind.Hound => HoundColor,
            EnemyKind.Spitter => SpitterColor,
            EnemyKind.Splitter => SplitterColor,
            EnemyKind.Mite => MiteColor,
            _ => SwarmerColor
        };

        static Mesh KindMesh(EnemyKind k) => k switch
        {
            EnemyKind.Boss => Models.DemonBody,
            EnemyKind.Brute => Models.Ogre,
            EnemyKind.Runner => Models.Runner,
            EnemyKind.Hound => Models.Hound,
            EnemyKind.Spitter => Models.Spitter,
            EnemyKind.Splitter => Models.Splitter,
            EnemyKind.Mite => Models.Mite,
            _ => Models.Grunt
        };

        // Kind colours are only used for hit sparks and death bursts now: the models carry
        // their own skin, iron and bone, and the render tint stays white unless a status
        // effect is painting them (frost, poison, oil, fire).
        Color BaseColor(int i) => KindColor(kind[i]);

        Color RenderTint(int i)
        {
            Color c = Color.white;
            if (slow[i] > 0f) c = Color.Lerp(c, SlowTint, 0.65f);
            if (oil[i] > 0f) c = Color.Lerp(c, OilTint, 0.6f);
            if (poison[i] > 0f) c = Color.Lerp(c, PoisonTint, 0.5f + 0.2f * Mathf.Sin(g.RunTime * 12f + i));
            if (burn[i] > 0f) c = Color.Lerp(c, BurnHot, 0.45f + 0.3f * Mathf.Sin(g.RunTime * 22f + i));
            return c;
        }
        float KnockMul(int i) => kind[i] switch
        {
            EnemyKind.Boss => 0.03f,
            EnemyKind.Brute => 0.3f,
            EnemyKind.Splitter => 0.45f,
            EnemyKind.Mite => 1.4f,
            _ => 1f
        };

        public void Slow(int i, float seconds)
        {
            if (Alive[i]) slow[i] = Mathf.Max(slow[i], seconds);
        }

        /// <summary>Hits every enemy within radius (used by the ultimate). Returns how many were hit.</summary>
        public int DamageAll(Vector2 center, float radius, float damage, float knockback, float slowSeconds)
        {
            int n = 0;
            float r2 = radius * radius;
            for (int i = 0; i < Count; i++)
            {
                if (!Alive[i] || (Pos[i] - center).sqrMagnitude > r2) continue;
                slow[i] = Mathf.Max(slow[i], slowSeconds);
                bool isBoss = kind[i] == EnemyKind.Boss;
                Hit(i, isBoss ? Mathf.Min(damage, bossMaxHp * 0.08f) : damage, center, knockback, isBoss);   // the ultimate never one-shots a boss
                n++;
            }
            return n;
        }

        // Boss 1 only dashes; boss 2 adds bullet rings; boss 3+ also summons, and later bosses
        // chain more dashes, fire denser double rings and attack more often. Below 50% HP, enraged.
        void BossBrain(float dt, float t)
        {
            int b = -1;
            for (int i = 0; i < Count; i++) if (kind[i] == EnemyKind.Boss && Alive[i]) { b = i; break; }
            if (b < 0) { dashMarker.enabled = chargeBeam.enabled = chargeGlow.enabled = false; return; }

            // Dodging the fight doesn't work: a living boss hits harder, moves faster and attacks more
            // often every second. Around 2-3 minutes in, one contact is lethal.
            bossAge += dt;
            float rage = 1f + bossAge / 30f;
            contact[b] = bossBaseContact * rage;
            speed[b] = bossBaseSpeed * Mathf.Min(2.2f, 1f + bossAge / 60f);
            if (!bossRageWarned && bossAge > 40f)
            {
                bossRageWarned = true;
                g.Hud.Banner("THE BOSS IS ENRAGING - KILL IT!", new Color(1f, 0.3f, 0.3f));
                g.Sfx.Play(Sound.BossWarn, 0f);
            }
            TickChargeTell(b, dt, t);
            bool enraged = hp[b] < bossMaxHp * 0.5f;
            bossTimer -= dt * (enraged ? 1.6f : 1f) * Mathf.Min(2f, 1f + bossAge / 90f);
            if (bossTimer > 0f) return;

            int tier = bossTier;
            float rest = Mathf.Max(1.2f, 2.8f - tier * 0.35f);
            switch (bossPhase)
            {
                case 0:
                    int roll = Random.Range(0, Mathf.Min(3, 1 + tier));
                    if (roll == 0)
                    {
                        bossDashesLeft = tier >= 3 ? 3 : tier >= 1 ? 2 : 1;
                        bossPhase = 1;
                        bossTimer = bossWindMax = 0.85f;
                        bossWind = 0f;
                        LockCharge(b);
                    }
                    else if (roll == 1) { Volley(b, tier); bossPhase = 3; bossTimer = 0.8f; }
                    else { Summon(b, tier, t); bossPhase = 3; bossTimer = 0.9f; }
                    break;
                case 1:
                    bossPhase = 2;
                    bossTimer = 0.45f;
                    g.Shake(0.3f);
                    g.Sfx.Play(Sound.Boom, 0.1f);
                    bossDashSpeed = Mathf.Max(speed[b] * 5f, (bossDashTarget - Pos[b]).magnitude / 0.4f);
                    break;
                case 2:
                    if (--bossDashesLeft > 0) { bossPhase = 1; bossTimer = bossWindMax = 0.55f; bossWind = 0f; LockCharge(b); }
                    else { bossPhase = 0; bossTimer = rest; }
                    break;
                default:
                    bossPhase = 0;
                    bossTimer = rest;
                    break;
            }
        }

        // Wind-up you can read without looking at the floor: the boss rears back, a ring
        // slams shut on it, and a spear of light shows exactly where it is about to go.
        void TickChargeTell(int b, float dt, float t)
        {
            bool winding = bossPhase == 1, dashing = bossPhase == 2;
            dashMarker.enabled = chargeGlow.enabled = winding;
            chargeBeam.enabled = winding || dashing;
            if (winding) bossWind += dt;
            float windK = bossWindMax > 0f ? Mathf.Clamp01(bossWind / bossWindMax) : 1f;
            float ang = Mathf.Atan2(bossAimDir.y, bossAimDir.x) * Mathf.Rad2Deg;
            float bossSpeed = bossPhase == 2 ? 6f : speed[b];
            bossRig.Place(Pos[b], bossPhase == 0 || bossPhase == 3 ? face[b] : ang,
                          Radius[b] * VisualScale(EnemyKind.Boss), dt, bossSpeed, bossPhase,
                          windK, RenderTint(b), winding ? windK * 0.35f : 0f, Mathf.Clamp01(flash[b]) * 0.85f);

            if (winding)
            {
                float k = windK;
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * 30f);

                dashMarker.transform.position = Pos[b];
                dashMarker.transform.localScale = Vector3.one * (Radius[b] * 2f * Mathf.Lerp(3.4f, 1.15f, k * k));
                dashMarker.color = new Color(1f, 0.28f, 0.16f, 0.35f + 0.6f * k);

                chargeGlow.transform.position = Pos[b];
                chargeGlow.transform.localScale = Vector3.one * (Radius[b] * 5f * (0.8f + 0.5f * k));
                chargeGlow.color = new Color(1f, 0.35f, 0.1f, (0.2f + 0.45f * k) * (0.7f + 0.3f * pulse));

                if (Random.value < dt * 30f)
                    g.Fx.Burst(Pos[b] + Random.insideUnitCircle * Radius[b] * 1.4f, new Color(1f, 0.4f, 0.12f), 2, 5f);

                float len = Mathf.Lerp(1.5f, 9f, k);
                Beam(Pos[b], bossAimDir, len, Radius[b] * (1.1f + 0.5f * k), 0.25f + 0.55f * k);
            }
            else if (dashing)
            {
                Beam(Pos[b] - bossAimDir * 4f, bossAimDir, 8f, Radius[b] * 1.6f, 0.4f);
                if (Random.value < dt * 60f)
                    g.Fx.Burst(Pos[b] - bossAimDir * Radius[b], new Color(1f, 0.55f, 0.2f), 3, 7f);
            }
            else bossWind = 0f;
        }

        void Beam(Vector2 from, Vector2 dir, float len, float width, float alpha)
        {
            var trm = chargeBeam.transform;
            trm.position = from + dir * (len * 0.5f);
            trm.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            trm.localScale = new Vector3(len, width, 1f);
            chargeBeam.color = new Color(1f, 0.35f, 0.15f, alpha);
        }

        void Volley(int b, int tier)
        {
            int n = 12 + tier * 4;
            float speed = 4.5f + tier * 0.6f, damage = (16f + tier * 6f) * (1f + bossAge / 30f), offset = Random.value * 360f;
            int rings = tier >= 3 ? 2 : 1;
            for (int r = 0; r < rings; r++)
                for (int k = 0; k < n; k++)
                {
                    float a = (offset + (k + r * 0.5f) * 360f / n) * Mathf.Deg2Rad;
                    g.BossBullets.Fire(Pos[b], new Vector2(Mathf.Cos(a), Mathf.Sin(a)), speed * (r == 0 ? 1f : 0.65f), damage);
                }
            g.Fx.Pop(Pos[b], BossColor, Radius[b] * 4f);
            g.Sfx.Play(Sound.Boom, 0f);
        }

        void Summon(int b, int tier, float t)
        {
            int n = 6 + tier * 2;
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2f / n;
                Spawn(t, false, Pos[b] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (Radius[b] + 1.2f));
            }
            g.Fx.Pop(Pos[b], SwarmerColor, Radius[b] * 5f);
            g.Sfx.Play(Sound.BossWarn, 0f);
        }

        void LockCharge(int b)
        {
            bossDashTarget = g.Player.Pos;   // fixed the moment the wind-up starts
            Vector2 d = bossDashTarget - Pos[b];
            bossAimDir = d.sqrMagnitude > 1e-4f ? d.normalized : Vector2.right;
            g.Sfx.Play(Sound.BossWarn, 0.15f);
            g.Shake(0.15f);
        }

        Vector2 PickBossPoint()
        {
            float a = Random.value * Mathf.PI * 2f, d = 11f + Random.value * 7f;
            Vector2 p = g.Player.Pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
            p.x = Mathf.Clamp(p.x, -Game.ArenaHalfW + 3f, Game.ArenaHalfW - 3f);
            p.y = Mathf.Clamp(p.y, -Game.ArenaHalfH + 3f, Game.ArenaHalfH - 3f);
            return p;
        }

        /// <summary>Hits every enemy inside the camera view (the ultimate). Bosses take at most 8% of max HP.</summary>
        public int DamageInView(Vector2 center, float halfW, float halfH, float damage, float knockback, float slowSeconds)
        {
            int n = 0;
            for (int i = 0; i < Count; i++)
            {
                if (!Alive[i]) continue;
                if (Mathf.Abs(Pos[i].x - center.x) > halfW + Radius[i] || Mathf.Abs(Pos[i].y - center.y) > halfH + Radius[i]) continue;
                slow[i] = Mathf.Max(slow[i], slowSeconds);
                bool isBoss = kind[i] == EnemyKind.Boss;
                Hit(i, isBoss ? Mathf.Min(damage, bossMaxHp * 0.08f) : damage, center, knockback, isBoss);
                n++;
            }
            return n;
        }

        void KillBoss(int i)
        {
            BossAlive = false;
            for (int k = 0; k < 3; k++)
                g.Xp.Drop(Pos[i] + new Vector2(Mathf.Cos(k * 2.1f), Mathf.Sin(k * 2.1f)) * 0.6f, 25f);
            g.Fx.Burst(Pos[i], BossColor, 40, 10f);
            g.Fx.Pop(Pos[i], BossColor, 8f);
            g.Fx.Pop(Pos[i], Color.white, 4f);
            g.Sfx.Play(Sound.Boom, 0f);
            g.Shake(0.7f);
            BossesKilled++;
            g.QueueBossReward();
        }
        Color TintedColor(int i)
        {
            Color c = BaseColor(i);
            if (slow[i] > 0f) c = Color.Lerp(c, SlowTint, 0.6f);
            if (oil[i] > 0f) c = Color.Lerp(c, OilTint, 0.55f);
            if (poison[i] > 0f) c = Color.Lerp(c, PoisonTint, 0.55f + 0.2f * Mathf.Sin(g.RunTime * 12f + i));
            if (burn[i] > 0f) c = Color.Lerp(c, BurnHot, 0.45f + 0.3f * Mathf.Sin(g.RunTime * 22f + i));
            return c;
        }

        // ---- damage over time: fire (Ember, Napalm) and poison (Venom), plus Napalm's oil coat ----
        public void Ignite(int i, float dps, float seconds)
        {
            if (!Alive[i]) return;
            burnDps[i] = Mathf.Max(burn[i] > 0f ? burnDps[i] : 0f, dps);
            burn[i] = Mathf.Max(burn[i], seconds);
        }

        public void Poison(int i, float dps, float seconds)
        {
            if (!Alive[i]) return;
            poisonDps[i] = Mathf.Max(poison[i] > 0f ? poisonDps[i] : 0f, dps);
            poison[i] = Mathf.Max(poison[i], seconds);
        }

        public void Oil(int i, float seconds)
        {
            if (Alive[i]) oil[i] = Mathf.Max(oil[i], seconds);
        }

        public bool IsOiled(int i) => oil[i] > 0f;

        void TickDot(int i, float dt)
        {
            bool burning = burn[i] > 0f;
            float dps = (burning ? burnDps[i] : 0f) + (poison[i] > 0f ? poisonDps[i] : 0f);
            if (burning) burn[i] -= dt;
            if (poison[i] > 0f) poison[i] -= dt;
            dotAcc[i] += dps * dt;
            // flames lick up off burning enemies; toxic bubbles rise off poisoned ones
            if (Random.value < dt * 10f)
            {
                Vector2 at = Pos[i] + new Vector2(Random.Range(-Radius[i], Radius[i]), Radius[i] * 0.5f);
                g.Fx.Burst(at, burning ? (Random.value < 0.5f ? BurnTint : BurnHot) : PoisonTint, 2, 1.8f);
            }
            dotTick[i] -= dt;
            if (dotTick[i] > 0f || dotAcc[i] <= 0f) return;
            dotTick[i] = 0.5f;
            float d = dotAcc[i];
            dotAcc[i] = 0f;
            hp[i] -= d;
            g.Numbers.Damage(Pos[i], d);
            if (hp[i] <= 0f) Kill(i);
        }

        // When an enemy lands a hit, everything near the hero is slowed and shoved back,
        // so a mistake costs HP but never traps you.
        void SlowAround(Vector2 center, float radius, float seconds)
        {
            float r2 = radius * radius;
            int x0 = Grid.CellX(center.x - radius), x1 = Grid.CellX(center.x + radius);
            int y0 = Grid.CellY(center.y - radius), y1 = Grid.CellY(center.y + radius);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int j = Grid.Head(x, y); j != -1; j = Grid.Next(j))
                    {
                        if (!Alive[j]) continue;
                        Vector2 d = Pos[j] - center;
                        float d2 = d.sqrMagnitude;
                        if (d2 > r2) continue;
                        slow[j] = Mathf.Max(slow[j], seconds);
                        if (d2 > 1e-6f) knock[j] += d / Mathf.Sqrt(d2) * (KnockMul(j) * 5f);
                    }
        }
    }
}
