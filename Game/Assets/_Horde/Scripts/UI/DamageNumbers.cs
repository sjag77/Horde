using UnityEngine;
using UnityEngine.UI;

namespace Horde
{
    /// <summary>Pooled floating numbers on the HUD canvas; digit strings are cached so hits don't allocate.</summary>
    public sealed class DamageNumbers
    {
        const int Capacity = 40;
        const float Life = 0.6f;
        static string[] digits;

        readonly Game g;
        readonly Text[] text = new Text[Capacity];
        readonly Vector2[] world = new Vector2[Capacity];
        readonly float[] age = new float[Capacity], scale = new float[Capacity];
        readonly Color[] color = new Color[Capacity];
        int count, recycle;

        public DamageNumbers(Game g, Hud hud)
        {
            this.g = g;
            if (digits == null)
            {
                digits = new string[1000];
                for (int i = 0; i < digits.Length; i++) digits[i] = i.ToString();
            }
            var layer = hud.CreateLayerBelowPanels("DamageNumbers");
            for (int i = 0; i < Capacity; i++)
            {
                var go = new GameObject("Number", typeof(RectTransform));
                var rt = (RectTransform)go.transform;
                rt.SetParent(layer, false);
                rt.sizeDelta = new Vector2(360f, 90f);
                var t = go.AddComponent<Text>();
                t.font = hud.Font;
                t.fontSize = 46;
                t.alignment = TextAnchor.MiddleCenter;
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
                outline.effectDistance = new Vector2(3f, -3f);
                t.enabled = false;
                text[i] = t;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < Capacity; i++) text[i].enabled = false;
            count = 0;
            recycle = 0;
        }

        public void Damage(Vector2 at, float amount)
        {
            int v = Mathf.Clamp(Mathf.RoundToInt(amount), 0, 999);
            Show(at + new Vector2(Random.Range(-0.2f, 0.2f), 0.3f), digits[v], Color.white, 1f);
        }

        public void ShowText(Vector2 at, string s, Color c) => Show(at + new Vector2(0f, 0.6f), s, c, 1.25f);

        void Show(Vector2 at, string s, Color c, float sc)
        {
            int i;
            if (count < Capacity) i = count++;
            else { i = recycle; recycle = (recycle + 1) % Capacity; }
            world[i] = at;
            age[i] = 0f;
            color[i] = c;
            scale[i] = sc;
            text[i].text = s;
            text[i].color = c;
            text[i].enabled = true;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < count; i++)
            {
                age[i] += dt;
                if (age[i] >= Life) { Remove(i); i--; continue; }
                float k = age[i] / Life;
                Vector2 screen = g.Cam.WorldToScreenPoint(world[i] + new Vector2(0f, k * 0.9f));
                var rt = text[i].rectTransform;
                rt.anchoredPosition = g.Hud.ScreenToCanvas(screen);
                float pop = k < 0.15f ? 1f + (0.15f - k) * 3f : 1f;
                rt.localScale = Vector3.one * (scale[i] * pop);
                var c = color[i];
                c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                text[i].color = c;
            }
        }

        void Remove(int i)
        {
            text[i].enabled = false;
            int last = --count;
            if (i != last)
            {
                (text[i], text[last]) = (text[last], text[i]);
                (world[i], world[last]) = (world[last], world[i]);
                (age[i], age[last]) = (age[last], age[i]);
                (scale[i], scale[last]) = (scale[last], scale[i]);
                (color[i], color[last]) = (color[last], color[i]);
            }
            if (recycle >= count) recycle = 0;
        }
    }
}
