using UnityEngine;

namespace Horde
{
    /// <summary>Pooled sparks and expanding rings: cheap juice with no ParticleSystem or extra materials.</summary>
    public sealed class Fx
    {
        const int SparkCap = 320, PopCap = 48;
        const float SparkHeight = 0.45f;   // sparks fly at chest height, rings stay on the floor
        const float PopTime = 0.28f;

        int sparks, pops;
        readonly Vector2[] sPos = new Vector2[SparkCap], sVel = new Vector2[SparkCap];
        readonly float[] sLife = new float[SparkCap], sMax = new float[SparkCap], sSize = new float[SparkCap];
        readonly Color[] sColor = new Color[SparkCap];
        readonly SpriteRenderer[] sSr = new SpriteRenderer[SparkCap];
        readonly Transform[] sTr = new Transform[SparkCap];
        readonly float[] pLife = new float[PopCap], pSize = new float[PopCap];
        readonly Color[] pColor = new Color[PopCap];
        readonly SpriteRenderer[] pSr = new SpriteRenderer[PopCap];
        readonly Transform[] pTr = new Transform[PopCap];

        readonly Game game;

        public Fx(Game g)
        {
            game = g;
            var root = new GameObject("Fx").transform;
            for (int i = 0; i < SparkCap; i++)
            {
                sSr[i] = g.NewSprite("Spark", Sprites.Square, Color.white, 22, root);
                sTr[i] = sSr[i].transform;
                sSr[i].enabled = false;
            }
            for (int i = 0; i < PopCap; i++)
            {
                pSr[i] = g.NewSprite("Pop", Sprites.Ring, Color.white, 21, root);
                pTr[i] = pSr[i].transform;
                pSr[i].enabled = false;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < SparkCap; i++) sSr[i].enabled = false;
            for (int i = 0; i < PopCap; i++) pSr[i].enabled = false;
            sparks = pops = 0;
        }

        public void Burst(Vector2 at, Color color, int n, float speed)
        {
            for (int k = 0; k < n; k++)
            {
                int i = sparks < SparkCap ? sparks++ : Random.Range(0, SparkCap); // full: recycle one
                float a = Random.value * Mathf.PI * 2f, v = speed * (0.35f + Random.value * 0.65f);
                sPos[i] = at;
                sVel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v;
                sMax[i] = sLife[i] = 0.18f + Random.value * 0.22f;
                sSize[i] = 0.06f + Random.value * 0.08f;
                sColor[i] = color;
                sSr[i].enabled = true;
                sTr[i].SetPositionAndRotation(Rig.At(at, SparkHeight), game.Billboard);   // sparks face the camera
            }
        }

        public void Pop(Vector2 at, Color color, float size)
        {
            int i = pops < PopCap ? pops++ : Random.Range(0, PopCap);
            pLife[i] = PopTime;
            pSize[i] = size;
            pColor[i] = color;
            pSr[i].enabled = true;
            pTr[i].position = at;
            pTr[i].localScale = Vector3.one * (size * 0.3f);
        }

        public void Tick(float dt)
        {
            float drag = Mathf.Exp(-6f * dt);
            for (int i = 0; i < sparks; i++)
            {
                sLife[i] -= dt;
                if (sLife[i] <= 0f) { RemoveSpark(i); i--; continue; }
                sPos[i] += sVel[i] * dt;
                sVel[i] *= drag;
                float k = sLife[i] / sMax[i];
                float s = sSize[i] * (0.4f + k);
                sTr[i].position = Rig.At(sPos[i], SparkHeight);
                sTr[i].localScale = new Vector3(s, s, 1f);
                var c = sColor[i]; c.a = k; sSr[i].color = c;
            }
            for (int i = 0; i < pops; i++)
            {
                pLife[i] -= dt;
                if (pLife[i] <= 0f) { RemovePop(i); i--; continue; }
                float k = 1f - pLife[i] / PopTime;
                float s = pSize[i] * (0.3f + 0.7f * (1f - (1f - k) * (1f - k)));
                pTr[i].localScale = new Vector3(s, s, 1f);
                var c = pColor[i]; c.a = 1f - k; pSr[i].color = c;
            }
        }

        void RemoveSpark(int i)
        {
            sSr[i].enabled = false;
            int last = --sparks;
            if (i == last) return;
            (sPos[i], sPos[last]) = (sPos[last], sPos[i]);
            (sVel[i], sVel[last]) = (sVel[last], sVel[i]);
            (sLife[i], sLife[last]) = (sLife[last], sLife[i]);
            (sMax[i], sMax[last]) = (sMax[last], sMax[i]);
            (sSize[i], sSize[last]) = (sSize[last], sSize[i]);
            (sColor[i], sColor[last]) = (sColor[last], sColor[i]);
            (sSr[i], sSr[last]) = (sSr[last], sSr[i]);
            (sTr[i], sTr[last]) = (sTr[last], sTr[i]);
        }

        void RemovePop(int i)
        {
            pSr[i].enabled = false;
            int last = --pops;
            if (i == last) return;
            (pLife[i], pLife[last]) = (pLife[last], pLife[i]);
            (pSize[i], pSize[last]) = (pSize[last], pSize[i]);
            (pColor[i], pColor[last]) = (pColor[last], pColor[i]);
            (pSr[i], pSr[last]) = (pSr[last], pSr[i]);
            (pTr[i], pTr[last]) = (pTr[last], pTr[i]);
        }
    }
}
