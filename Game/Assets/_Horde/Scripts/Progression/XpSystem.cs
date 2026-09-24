using UnityEngine;

namespace Horde
{
    /// <summary>XP gems in parallel arrays. Gems inside the pickup radius fly to the hero.</summary>
    public sealed class XpSystem
    {
        static readonly Color SmallColor = new Color(0.4f, 0.95f, 1f);
        static readonly Color BigColor = new Color(0.78f, 0.52f, 1f);

        readonly Game g;
        readonly int capacity;
        int count;
        readonly Vector2[] pos;
        readonly float[] value;
        readonly bool[] pulled, rush;
        readonly Vector2[] rushFrom;
        readonly float[] rushT, rushDur;
        readonly SpriteRenderer[] sr;
        readonly Transform[] tr;

        public int PendingLevelUps;
        public int Count => count;
        public Vector2 PosAt(int i) => pos[i];

        public XpSystem(Game g, int capacity)
        {
            this.g = g;
            this.capacity = capacity;
            pos = new Vector2[capacity];
            value = new float[capacity];
            pulled = new bool[capacity];
            rush = new bool[capacity];
            rushFrom = new Vector2[capacity];
            rushT = new float[capacity];
            rushDur = new float[capacity];
            sr = new SpriteRenderer[capacity];
            tr = new Transform[capacity];

            var root = new GameObject("XpGems").transform;
            for (int i = 0; i < capacity; i++)
            {
                sr[i] = g.NewSprite("Gem", Sprites.Gem, SmallColor, 8, root);
                tr[i] = sr[i].transform;
                sr[i].enabled = false;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < capacity; i++) sr[i].enabled = false;
            count = 0;
            PendingLevelUps = 0;
        }

        public void Drop(Vector2 p, float v)
        {
            if (count >= capacity)
            {
                value[Random.Range(0, count)] += v;   // pool full: merge rather than lose XP
                return;
            }
            int i = count++;
            pos[i] = p;
            value[i] = v;
            pulled[i] = false;
            rush[i] = false;
            bool big = v >= 5f;
            sr[i].enabled = true;
            sr[i].color = big ? BigColor : SmallColor;
            tr[i].localScale = Vector3.one * (big ? 0.42f : 0.26f);
            tr[i].position = p;
        }

        public void Tick(float dt)
        {
            var player = g.Player;
            float pickup2 = player.PickupRadius * player.PickupRadius;
            for (int i = 0; i < count; i++)
            {
                if (rush[i])   // magnet: slow start, then a whoosh home
                {
                    rushT[i] += dt;
                    float k = Mathf.Min(1f, rushT[i] / rushDur[i]);
                    pos[i] = Vector2.LerpUnclamped(rushFrom[i], player.Pos, k * k * k);
                    tr[i].position = pos[i];
                    if (k >= 1f) { Collect(value[i]); Remove(i); i--; }
                    continue;
                }
                Vector2 d = player.Pos - pos[i];
                float d2 = d.sqrMagnitude;
                if (d2 < 0.35f * 0.35f)
                {
                    Collect(value[i]);
                    Remove(i);
                    i--;
                    continue;
                }
                if (d2 < pickup2) pulled[i] = true;
                if (pulled[i])
                {
                    float len = Mathf.Sqrt(d2);
                    pos[i] += d / len * Mathf.Min(len, 12f * dt);
                    tr[i].position = pos[i];
                }
            }
        }

        /// <summary>Sends every gem on the map to the hero; returns how many were pulled.</summary>
        public int RushAll()
        {
            Vector2 p = g.Player.Pos;
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                if (rush[i]) continue;
                rush[i] = true;
                rushFrom[i] = pos[i];
                rushT[i] = 0f;
                rushDur[i] = 0.25f + Vector2.Distance(pos[i], p) / 40f + Random.value * 0.1f;
                n++;
            }
            return n;
        }

        void Collect(float v)
        {
            var player = g.Player;
            player.Xp += v;
            g.Sfx.Play(Sound.Gem, 0.12f);
            while (player.Xp >= player.XpToNext)
            {
                player.Xp -= player.XpToNext;
                player.Level++;
                player.XpToNext = Player.XpFor(player.Level);
                PendingLevelUps++;
            }
        }

        void Remove(int i)
        {
            sr[i].enabled = false;
            int last = count - 1;
            if (i != last)
            {
                (pos[i], pos[last]) = (pos[last], pos[i]);
                (value[i], value[last]) = (value[last], value[i]);
                (pulled[i], pulled[last]) = (pulled[last], pulled[i]);
                (rush[i], rush[last]) = (rush[last], rush[i]);
                (rushFrom[i], rushFrom[last]) = (rushFrom[last], rushFrom[i]);
                (rushT[i], rushT[last]) = (rushT[last], rushT[i]);
                (rushDur[i], rushDur[last]) = (rushDur[last], rushDur[i]);
                (sr[i], sr[last]) = (sr[last], sr[i]);
                (tr[i], tr[last]) = (tr[last], tr[i]);
            }
            count--;
        }
    }
}
