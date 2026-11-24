using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RickMorty
{
    /// <summary>
    /// Cualquier objeto del juego que se puede agarrar y mezclar en el tazon.
    /// Mantiene un registro global para el tazon, el guantelete, los Meeseeks, etc.
    /// Lo usan: el tazon, el soporte final, sockets, frascos base, criaturas, Meeseeks, guantelete, basurero y GameManager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MixableItem : MonoBehaviour
    {
        public static readonly List<MixableItem> All = new List<MixableItem>();

        public string itemId;

        public ItemDef Def { get; private set; }
        public XRGrabInteractable Grab { get; private set; }
        public Rigidbody Body { get; private set; }
        public BaseIngredient Base { get; private set; }

        /// <summary>No disponible (regresando por portal, robado, siendo destruido...)</summary>
        public bool Unavailable { get; set; }

        public bool IsBase => Def != null && Def.IsBase;

        public bool InSocket
        {
            get
            {
                if (Grab == null || !Grab.isSelected) return false;
                foreach (var i in Grab.interactorsSelecting)
                    if (i is XRSocketInteractor) return true;
                return false;
            }
        }

        /// <summary>Sostenido por una mano del jugador (no por un socket).</summary>
        public bool IsHeld
        {
            get
            {
                if (Grab == null || !Grab.isSelected) return false;
                foreach (var i in Grab.interactorsSelecting)
                    if (!(i is XRSocketInteractor)) return true;
                return false;
            }
        }

        public Hand HoldingHand
        {
            get
            {
                if (Grab == null) return Hand.Both;
                foreach (var i in Grab.interactorsSelecting)
                    if (!(i is XRSocketInteractor)) return PlayerRig.HandOf(i);
                return Hand.Both;
            }
        }

        void Awake()
        {
            Def = GameDatabase.GetItem(itemId);
            Body = GetComponent<Rigidbody>();
            Grab = GetComponent<XRGrabInteractable>();
            Base = GetComponent<BaseIngredient>();
            if (Grab != null)
            {
                Grab.selectEntered.AddListener(OnSelectEntered);
                Grab.selectExited.AddListener(OnSelectExited);
                Grab.activated.AddListener(OnActivated);
                Grab.hoverEntered.AddListener(OnHoverEntered);
                Grab.hoverExited.AddListener(OnHoverExited);
            }
        }

        public void Init(string id)
        {
            itemId = id;
            Def = GameDatabase.GetItem(id);
            Base = GetComponent<BaseIngredient>();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            SetHighlight(false);
            if (args.interactorObject is XRSocketInteractor)
            {
                // "clic" al encajar en un soporte (no al arrancar la escena, cuando todos los frascos se acomodan a la vez)
                if (Time.timeSinceLevelLoad > 2f) AudioFX.Play("socket", transform.position, 0.35f);
                return;
            }
            AudioFX.Play("grab", transform.position, 0.4f, Random.Range(0.92f, 1.08f));
            GameEvents.RaiseItemGrabbed(itemId);
            IgnorePlayerBody(true);
        }

        void OnSelectExited(SelectExitEventArgs args)
        {
            if (!(args.interactorObject is XRSocketInteractor)) IgnorePlayerBody(false);
        }

        /// <summary>Lo que tienes en la mano no choca con tu propio cuerpo (una espada te empujaba al caminar).</summary>
        void IgnorePlayerBody(bool ignore)
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            var body = rig.GetComponent<CharacterController>();
            if (body == null) return;
            foreach (var c in GetComponentsInChildren<Collider>())
                if (c != null) Physics.IgnoreCollision(body, c, ignore);
        }

        // ---- Resalte: el objeto que la mano puede agarrar crece un 8 %. Antes solo habia una vibracion y,
        // con varios frascos juntos, no se sabia cual iba a agarrarse.
        Transform m_Visual;
        Vector3 m_VisualScale;
        int m_Hovers;

        void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) return;
            m_Hovers++;
            if (!IsHeld) SetHighlight(true);
        }

        void OnHoverExited(HoverExitEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) return;
            m_Hovers = Mathf.Max(0, m_Hovers - 1);
            if (m_Hovers == 0) SetHighlight(false);
        }

        void SetHighlight(bool on)
        {
            if (m_Visual == null)
            {
                m_Visual = transform.Find("Visual");
                if (m_Visual == null) return;
                m_VisualScale = m_Visual.localScale;
            }
            m_Visual.localScale = on ? m_VisualScale * 1.08f : m_VisualScale;
        }

        void OnActivated(ActivateEventArgs args)
        {
            GameEvents.RaiseItemActivated(itemId);
        }

        /// <summary>Fuerza a soltar el objeto (mano o socket).</summary>
        public void ForceRelease()
        {
            if (Grab != null && Grab.isSelected && Grab.interactionManager != null)
                Grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)Grab);
        }

        public void Haptic(float amplitude, float duration)
        {
            if (!IsHeld || PlayerRig.Instance == null) return;
            PlayerRig.Instance.Haptic(HoldingHand, amplitude, duration);
        }

        /// <summary>Consumir en una mezcla/explosion: los base regresan a su lugar, el resto se destruye.</summary>
        public void Consume(float baseReturnDelay = 0.3f, bool dust = true)
        {
            if (Base != null)
            {
                Base.ReturnHome(false, baseReturnDelay);
                return;
            }
            Unavailable = true;
            ForceRelease();
            if (dust) FX.Burst(transform.position, Def != null ? Def.Color : Color.white, 20, 1.2f, 0.04f, 0.6f, 0.2f);
            Destroy(gameObject);
        }

        public static MixableItem FindById(string id)
        {
            foreach (var m in All) if (m != null && m.itemId == id && !m.Unavailable) return m;
            return null;
        }
    }
}
