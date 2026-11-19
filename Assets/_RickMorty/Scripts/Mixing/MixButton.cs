using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace RickMorty
{
    /// <summary>
    /// Boton fisico XR (se presiona con el dedo/poke, con agarre o con el rayo).
    /// Por defecto llama a MixingBowl.Mix().
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class MixButton : MonoBehaviour
    {
        public Transform cap;
        public float pressDepth = 0.015f;
        public float cooldown = 1f;
        public MixingBowl bowl;
        public UnityEvent onPressed = new UnityEvent();

        XRSimpleInteractable m_Interactable;
        Vector3 m_CapRest;
        float m_Next;

        public static event System.Action AnyPressed;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            m_Interactable.selectEntered.AddListener(OnSelect);
            m_Interactable.activated.AddListener(_ => Press(null));
            // Tocar el boton con la mano ya lo presiona. El XRPokeFilter por defecto solo aceptaba un empuje
            // HORIZONTAL (eje Z): al presionar hacia abajo, como cualquier boton, no pasaba nada.
            m_Interactable.hoverEntered.AddListener(OnHover);
            if (cap != null) m_CapRest = cap.localPosition;
        }

        void OnSelect(SelectEnterEventArgs args) => Press(args.interactorObject);

        void OnHover(HoverEnterEventArgs args)
        {
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRPokeInteractor poke)
            {
                // solo cuenta si la mano llega desde arriba o de frente (no al rozarlo por debajo de la mesa)
                var tip = poke.attachTransform != null ? poke.attachTransform.position : poke.transform.position;
                if (tip.y >= transform.position.y - 0.02f) Press(args.interactorObject);
            }
        }

        public void Press(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor by)
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + cooldown;
            AudioFX.Play("button", transform.position, 1f);
            PlayerRig.Instance?.Haptic(by != null ? PlayerRig.HandOf(by) : Hand.Both, 0.6f, 0.08f);
            if (cap != null) StartCoroutine(CoPress());
            AnyPressed?.Invoke();
            if (bowl != null) bowl.Mix();
            onPressed.Invoke();
        }

        IEnumerator CoPress()
        {
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float k = t < 0.08f ? t / 0.08f : 1f - (t - 0.08f) / 0.17f;
                cap.localPosition = m_CapRest + Vector3.down * pressDepth * Mathf.Clamp01(k);
                yield return null;
            }
            cap.localPosition = m_CapRest;
        }
    }
}
