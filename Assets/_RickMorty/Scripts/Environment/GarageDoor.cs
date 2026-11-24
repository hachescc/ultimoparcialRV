using System.Collections;
using UnityEngine;

namespace RickMorty
{
    /// <summary>
    /// Puerta metalica del garaje: se abre cuando Rick se va. Con swingUp (como esta en la escena) bascula sobre la bisagra de arriba; si no, sube recta.
    /// Lo usan: GameManager (Open cuando Rick se va); la crea el builder de escena.
    /// </summary>
    public class GarageDoor : MonoBehaviour
    {
        public Transform panel;
        public float openHeight = 2.5f;
        public float duration = 3.5f;
        [Tooltip("Puerta basculante: gira sobre la bisagra superior hacia adentro en vez de subir recta")]
        public bool swingUp;
        public float swingAngle = -90f;
        public bool IsOpen { get; private set; }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            StartCoroutine(CoOpen());
        }

        IEnumerator CoOpen()
        {
            var start = panel.localPosition;
            var end = start + Vector3.up * openHeight;
            var r0 = panel.localRotation;
            var r1 = r0 * Quaternion.AngleAxis(swingAngle, Vector3.forward);
            AudioFX.Play("rummage", panel.position, 1f, 0.5f);
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, t / duration);
                if (swingUp) panel.localRotation = Quaternion.Slerp(r0, r1, k);
                else panel.localPosition = Vector3.Lerp(start, end, k);
                yield return null;
            }
            AudioFX.Play("impact_metal", panel.position, 1f, 0.6f);
        }
    }
}
