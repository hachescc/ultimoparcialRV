using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Easter egg: capibara durmiendo con gafas VR. Si le tiras algo solo mueve una oreja.
    /// El laser le rebota en la cabeza. Si lo matas, Rick se indigna.
    /// </summary>
    public class Capybara : MonoBehaviour
    {
        public Transform ear;
        float m_Next;

        void OnCollisionEnter(Collision c)
        {
            if (c.rigidbody == null || Time.time < m_Next) return;
            m_Next = Time.time + 0.8f;
            StartCoroutine(CoWiggle());
            var egg = GetComponent<EasterEgg>();
            if (egg != null) egg.Found();
        }

        IEnumerator CoWiggle()
        {
            if (ear == null) yield break;
            var baseRot = ear.localRotation;
            float t = 0;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                ear.localRotation = baseRot * Quaternion.Euler(0, 0, Mathf.Sin(t * 40f) * 25f * (1f - t / 0.6f));
                yield return null;
            }
            ear.localRotation = baseRot;
        }
    }
}
