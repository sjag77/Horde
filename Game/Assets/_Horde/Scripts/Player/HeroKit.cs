using UnityEngine;

namespace Horde
{
    public enum HeroId { Ember, Venom, Napalm }

    /// <summary>
    /// The chosen hero: colour, starting ability, and an innate passive (a weaker, always-on
    /// ability) with its own aura animation around the ship.
    /// </summary>
    public sealed class HeroKit
    {
        public const int Count = 3;
        const int AuraCap = 14;
        // v0.0.2: innates start tight and grow with your items - Wide Reach widens the ring,
        // Power Rune makes it bite harder. They are part of the build, not a fixed aura.
        const float EmberBase = 1.05f, NapalmBase = 0.95f, VenomBase = 0.62f, TAU = Mathf.PI * 2f;
        float EmberRadius => EmberBase * g.Player.AreaMul;
        float NapalmRadius => NapalmBase * g.Player.AreaMul;
        float VenomRadius => VenomBase * g.Player.AreaMul;
        static readonly Color EmberColor = new Color(1f, 0.5f, 0.22f);
        static readonly Color VenomColor = new Color(0.5f, 1f, 0.35f);
        static readonly Color NapalmColor = new Color(1f, 0.72f, 0.2f);

        readonly Game g;
        readonly SpriteRenderer[] aura = new SpriteRenderer[AuraCap];
        readonly SpriteRenderer ring, haze;
        float clock, applyTimer;
        public HeroId Id { get; private set; }

        public HeroKit(Game g)
        {
            this.g = g;
            var root = new GameObject("HeroAura").transform;
            haze = g.NewSprite("Haze", Sprites.Glow, Color.clear, 4, root);
            ring = g.NewSprite("AuraRing", Sprites.Ring, Color.clear, 4, root);
            for (int i = 0; i < AuraCap; i++) aura[i] = g.NewSprite("Aura", Sprites.Glow, Color.clear, 22, root);
        }

        public static string Name(HeroId id) => id switch { HeroId.Ember => "EMBER", HeroId.Venom => "VENOM", _ => "NAPALM" };

        public static string Title(HeroId id) => id switch
        {
            HeroId.Ember => "The Fire Walker",
            HeroId.Venom => "The Plague Caster",
            _ => "The Oil Burner"
        };

        public static string Innate(HeroId id) => id switch
        {
            HeroId.Ember => "Burning Aura: anything that comes close is set alight and melts (26 dmg/s). The ring starts small - Wide Reach grows it, Power Rune feeds it.",
            HeroId.Venom => "Toxic Touch: Spark Bolt, Chain Lightning and Orbit Blades poison enemies (7 dmg/s). Spark bounces from level 2 and hits a bit harder. Power Rune deepens the poison.",
            _ => "Oil Slick: enemies that touch your oil ring get coated - the ring grows with Wide Reach. Bombs, Meteors and Flame Wave set oiled enemies ablaze (9 dmg/s)."
        };

        public static AbilityId StartAbility(HeroId id) => id switch
        {
            HeroId.Ember => AbilityId.FrostNova,
            HeroId.Venom => AbilityId.SparkBolt,
            _ => AbilityId.Meteor
        };

        public static Color Tint(HeroId id) => id switch { HeroId.Ember => EmberColor, HeroId.Venom => VenomColor, _ => NapalmColor };

        public void Reset(HeroId id)
        {
            Id = id;
            clock = 0f;
            applyTimer = 0f;
            for (int i = 0; i < AuraCap; i++)
            {
                aura[i].sprite = id == HeroId.Napalm && i % 2 == 0 ? Sprites.Circle : Sprites.Glow;
                aura[i].enabled = id != HeroId.Venom || i < 8;
            }
            ring.enabled = id != HeroId.Venom;
            UpdateAura(g.Player.Pos, 0f);
        }

        public void Tick(float dt)
        {
            clock += dt;
            UpdateAura(g.Player.Pos, dt);
            applyTimer -= dt;
            if (applyTimer > 0f) return;
            applyTimer = 0.2f;
            if (Id == HeroId.Ember) ApplyAround(g.Player.Pos, EmberRadius, true);
            else if (Id == HeroId.Napalm) ApplyAround(g.Player.Pos, NapalmRadius, false);
        }

        /// <summary>Venom: direct spells (Spark, Chain, Orbit) poison what they hit.</summary>
        public void OnDirectHit(int i)
        {
            if (Id != HeroId.Venom) return;
            var e = g.Enemies;
            if (e.Alive[i]) e.Poison(i, 7f * g.Player.DamageMul, 3f);
        }

        /// <summary>Napalm: Bomb and Meteor blasts ignite oiled enemies.</summary>
        public void OnBlastHit(int i)
        {
            if (Id != HeroId.Napalm) return;
            var e = g.Enemies;
            if (e.Alive[i] && e.IsOiled(i)) e.Ignite(i, 9f * g.Player.DamageMul, 4f);
        }

        void ApplyAround(Vector2 c, float r, bool ignite)
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
                        if (ignite) e.Ignite(j, 26f * g.Player.DamageMul, 1.5f);   // Ember melts the early waves in a couple of ticks
                        else e.Oil(j, 4f);
                    }
        }

        void UpdateAura(Vector2 at, float dt)
        {
            haze.transform.position = ring.transform.position = at;
            switch (Id)
            {
                case HeroId.Ember:   // a ring of licking flames with embers drifting off it
                    ring.transform.localScale = Vector3.one * (EmberRadius * 2f + Mathf.Sin(clock * 6f) * 0.08f);
                    ring.color = new Color(1f, 0.45f, 0.1f, 0.22f + 0.08f * Mathf.Sin(clock * 9f));
                    haze.transform.localScale = Vector3.one * (EmberRadius * 2.5f);
                    haze.color = new Color(1f, 0.4f, 0.1f, 0.18f);
                    for (int k = 0; k < AuraCap; k++)
                    {
                        float a = clock * 1.6f + k * TAU / AuraCap;
                        float r = EmberRadius * (0.92f + 0.06f * Mathf.Sin(clock * 11f + k * 1.7f));
                        var tr = aura[k].transform;
                        tr.position = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        tr.localScale = Vector3.one * (0.45f + 0.2f * (0.5f + 0.5f * Mathf.Sin(clock * 14f + k * 2.3f)));
                        aura[k].color = k % 2 == 0 ? new Color(1f, 0.55f, 0.1f, 0.85f) : new Color(1f, 0.9f, 0.35f, 0.75f);
                    }
                    if (Random.value < dt * 10f)
                        g.Fx.Burst(at + Random.insideUnitCircle * EmberRadius, new Color(1f, 0.6f, 0.15f), 1, 1.5f);
                    break;

                case HeroId.Venom:   // toxic bubbles bobbing close around the ship
                    haze.transform.localScale = Vector3.one * (VenomRadius * 4.2f);
                    haze.color = new Color(0.4f, 1f, 0.3f, 0.2f + 0.06f * Mathf.Sin(clock * 4f));
                    for (int k = 0; k < 8; k++)
                    {
                        float a = clock * 2.4f + k * TAU / 8f;
                        float r = VenomRadius * (1.25f + 0.18f * Mathf.Sin(clock * 5f + k));
                        var tr = aura[k].transform;
                        tr.position = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        tr.localScale = Vector3.one * (0.22f + 0.08f * Mathf.Sin(clock * 7f + k * 1.9f));
                        aura[k].color = new Color(0.55f, 1f, 0.4f, 0.9f);
                    }
                    if (Random.value < dt * 6f)
                        g.Fx.Burst(at + Random.insideUnitCircle * VenomRadius, VenomColor, 1, 1.2f);
                    break;

                default:             // slow-turning slick of black oil with an amber sheen
                    ring.transform.localScale = Vector3.one * (NapalmRadius * 2f);
                    ring.color = new Color(0.45f, 0.28f, 0.06f, 0.4f);
                    haze.transform.localScale = Vector3.one * (NapalmRadius * 2.4f);
                    haze.color = new Color(1f, 0.6f, 0.1f, 0.14f);
                    for (int k = 0; k < AuraCap; k++)
                    {
                        float a = clock * 0.9f + k * TAU / AuraCap;
                        float r = NapalmRadius * (0.85f + 0.1f * Mathf.Sin(clock * 3f + k * 1.3f));
                        var tr = aura[k].transform;
                        tr.position = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                        bool blob = k % 2 == 0;
                        tr.localScale = Vector3.one * (blob ? 0.42f + 0.1f * Mathf.Sin(clock * 4f + k) : 0.3f);
                        aura[k].color = blob ? new Color(0.1f, 0.06f, 0.02f, 0.9f) : new Color(1f, 0.65f, 0.15f, 0.6f);
                    }
                    if (Random.value < dt * 5f)
                        g.Fx.Burst(at + Random.insideUnitCircle * NapalmRadius, new Color(0.35f, 0.22f, 0.05f), 1, 1f);
                    break;
            }
        }
    }
}
