using UnityEngine;

namespace Horde
{
    public sealed class Player
    {
        public const float Radius = 0.38f;
        Color BodyColor = new Color(0.35f, 0.95f, 1f);
        public Color HeroColor => BodyColor;

        readonly Game g;
        readonly SpriteRenderer body, aura;
        readonly Transform bodyTr, auraTr;
        float hurtFlash;
        const int GhostCap = 6;
        readonly SpriteRenderer[] ghosts = new SpriteRenderer[GhostCap];
        readonly float[] ghostLife = new float[GhostCap];

        public Vector2 Pos;
        public Vector2 Facing = Vector2.up;
        public float MaxHp, Hp, MoveSpeed, PickupRadius, DamageMul, CooldownMul, AreaMul, Iframes;
        public int Level;
        public float Xp, XpToNext;

        public Player(Game g)
        {
            this.g = g;
            aura = g.NewSprite("PlayerAura", Sprites.Glow, new Color(0.3f, 0.9f, 1f, 0.35f), 5);
            body = g.NewSprite("Player", Sprites.Hero, BodyColor, 20);
            auraTr = aura.transform;
            bodyTr = body.transform;
            auraTr.localScale = Vector3.one * 2.4f;
            bodyTr.localScale = Vector3.one * 0.95f;
            for (int i = 0; i < GhostCap; i++)
            {
                ghosts[i] = g.NewSprite("DashGhost", Sprites.Hero, Color.clear, 19);
                ghosts[i].enabled = false;
            }
        }

        public static float XpFor(int level) => Mathf.Round(4f + Mathf.Pow(level, 1.4f) * 3f);

        public void Reset()
        {
            Pos = Vector2.zero;
            Facing = Vector2.up;
            MaxHp = 150f;
            Hp = MaxHp;
            MoveSpeed = 4.3f;
            PickupRadius = 1.7f;
            DamageMul = 1f;
            CooldownMul = 1f;
            AreaMul = 1f;
            Iframes = 0f;
            BlinkCooldown = 0f;
            for (int i = 0; i < GhostCap; i++) { ghostLife[i] = 0f; ghosts[i].enabled = false; }
            hurtFlash = 0f;
            Level = 1;
            Xp = 0f;
            XpToNext = XpFor(1);
            Apply();
        }

        public void Tick(float dt, Vector2 move)
        {
            if (move.sqrMagnitude > 0.0001f) Facing = move.normalized;
            Pos += move * (MoveSpeed * dt);
            Pos.x = Mathf.Clamp(Pos.x, -Game.ArenaHalfW + Radius, Game.ArenaHalfW - Radius);
            Pos.y = Mathf.Clamp(Pos.y, -Game.ArenaHalfH + Radius, Game.ArenaHalfH - Radius);
            if (Iframes > 0f) Iframes -= dt;
            if (BlinkCooldown > 0f) BlinkCooldown -= dt;
            if (hurtFlash > 0f) hurtFlash -= dt * 4f;
            for (int i = 0; i < GhostCap; i++)   // dash afterimages fade and swell
            {
                if (ghostLife[i] <= 0f) continue;
                ghostLife[i] -= dt;
                if (ghostLife[i] <= 0f) { ghosts[i].enabled = false; continue; }
                float k = ghostLife[i] / 0.5f;
                ghosts[i].color = new Color(BodyColor.r, BodyColor.g, BodyColor.b, 0.55f * k);
                ghosts[i].transform.localScale = Vector3.one * (0.95f * (1.3f - 0.3f * k));
            }
            Apply();
        }

        void Apply()
        {
            bodyTr.position = Pos;
            auraTr.position = Pos;
            bodyTr.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg);
            body.color = Color.Lerp(BodyColor, Color.white, Mathf.Clamp01(hurtFlash));
            body.enabled = !(Iframes > 0f && ((int)(Iframes * 20f)) % 2 == 0);
        }

        public bool Damage(float amount)
        {
            if (Iframes > 0f || Hp <= 0f) return false;
            Hp -= amount;
            Iframes = 0.8f;
            hurtFlash = 1f;
            g.Hud.FlashHurt();
            g.Shake(0.45f);
            g.Sfx.Play(Sound.Hurt, 0.03f);
            g.Fx.Burst(Pos, new Color(1f, 0.3f, 0.36f), 10, 5f);
            return true;
        }

        public const float BlinkCd = 2.5f;
        public float BlinkCooldown { get; private set; }
        public float BlinkFrac => Mathf.Clamp01(BlinkCooldown / BlinkCd);

        /// <summary>A short dash in the facing direction with a moment of invulnerability.</summary>
        public void Blink()
        {
            if (BlinkCooldown > 0f || Hp <= 0f) return;
            Vector2 from = Pos;
            Pos += Facing * 3.4f;
            Pos.x = Mathf.Clamp(Pos.x, -Game.ArenaHalfW + Radius, Game.ArenaHalfW - Radius);
            Pos.y = Mathf.Clamp(Pos.y, -Game.ArenaHalfH + Radius, Game.ArenaHalfH - Radius);
            Iframes = Mathf.Max(Iframes, 0.4f);   // a split second of invulnerability
            BlinkCooldown = BlinkCd;
            for (int k = 0; k <= 4; k++) g.Fx.Burst(Vector2.Lerp(from, Pos, k / 4f), BodyColor, 4, 2.5f);
            g.Fx.Pop(from, BodyColor, 1.6f);
            g.Fx.Pop(Pos, Color.white, 2.2f);
            g.Fx.Pop(Pos, BodyColor, 3.4f);
            float deg = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg;
            for (int i = 0; i < GhostCap; i++)
            {
                float t = i / (float)(GhostCap - 1);
                ghosts[i].transform.position = Vector2.Lerp(from, Pos, t);
                ghosts[i].transform.rotation = Quaternion.Euler(0f, 0f, deg);
                ghostLife[i] = 0.2f + 0.3f * t;
                ghosts[i].enabled = true;
            }
            g.Shake(0.12f);
            g.Sfx.Play(Sound.Zap, 0.1f);
            Apply();
        }

        public void SetColor(Color c)
        {
            BodyColor = c;
            aura.color = new Color(c.r, c.g, c.b, 0.35f);
            Apply();
        }

        public void Heal(float amount) => Hp = Mathf.Min(MaxHp, Hp + amount);
    }
}
