using System.Collections.Generic;
using UnityEngine;

namespace Horde
{
    public sealed class UpgradeOption
    {
        public string Title;
        public string Desc;
        public System.Action Apply;
    }

    /// <summary>Builds the "pick 1 of 3" choices offered on every level-up.</summary>
    public sealed class Upgrades
    {
        readonly Game g;
        readonly List<UpgradeOption> pool = new List<UpgradeOption>(12);
        readonly List<UpgradeOption> picks = new List<UpgradeOption>(3);
        int speedRank, hpRank, cooldownRank, magnetRank, damageRank, areaRank;

        public Upgrades(Game g) { this.g = g; }

        public void Reset() => speedRank = hpRank = cooldownRank = magnetRank = damageRank = areaRank = 0;

        public List<UpgradeOption> Roll()
        {
            var abilities = g.Abilities;
            var player = g.Player;
            pool.Clear();

            for (int k = 0; k < AbilitySet.Count; k++)
            {
                var id = (AbilityId)k;
                int lv = abilities.Level(id);
                if (lv >= AbilitySet.MaxLevel || (lv == 0 && abilities.OwnedCount >= abilities.SlotLimit)) continue;   // 4 slots, +1 per boss reward (max 6)
                pool.Add(new UpgradeOption
                {
                    Title = lv == 0 ? "NEW  " + AbilitySet.Name(id) : AbilitySet.Name(id) + "  Lv " + (lv + 1),
                    Desc = AbilitySet.Describe(id, lv + 1),
                    Apply = () => abilities.Grant(id)
                });
            }

            if (damageRank < 6)
                pool.Add(new UpgradeOption { Title = "Power Rune", Desc = "+15% damage for every ability.", Apply = () => { damageRank++; player.DamageMul *= 1.15f; } });
            if (cooldownRank < 5)
                pool.Add(new UpgradeOption { Title = "Quickening", Desc = "Abilities recharge 8% faster.", Apply = () => { cooldownRank++; player.CooldownMul *= 0.92f; } });
            if (areaRank < 5)
                pool.Add(new UpgradeOption { Title = "Wide Reach", Desc = "+12% ability area.", Apply = () => { areaRank++; player.AreaMul *= 1.12f; } });
            if (hpRank < 5)
                pool.Add(new UpgradeOption { Title = "Vitality", Desc = "+25 max HP and heal 25.", Apply = () => { hpRank++; player.MaxHp += 25f; player.Heal(25f); } });
            if (speedRank < 5)
                pool.Add(new UpgradeOption { Title = "Swift Boots", Desc = "+10% move speed.", Apply = () => { speedRank++; player.MoveSpeed *= 1.10f; } });
            if (magnetRank < 4)
                pool.Add(new UpgradeOption { Title = "Magnet Core", Desc = "+35% XP pickup radius.", Apply = () => { magnetRank++; player.PickupRadius *= 1.35f; } });

            picks.Clear();
            while (picks.Count < 3 && pool.Count > 0)
            {
                int k = Random.Range(0, pool.Count);
                picks.Add(pool[k]);
                pool.RemoveAt(k);
            }
            return picks;
        }
    }
}
