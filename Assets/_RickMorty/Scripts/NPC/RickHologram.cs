using UnityEngine;

namespace RickMorty
{
    public class RickHologram : MonoBehaviour
    {
        public Renderer[] parts;
        public Light beam;
        float m_Alpha;
        bool m_Visible;

        void Awake()
        {
            if (parts == null || parts.Length == 0) parts = GetComponentsInChildren<Renderer>(true);
            SetAlpha(0);
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            m_Visible = true;
            AudioFX.Play("static", transform.position, 0.8f);
        }

        public void Hide() => m_Visible = false;

        void Update()
        {
            m_Alpha = Mathf.MoveTowards(m_Alpha, m_Visible ? 1f : 0f, Time.deltaTime * 1.5f);
            float flicker = Random.value > 0.95f ? 0.4f : 1f;
            SetAlpha(m_Alpha * flicker);
            transform.Rotate(0, 20f * Time.deltaTime, 0);
            var rig = PlayerRig.Instance;
            if (rig != null)
            {
                var d = rig.HeadPosition - transform.position; d.y = 0;
                if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
            }
            if (!m_Visible && m_Alpha <= 0f) gameObject.SetActive(false);
        }

        void SetAlpha(float a)
        {
            foreach (var r in parts) if (r != null) MatLib.SetColor(r, new Color(0.4f, 0.9f, 1f, 0.45f * a));
            if (beam != null) beam.intensity = 2f * a;
        }
    }
}
