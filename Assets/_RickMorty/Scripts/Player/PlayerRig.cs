using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace RickMorty
{
    public enum Hand { Left, Right, Both }

    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Instance { get; private set; }

        public float walkSpeed = 2.0f;
        public float runSpeed = 3.4f;
        InputAction m_Run;
        ContinuousMoveProvider m_Move;

        void OnEnable()
        {
            m_Run = new InputAction("Correr", InputActionType.Button);
            m_Run.AddBinding("<XRController>{LeftHand}/thumbstickClicked");
            m_Run.AddBinding("<XRController>{LeftHand}/primary2DAxisClick");
            m_Run.AddBinding("<Keyboard>/f");
            m_Run.Enable();
        }

        void OnDisable() { m_Run?.Disable(); m_Run?.Dispose(); m_Run = null; }

        void LateUpdate()
        {
            if (m_Move == null) m_Move = GetComponentInChildren<ContinuousMoveProvider>(true);
            if (m_Move != null && m_Run != null)
            {
                float target = m_Run.IsPressed() ? runSpeed : walkSpeed;
                m_Move.moveSpeed = Mathf.MoveTowards(m_Move.moveSpeed, target, Time.deltaTime * 6f);
            }

            if (EditorSimulatorToggle.SimulatorOn)
            {
                BlockSimulatorWalls();
                ReleaseFixedThings();
            }
        }

        XRBaseInputInteractor[] m_PcInteractors;
        XRBaseInputInteractor.InputTriggerType[] m_OriginalTriggers;

        public void UsePcMode()
        {
            if (m_PcInteractors != null) return;
            m_PcInteractors = GetComponentsInChildren<XRBaseInputInteractor>(true);
            m_OriginalTriggers = new XRBaseInputInteractor.InputTriggerType[m_PcInteractors.Length];
            for (int i = 0; i < m_PcInteractors.Length; i++)
            {
                m_OriginalTriggers[i] = m_PcInteractors[i].selectActionTrigger;
                m_PcInteractors[i].selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Toggle;
            }
            foreach (var nearFar in GetComponentsInChildren<NearFarInteractor>(true))
                nearFar.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractorFarAttachMode.Far;
        }

        public void UseHeadsetMode()
        {
            if (m_PcInteractors != null && m_OriginalTriggers != null)
                for (int i = 0; i < m_PcInteractors.Length; i++)
                    if (m_PcInteractors[i] != null) m_PcInteractors[i].selectActionTrigger = m_OriginalTriggers[i];
            foreach (var nearFar in GetComponentsInChildren<NearFarInteractor>(true))
                nearFar.farAttachMode = UnityEngine.XR.Interaction.Toolkit.Attachment.InteractorFarAttachMode.Near;
            m_PcInteractors = null;
            m_HasLastHead = false;
        }

        void ReleaseFixedThings()
        {
            if (m_PcInteractors == null) return;
            foreach (var interactor in m_PcInteractors)
            {
                if (interactor == null || !interactor.hasSelection) continue;
                var selected = interactor.firstInteractableSelected;
                if (selected is XRSimpleInteractable)
                    interactor.interactionManager.SelectExit((IXRSelectInteractor)interactor, selected);
            }
        }

        Vector3 m_LastHeadLocal;
        bool m_HasLastHead;

        void BlockSimulatorWalls()
        {
            if (head == null || head.parent == null || m_CC == null) return;
            Vector3 headLocal = head.localPosition;
            if (!m_HasLastHead) { m_LastHeadLocal = headLocal; m_HasLastHead = true; return; }

            Vector3 step = head.parent.TransformVector(headLocal - m_LastHeadLocal);
            step.y = 0f;
            m_LastHeadLocal = headLocal;
            if (step.sqrMagnitude < 0.000001f) return;

            transform.position -= step;
            Physics.SyncTransforms();
            if (m_Run != null && m_Run.IsPressed()) step *= runSpeed / walkSpeed;
            m_CC.Move(step);
        }

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

        public void TeleportTo(Vector3 feet, Vector3? forward = null)
        {
            if (origin == null) { transform.position = feet; return; }

            if (forward.HasValue)
            {
                var f = forward.Value; f.y = 0;
                if (f.sqrMagnitude > 0.001f) origin.MatchOriginUpCameraForward(Vector3.up, f.normalized);
            }
            var headFloor = HeadPosition; headFloor.y = origin.transform.position.y;
            var delta = feet - headFloor;
            origin.transform.position += delta;
            Physics.SyncTransforms();
        }
    }
}
