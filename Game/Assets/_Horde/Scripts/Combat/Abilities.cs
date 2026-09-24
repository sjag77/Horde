using UnityEngine;

namespace Horde
{
    public enum AbilityId { SparkBolt, OrbitBlades, FrostNova, ChainLightning, Meteor, Bomb }

    /// <summary>
    /// The hero's automatic abilities. Levels run 0 (not owned) to 8 and every level compounds
    /// damage, so the kit keeps pace with enemy health, which grows with run time.
    /// </summary>
    public sealed class AbilitySet
    {
        public const int MaxLevel = 8;
        public const int Count = 6;
        const int MaxBlades = 8, BeamCap = 24, MeteorCap = 8;
        const float BeamTime = 0.14f, MeteorDelay = 0.7f;

        static readonly Color BladeColor = new Color(1f, 0.85f, 0.4f);
        static readonly Color FrostColor = new Color(0.55f, 0.85f, 1f);
        static readonly Color ZapColor = new Color(0.78f, 0.82f, 1f);
        static readonly Color MeteorColor = new Color(1f, 0.52f, 0.18f);
        static readonly Color BombColor = new Color(1f, 0.28f, 0.2f);

        readonly Game g;
        readonly int[] level = new int[Count];

        readonly SpriteRenderer[] blades = new SpriteRenderer[MaxBlades];
        readonly Transform[] bladeTr = new Transform[MaxBlades];

        readonly SpriteRenderer[] beamSr = new SpriteRenderer[BeamCap];
        readonly Transform[] beamTr = new Transform[BeamCap];
        readonly float[] beamLife = new float[BeamCap];
        readonly Color[] beamColor = new Color[BeamCap];
        int beams;

        readonly Vector2[] mPos = new Vector2[MeteorCap];
        readonly float[] mTimer = new float[MeteorCap], mRadius = new float[MeteorCap], mDamage = new float[MeteorCap], mDur = new float[MeteorCap];
        readonly Vector2[] mStart = new Vector2[MeteorCap];
        readonly SpriteRenderer[] mBall = new SpriteRenderer[MeteorCap], mCore = new SpriteRenderer[MeteorCap];
        readonly SpriteRenderer[] mSr = new SpriteRenderer[MeteorCap];
        readonly Transform[] mTr = new Transform[MeteorCap];
        int meteors;

        readonly int[] chained = new int[12];
        const int MineCap = 16;
        readonly Vector2[] mnPos = new Vector2[MineCap];
        readonly float[] mnTimer = new float[MineCap], mnRadius = new float[MineCap], mnDamage = new float[MineCap];
        readonly SpriteRenderer[] mnBody = new SpriteRenderer[MineCap], mnLight = new SpriteRenderer[MineCap];
        int mines;
        readonly SpriteRenderer[] widgets = new SpriteRenderer[MaxOwned];
        readonly SpriteRenderer[] struts = new SpriteRenderer[MaxOwned];
        // Ship-local hardpoints (ship points along +X): nose, wing tips, tail, then two inner mounts.
        static readonly Vector2[] Hardpoints =
        {
            new Vector2(0.52f, 0f), new Vector2(-0.24f, 0.48f), new Vector2(-0.24f, -0.48f),
            new Vector2(-0.52f, 0f), new Vector2(0.14f, 0.34f), new Vector2(0.14f, -0.34f)
        };
        float widgetAngle;
        float sparkCd, orbitAngle, novaCd, chainCd, meteorCd, bombCd;

        public const int MaxOwned = 6;
        /// <summary>Starts at 4; each boss reward can raise it by one, up to 6.</summary>
        public int SlotLimit { get; private set; } = 4;
        public void AddSlot() => SlotLimit = Mathf.Min(MaxOwned, SlotLimit + 1);
        readonly float[] cdMax = new float[Count];
        readonly AbilityId[] slots = new AbilityId[MaxOwned];
        public int OwnedCount { get; private set; }

        public AbilitySet(Game g)
        {
            this.g = g;
            var root = new GameObject("Abilities").transform;
            for (int i = 0; i < MaxBlades; i++)
            {
                blades[i] = g.NewSprite("Blade", Sprites.Diamond, BladeColor, 18, root);
                bladeTr[i] = blades[i].transform;
                bladeTr[i].localScale = new Vector3(0.62f, 0.28f, 1f);
                blades[i].enabled = false;
            }
            for (int i = 0; i < MaxOwned; i++)
            {
                struts[i] = g.NewSprite("Strut", Sprites.Square, Color.white, 19, root);
                widgets[i] = g.NewSprite("Pod", Sprites.IconSpark, Color.white, 21, root);
                widgets[i].enabled = struts[i].enabled = false;
            }
            for (int i = 0; i < MineCap; i++)
            {
                mnBody[i] = g.NewSprite("Mine", Sprites.Circle, new Color(0.18f, 0.18f, 0.22f), 7, root);
                mnLight[i] = g.NewSprite("MineLight", Sprites.Glow, BombColor, 8, root);
                mnBody[i].transform.localScale = Vector3.one * 0.5f;
                mnLight[i].transform.localScale = Vector3.one * 0.7f;
                mnBody[i].enabled = mnLight[i].enabled = false;
            }
            for (int i = 0; i < BeamCap; i++)
            {
                beamSr[i] = g.NewSprite("Beam", Sprites.Square, ZapColor, 19, root);
                beamTr[i] = beamSr[i].transform;
                beamSr[i].enabled = false;
            }
            for (int i = 0; i < MeteorCap; i++)
            {
                mSr[i] = g.NewSprite("MeteorMark", Sprites.Ring, MeteorColor, 6, root);
                mBall[i] = g.NewSprite("Fireball", Sprites.Glow, MeteorColor, 23, root);
                mCore[i] = g.NewSprite("FireballCore", Sprites.Circle, new Color(1f, 0.95f, 0.7f), 24, root);
                mBall[i].transform.localScale = Vector3.one * 1.3f;
                mCore[i].transform.localScale = Vector3.one * 0.4f;
                mBall[i].enabled = mCore[i].enabled = false;
                mTr[i] = mSr[i].transform;
                mSr[i].enabled = false;
            }
        }

        public int Level(AbilityId id) => level[(int)id];
        public AbilityId Slot(int i) => slots[i];

        /// <summary>Secret reward: every ability you already own jumps to max level.</summary>
        public void MaxAllOwned()
        {
            for (int i = 0; i < OwnedCount; i++) level[(int)slots[i]] = MaxLevel;
        }

        /// <summary>0 = ready, 1 = just fired. Orbit Blades are always active.</summary>
        public float CooldownFrac(AbilityId id)
        {
            float cur = id switch
            {
                AbilityId.SparkBolt => sparkCd,
                AbilityId.FrostNova => novaCd,
                AbilityId.ChainLightning => chainCd,
                AbilityId.Meteor => meteorCd,
                AbilityId.Bomb => bombCd,
                _ => 0f
            };
            float max = cdMax[(int)id];
            return max > 0f ? Mathf.Clamp01(cur / max) : 0f;
        }

        public void Reset()
        {
            for (int i = 0; i < Count; i++) { level[i] = 0; cdMax[i] = 0f; }
            OwnedCount = 0;
            SlotLimit = 4;
            bombCd = 0.5f;
            sparkCd = 0.4f;
            orbitAngle = 0f;
            novaCd = chainCd = meteorCd = 0.5f;
            for (int i = 0; i < MaxBlades; i++) blades[i].enabled = false;
            for (int i = 0; i < BeamCap; i++) beamSr[i].enabled = false;
            for (int i = 0; i < MineCap; i++) mnBody[i].enabled = mnLight[i].enabled = false;
            mines = 0;
            for (int i = 0; i < MeteorCap; i++) { mSr[i].enabled = false; mBall[i].enabled = mCore[i].enabled = false; }
            for (int i = 0; i < MaxOwned; i++) widgets[i].enabled = struts[i].enabled = false;
            beams = meteors = 0;
        }

        public void Grant(AbilityId id)
        {
            int k = (int)id;
            if (level[k] == 0)
            {
                if (OwnedCount >= SlotLimit) return;
                slots[OwnedCount++] = id;
            }
            level[k] = Mathf.Min(MaxLevel, level[k] + 1);
        }

        public static string Name(AbilityId id) => id switch
        {
            AbilityId.SparkBolt => "Spark Bolt",
            AbilityId.OrbitBlades => "Orbit Blades",
            AbilityId.FrostNova => "Frost Nova",
            AbilityId.ChainLightning => "Chain Lightning",
            AbilityId.Meteor => "Meteor",
            _ => "Bomb Mines"
        };

        public static string Describe(AbilityId id, int nextLevel)
        {
            if (nextLevel == 1)
                return id switch
                {
                    AbilityId.SparkBolt => "Seeking bolts that knock enemies back.",
                    AbilityId.OrbitBlades => "Blades circle you and cut anything close.",
                    AbilityId.FrostNova => "A freezing ring pulses out, damaging and slowing enemies.",
                    AbilityId.ChainLightning => "Lightning jumps from enemy to enemy.",
                    AbilityId.Meteor => "Your hero hurls fireballs that crash into crowds.",
                    _ => "Drop mines on your path that blow up a second later - punish whatever chases you."
                };
            return id switch
            {
                AbilityId.SparkBolt => "+30% damage, extra bolts and bounces.",
                AbilityId.OrbitBlades => "+35% damage and another blade.",
                AbilityId.FrostNova => "+35% damage, bigger ring, pulses faster.",
                AbilityId.ChainLightning => "+35% damage and more jumps.",
                AbilityId.Meteor => "+40% damage and more fireballs.",
                _ => "+40% damage, bigger blasts, drops mines faster."
            };
        }

        static float Grow(int lv, float perLevel) => 1f + perLevel * (lv - 1);

        public void Tick(float dt)
        {
            TickSpark(dt);
            TickOrbit(dt);
            TickNova(dt);
            TickChain(dt);
            TickMeteor(dt);
            TickBomb(dt);
            TickBeams(dt);
            TickWidgets(dt);
        }

        void TickSpark(float dt)
        {
            int lv = level[(int)AbilityId.SparkBolt];
            if (lv == 0) return;
            sparkCd -= dt;
            if (sparkCd > 0f) return;

            var p = g.Player;
            int target = g.Enemies.Nearest(p.Pos, 9f);
            if (target < 0) { sparkCd = 0.1f; return; }

            Vector2 dir = g.Enemies.Pos[target] - p.Pos;
            Vector2 muzzle = Origin(AbilityId.SparkBolt);
            bool venom = g.Heroes.Id == HeroId.Venom;
            float damage = 20f * Grow(lv, 0.3f) * p.DamageMul * (venom ? 1.12f : 1f);
            int shots = 1 + (lv - 1) / 2;   // 1,1,2,2,3,3,4,4
            int bounce = venom ? (lv + 1) / 3 : lv / 3;   // Venom bounces from level 2
            for (int s = 0; s < shots; s++)
                g.Projectiles.Fire(muzzle, Rotate(dir, (s - (shots - 1) * 0.5f) * 11f), 11.5f, damage, 0.16f, bounce);

            g.Fx.Pop(muzzle, Tint(AbilityId.SparkBolt), 0.8f);
            g.Sfx.Play(Sound.Shoot);
            sparkCd = cdMax[(int)AbilityId.SparkBolt] = Mathf.Max(0.2f, 0.8f - lv * 0.05f) * p.CooldownMul;
        }

        // Each enemy can only be cut every 0.35 s.
        void TickOrbit(float dt)
        {
            var e = g.Enemies;
            for (int i = 0; i < e.Count; i++)
                if (e.OrbitCooldown[i] > 0f) e.OrbitCooldown[i] -= dt;

            int lv = level[(int)AbilityId.OrbitBlades];
            int count = lv > 0 ? Mathf.Min(MaxBlades, lv + 1) : 0;
            for (int i = 0; i < MaxBlades; i++) blades[i].enabled = i < count;
            if (count == 0) return;

            var p = g.Player;
            orbitAngle += dt * (170f + lv * 15f);
            float radius = (1.5f + lv * 0.08f) * p.AreaMul;
            float damage = 16f * Grow(lv, 0.35f) * p.DamageMul;

            for (int b = 0; b < count; b++)
            {
                float deg = orbitAngle + b * 360f / count;
                float rad = deg * Mathf.Deg2Rad;
                Vector2 bp = p.Pos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                bladeTr[b].position = bp;
                bladeTr[b].rotation = Quaternion.Euler(0f, 0f, deg + 90f);
                CutAround(bp, 0.38f * p.AreaMul, damage, p.Pos);
            }
        }

        void TickNova(float dt)
        {
            int lv = level[(int)AbilityId.FrostNova];
            if (lv == 0) return;
            novaCd -= dt;
            if (novaCd > 0f) return;

            var p = g.Player;
            float radius = (2.4f + lv * 0.22f) * p.AreaMul;
            if (g.Enemies.Nearest(p.Pos, radius) < 0) { novaCd = 0.2f; return; }

            AreaHit(p.Pos, radius, 24f * Grow(lv, 0.35f) * p.DamageMul, 4.5f, 2.2f);   // longer freeze
            g.Fx.Pop(p.Pos, FrostColor, radius * 2f);
            g.Fx.Pop(Origin(AbilityId.FrostNova), Color.white, 0.9f);
            g.Fx.Burst(p.Pos, FrostColor, 14, radius * 3f);
            g.Sfx.Play(Sound.Nova, 0.04f);
            novaCd = cdMax[(int)AbilityId.FrostNova] = Mathf.Max(1.2f, 3.3f - lv * 0.2f) * p.CooldownMul;
        }

        void TickChain(float dt)
        {
            int lv = level[(int)AbilityId.ChainLightning];
            if (lv == 0) return;
            chainCd -= dt;
            if (chainCd > 0f) return;

            var p = g.Player;
            var e = g.Enemies;
            int cur = e.Nearest(p.Pos, 8f);
            if (cur < 0) { chainCd = 0.15f; return; }

            float damage = 30f * Grow(lv, 0.35f) * p.DamageMul;
            int links = 3 + lv / 2;   // 3..7 enemies per cast
            Vector2 from = Origin(AbilityId.ChainLightning);
            g.Fx.Pop(from, ZapColor, 0.9f);
            int n = 0;
            while (cur >= 0 && n < links)
            {
                Vector2 at = e.Pos[cur];
                Beam(from, at);
                chained[n++] = cur;
                e.Hit(cur, damage, from, 1.5f);
                g.Heroes.OnDirectHit(cur);
                g.Fx.Burst(at, ZapColor, 3, 3f);
                from = at;
                cur = NearestUnchained(at, 3.6f, n);
            }
            g.Sfx.Play(Sound.Zap);
            chainCd = cdMax[(int)AbilityId.ChainLightning] = Mathf.Max(0.6f, 1.9f - lv * 0.12f) * p.CooldownMul;
        }

        void TickMeteor(float dt)
        {
            for (int i = 0; i < meteors; i++)
            {
                mTimer[i] -= dt;
                float k = 1f - Mathf.Clamp01(mTimer[i] / MeteorDelay);
                mTr[i].localScale = Vector3.one * (mRadius[i] * 2f * (1.35f - 0.35f * k));
                var c = MeteorColor;
                c.a = 0.35f + 0.55f * k;
                mSr[i].color = c;
                // the fireball is thrown from the hero and arcs down onto the mark
                float f = mDur[i] > 0f ? 1f - Mathf.Clamp01(mTimer[i] / mDur[i]) : 1f;
                Vector2 bp = Vector2.Lerp(mStart[i], mPos[i], f) + Vector2.up * (Mathf.Sin(f * Mathf.PI) * 2.2f);
                mBall[i].transform.position = bp;
                mCore[i].transform.position = bp;
                if (mTimer[i] > 0f) continue;

                Vector2 at = mPos[i];
                AreaHit(at, mRadius[i], mDamage[i], 6f, 0f, true);
                g.Fx.Pop(at, MeteorColor, mRadius[i] * 2.4f);
                g.Fx.Burst(at, MeteorColor, 16, 8f);
                g.Shake(0.12f);
                g.Sfx.Play(Sound.Boom, 0.05f);
                RemoveMeteor(i);
                i--;
            }

            int lv = level[(int)AbilityId.Meteor];
            if (lv == 0) return;
            meteorCd -= dt;
            if (meteorCd > 0f) return;

            var p = g.Player;
            var e = g.Enemies;
            if (e.Nearest(p.Pos, 8.5f) < 0) { meteorCd = 0.25f; return; }

            int shots = 1 + lv / 3;   // 1,1,2,2,2,3,3,3
            float radius = (1.4f + lv * 0.1f) * p.AreaMul;
            float damage = 60f * Grow(lv, 0.4f) * p.DamageMul;
            for (int s = 0; s < shots && meteors < MeteorCap; s++)
            {
                int t = RandomTargetNear(p.Pos, 8.5f);
                if (t < 0) break;
                int m = meteors++;
                mPos[m] = e.Pos[t];
                mTimer[m] = MeteorDelay + s * 0.12f;
                mDur[m] = mTimer[m];
                mStart[m] = Origin(AbilityId.Meteor);
                mBall[m].enabled = mCore[m].enabled = true;
                mRadius[m] = radius;
                mDamage[m] = damage;
                mSr[m].enabled = true;
                mTr[m].position = mPos[m];
            }
            meteorCd = cdMax[(int)AbilityId.Meteor] = Mathf.Max(1.5f, 4.6f - lv * 0.3f) * p.CooldownMul;
        }

        // Mines are dropped from the bomb pod as you move and blow up a second later,
        // catching whatever is chasing you.
        void TickBomb(float dt)
        {
            for (int i = 0; i < mines; i++)
            {
                mnTimer[i] -= dt;
                float blink = mnTimer[i] < 0.35f ? 1f : (Mathf.Sin(mnTimer[i] * 18f) > 0f ? 1f : 0.25f);
                var lc = BombColor;
                lc.a = blink;
                mnLight[i].color = lc;
                if (mnTimer[i] > 0f) continue;

                Vector2 at = mnPos[i];
                AreaHit(at, mnRadius[i], mnDamage[i], 7f, 0f, true);
                g.Fx.Pop(at, BombColor, mnRadius[i] * 2.4f);
                g.Fx.Pop(at, Color.white, mnRadius[i] * 1.2f);
                g.Fx.Burst(at, new Color(1f, 0.7f, 0.3f), 18, 9f);
                g.Shake(0.1f);
                g.Sfx.Play(Sound.Boom, 0.06f);
                RemoveMine(i);
                i--;
            }

            int lv = level[(int)AbilityId.Bomb];
            if (lv == 0) return;
            bombCd -= dt;
            if (bombCd > 0f) return;

            var p = g.Player;
            int drops = 1 + lv / 4;   // 1,1,1,2,2,2,2,3
            float radius = (1.7f + lv * 0.1f) * p.AreaMul;
            float damage = 55f * Grow(lv, 0.4f) * p.DamageMul;
            Vector2 drop = Origin(AbilityId.Bomb);
            for (int k = 0; k < drops && mines < MineCap; k++)
            {
                int m = mines++;
                mnPos[m] = drop - p.Facing * (k * 0.9f);
                mnTimer[m] = 1f;
                mnRadius[m] = radius;
                mnDamage[m] = damage;
                mnBody[m].enabled = mnLight[m].enabled = true;
                mnBody[m].transform.position = mnLight[m].transform.position = mnPos[m];
            }
            g.Sfx.Play(Sound.Shoot, 0.2f);
            bombCd = cdMax[(int)AbilityId.Bomb] = Mathf.Max(0.9f, 2.4f - lv * 0.14f) * p.CooldownMul;
        }

        void RemoveMine(int i)
        {
            mnBody[i].enabled = mnLight[i].enabled = false;
            int last = --mines;
            if (i == last) return;
            (mnPos[i], mnPos[last]) = (mnPos[last], mnPos[i]);
            (mnTimer[i], mnTimer[last]) = (mnTimer[last], mnTimer[i]);
            (mnRadius[i], mnRadius[last]) = (mnRadius[last], mnRadius[i]);
            (mnDamage[i], mnDamage[last]) = (mnDamage[last], mnDamage[i]);
            (mnBody[i], mnBody[last]) = (mnBody[last], mnBody[i]);
            (mnLight[i], mnLight[last]) = (mnLight[last], mnLight[i]);
        }

        // Each owned ability is a pod bolted onto the ship by a strut; its attacks fire from that pod.
        Vector2 Mount(int slot, float along = 1f)
        {
            var p = g.Player;
            float a = Mathf.Atan2(p.Facing.y, p.Facing.x), c = Mathf.Cos(a), s = Mathf.Sin(a);
            Vector2 h = Hardpoints[slot] * along;
            return p.Pos + new Vector2(h.x * c - h.y * s, h.x * s + h.y * c);
        }

        Vector2 Origin(AbilityId id)
        {
            for (int i = 0; i < OwnedCount; i++) if (slots[i] == id) return Mount(i);
            return g.Player.Pos;
        }

        void TickWidgets(float dt)
        {
            var p = g.Player;
            float deg = Mathf.Atan2(p.Facing.y, p.Facing.x) * Mathf.Rad2Deg;
            float pulse = 1f + Mathf.Sin(g.RunTime * 6f) * 0.06f;
            for (int i = 0; i < MaxOwned; i++)
            {
                bool on = i < OwnedCount;
                widgets[i].enabled = struts[i].enabled = on;
                if (!on) continue;
                var id = slots[i];
                Color tint = Tint(id);
                Vector2 pod = Mount(i), baseP = Mount(i, 0.3f), d = pod - baseP;
                var st = struts[i].transform;
                st.position = (pod + baseP) * 0.5f;
                st.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                st.localScale = new Vector3(d.magnitude, 0.09f, 1f);
                struts[i].color = new Color(tint.r * 0.75f, tint.g * 0.75f, tint.b * 0.75f, 0.95f);
                var w = widgets[i].transform;
                widgets[i].sprite = Icon(id);
                widgets[i].color = tint;
                w.position = pod;
                w.rotation = Quaternion.Euler(0f, 0f, deg);
                w.localScale = Vector3.one * (0.36f * pulse);
            }
        }

        public static Sprite Icon(AbilityId id) => id switch
        {
            AbilityId.SparkBolt => Sprites.IconSpark,
            AbilityId.OrbitBlades => Sprites.IconBlades,
            AbilityId.FrostNova => Sprites.IconNova,
            AbilityId.ChainLightning => Sprites.IconChain,
            AbilityId.Meteor => Sprites.IconMeteor,
            _ => Sprites.IconBomb
        };

        public static Color Tint(AbilityId id) => id switch
        {
            AbilityId.SparkBolt => new Color(0.7f, 0.97f, 1f),
            AbilityId.OrbitBlades => BladeColor,
            AbilityId.FrostNova => FrostColor,
            AbilityId.ChainLightning => ZapColor,
            AbilityId.Meteor => MeteorColor,
            _ => BombColor
        };

        void TickBeams(float dt)
        {
            for (int i = 0; i < beams; i++)
            {
                beamLife[i] -= dt;
                if (beamLife[i] <= 0f)
                {
                    beamSr[i].enabled = false;
                    int last = --beams;
                    if (i != last)
                    {
                        (beamSr[i], beamSr[last]) = (beamSr[last], beamSr[i]);
                        (beamTr[i], beamTr[last]) = (beamTr[last], beamTr[i]);
                        (beamLife[i], beamLife[last]) = (beamLife[last], beamLife[i]);
                        (beamColor[i], beamColor[last]) = (beamColor[last], beamColor[i]);
                    }
                    i--;
                    continue;
                }
                var c = beamColor[i];
                c.a = beamLife[i] / BeamTime;
                beamSr[i].color = c;
            }
        }

        void Beam(Vector2 a, Vector2 b, float width = 0.13f, Color? tint = null)
        {
            int i = beams < BeamCap ? beams++ : Random.Range(0, BeamCap);
            Vector2 d = b - a;
            beamTr[i].position = (a + b) * 0.5f;
            beamTr[i].rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            beamTr[i].localScale = new Vector3(d.magnitude, width, 1f);
            beamLife[i] = BeamTime;
            beamColor[i] = tint ?? ZapColor;
            beamSr[i].enabled = true;
        }

        void RemoveMeteor(int i)
        {
            mSr[i].enabled = false;
            mBall[i].enabled = mCore[i].enabled = false;
            int last = --meteors;
            if (i == last) return;
            (mPos[i], mPos[last]) = (mPos[last], mPos[i]);
            (mTimer[i], mTimer[last]) = (mTimer[last], mTimer[i]);
            (mRadius[i], mRadius[last]) = (mRadius[last], mRadius[i]);
            (mDamage[i], mDamage[last]) = (mDamage[last], mDamage[i]);
            (mSr[i], mSr[last]) = (mSr[last], mSr[i]);
            (mTr[i], mTr[last]) = (mTr[last], mTr[i]);
            (mDur[i], mDur[last]) = (mDur[last], mDur[i]);
            (mStart[i], mStart[last]) = (mStart[last], mStart[i]);
            (mBall[i], mBall[last]) = (mBall[last], mBall[i]);
            (mCore[i], mCore[last]) = (mCore[last], mCore[i]);
        }

        int RandomTargetNear(Vector2 c, float r)
        {
            var e = g.Enemies;
            if (e.Count == 0) return -1;
            float r2 = r * r;
            for (int tries = 0; tries < 10; tries++)
            {
                int i = Random.Range(0, e.Count);
                if (e.Alive[i] && (e.Pos[i] - c).sqrMagnitude < r2) return i;
            }
            return e.Nearest(c, r);
        }

        int NearestUnchained(Vector2 at, float r, int n)
        {
            var e = g.Enemies;
            var grid = e.Grid;
            int best = -1;
            float bestD = r * r;
            int x0 = grid.CellX(at.x - r), x1 = grid.CellX(at.x + r);
            int y0 = grid.CellY(at.y - r), y1 = grid.CellY(at.y + r);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int j = grid.Head(x, y); j != -1; j = grid.Next(j))
                    {
                        if (!e.Alive[j]) continue;
                        float d = (e.Pos[j] - at).sqrMagnitude;
                        if (d >= bestD) continue;
                        bool used = false;
                        for (int k = 0; k < n; k++) if (chained[k] == j) { used = true; break; }
                        if (used) continue;
                        bestD = d;
                        best = j;
                    }
            return best;
        }

        void AreaHit(Vector2 c, float r, float damage, float knock, float slowSeconds, bool blast = false)
        {
            var e = g.Enemies;
            var grid = e.Grid;
            float reach = r + 1.6f;
            int x0 = grid.CellX(c.x - reach), x1 = grid.CellX(c.x + reach);
            int y0 = grid.CellY(c.y - reach), y1 = grid.CellY(c.y + reach);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int j = grid.Head(x, y); j != -1; j = grid.Next(j))
                    {
                        if (!e.Alive[j]) continue;
                        float rr = r + e.Radius[j];
                        if ((e.Pos[j] - c).sqrMagnitude >= rr * rr) continue;
                        if (slowSeconds > 0f) e.Slow(j, slowSeconds);
                        e.Hit(j, damage, c, knock);
                        if (blast) g.Heroes.OnBlastHit(j);
                    }
        }

        void CutAround(Vector2 center, float r, float damage, Vector2 from)
        {
            var e = g.Enemies;
            var grid = e.Grid;
            float reach = r + 1.6f;
            int x0 = grid.CellX(center.x - reach), x1 = grid.CellX(center.x + reach);
            int y0 = grid.CellY(center.y - reach), y1 = grid.CellY(center.y + reach);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    for (int j = grid.Head(x, y); j != -1; j = grid.Next(j))
                    {
                        if (!e.Alive[j] || e.OrbitCooldown[j] > 0f) continue;
                        float rr = r + e.Radius[j];
                        if ((e.Pos[j] - center).sqrMagnitude >= rr * rr) continue;
                        e.OrbitCooldown[j] = 0.35f;
                        e.Hit(j, damage, from, 3.5f);
                        g.Heroes.OnDirectHit(j);
                    }
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
