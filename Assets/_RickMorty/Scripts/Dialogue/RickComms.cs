using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Sistema de dialogos de Rick (radio del reloj). Cola de lineas con subtitulos por
    /// fragmentos, audio opcional (Resources/Audio/Rick/&lt;id&gt;), pitido + vibracion en la
    /// muneca y el "modo perro" del casco de Snowball.
    /// </summary>
    public class RickComms : MonoBehaviour
    {
        public static RickComms Instance { get; private set; }

        [Tooltip("Desde donde suena la voz (por defecto el reloj de la muñeca)")]
        public Transform voiceSource;
        [Tooltip("Segundos por caracter para calcular la duracion de un subtitulo")]
        public float secondsPerChar = 0.055f;
        public float minChunkSeconds = 2f;

        /// <summary>(hablante, texto). texto null = limpiar subtitulo.</summary>
        public event Action<string, string> SubtitleChanged;

        public bool IsSpeaking => m_Current != null || m_Queue.Count > 0;
        public bool DogMode => Time.time < m_DogUntil;
        public Transform SpeakerOverride { get; set; }

        class Entry
        {
            public string id;
            public string speaker;
            public string text;
        }

        readonly Queue<Entry> m_Queue = new Queue<Entry>();
        Entry m_Current;
        Coroutine m_Co;
        AudioSource m_Voice;
        float m_DogUntil;

        void Awake()
        {
            Instance = this;
            var go = new GameObject("RickVoice");
            go.transform.SetParent(transform, false);
            m_Voice = go.AddComponent<AudioSource>();
            m_Voice.playOnAwake = false;
            m_Voice.spatialBlend = 0.6f;
            m_Voice.minDistance = 0.5f;
            m_Voice.maxDistance = 20f;
        }

        public void EnableDogMode(float seconds) => m_DogUntil = Time.time + seconds;

        /// <summary>Dice una linea del archivo dialogues.json por id.</summary>
        public void Say(string id, bool interrupt = false)
        {
            var line = GameDatabase.GetLine(id);
            if (line == null)
            {
                Debug.LogWarning("[RickComms] Linea no encontrada: " + id);
                return;
            }
            Enqueue(new Entry { id = id, speaker = string.IsNullOrEmpty(line.speaker) ? "Rick" : line.speaker, text = line.PickText() }, interrupt);
        }

        public void SayText(string text, string speaker = "Rick", string audioId = null, bool interrupt = false)
        {
            Enqueue(new Entry { id = audioId, speaker = speaker, text = text }, interrupt);
        }

        void Enqueue(Entry e, bool interrupt)
        {
            if (interrupt)
            {
                m_Queue.Clear();
                if (m_Co != null) StopCoroutine(m_Co);
                m_Co = null;
                m_Current = null;
                m_Voice.Stop();
            }
            m_Queue.Enqueue(e);
            if (m_Co == null) m_Co = StartCoroutine(CoProcess());
        }

        public void StopAll()
        {
            m_Queue.Clear();
            if (m_Co != null) StopCoroutine(m_Co);
            m_Co = null;
            m_Current = null;
            m_Voice.Stop();
            SubtitleChanged?.Invoke(null, null);
        }

        public IEnumerator WaitUntilIdle()
        {
            while (IsSpeaking) yield return null;
        }

        IEnumerator CoProcess()
        {
            while (m_Queue.Count > 0)
            {
                m_Current = m_Queue.Dequeue();
                yield return CoSpeak(m_Current);
                m_Current = null;
                yield return new WaitForSeconds(0.35f);
            }
            SubtitleChanged?.Invoke(null, null);
            m_Co = null;
        }

        IEnumerator CoSpeak(Entry e)
        {
            string speaker = e.speaker;
            string text = e.text;
            AudioClip clip = null;
            bool dog = DogMode && speaker == "Rick";

            if (dog)
            {
                text = "¡GUAU GUAU! ¡GRRRRR! ¡GUAU! (el casco traduce todo a perruno)";
                clip = AudioFX.Clip("bark");
            }
            else if (!string.IsNullOrEmpty(e.id))
            {
                clip = Resources.Load<AudioClip>("Audio/Rick/" + e.id);
            }

            // Pitido + vibracion del reloj
            var src = SpeakerOverride != null ? SpeakerOverride : voiceSource;
            if (SpeakerOverride == null)
            {
                AudioFX.Play("watch_ping", src != null ? src.position : transform.position, 0.5f);
                PlayerRig.Instance?.Haptic(Hand.Left, 0.3f, 0.12f);
            }

            var chunks = Chunk(text);
            int total = 0;
            foreach (var c in chunks) total += c.Length;

            float clipLen = 0f;
            if (clip != null)
            {
                m_Voice.transform.position = src != null ? src.position : transform.position;
                m_Voice.clip = clip;
                m_Voice.Play();
                clipLen = clip.length;
            }
            else if (!dog && speaker == "Rick" && text.Contains("(Eructa)"))
            {
                AudioFX.Play("burp", src != null ? src.position : transform.position, 0.6f);
            }

            foreach (var chunk in chunks)
            {
                SubtitleChanged?.Invoke(speaker, chunk);
                float dur = clipLen > 0f
                    ? clipLen * chunk.Length / Mathf.Max(1, total)
                    : Mathf.Max(minChunkSeconds, chunk.Length * secondsPerChar);
                float t = 0;
                // Sin grabaciones de voz, cada fragmento arranca con una rafaga corta tipo radio desde donde esta Rick
                // (antes, en la intro y el tutorial no sonaba NADA y el jugador no notaba que habia una linea nueva).
                float nextBlip = clip == null && !dog ? 0f : float.MaxValue;
                int blips = 0;
                float voicePitch = speaker == "Rick" ? 1f : 1.55f;
                while (t < dur)
                {
                    if (t >= nextBlip && blips < 3)
                    {
                        blips++;
                        nextBlip = t + 0.62f;
                        AudioFX.Play("rick_blip", src != null ? src.position : transform.position, 0.42f, voicePitch * UnityEngine.Random.Range(0.92f, 1.1f));
                    }
                    t += Time.deltaTime;
                    if (src != null && m_Voice.isPlaying) m_Voice.transform.position = src.position;
                    yield return null;
                }
            }
        }

        /// <summary>Divide un texto largo en fragmentos de ~85 caracteres por oraciones (y por comas si una oracion es muy larga):
        /// asi caben con letra grande en el monitor y en la franja de avisos.</summary>
        static List<string> Chunk(string text, int max = 85)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;
            var sentences = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                sb.Append(text[i]);
                char c = text[i];
                bool end = (c == '.' || c == '!' || c == '?' || c == ')') && (i + 1 >= text.Length || text[i + 1] == ' ');
                // oraciones largas: tambien se cortan en la coma o los dos puntos (si ya llevan buen tramo)
                if (!end && (c == ',' || c == ';' || c == ':') && sb.Length > 55 && i + 1 < text.Length && text[i + 1] == ' ') end = true;
                if (end)
                {
                    sentences.Add(sb.ToString().Trim());
                    sb.Clear();
                }
            }
            if (sb.Length > 0) sentences.Add(sb.ToString().Trim());

            var cur = new StringBuilder();
            foreach (var s in sentences)
            {
                if (cur.Length > 0 && cur.Length + s.Length + 1 > max)
                {
                    result.Add(cur.ToString());
                    cur.Clear();
                }
                if (cur.Length > 0) cur.Append(' ');
                cur.Append(s);
            }
            if (cur.Length > 0) result.Add(cur.ToString());
            return result;
        }
    }
}
