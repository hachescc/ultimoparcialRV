using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace RickMorty
{
    /// <summary>
    /// Base para comportamientos de objetos agarrables: expone eventos de agarre,
    /// soltar y gatillo (activate) del XRGrabInteractable.
    /// Lo usan: todos los comportamientos de Items/Behaviours (armas, consumibles, caja Meeseeks...) y PetHinter.
    /// </summary>
    [RequireComponent(typeof(MixableItem))]
    public abstract class ItemBehaviour : MonoBehaviour
    {
        protected MixableItem Item { get; private set; }
        protected Rigidbody Body => Item != null ? Item.Body : null;

        protected virtual void Awake()
        {
            Item = GetComponent<MixableItem>();
        }

        protected virtual void Start()
        {
            var g = GetComponent<XRGrabInteractable>();
            if (g == null) return;
            g.activated.AddListener(HandleActivated);
            g.deactivated.AddListener(HandleDeactivated);
            g.selectEntered.AddListener(HandleSelectEntered);
            g.selectExited.AddListener(HandleSelectExited);
        }

        void HandleActivated(ActivateEventArgs args) => OnActivate();
        void HandleDeactivated(DeactivateEventArgs args) => OnDeactivate();

        // Un soporte (socket) tambien "agarra" el objeto: eso no cuenta como agarre del jugador
        void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if (!(args.interactorObject is XRSocketInteractor)) OnGrabbed();
        }

        void HandleSelectExited(SelectExitEventArgs args)
        {
            if (!(args.interactorObject is XRSocketInteractor)) OnReleased();
        }

        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnGrabbed() { }
        protected virtual void OnReleased() { }

        protected Vector3 Muzzle(float forward = 0.15f) => transform.position + transform.forward * forward;

        /// <summary>Aplica el impacto de un proyectil (laser, bala) sobre lo que golpeo.</summary>
        protected static void ApplyProjectileHit(RaycastHit hit, Vector3 dir, float force, bool laser)
        {
            var creature = hit.collider.GetComponentInParent<Creature>();
            if (creature != null)
            {
                if (laser && creature.invulnerableToLaser)
                {
                    creature.OnLaserBounce();
                    return;
                }
                creature.Kill(DamageType.Firearm);
                return;
            }
            var burn = hit.collider.GetComponentInParent<Burnable>();
            if (burn != null && laser) { burn.Burn(); return; }

            var rb = hit.rigidbody;
            if (rb != null && !rb.isKinematic) rb.AddForceAtPosition(dir * force, hit.point, ForceMode.Impulse);
        }
    }
}
