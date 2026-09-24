using UnityEngine;

namespace Horde
{
    /// <summary>
    /// A tap-to-cast screen-clearing blast. Killing a mini-boss unlocks it; each further
    /// mini-boss ranks it up (more damage, shorter cooldown). Damage tracks enemy health
    /// growth, so it clears a wave at any point in the run.
    /// </summary>
    public sealed class Ultimate
    {
        public const int MaxRank = 3;
        static readonly float[] Cooldowns = { 0f, 90f, 75f, 60f };
        static readonly Color Gold = new Color(1f, 0.78f, 0.25f);

        readonly Game g;
        public int Rank { get; private set; }
        public float Cooldown { get; private set; }
        public float MaxCooldown => Cooldowns[Rank];
        public bool Ready => Rank > 0 && Cooldown <= 0f;

        public Ultimate(Game g) { this.g = g; }

        public void Reset()
        {
            Rank = 0;
            Cooldown = 0f;
        }

        public void Tick(float dt)
        {
            if (Cooldown > 0f) Cooldown = Mathf.Max(0f, Cooldown - dt);
        }

        public void OnBossKilled()
        {
            bool first = Rank == 0;
            Rank = Mathf.Min(MaxRank, Rank + 1);
            Cooldown = 0f;
            g.Hud.Banner(first ? "ULTIMATE UNLOCKED" : "ULTIMATE RANK " + Rank, Gold);
            g.Sfx.Play(Sound.LevelUp, 0f);
        }

        public void Cast()
        {
            if (g.State != GameState.Playing || !Ready) return;

            var p = g.Player;
            float healthGrowth = 1f + g.RunTime / 60f * 0.2f + Mathf.Max(0f, g.RunTime - 90f) / 60f * 0.7f + Mathf.Max(0f, g.RunTime - 540f) / 60f * 1.2f;   // same curve enemy HP uses
            float damage = 5000f * healthGrowth * (1f + 0.5f * (Rank - 1)) * p.DamageMul;
            var cam = g.Cam;                      // wipe out everything the player can see
            float vh = cam.orthographicSize, vw = vh * cam.aspect;
            g.Enemies.DamageInView(cam.transform.position, vw, vh, damage, 9f, 2.5f);

            g.Fx.Pop(p.Pos, Color.white, 6f);
            g.Fx.Pop(p.Pos, Gold, 16f);
            g.Fx.Pop(p.Pos, Gold, 26f);
            g.Fx.Burst(p.Pos, Gold, 40, 14f);
            g.Hud.FlashScreen(new Color(1f, 0.9f, 0.6f), 0.55f);
            g.Shake(0.9f);
            g.Sfx.Play(Sound.Ulti, 0f);
            p.Iframes = Mathf.Max(p.Iframes, 1f);
            Cooldown = MaxCooldown;
        }
    }
}
