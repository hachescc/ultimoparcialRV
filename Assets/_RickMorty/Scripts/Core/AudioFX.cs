using System.Collections.Generic;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Audio espacial con pool de AudioSources. Cada sonido se busca primero en
    /// Resources/Audio/SFX/&lt;nombre&gt; (para que el equipo ponga sonidos reales) y si no
    /// existe se sintetiza un placeholder procedural.
    /// </summary>
    public static class AudioFX
    {
        const int PoolSize = 20;
        static readonly List<AudioSource> s_Pool = new List<AudioSource>();
        static readonly Dictionary<string, AudioClip> s_Clips = new Dictionary<string, AudioClip>();
        static int s_Next;
        static Transform s_Root;

        static void EnsurePool()
        {
            if (s_Root != null) return;
            s_Pool.Clear();
            s_Root = new GameObject("[AudioFX Pool]").transform;
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("sfx_" + i);
                go.transform.SetParent(s_Root);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 1f;
                src.maxDistance = 25f;
                src.dopplerLevel = 0f;
                s_Pool.Add(src);
            }
        }

        public static AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (s_Clips.TryGetValue(name, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>("Audio/SFX/" + name);
            if (c == null) c = SfxSynth.Make(name);
            s_Clips[name] = c;
            return c;
        }

        public static AudioSource Play(string name, Vector3 position, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            var clip = Clip(name);
            if (clip == null) return null;
            EnsurePool();
            AudioSource src = null;
            for (int i = 0; i < s_Pool.Count; i++)
            {
                var candidate = s_Pool[(s_Next + i) % s_Pool.Count];
                if (!candidate.isPlaying) { src = candidate; s_Next = (s_Next + i + 1) % s_Pool.Count; break; }
            }
            if (src == null) { src = s_Pool[s_Next]; s_Next = (s_Next + 1) % s_Pool.Count; }
            src.transform.position = position;
            src.spatialBlend = spatial ? 1f : 0f;
            src.volume = volume;
            src.pitch = pitch;
            src.clip = clip;
            src.loop = false;
            src.Play();
            return src;
        }

        /// <summary>Sonido 2D (por ejemplo el reloj o la radio de Rick).</summary>
        public static AudioSource Play2D(string name, float volume = 1f, float pitch = 1f)
        {
            var cam = Camera.main;
            return Play(name, cam != null ? cam.transform.position : Vector3.zero, volume, pitch, false);
        }

        /// <summary>Crea un AudioSource en loop pegado a un objeto (zumbidos, burbujeo...).</summary>
        public static AudioSource AttachLoop(GameObject go, string name, float volume = 0.3f, float maxDistance = 6f, float pitch = 1f)
        {
            var src = go.AddComponent<AudioSource>();
            src.clip = Clip(name);
            src.loop = true;
            src.spatialBlend = 1f;
            src.volume = volume;
            src.pitch = pitch;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 0.3f;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            src.playOnAwake = true;
            if (src.clip != null) src.Play();
            return src;
        }
    }

    /// <summary>Sintetizador de efectos placeholder (para no depender de assets externos).</summary>
    public static class SfxSynth
    {
        const int Rate = 22050;

        public static AudioClip Make(string name)
        {
            var rng = new System.Random(name.GetHashCode());
            float[] d;
            switch (name)
            {
                case "beep": d = Tone(880, 0.12f, Wave.Sine, 0.5f); break;
                case "watch_ping": d = Concat(Tone(1200, 0.07f, Wave.Sine, 0.5f), Silence(0.03f), Tone(1600, 0.09f, Wave.Sine, 0.5f)); break;
                case "arcade_win": d = Concat(Tone(523, 0.08f, Wave.Square, 0.25f), Tone(659, 0.08f, Wave.Square, 0.25f), Tone(784, 0.08f, Wave.Square, 0.25f), Tone(1047, 0.25f, Wave.Square, 0.25f)); break;
                case "explosion": d = Mix(Noise(rng, 1.4f, 0.9f, 3.0f, 0.08f), Sweep(90, 35, 0.9f, Wave.Sine, 0.9f)); break;
                case "tinnitus": d = Fade(Tone(4200, 3f, Wave.Sine, 0.12f), 0.05f, 2.5f); break;
                case "splat": d = Mix(Noise(rng, 0.3f, 0.8f, 9f, 0.15f), Sweep(300, 60, 0.3f, Wave.Sine, 0.6f)); break;
                case "portal": d = Wobble(Sweep(180, 520, 1.3f, Wave.Sine, 0.5f), 9f, 0.4f); break;
                case "hum": d = Mix(Tone(90, 1f, Wave.Sine, 0.45f, false), Tone(180, 1f, Wave.Sine, 0.2f, false)); break;
                case "bubbles": d = Bubbles(rng, 1.5f); break;
                case "laser": d = Sweep(1800, 250, 0.25f, Wave.Square, 0.3f); break;
                case "shot": d = Noise(rng, 0.12f, 0.8f, 30f, 0.5f); break;
                case "sniper": d = Mix(Noise(rng, 0.6f, 1f, 6f, 0.3f), Sweep(200, 50, 0.4f, Wave.Sine, 0.8f)); break;
                case "chomp": d = Concat(Noise(rng, 0.06f, 0.6f, 20f, 0.2f), Silence(0.08f), Noise(rng, 0.06f, 0.6f, 20f, 0.2f)); break;
                case "alarm": d = Concat(Tone(700, 0.25f, Wave.Square, 0.25f), Tone(950, 0.25f, Wave.Square, 0.25f), Tone(700, 0.25f, Wave.Square, 0.25f), Tone(950, 0.25f, Wave.Square, 0.25f)); break;
                case "scream": d = Wobble(Sweep(500, 850, 1.0f, Wave.Saw, 0.25f), 14f, 0.08f); break;
                case "button": d = Concat(Tone(1500, 0.02f, Wave.Square, 0.3f), Tone(900, 0.03f, Wave.Square, 0.2f)); break;
                case "impact_metal": d = Mix(Decay(Tone(620, 0.5f, Wave.Sine, 0.4f), 7f), Decay(Tone(1430, 0.5f, Wave.Sine, 0.25f), 9f)); break;
                case "impact_glass": d = Mix(Decay(Tone(2600, 0.35f, Wave.Sine, 0.3f), 10f), Decay(Tone(3900, 0.35f, Wave.Sine, 0.2f), 12f)); break;
                case "impact_flesh": d = Mix(Noise(rng, 0.18f, 0.7f, 18f, 0.08f), Decay(Tone(110, 0.18f, Wave.Sine, 0.5f), 15f)); break;
                case "impact_wood": d = Mix(Noise(rng, 0.12f, 0.6f, 25f, 0.2f), Decay(Tone(240, 0.15f, Wave.Sine, 0.4f), 20f)); break;
                case "whoosh": d = Fade(Noise(rng, 0.45f, 0.4f, 0f, 0.3f), 0.2f, 0.25f); break;
                case "burp": d = Wobble(Sweep(95, 70, 0.6f, Wave.Saw, 0.35f), 25f, 0.2f); break;
                case "chord": d = Fade(Mix(Tone(220, 3.5f, Wave.Saw, 0.12f), Tone(277.2f, 3.5f, Wave.Saw, 0.12f), Tone(329.6f, 3.5f, Wave.Saw, 0.12f), Tone(440, 3.5f, Wave.Sine, 0.15f)), 0.6f, 1.5f); break;
                case "bark": d = Concat(Mix(Sweep(520, 300, 0.15f, Wave.Saw, 0.3f), Noise(rng, 0.15f, 0.3f, 15f, 0.3f)), Silence(0.1f), Mix(Sweep(560, 280, 0.18f, Wave.Saw, 0.3f), Noise(rng, 0.18f, 0.3f, 15f, 0.3f))); break;
                case "siren": d = Concat(Sweep(600, 1200, 0.8f, Wave.Sine, 0.3f), Sweep(1200, 600, 0.8f, Wave.Sine, 0.3f)); break;
                case "static": d = Noise(rng, 0.5f, 0.25f, 0f, 0.6f); break;
                case "rummage": d = Rummage(rng, 2f); break;
                case "dust": d = Fade(Noise(rng, 0.7f, 0.35f, 4f, 0.05f), 0.02f, 0.5f); break;
                case "fuuump": d = Mix(Sweep(320, 40, 0.7f, Wave.Sine, 0.6f), Fade(Noise(rng, 0.7f, 0.25f, 4f, 0.05f), 0.05f, 0.5f)); break;
                case "zap": d = Wobble(Noise(rng, 0.3f, 0.5f, 8f, 0.6f), 60f, 0.5f); break;
                case "saber_on": d = Sweep(80, 160, 0.4f, Wave.Saw, 0.3f); break;
                case "snap": d = Noise(rng, 0.05f, 0.9f, 60f, 0.9f); break;
                case "meeseeks": d = Wobble(Sweep(600, 900, 0.5f, Wave.Square, 0.2f), 12f, 0.1f); break;
                default: d = Tone(440, 0.15f, Wave.Sine, 0.4f); break;
            }
            var clip = AudioClip.Create(name, d.Length, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        enum Wave { Sine, Square, Saw }

        static float Osc(Wave w, float phase)
        {
            switch (w)
            {
                case Wave.Square: return Mathf.Sin(phase) >= 0 ? 1f : -1f;
                case Wave.Saw: return 2f * ((phase / (2f * Mathf.PI)) % 1f) - 1f;
                default: return Mathf.Sin(phase);
            }
        }

        static float[] Tone(float hz, float dur, Wave w, float vol, bool envelope = true)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float env = envelope ? Mathf.Clamp01(t / 0.005f) * Mathf.Clamp01((dur - t) / 0.02f) : 1f;
                d[i] = Osc(w, 2f * Mathf.PI * hz * t) * vol * env;
            }
            return d;
        }

        static float[] Sweep(float from, float to, float dur, Wave w, float vol)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            float phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float hz = Mathf.Lerp(from, to, t);
                phase += 2f * Mathf.PI * hz / Rate;
                float env = Mathf.Clamp01(t * n / (0.005f * Rate)) * (1f - t);
                d[i] = Osc(w, phase) * vol * env;
            }
            return d;
        }

        static float[] Noise(System.Random rng, float dur, float vol, float decay, float lowpass)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            float y = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                float x = (float)(rng.NextDouble() * 2 - 1);
                y = lowpass > 0 ? y + (x - y) * Mathf.Clamp01(lowpass + 0.02f) : x;
                float env = decay > 0 ? Mathf.Exp(-decay * t) : 1f;
                d[i] = y * vol * env * Mathf.Clamp01(t / 0.003f);
            }
            return d;
        }

        static float[] Bubbles(System.Random rng, float dur)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int b = 0; b < 12; b++)
            {
                int start = rng.Next(0, n - 3000);
                float hz = 300 + (float)rng.NextDouble() * 500;
                for (int i = 0; i < 2500; i++)
                {
                    float t = (float)i / Rate;
                    d[start + i] += Mathf.Sin(2 * Mathf.PI * (hz + 3000 * t) * t) * 0.15f * Mathf.Exp(-t * 40);
                }
            }
            return d;
        }

        static float[] Rummage(System.Random rng, float dur)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            for (int b = 0; b < 25; b++)
            {
                int start = rng.Next(0, n - 4000);
                var hit = Noise(rng, 0.12f, 0.4f, 30f, 0.2f + (float)rng.NextDouble() * 0.5f);
                for (int i = 0; i < hit.Length && start + i < n; i++) d[start + i] += hit[i];
            }
            return d;
        }

        static float[] Silence(float dur) => new float[(int)(dur * Rate)];

        static float[] Concat(params float[][] parts)
        {
            int len = 0;
            foreach (var p in parts) len += p.Length;
            var d = new float[len];
            int o = 0;
            foreach (var p in parts) { p.CopyTo(d, o); o += p.Length; }
            return d;
        }

        static float[] Mix(params float[][] parts)
        {
            int len = 0;
            foreach (var p in parts) len = Mathf.Max(len, p.Length);
            var d = new float[len];
            foreach (var p in parts)
                for (int i = 0; i < p.Length; i++) d[i] += p[i];
            for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
            return d;
        }

        static float[] Decay(float[] d, float k)
        {
            for (int i = 0; i < d.Length; i++) d[i] *= Mathf.Exp(-k * i / (float)Rate);
            return d;
        }

        static float[] Fade(float[] d, float fadeIn, float fadeOut)
        {
            int fi = (int)(fadeIn * Rate), fo = (int)(fadeOut * Rate);
            for (int i = 0; i < d.Length; i++)
            {
                float a = fi > 0 ? Mathf.Clamp01((float)i / fi) : 1f;
                float b = fo > 0 ? Mathf.Clamp01((float)(d.Length - i) / fo) : 1f;
                d[i] *= a * b;
            }
            return d;
        }

        static float[] Wobble(float[] d, float hz, float depth)
        {
            for (int i = 0; i < d.Length; i++)
                d[i] *= 1f - depth + depth * Mathf.Sin(2 * Mathf.PI * hz * i / Rate);
            return d;
        }
    }
}
