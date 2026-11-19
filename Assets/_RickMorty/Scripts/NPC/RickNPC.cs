using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Rick fisico (placeholder) durante la introduccion: golpea la mesa, mira al jugador y al
    /// final camina hacia la puerta del garaje y se va por un portal.
    /// </summary>
    public class RickNPC : MonoBehaviour
    {
        public Transform exitPoint;
        public Transform mouth;
        public float walkSpeed = 1.1f;

        Transform m_Visual;
        Animation m_Anim;
        string m_Current;

        void Awake()
        {
            m_Visual = transform.Find("Visual");
            if (mouth == null) mouth = transform;
            // Modelo real con animaciones Idle/Talk/Walk (hechas en Blender, importadas como clips Legacy)
            m_Anim = GetComponentInChildren<Animation>();
            if (m_Anim != null)
                foreach (AnimationState st in m_Anim) st.wrapMode = WrapMode.Loop;
        }

        /// <summary>Reproduce el clip cuyo nombre contiene 'key' (Idle, Talk, Walk).</summary>
        void PlayClip(string key)
        {
            if (m_Anim == null || m_Current == key) return;
            foreach (AnimationState st in m_Anim)
            {
                if (!st.name.Contains(key)) continue;
                m_Anim.CrossFade(st.name, 0.25f);
                m_Current = key;
                return;
            }
        }

        void Update()
        {
            // Mira al jugador mientras habla
            var rig = PlayerRig.Instance;
            if (rig == null || m_Walking) return;
            var d = rig.HeadPosition - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 3f);
            bool talking = RickComms.Instance != null && RickComms.Instance.IsSpeaking;
            if (m_Anim != null) PlayClip(talking ? "Talk" : "Idle");
            else if (m_Visual != null && talking)
                m_Visual.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.01f;
        }

        bool m_Walking;

        public void SlamTable()
        {
            AudioFX.Play("impact_wood", transform.position + transform.forward * 0.5f + Vector3.up * 0.9f, 1f, 0.7f);
            PlayerRig.Instance?.Haptic(Hand.Both, 0.8f, 0.2f);
        }

        public IEnumerator CoLeave()
        {
            if (exitPoint == null) { gameObject.SetActive(false); yield break; }
            m_Walking = true;
            while (true)
            {
                var d = exitPoint.position - transform.position; d.y = 0;
                if (d.magnitude < 0.1f) break;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Time.deltaTime * 6f);
                transform.position += d.normalized * walkSpeed * Time.deltaTime;
                if (m_Anim != null) PlayClip("Walk");
                else if (m_Visual != null) m_Visual.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.04f;
                yield return null;
            }
            var portalPos = transform.position + transform.forward * 0.8f + Vector3.up * 1f;
            var portal = FX.Portal(portalPos, Quaternion.LookRotation(-transform.forward), 0.7f, 3f);
            yield return new WaitForSeconds(0.7f);
            float t = 0;
            var start = transform.position;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, portalPos - Vector3.up * 1f, t / 0.6f);
                transform.localScale = Vector3.one * (1f - t / 0.6f * 0.8f);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
