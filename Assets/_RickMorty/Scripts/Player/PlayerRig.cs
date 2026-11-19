using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RickMorty
{
    public enum Hand { Left, Right, Both }

    /// <summary>
    /// Punto de acceso al jugador (XR Origin): cabeza, manos, haptica y teletransporte.
    /// </summary>
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Instance { get; private set; }

        public XROrigin origin;
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;
        public HapticImpulsePlayer leftHaptics;
        public HapticImpulsePlayer rightHaptics;

        CharacterController m_CC;

        public Vector3 HeadPosition => head != null ? head.position : transform.position + Vector3.up * 1.6f;

        public Vector3 FeetPosition
        {
            get
            {
                var p = HeadPosition;
                p.y = origin != null ? origin.transform.position.y : 0f;
                return p;
            }
        }

        void Awake()
        {
            Instance = this;
            if (origin == null) origin = GetComponent<XROrigin>();
            if (head == null && origin != null && origin.Camera != null) head = origin.Camera.transform;
            m_CC = GetComponent<CharacterController>();
            // El XR Body Transformer ajusta la altura del CharacterController a la cabeza; un step offset
            // pequeno evita el error "Step Offset must be less or equal..." cuando la altura es baja.
            if (m_CC != null) m_CC.stepOffset = Mathf.Min(m_CC.stepOffset, 0.1f);
            if (leftHand != null && leftHaptics == null) leftHaptics = leftHand.GetComponentInChildren<HapticImpulsePlayer>();
            if (rightHand != null && rightHaptics == null) rightHaptics = rightHand.GetComponentInChildren<HapticImpulsePlayer>();
        }

        public void Haptic(Hand hand, float amplitude, float duration)
        {
            amplitude = Mathf.Clamp01(amplitude);
            if ((hand == Hand.Left || hand == Hand.Both) && leftHaptics != null) leftHaptics.SendHapticImpulse(amplitude, duration);
            if ((hand == Hand.Right || hand == Hand.Both) && rightHaptics != null) rightHaptics.SendHapticImpulse(amplitude, duration);
        }

        public static Hand HandOf(IXRInteractor interactor)
        {
            if (interactor == null) return Hand.Both;
            if (interactor.handedness == InteractorHandedness.Left) return Hand.Left;
            if (interactor.handedness == InteractorHandedness.Right) return Hand.Right;
            var t = interactor.transform;
            var inst = Instance;
            if (inst != null)
            {
                if (inst.leftHand != null && t.IsChildOf(inst.leftHand)) return Hand.Left;
                if (inst.rightHand != null && t.IsChildOf(inst.rightHand)) return Hand.Right;
            }
            return Hand.Both;
        }

        public void HapticPattern(Hand hand, float amplitude, int pulses, float pulse, float gap)
        {
            StartCoroutine(CoPattern(hand, amplitude, pulses, pulse, gap));
        }

        IEnumerator CoPattern(Hand hand, float amplitude, int pulses, float pulse, float gap)
        {
            for (int i = 0; i < pulses; i++)
            {
                Haptic(hand, amplitude, pulse);
                yield return new WaitForSeconds(pulse + gap);
            }
        }

        /// <summary>Mueve al jugador para que sus pies queden en 'feet' mirando hacia 'forward'.</summary>
        public void TeleportTo(Vector3 feet, Vector3? forward = null)
        {
            if (origin == null) { transform.position = feet; return; }

            if (forward.HasValue)
            {
                var f = forward.Value; f.y = 0;
                if (f.sqrMagnitude > 0.001f) origin.MatchOriginUpCameraForward(Vector3.up, f.normalized);
            }
            // Desplaza el origen de modo que la cabeza (proyectada al piso) quede en 'feet'
            var headFloor = HeadPosition; headFloor.y = origin.transform.position.y;
            var delta = feet - headFloor;
            origin.transform.position += delta;
            // Sin desactivar el CharacterController: solo sincronizamos la fisica con el nuevo transform
            Physics.SyncTransforms();
        }
    }
}
