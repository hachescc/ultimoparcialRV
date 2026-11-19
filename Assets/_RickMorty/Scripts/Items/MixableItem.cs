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
                Grab.activated.AddListener(OnActivated);
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
            if (args.interactorObject is XRSocketInteractor) return;
            GameEvents.RaiseItemGrabbed(itemId);
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
