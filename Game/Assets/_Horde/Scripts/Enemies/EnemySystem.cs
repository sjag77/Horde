using UnityEngine;

namespace Horde
{
    public enum EnemyKind : byte { Swarmer, Brute, Boss }

    /// <summary>
    /// Every enemy lives in parallel arrays updated in one loop. Dead enemies are only flagged
    /// during a frame and compacted at the start of the next, so the indices stored in the
    /// spatial grid stay valid for everything that queries it this frame.
    /// </summary>
    public sealed class EnemySystem
    {
        static readonly Color SwarmerColor = new Color(1f, 0.27f, 0.36f);
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
        public readonly bool[] Alive;
        readonly Vector2[] knock;
        readonly float[] hp, speed, contact, flash, slow;
        readonly float[] burn, burnDps, poison, poisonDps, oil, dotAcc, dotTick;
        readonly EnemyKind[] kind;
        readonly SpriteRenderer[] sr;
        readonly Transform[] tr;
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
        Vector2 bossDashDir, bossDashTarget;
        float bossDashSpeed;
        SpriteRenderer dashMarker;
        public bool BossAlive { get; private set; }
        public float BossHpFrac { get; private set; }

        public EnemySystem(Game g, int capacity)
        {
            this.g = g;
            Capacity = capacity;
            Pos = new Vector2[capacity];
            Radius = new float[capacity];
            OrbitCooldown = new float[capacity];
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
            kind = new EnemyKind[capacity];
            sr = new SpriteRenderer[capacity];
            tr = new Transform[capacity];

            var root = new GameObject("Enemies").transform;
            for (int i = 0; i < capacity; i++)
            {
                sr[i] = g.NewSprite("Enemy", Sprites.Swarmer, SwarmerColor, 10, root);
                tr[i] = sr[i].transform;
                sr[i].enabled = false;
            }
            Grid = new SpatialGrid(Game.ArenaHalfW, Game.ArenaHalfH, 3f, 1.2f, capacity);
            bossMarker = g.NewSprite("BossLanding", Sprites.Ring, BossColor, 4, root);
            bossMarker.enabled = false;
            dashMarker = g.NewSprite("BossChargeMark", Sprites.Ring, new Color(1f, 0.25f, 0.2f), 5, root);
            dashMarker.enabled = false;
        }

        public void Reset()
        {
            for (int i = 0; i < Capacity; i++)
            {
                sr[i].enabled = false;
                Alive[i] = false;
            }
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
            dashMarker.enabled = false;
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
                    if (Count >= Capacity) { Alive[Count - 1] = false; sr[Count - 1].enabled = false; Count--; }
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
                tr[i].position = Pos[i];
                bool hadStatus = slow[i] > 0f || burn[i] > 0f || poison[i] > 0f || oil[i] > 0f;
                if (slow[i] > 0f) slow[i] -= dt;
                if (oil[i] > 0f) oil[i] -= dt;
                if (burn[i] > 0f || poison[i] > 0f) TickDot(i, dt);
                if (!Alive[i]) continue;
                if (flash[i] > 0f)
                {
                    flash[i] -= dt * 6f;
                    sr[i].color = Color.Lerp(TintedColor(i), Color.white, Mathf.Clamp01(flash[i]));
                }
                else if (hadStatus)
                {
                    sr[i].color = TintedColor(i);
                }
            }
            BossAlive = bossSeen;
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
            sr[i].enabled = false;
            g.Kills++;
            if (kind[i] == EnemyKind.Boss) { KillBoss(i); return; }
            bool brute = kind[i] == EnemyKind.Brute;
            g.Xp.Drop(Pos[i], brute ? 10f : 2f);   // fewer, harder kills still level you up
            g.Fx.Burst(Pos[i], BaseColor(i), brute ? 16 : 8, 6f);
            g.Fx.Pop(Pos[i], BaseColor(i), Radius[i] * 4f);
            g.Sfx.Play(Sound.Kill);
            if (brute) g.Shake(0.18f);
            g.Pickups.MaybeDrop(Pos[i], brute);
        }

        void Spawn(float t, bool boss = false, Vector2? at = null)
        {
            if (Count >= Capacity) return;

            float halfH = g.Cam.orthographicSize, halfW = halfH * g.Cam.aspect;
            Vector2 cam = g.Cam.transform.position;
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
            bool brute = !boss && t > 75f && Random.value < Mathf.Min(0.2f, 0.05f + t / 1200f);

            int i = Count++;
            kind[i] = boss ? EnemyKind.Boss : brute ? EnemyKind.Brute : EnemyKind.Swarmer;
            Pos[i] = p;
            knock[i] = Vector2.zero;
            Radius[i] = brute ? 0.55f : 0.28f;
            hp[i] = (brute ? 160f : 23f) * scale;   // Spark Lv1 can't one-shot a swarmer, Lv2 can - until time outgrows it
            speed[i] = brute ? 1.2f : 2.1f + Random.value * 0.35f;
            contact[i] = (brute ? 18f : 8f) * (1f + t / 60f * 0.12f);
            flash[i] = 0f;
            slow[i] = 0f;
            burn[i] = burnDps[i] = poison[i] = poisonDps[i] = oil[i] = dotAcc[i] = 0f;
            dotTick[i] = 0.5f;
            OrbitCooldown[i] = 0f;
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

            sr[i].enabled = true;
            sr[i].sprite = boss ? Sprites.Boss : brute ? Sprites.Brute : Sprites.Swarmer;
            sr[i].color = BaseColor(i);
            tr[i].localScale = Vector3.one * (Radius[i] * 2.2f);
            tr[i].position = p;
        }

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
                sr[i].enabled = false;
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
            (kind[a], kind[b]) = (kind[b], kind[a]);
            (sr[a], sr[b]) = (sr[b], sr[a]);
            (tr[a], tr[b]) = (tr[b], tr[a]);
        }

        Color BaseColor(int i) => kind[i] == EnemyKind.Boss ? BossColor : kind[i] == EnemyKind.Brute ? BruteColor : SwarmerColor;
        float KnockMul(int i) => kind[i] == EnemyKind.Boss ? 0.03f : kind[i] == EnemyKind.Brute ? 0.3f : 1f;

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
            if (b < 0) { dashMarker.enabled = false; return; }

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
            bool telegraph = bossPhase == 1 || bossPhase == 2;   // red ring where the charge will land
            dashMarker.enabled = telegraph;
            if (telegraph)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * 25f);
                dashMarker.transform.localScale = Vector3.one * (Radius[b] * 2.4f + pulse * 0.4f);
                dashMarker.color = new Color(1f, 0.25f, 0.2f, 0.45f + 0.45f * pulse);
            }
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
                        bossTimer = 0.7f;
                        LockCharge(b);
                    }
                    else if (roll == 1) { Volley(b, tier); bossPhase = 3; bossTimer = 0.8f; }
                    else { Summon(b, tier, t); bossPhase = 3; bossTimer = 0.9f; }
                    break;
                case 1:
                    bossPhase = 2;
                    bossTimer = 0.45f;
                    bossDashSpeed = Mathf.Max(speed[b] * 5f, (bossDashTarget - Pos[b]).magnitude / 0.4f);
                    break;
                case 2:
                    if (--bossDashesLeft > 0) { bossPhase = 1; bossTimer = 0.4f; LockCharge(b); }
                    else { bossPhase = 0; bossTimer = rest; }
                    break;
                default:
                    bossPhase = 0;
                    bossTimer = rest;
                    break;
            }
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
            dashMarker.transform.position = bossDashTarget;
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
