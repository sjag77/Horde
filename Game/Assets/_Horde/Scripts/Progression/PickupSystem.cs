using UnityEngine;

namespace Horde
{
    public enum PickupKind : byte { Cheese, Magnet }

    /// <summary>
    /// Two rare pickups that sit on the floor until the hero walks onto them:
    /// cheese heals, a magnet pulls every XP gem on the map in.
    /// </summary>
    public sealed class PickupSystem
    {
        const int Capacity = 16, MaxOnGround = 2;
        const float CheeseChance = 0.006f, MagnetChance = 0.004f;
        static readonly Color CheeseColor = new Color(1f, 0.82f, 0.3f);
        static readonly Color MagnetColor = new Color(1f, 0.28f, 0.35f);
        static readonly Color HealColor = new Color(0.45f, 1f, 0.6f);

        readonly Game g;
        readonly Vector2[] pos = new Vector2[Capacity];
        readonly PickupKind[] kind = new PickupKind[Capacity];
        readonly float[] age = new float[Capacity];
        readonly SpriteRenderer[] body = new SpriteRenderer[Capacity];
        readonly SpriteRenderer[] glow = new SpriteRenderer[Capacity];
        readonly SpriteRenderer[] tipL = new SpriteRenderer[Capacity];
        readonly SpriteRenderer[] tipR = new SpriteRenderer[Capacity];
        int count;
        float fullHintCooldown;
        public int Count => count;
        public Vector2 PosAt(int i) => pos[i];
        public bool IsCheese(int i) => kind[i] == PickupKind.Cheese;

        public PickupSystem(Game g)
        {
            this.g = g;
            var root = new GameObject("Pickups").transform;
            for (int i = 0; i < Capacity; i++)
            {
                glow[i] = g.NewSprite("PickupGlow", Sprites.Glow, Color.white, 7, root);
                body[i] = g.NewSprite("Pickup", Sprites.Cheese, Color.white, 9, root);
                body[i].transform.localScale = Vector3.one * 0.85f;
                tipL[i] = g.NewSprite("Tip", Sprites.Square, Color.white, 10, body[i].transform);
                tipR[i] = g.NewSprite("Tip", Sprites.Square, Color.white, 10, body[i].transform);
                tipL[i].transform.localPosition = new Vector3(-0.30f, -0.36f, 0f);
                tipR[i].transform.localPosition = new Vector3(0.30f, -0.36f, 0f);
                tipL[i].transform.localScale = new Vector3(0.21f, 0.09f, 1f);
                tipR[i].transform.localScale = new Vector3(0.21f, 0.09f, 1f);
                SetVisible(i, false);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < Capacity; i++) SetVisible(i, false);
            count = 0;
            fullHintCooldown = 0f;
        }

        public void MaybeDrop(Vector2 at, bool brute)
        {
            float m = brute ? 4f : 1f, r = Random.value;
            if (r < CheeseChance * m) Spawn(at, PickupKind.Cheese);
            else if (r < (CheeseChance + MagnetChance) * m) Spawn(at, PickupKind.Magnet);
        }

        void Spawn(Vector2 at, PickupKind k)
        {
            if (count >= MaxOnGround) return;
            int i = count++;
            pos[i] = new Vector2(Mathf.Clamp(at.x, -Game.ArenaHalfW + 0.6f, Game.ArenaHalfW - 0.6f),
                                 Mathf.Clamp(at.y, -Game.ArenaHalfH + 0.6f, Game.ArenaHalfH - 0.6f));
            kind[i] = k;
            age[i] = 0f;
            body[i].sprite = k == PickupKind.Cheese ? Sprites.Cheese : Sprites.Magnet;
            body[i].color = k == PickupKind.Cheese ? CheeseColor : MagnetColor;
            SetVisible(i, true);
            g.Fx.Pop(pos[i], body[i].color, 2f);
        }

        public void Tick(float dt)
        {
            fullHintCooldown -= dt;
            var player = g.Player;
            float reach = Player.Radius + 0.45f;
            for (int i = 0; i < count; i++)
            {
                age[i] += dt;
                float pulse = 0.5f + Mathf.Sin(age[i] * 4f) * 0.5f;
                body[i].transform.position = pos[i] + new Vector2(0f, Mathf.Sin(age[i] * 3.2f) * 0.06f);
                glow[i].transform.position = pos[i];
                glow[i].transform.localScale = Vector3.one * (1.6f + pulse * 0.3f);
                var gc = body[i].color; gc.a = 0.25f + pulse * 0.25f; glow[i].color = gc;

                if ((pos[i] - player.Pos).sqrMagnitude > reach * reach) continue;
                if (kind[i] == PickupKind.Cheese && player.Hp >= player.MaxHp)
                {
                    if (fullHintCooldown <= 0f)
                    {
                        g.Numbers.ShowText(pos[i], "HP FULL", CheeseColor);   // it waits for you
                        fullHintCooldown = 1.5f;
                    }
                    continue;
                }
                Take(i);
                Remove(i);
                i--;
            }
        }

        void Take(int i)
        {
            var player = g.Player;
            if (kind[i] == PickupKind.Cheese)
            {
                float heal = Mathf.Min(player.MaxHp * 0.25f, player.MaxHp - player.Hp);
                player.Heal(heal);
                g.Numbers.ShowText(player.Pos, "+" + Mathf.RoundToInt(heal) + " HP", HealColor);
                g.Fx.Burst(pos[i], CheeseColor, 18, 5f);
                g.Fx.Pop(pos[i], CheeseColor, 2.6f);
                g.Sfx.Play(Sound.Cheese, 0f);
            }
            else
            {
                int n = g.Xp.RushAll();
                g.Numbers.ShowText(player.Pos, n > 0 ? "MAGNET" : "NO XP LEFT", MagnetColor);
                g.Fx.Burst(pos[i], MagnetColor, 20, 6f);
                g.Fx.Pop(player.Pos, MagnetColor, 7f);
                g.Sfx.Play(Sound.Magnet, 0f);
                g.Shake(0.3f);
            }
        }

        void SetVisible(int i, bool on)
        {
            body[i].enabled = on;
            glow[i].enabled = on;
            bool tips = on && kind[i] == PickupKind.Magnet;
            tipL[i].enabled = tips;
            tipR[i].enabled = tips;
        }

        void Remove(int i)
        {
            SetVisible(i, false);
            int last = --count;
            if (i == last) return;
            (pos[i], pos[last]) = (pos[last], pos[i]);
            (kind[i], kind[last]) = (kind[last], kind[i]);
            (age[i], age[last]) = (age[last], age[i]);
            (body[i], body[last]) = (body[last], body[i]);
            (glow[i], glow[last]) = (glow[last], glow[i]);
            (tipL[i], tipL[last]) = (tipL[last], tipL[i]);
            (tipR[i], tipR[last]) = (tipR[last], tipR[i]);
        }
    }
}
