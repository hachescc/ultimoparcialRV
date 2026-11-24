using UnityEngine;

namespace RickMorty
{
    public class FlickerLight : MonoBehaviour
    {
        public Light targetLight;
        public Renderer tube;
        public float baseIntensity = 2.2f;
        public Color tubeColor = new Color(0.9f, 1f, 0.95f);

        float m_NextFlicker;
        float m_FlickerEnd;
        bool m_Blackout, m_Low;
        float m_NextFlip;
        AudioSource m_Hum;

        void Start()
        {
            m_NextFlicker = Time.time + Random.Range(4f, 15f);
            m_Hum = AudioFX.AttachLoop(gameObject, "hum", 0.06f, 4f, 1.9f);
        }

        public void SetBlackout(bool on)
        {
            m_Blackout = on;
            if (m_Hum != null) m_Hum.mute = on;
        }

        void Update()
        {
            float k = 1f;
            if (m_Blackout)
            {
                k = Random.value > 0.985f ? 0.4f : 0f;
            }
            else if (Time.time > m_NextFlicker)
            {
                if (m_FlickerEnd < Time.time) m_FlickerEnd = Time.time + Random.Range(0.15f, 0.6f);
                if (Time.time >= m_NextFlip) { m_NextFlip = Time.time + Random.Range(0.1f, 0.22f); m_Low = !m_Low; }
                k = m_Low ? 0.6f : 1f;
                if (Time.time > m_FlickerEnd) m_NextFlicker = Time.time + Random.Range(6f, 20f);
            }
            if (targetLight != null) targetLight.intensity = baseIntensity * k;
            if (tube != null) MatLib.SetColor(tube, tubeColor * (0.2f + 0.8f * k));
        }
    }
}
