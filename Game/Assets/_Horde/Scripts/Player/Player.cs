using UnityEngine;

namespace Horde
{
    public sealed class Player
    {
        public const float Radius = 0.38f;
        Color BodyColor = new Color(0.35f, 0.95f, 1f);
        public Color HeroColor => BodyColor;

        readonly Game g;
        readonly SpriteRenderer aura;      // ground glow under the ship
        readonly Transform auraTr;
        readonly HeroRig rig;
        float hurtFlash;
        Vector2 lastMove;

        public Vector2 Pos;
        public Vector2 Facing = Vector2.up;
        public float MaxHp, Hp, MoveSpeed, PickupRadius, DamageMul, CooldownMul, AreaMul, Iframes;
        public int Level;
        public float Xp, XpToNext;

        public Player(Game g)
        {
            this.g = g;
            aura = g.NewSprite("PlayerAura", Sprites.Glow, new Color(0.3f, 0.9f, 1f, 0.35f), 5);
            auraTr = aura.transform;
            auraTr.localScale = Vector3.one * 2.4f;
            rig = new HeroRig(g);
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
            rig.ClearGhosts();
            rig.Visible = true;
            hurtFlash = 0f;
            Level = 1;
            Xp = 0f;
            XpToNext = XpFor(1);
            Apply();
        }

        public void Tick(float dt, Vector2 move)
        {
            if (move.sqrMagnitude > 0.0001f) Facing = move.normalized;
            lastMove = move;
            Pos += move * (MoveSpeed * dt);
            Pos.x = Mathf.Clamp(Pos.x, -Game.ArenaHalfW + Radius, Game.ArenaHalfW - Radius);
            Pos.y = Mathf.Clamp(Pos.y, -Game.ArenaHalfH + Radius, Game.ArenaHalfH - Radius);
            if (Iframes > 0f) Iframes -= dt;
            if (BlinkCooldown > 0f) BlinkCooldown -= dt;
            if (hurtFlash > 0f) hurtFlash -= dt * 4f;
            rig.Tick(dt, Pos, Facing, lastMove, hurtFlash, Iframes > 0f);
            Apply();
        }

        void Apply()
        {
            auraTr.position = Pos;
            // blink the ship while invulnerable, exactly as the 2D build did
            rig.Visible = !(Iframes > 0f && ((int)(Iframes * 20f)) % 2 == 0);
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
        public const float DashDistance = 5.8f;   // v0.0.2: a real escape, not a hop
        public float BlinkCooldown { get; private set; }
        public float BlinkFrac => Mathf.Clamp01(BlinkCooldown / BlinkCd);

        /// <summary>A short dash in the facing direction with a moment of invulnerability.</summary>
        public void Blink()
        {
            if (BlinkCooldown > 0f || Hp <= 0f) return;
            Vector2 from = Pos;
            Pos += Facing * DashDistance;
            Pos.x = Mathf.Clamp(Pos.x, -Game.ArenaHalfW + Radius, Game.ArenaHalfW - Radius);
            Pos.y = Mathf.Clamp(Pos.y, -Game.ArenaHalfH + Radius, Game.ArenaHalfH - Radius);
            Iframes = Mathf.Max(Iframes, 0.5f);   // a split second of invulnerability
            BlinkCooldown = BlinkCd;
            for (int k = 0; k <= 7; k++) g.Fx.Burst(Vector2.Lerp(from, Pos, k / 7f), BodyColor, 4, 2.5f);
            g.Fx.Pop(from, BodyColor, 1.6f);
            g.Fx.Pop(Pos, Color.white, 2.2f);
            g.Fx.Pop(Pos, BodyColor, 3.4f);
            rig.Dash(from, Pos, Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg);
            g.Shake(0.12f);
            g.Sfx.Play(Sound.Zap, 0.1f);
            Apply();
        }

        public void SetColor(Color c)
        {
            BodyColor = c;
            aura.color = new Color(c.r, c.g, c.b, 0.35f);
            rig.SetColor(c);
            Apply();
        }

        public void Heal(float amount) => Hp = Mathf.Min(MaxHp, Hp + amount);
    }
}
