using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RickMorty
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class MixButton : MonoBehaviour
    {
        public Transform cap;
        public float pressDepth = 0.015f;
        public float cooldown = 1f;
        public MixingBowl bowl;

        XRSimpleInteractable m_Interactable;
        Vector3 m_CapRest;
        float m_Next;

        void Awake()
        {
            m_Interactable = GetComponent<XRSimpleInteractable>();
            m_Interactable.selectEntered.AddListener(OnSelect);
            m_Interactable.activated.AddListener(_ => Press(null));
            m_Interactable.hoverEntered.AddListener(OnHover);
            if (cap != null) m_CapRest = cap.localPosition;
        }

        void OnSelect(SelectEnterEventArgs args) => Press(args.interactorObject);

        void OnHover(HoverEnterEventArgs args)
        {
            if (args.interactorObject is XRPokeInteractor poke)
            {
                var tip = poke.attachTransform != null ? poke.attachTransform.position : poke.transform.position;
                if (tip.y >= transform.position.y - 0.02f) Press(args.interactorObject);
            }
        }

        public void Press(IXRInteractor by)
        {
            if (Time.time < m_Next) return;
            m_Next = Time.time + cooldown;
            AudioFX.Play("button", transform.position, 1f);
            PlayerRig.Instance?.Haptic(by != null ? PlayerRig.HandOf(by) : Hand.Both, 0.6f, 0.08f);
            if (cap != null) StartCoroutine(CoPress());
            if (bowl != null) bowl.Mix();
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
