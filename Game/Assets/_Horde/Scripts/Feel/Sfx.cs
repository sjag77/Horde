using UnityEngine;

namespace Horde
{
    public enum Sound { Hit, Kill, Shoot, Gem, Hurt, LevelUp, Cheese, Magnet, Zap, Boom, Nova, Ulti, BossWarn }

    /// <summary>
    /// Sounds are synthesised into AudioClips at startup (no audio assets) and played through a
    /// small source pool with per-sound throttling, so a big fight stays readable, not noisy.
    /// </summary>
    public sealed class Sfx
    {
        const int Rate = 44100;
        readonly AudioClip[] clips;
        readonly float[] volume, minGap, lastPlayed;
        readonly AudioSource[] sources = new AudioSource[6];
        int next;

        public Sfx(GameObject host)
        {
            int n = System.Enum.GetValues(typeof(Sound)).Length;
            clips = new AudioClip[n];
            volume = new float[n];
            minGap = new float[n];
            lastPlayed = new float[n];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = host.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }
            Define(Sound.Hit, 0.35f, 0.035f, Noise(0.045f, 2200f));
            Define(Sound.Kill, 0.45f, 0.03f, Sweep(0.08f, 900f, 260f, 0.5f));
            Define(Sound.Shoot, 0.2f, 0.08f, Sweep(0.06f, 1800f, 900f, 0.2f));
            Define(Sound.Gem, 0.22f, 0.045f, Sweep(0.05f, 1400f, 2100f, 0f));
            Define(Sound.Hurt, 0.7f, 0.1f, Sweep(0.18f, 220f, 70f, 0.6f));
            Define(Sound.LevelUp, 0.6f, 0.3f, Arp(new[] { 523f, 659f, 784f, 1047f }, 0.07f));
            Define(Sound.Cheese, 0.6f, 0.2f, Arp(new[] { 660f, 990f }, 0.09f));
            Define(Sound.Magnet, 0.55f, 0.2f, Sweep(0.45f, 300f, 1600f, 0.1f));
            Define(Sound.Zap, 0.3f, 0.12f, Sweep(0.09f, 2600f, 700f, 0.45f));
            Define(Sound.Boom, 0.55f, 0.1f, Sweep(0.35f, 170f, 45f, 0.65f));
            Define(Sound.Nova, 0.4f, 0.2f, Sweep(0.25f, 900f, 300f, 0.15f));
            Define(Sound.Ulti, 0.9f, 0.5f, Sweep(0.9f, 130f, 30f, 0.8f));
            Define(Sound.BossWarn, 0.7f, 1f, Arp(new[] { 220f, 165f, 220f, 165f }, 0.12f));
        }

        public void Play(Sound s, float pitchJitter = 0.08f)
        {
            int i = (int)s;
            float now = Time.unscaledTime;
            if (now - lastPlayed[i] < minGap[i]) return;
            lastPlayed[i] = now;
            var src = sources[next];
            next = (next + 1) % sources.Length;
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clips[i], volume[i]);
        }

        void Define(Sound s, float vol, float gap, float[] data)
        {
            var clip = AudioClip.Create(s.ToString(), data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            int i = (int)s;
            clips[i] = clip;
            volume[i] = vol;
            minGap[i] = gap;
            lastPlayed[i] = -1f;
        }

        static float[] Sweep(float seconds, float f0, float f1, float noise)
        {
            int len = Mathf.CeilToInt(seconds * Rate);
            var d = new float[len];
            double phase = 0;
            uint seed = 12345;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                phase += 2.0 * System.Math.PI * Mathf.Lerp(f0, f1, t) / Rate;
                float env = Mathf.Exp(-5f * t) * Mathf.Min(1f, i / 64f);
                d[i] = ((float)System.Math.Sin(phase) * (1f - noise) + NextNoise(ref seed) * noise) * env;
            }
            return d;
        }

        static float[] Noise(float seconds, float cutoff)
        {
            int len = Mathf.CeilToInt(seconds * Rate);
            var d = new float[len];
            uint seed = 777;
            float a = Mathf.Clamp01(2f * Mathf.PI * cutoff / Rate), y = 0f;
            for (int i = 0; i < len; i++)
            {
                y += a * (NextNoise(ref seed) - y);
                d[i] = Mathf.Clamp(y * 2.5f, -1f, 1f) * Mathf.Exp(-7f * i / len);
            }
            return d;
        }

        static float[] Arp(float[] notes, float step)
        {
            int per = Mathf.CeilToInt(step * Rate);
            int len = per * (notes.Length + 2);
            var d = new float[len];
            for (int n = 0; n < notes.Length; n++)
            {
                double phase = 0;
                int start = n * per, noteLen = per * 3;
                for (int i = 0; i < noteLen && start + i < len; i++)
                {
                    phase += 2.0 * System.Math.PI * notes[n] / Rate;
                    float tri = (float)(2.0 / System.Math.PI * System.Math.Asin(System.Math.Sin(phase)));
                    d[start + i] += tri * 0.33f * Mathf.Exp(-4f * i / noteLen) * Mathf.Min(1f, i / 64f);
                }
            }
            return d;
        }

        static float NextNoise(ref uint s)
        {
            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
            return s / (float)uint.MaxValue * 2f - 1f;
        }
    }
}
