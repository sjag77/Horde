using UnityEngine;

namespace Horde
{
    /// <summary>Four swirling holes near the arena corners. Step in and you warp to one of the other three.</summary>
    public sealed class Portals
    {
        public const int Count = 4;
        const float Inset = 3.2f, EnterRadius = 0.9f, ExitOffset = 1.8f;
        public const float WarpCooldown = 12f;   // all portals close for this long after a warp
        static readonly Color[] Colors =
        {
            new Color(1f, 0.35f, 0.85f), new Color(0.35f, 1f, 0.55f),
            new Color(1f, 0.6f, 0.2f), new Color(0.35f, 0.6f, 1f)
        };

        readonly Game g;
        readonly Vector2[] pos = new Vector2[Count];
        readonly SpriteRenderer[] glow = new SpriteRenderer[Count], core = new SpriteRenderer[Count];
        readonly SpriteRenderer[] ringA = new SpriteRenderer[Count], ringB = new SpriteRenderer[Count];
        float cooldown, clock;
        int visited;       // bit per portal entered this run
        bool secretDone;

        public Portals(Game g)
        {
            this.g = g;
            float cx = Game.ArenaHalfW - Inset, cy = Game.ArenaHalfH - Inset;
            pos[0] = new Vector2(-cx, cy);
            pos[1] = new Vector2(cx, cy);
            pos[2] = new Vector2(cx, -cy);
            pos[3] = new Vector2(-cx, -cy);
            var root = new GameObject("Portals").transform;
            for (int i = 0; i < Count; i++)
            {
                var c = Colors[i];
                glow[i] = g.NewSprite("PortalGlow", Sprites.Glow, new Color(c.r, c.g, c.b, 0.55f), 2, root);
                core[i] = g.NewSprite("PortalCore", Sprites.Circle, new Color(0.02f, 0.02f, 0.05f, 0.95f), 3, root);
                ringA[i] = g.NewSprite("PortalRing", Sprites.JoyBase, c, 4, root);
                ringB[i] = g.NewSprite("PortalRing", Sprites.Ring, c, 4, root);
                glow[i].transform.position = core[i].transform.position = ringA[i].transform.position = ringB[i].transform.position = pos[i];
                glow[i].transform.localScale = Vector3.one * 4f;
                core[i].transform.localScale = Vector3.one * 1.6f;
            }
        }

        public Vector2 PosAt(int i) => pos[i];
        public Color MapColor(int i) => Colors[i];
        public float CooldownFrac => Mathf.Clamp01(cooldown / WarpCooldown);

        public void Reset()
        {
            cooldown = 0f;
            visited = 0;
            secretDone = false;
        }

        public void Tick(float dt)
        {
            clock += dt;
            cooldown -= dt;
            for (int i = 0; i < Count; i++)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(clock * 3f + i);
                ringA[i].transform.localScale = Vector3.one * (2.4f + pulse * 0.2f);
                ringA[i].transform.rotation = Quaternion.Euler(0f, 0f, clock * 90f * (i % 2 == 0 ? 1f : -1f));
                ringB[i].transform.localScale = Vector3.one * (1.7f - pulse * 0.25f);
                bool open = cooldown <= 0f;   // closed portals go grey and dim until they recharge
                var c = open ? Colors[i] : new Color(0.45f, 0.45f, 0.5f);
                ringA[i].color = ringB[i].color = new Color(c.r, c.g, c.b, open ? 1f : 0.4f);
                glow[i].color = new Color(c.r, c.g, c.b, open ? 0.35f + 0.35f * pulse : 0.08f);
            }
            if (cooldown > 0f) return;

            var p = g.Player;
            for (int i = 0; i < Count; i++)
            {
                if ((p.Pos - pos[i]).sqrMagnitude > EnterRadius * EnterRadius) continue;
                int j = Random.Range(0, Count - 1);
                if (j >= i) j++;
                Warp(i, j);
                break;
            }
        }

        void Warp(int from, int to)
        {
            var p = g.Player;
            Vector2 dest = pos[to] - pos[to].normalized * ExitOffset;   // step out toward the arena centre
            g.Fx.Burst(p.Pos, Colors[from], 18, 6f);
            g.Fx.Pop(pos[from], Colors[from], 4f);
            p.Pos = dest;
            p.Iframes = Mathf.Max(p.Iframes, 0.6f);
            cooldown = WarpCooldown;
            g.Fx.Burst(dest, Colors[to], 18, 6f);
            g.Fx.Pop(pos[to], Colors[to], 4f);
            g.Numbers.ShowText(dest, "WARP", Colors[to]);
            g.SnapCamera();
            g.Shake(0.2f);
            g.Sfx.Play(Sound.Magnet, 0f);
            visited |= 1 << from;
            if (!secretDone && visited == (1 << Count) - 1) Secret();
        }

        // Enter all four portals in one run: every ability you own is maxed. Once per run.
        void Secret()
        {
            secretDone = true;
            g.Abilities.MaxAllOwned();
            var gold = new Color(1f, 0.85f, 0.2f);
            g.Hud.Banner("SECRET FOUND: ALL ABILITIES MAXED!", gold);
            g.Hud.FlashScreen(new Color(1f, 0.9f, 0.5f), 0.5f);
            g.Fx.Pop(g.Player.Pos, Color.white, 8f);
            g.Fx.Pop(g.Player.Pos, gold, 14f);
            g.Fx.Burst(g.Player.Pos, gold, 40, 12f);
            g.Sfx.Play(Sound.LevelUp, 0f);
            g.Shake(0.5f);
        }
    }
}
